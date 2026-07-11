using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;

namespace ElementEngine;

internal sealed unsafe partial class VulkanGraphicsBackend
{
    public object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(descriptions);

        var result = new object[descriptions.Length];
        var created = new List<VulkanShaderResource>();

        try
        {
            for (var index = 0; index < descriptions.Length; index++)
            {
                var description = descriptions[index];
                if (description.Stage is not (ShaderStages.Vertex or ShaderStages.Fragment))
                    throw new ArgumentException("Vulkan shaders must contain exactly one supported stage.", nameof(descriptions));
                if (description.ShaderBytes is null || description.ShaderBytes.Length == 0)
                    throw new ArgumentException("Vulkan shaders cannot be empty.", nameof(descriptions));
                if (string.IsNullOrWhiteSpace(description.EntryPoint))
                    throw new ArgumentException("Vulkan shaders require an entry point.", nameof(descriptions));

                var spirv = IsSpirv(description.ShaderBytes)
                    ? description.ShaderBytes
                    : CompileGlsl(description, options);

                if (spirv.Length % sizeof(uint) != 0)
                    throw new InvalidOperationException("Compiled SPIR-V length is not aligned to 32-bit words.");

                fixed (byte* code = spirv)
                {
                    var createInfo = new ShaderModuleCreateInfo
                    {
                        SType = StructureType.ShaderModuleCreateInfo,
                        CodeSize = (nuint)spirv.Length,
                        PCode = (uint*)code,
                    };
                    Check(_vk.CreateShaderModule(_device, &createInfo, null, out var module), "create a Vulkan shader module");
                    var shader = new VulkanShaderResource
                    {
                        Handle = module,
                        Stage = description.Stage,
                        EntryPoint = description.EntryPoint,
                    };
                    TrackResource(shader);
                    created.Add(shader);
                    result[index] = shader;
                }
            }

            return result;
        }
        catch
        {
            foreach (var shader in created)
                DestroyNativeResource(shader);
            throw;
        }
    }

    public object CreateResourceLayout(ResourceLayoutDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var elements = description.Elements ?? [];
        var bindings = new DescriptorSetLayoutBinding[elements.Length];

        for (var index = 0; index < elements.Length; index++)
        {
            if (elements[index].Stages == ShaderStages.None)
                throw new ArgumentException("Vulkan resource layout elements require at least one shader stage.", nameof(description));

            bindings[index] = new DescriptorSetLayoutBinding
            {
                Binding = (uint)index,
                DescriptorType = elements[index].Kind.ToVulkan(),
                DescriptorCount = 1,
                StageFlags = elements[index].Stages.ToVulkan(),
            };
        }

        fixed (DescriptorSetLayoutBinding* bindingsPointer = bindings)
        {
            var createInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = bindingsPointer,
            };
            Check(_vk.CreateDescriptorSetLayout(_device, &createInfo, null, out var layout), "create a Vulkan descriptor set layout");
            var resource = new VulkanResourceLayoutResource
            {
                Handle = layout,
                Bindings = bindings,
            };
            TrackResource(resource);
            return resource;
        }
    }

    public object CreateResourceSet(ResourceSetDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var layout = GetNative<VulkanResourceLayoutResource>(description.Layout);
        var boundResources = description.BoundResources ?? [];
        if (boundResources.Length != layout.Bindings.Length)
            throw new ArgumentException("The Vulkan resource count does not match its layout.", nameof(description));

        var layoutHandle = layout.Handle;
        var allocateInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layoutHandle,
        };
        Check(_vk.AllocateDescriptorSets(_device, &allocateInfo, out var descriptorSet), "allocate a Vulkan descriptor set");

        var writes = new WriteDescriptorSet[boundResources.Length];
        var bufferInfos = new DescriptorBufferInfo[boundResources.Length];
        var imageInfos = new DescriptorImageInfo[boundResources.Length];
        var textures = new List<VulkanTextureResource>();
        var ownedViews = new List<ImageView>();

        try
        {
            fixed (WriteDescriptorSet* writesPointer = writes)
            fixed (DescriptorBufferInfo* bufferInfosPointer = bufferInfos)
            fixed (DescriptorImageInfo* imageInfosPointer = imageInfos)
            {
                for (var index = 0; index < boundResources.Length; index++)
                {
                    var binding = layout.Bindings[index];
                    writesPointer[index] = new WriteDescriptorSet
                    {
                        SType = StructureType.WriteDescriptorSet,
                        DstSet = descriptorSet,
                        DstBinding = (uint)index,
                        DescriptorCount = 1,
                        DescriptorType = binding.DescriptorType,
                    };

                    switch (binding.DescriptorType)
                    {
                        case DescriptorType.UniformBuffer:
                            {
                                if (boundResources[index] is not DeviceBuffer buffer ||
                                    (buffer.Usage & BufferUsage.UniformBuffer) == 0)
                                {
                                    throw new ArgumentException($"Vulkan binding {index} requires a uniform buffer.", nameof(description));
                                }

                                var nativeBuffer = GetNative<VulkanBufferResource>(buffer);
                                bufferInfosPointer[index] = new DescriptorBufferInfo
                                {
                                    Buffer = nativeBuffer.Handle,
                                    Offset = 0,
                                    Range = nativeBuffer.Size,
                                };
                                writesPointer[index].PBufferInfo = &bufferInfosPointer[index];
                                break;
                            }
                        case DescriptorType.SampledImage:
                            {
                                VulkanTextureResource nativeTexture;
                                ImageView imageView;

                                if (boundResources[index] is Texture texture)
                                {
                                    nativeTexture = GetNative<VulkanTextureResource>(texture);
                                    imageView = CreateImageView(
                                        nativeTexture.Handle,
                                        nativeTexture.Format,
                                        nativeTexture.AspectMask.SampledAspectMask(),
                                        nativeTexture.Description.ToViewType(),
                                        nativeTexture.Description.MipLevels,
                                        nativeTexture.Description.ArrayLayers);
                                    ownedViews.Add(imageView);
                                }
                                else if (boundResources[index] is TextureView textureView)
                                {
                                    var nativeView = GetNative<VulkanTextureViewResource>(textureView);
                                    nativeTexture = nativeView.Texture;
                                    imageView = nativeView.Handle;
                                }
                                else
                                {
                                    throw new ArgumentException($"Vulkan binding {index} requires a texture or texture view.", nameof(description));
                                }

                                if ((nativeTexture.Description.Usage & TextureUsage.Sampled) == 0)
                                    throw new ArgumentException($"Vulkan binding {index} requires a texture with sampled usage.", nameof(description));

                                textures.Add(nativeTexture);
                                imageInfosPointer[index] = new DescriptorImageInfo
                                {
                                    ImageView = imageView,
                                    ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                                };
                                writesPointer[index].PImageInfo = &imageInfosPointer[index];
                                break;
                            }
                        case DescriptorType.Sampler:
                            {
                                if (boundResources[index] is not Sampler sampler)
                                    throw new ArgumentException($"Vulkan binding {index} requires a sampler.", nameof(description));

                                var nativeSampler = GetNative<VulkanSamplerResource>(sampler);
                                imageInfosPointer[index] = new DescriptorImageInfo
                                {
                                    Sampler = nativeSampler.Handle,
                                };
                                writesPointer[index].PImageInfo = &imageInfosPointer[index];
                                break;
                            }
                        default:
                            throw new NotSupportedException($"Unsupported Vulkan descriptor type {binding.DescriptorType}.");
                    }
                }

                _vk.UpdateDescriptorSets(_device, (uint)writes.Length, writesPointer, 0, null);
            }

            var resource = new VulkanResourceSetResource
            {
                Handle = descriptorSet,
                Layout = layout,
                Textures = textures.Distinct().ToArray(),
                OwnedViews = ownedViews.ToArray(),
            };
            TrackResource(resource);
            return resource;
        }
        catch
        {
            foreach (var view in ownedViews)
                _vk.DestroyImageView(_device, view, null);
            Check(_vk.FreeDescriptorSets(_device, _descriptorPool, 1, &descriptorSet), "free a failed Vulkan descriptor set");
            throw;
        }
    }

    public object CreateGraphicsPipeline(GraphicsPipelineDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var shaders = description.ShaderSet.Shaders ?? [];
        if (shaders.Length == 0)
            throw new ArgumentException("A Vulkan graphics pipeline requires shaders.", nameof(description));

        var shaderResources = shaders.Select(GetNative<VulkanShaderResource>).ToArray();
        if (shaderResources.Count(shader => shader.Stage == ShaderStages.Vertex) != 1 ||
            shaderResources.Count(shader => shader.Stage == ShaderStages.Fragment) != 1)
        {
            throw new ArgumentException("A Vulkan graphics pipeline requires one vertex and one fragment shader.", nameof(description));
        }

        if (!description.RasterizerState.DepthClipEnabled && !_physicalDeviceFeatures.DepthClamp)
            throw new NotSupportedException("The Vulkan device does not support disabling depth clipping.");

        var resourceLayouts = description.ResourceLayouts ?? [];
        if (resourceLayouts.Length > VulkanCommandState.MaximumBindings ||
            (uint)resourceLayouts.Length > _physicalDeviceProperties.Limits.MaxBoundDescriptorSets)
        {
            throw new NotSupportedException("The Vulkan pipeline has more resource layouts than the selected device supports.");
        }

        var descriptorLayouts = resourceLayouts.Select(GetNative<VulkanResourceLayoutResource>).ToArray();
        var descriptorLayoutHandles = descriptorLayouts.Select(layout => layout.Handle).ToArray();
        var vertexLayouts = description.ShaderSet.VertexLayouts ?? [];
        if (vertexLayouts.Length > VulkanCommandState.MaximumBindings ||
            (uint)vertexLayouts.Length > _physicalDeviceProperties.Limits.MaxVertexInputBindings)
        {
            throw new NotSupportedException("The Vulkan pipeline has more vertex layouts than the selected device supports.");
        }

        BuildVertexInput(vertexLayouts, out var vertexBindings, out var vertexAttributes);
        var stages = new PipelineShaderStageCreateInfo[shaderResources.Length];
        var entryPointNames = new byte*[shaderResources.Length];

        PipelineLayout pipelineLayout = default;
        RenderPass renderPass = default;

        try
        {
            for (var index = 0; index < shaderResources.Length; index++)
            {
                entryPointNames[index] = AllocateUtf8(shaderResources[index].EntryPoint);
                stages[index] = new PipelineShaderStageCreateInfo
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = shaderResources[index].Stage.ToVulkan(),
                    Module = shaderResources[index].Handle,
                    PName = entryPointNames[index],
                };
            }

            fixed (DescriptorSetLayout* layoutsPointer = descriptorLayoutHandles)
            {
                var layoutInfo = new PipelineLayoutCreateInfo
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = (uint)descriptorLayoutHandles.Length,
                    PSetLayouts = layoutsPointer,
                };
                Check(_vk.CreatePipelineLayout(_device, &layoutInfo, null, out pipelineLayout), "create a Vulkan pipeline layout");
            }

            renderPass = CreateRenderPass(description.Outputs);

            fixed (PipelineShaderStageCreateInfo* stagesPointer = stages)
            fixed (VertexInputBindingDescription* bindingsPointer = vertexBindings)
            fixed (VertexInputAttributeDescription* attributesPointer = vertexAttributes)
            {
                var vertexInput = new PipelineVertexInputStateCreateInfo
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = (uint)vertexBindings.Length,
                    PVertexBindingDescriptions = bindingsPointer,
                    VertexAttributeDescriptionCount = (uint)vertexAttributes.Length,
                    PVertexAttributeDescriptions = attributesPointer,
                };
                var inputAssembly = new PipelineInputAssemblyStateCreateInfo
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = description.PrimitiveTopology.ToVulkan(),
                    PrimitiveRestartEnable = false,
                };
                var viewportState = new PipelineViewportStateCreateInfo
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    ScissorCount = 1,
                };
                var rasterizer = new PipelineRasterizationStateCreateInfo
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = !description.RasterizerState.DepthClipEnabled,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    CullMode = description.RasterizerState.CullMode.ToVulkan(),
                    FrontFace = FrontFace.Clockwise,
                    DepthBiasEnable = false,
                    LineWidth = 1,
                };
                var multisample = new PipelineMultisampleStateCreateInfo
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                    SampleShadingEnable = false,
                };
                // Vulkan only writes depth while the depth test runs, so write-only pipelines
                // enable the test with an always-passing comparison, matching DX12 and Metal.
                var depthStencil = new PipelineDepthStencilStateCreateInfo
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    DepthTestEnable = description.DepthStencilState.DepthTestEnabled ||
                                      description.DepthStencilState.DepthWriteEnabled,
                    DepthWriteEnable = description.DepthStencilState.DepthWriteEnabled,
                    DepthCompareOp = description.DepthStencilState.DepthTestEnabled
                        ? description.DepthStencilState.DepthComparison.ToVulkan()
                        : CompareOp.Always,
                    DepthBoundsTestEnable = false,
                    StencilTestEnable = false,
                };
                var blendAttachment = description.BlendState.Attachment;
                var colorBlendAttachments = new PipelineColorBlendAttachmentState[
                    description.Outputs.ColorAttachments?.Length ?? 0];
                for (var index = 0; index < colorBlendAttachments.Length; index++)
                {
                    colorBlendAttachments[index] = new PipelineColorBlendAttachmentState
                    {
                        BlendEnable = blendAttachment.BlendEnabled,
                        SrcColorBlendFactor = blendAttachment.SourceColorFactor.ToVulkan(),
                        DstColorBlendFactor = blendAttachment.DestinationColorFactor.ToVulkan(),
                        ColorBlendOp = blendAttachment.ColorFunction.ToVulkan(),
                        SrcAlphaBlendFactor = blendAttachment.SourceAlphaFactor.ToVulkan(),
                        DstAlphaBlendFactor = blendAttachment.DestinationAlphaFactor.ToVulkan(),
                        AlphaBlendOp = blendAttachment.AlphaFunction.ToVulkan(),
                        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                                         ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    };
                }

                fixed (PipelineColorBlendAttachmentState* colorBlendPointer = colorBlendAttachments)
                {
                    var colorBlend = new PipelineColorBlendStateCreateInfo
                    {
                        SType = StructureType.PipelineColorBlendStateCreateInfo,
                        LogicOpEnable = false,
                        AttachmentCount = (uint)colorBlendAttachments.Length,
                        PAttachments = colorBlendPointer,
                    };
                    var dynamicStates = stackalloc DynamicState[2]
                    {
                        DynamicState.Viewport,
                        DynamicState.Scissor,
                    };
                    var dynamicState = new PipelineDynamicStateCreateInfo
                    {
                        SType = StructureType.PipelineDynamicStateCreateInfo,
                        DynamicStateCount = 2,
                        PDynamicStates = dynamicStates,
                    };
                    var pipelineInfo = new GraphicsPipelineCreateInfo
                    {
                        SType = StructureType.GraphicsPipelineCreateInfo,
                        StageCount = (uint)stages.Length,
                        PStages = stagesPointer,
                        PVertexInputState = &vertexInput,
                        PInputAssemblyState = &inputAssembly,
                        PViewportState = &viewportState,
                        PRasterizationState = &rasterizer,
                        PMultisampleState = &multisample,
                        PDepthStencilState = &depthStencil,
                        PColorBlendState = &colorBlend,
                        PDynamicState = &dynamicState,
                        Layout = pipelineLayout,
                        RenderPass = renderPass,
                        Subpass = 0,
                    };
                    Check(
                        _vk.CreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, out var pipeline),
                        "create a Vulkan graphics pipeline");
                    var resource = new VulkanPipelineResource
                    {
                        Handle = pipeline,
                        Layout = pipelineLayout,
                        CompatibilityRenderPass = renderPass,
                        Output = description.Outputs,
                        ScissorTestEnabled = description.RasterizerState.ScissorTestEnabled,
                        SetLayouts = descriptorLayouts,
                        VertexBufferCount = vertexLayouts.Length,
                    };
                    TrackResource(resource);
                    return resource;
                }
            }
        }
        catch
        {
            if (renderPass.Handle != 0)
                _vk.DestroyRenderPass(_device, renderPass, null);
            if (pipelineLayout.Handle != 0)
                _vk.DestroyPipelineLayout(_device, pipelineLayout, null);
            throw;
        }
        finally
        {
            foreach (var entryPoint in entryPointNames)
                FreeUtf8(entryPoint);
        }
    }

    private byte[] CompileGlsl(ShaderDescription description, CrossCompileOptions options)
    {
        var source = Encoding.UTF8.GetString(description.ShaderBytes);
        source = ApplyVertexFixups(source, description, options);
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var shaderc = NativeShaderTools.CreateShaderc();
        var compiler = shaderc.CompilerInitialize();
        if (compiler == null)
        {
            shaderc.Dispose();
            throw new InvalidOperationException("Shaderc failed to create a compiler.");
        }

        var compileOptions = shaderc.CompileOptionsInitialize();
        if (compileOptions == null)
        {
            shaderc.CompilerRelease(compiler);
            shaderc.Dispose();
            throw new InvalidOperationException("Shaderc failed to create compile options.");
        }

        CompilationResult* compilation = null;
        try
        {
            shaderc.CompileOptionsSetTargetEnv(compileOptions, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
            shaderc.CompileOptionsSetOptimizationLevel(
                compileOptions,
                _options.Debug ? OptimizationLevel.Zero : OptimizationLevel.Performance);
            if (_options.Debug)
                shaderc.CompileOptionsSetGenerateDebugInfo(compileOptions);
            shaderc.CompileOptionsSetWarningsAsErrors(compileOptions);

            fixed (byte* sourcePointer = sourceBytes)
            {
                compilation = shaderc.CompileIntoSpv(
                    compiler,
                    sourcePointer,
                    (nuint)sourceBytes.Length,
                    description.Stage == ShaderStages.Vertex ? ShaderKind.VertexShader : ShaderKind.FragmentShader,
                    "ElementEngine.glsl",
                    description.EntryPoint,
                    compileOptions);
            }

            if (compilation == null)
                throw new InvalidOperationException("Shaderc returned no compilation result.");

            var status = shaderc.ResultGetCompilationStatus(compilation);
            if (status != CompilationStatus.Success)
            {
                var message = Marshal.PtrToStringUTF8((nint)shaderc.ResultGetErrorMessage(compilation));
                throw new InvalidOperationException($"Shaderc failed with {status}: {message}");
            }

            var length = checked((int)shaderc.ResultGetLength(compilation));
            var spirv = new byte[length];
            new ReadOnlySpan<byte>(shaderc.ResultGetBytes(compilation), length).CopyTo(spirv);
            return spirv;
        }
        finally
        {
            if (compilation != null)
                shaderc.ResultRelease(compilation);
            shaderc.CompileOptionsRelease(compileOptions);
            shaderc.CompilerRelease(compiler);
            shaderc.Dispose();
        }
    }

    private static string ApplyVertexFixups(
        string source,
        ShaderDescription description,
        CrossCompileOptions options)
    {
        if (description.Stage != ShaderStages.Vertex || (!options.FixClipSpaceZ && !options.InvertVertexOutputY))
            return source;

        var signature = $"void {description.EntryPoint}";
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
            throw new InvalidOperationException($"Cannot apply Vulkan vertex fixups because {signature} was not found.");

        var internalEntryPoint = $"elementengine_{description.EntryPoint}";
        source = source.Remove(signatureIndex, signature.Length).Insert(signatureIndex, $"void {internalEntryPoint}");
        var builder = new StringBuilder(source);
        builder.AppendLine();
        builder.Append("void ").Append(description.EntryPoint).AppendLine("()");
        builder.AppendLine("{");
        builder.Append("    ").Append(internalEntryPoint).AppendLine("();");
        if (options.FixClipSpaceZ)
            builder.AppendLine("    gl_Position.z = (gl_Position.z + gl_Position.w) * 0.5;");
        if (options.InvertVertexOutputY)
            builder.AppendLine("    gl_Position.y = -gl_Position.y;");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static bool IsSpirv(byte[] bytes) =>
        bytes.Length >= sizeof(uint) && BitConverter.ToUInt32(bytes, 0) == 0x07230203;

    private static void BuildVertexInput(
        VertexLayoutDescription[] layouts,
        out VertexInputBindingDescription[] bindings,
        out VertexInputAttributeDescription[] attributes)
    {
        bindings = new VertexInputBindingDescription[layouts.Length];
        var attributeList = new List<VertexInputAttributeDescription>();
        uint location = 0;

        for (var binding = 0; binding < layouts.Length; binding++)
        {
            var elements = layouts[binding].Elements ?? [];
            uint offset = 0;

            foreach (var element in elements)
            {
                attributeList.Add(element.Format.ToVulkan(location++, (uint)binding, offset));
                offset += element.Format.SizeInBytes();
            }

            bindings[binding] = new VertexInputBindingDescription
            {
                Binding = (uint)binding,
                Stride = layouts[binding].Stride == 0 ? offset : layouts[binding].Stride,
                InputRate = VertexInputRate.Vertex,
            };

            if (bindings[binding].Stride < offset)
                throw new ArgumentException($"Vertex layout {binding} stride is smaller than its elements.", nameof(layouts));
        }

        attributes = attributeList.ToArray();
    }
}
