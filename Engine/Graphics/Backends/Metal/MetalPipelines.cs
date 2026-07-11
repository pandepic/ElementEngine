using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using SharpMetal.Foundation;
using SharpMetal.Metal;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MetalGraphicsBackend
{
    public object CreateGraphicsPipeline(GraphicsPipelineDescription description)
    {
        ThrowIfDisposed();
        using var pool = new NSAutoreleasePool();
        var shaders = description.ShaderSet.Shaders ?? [];
        if (shaders.Length == 0)
            throw new ArgumentException("A Metal graphics pipeline requires shaders.", nameof(description));

        var nativeShaders = shaders.Select(GetNative<MetalShaderResource>).ToArray();
        if (nativeShaders.Count(shader => shader.Description.Stage == ShaderStages.Vertex) != 1 ||
            nativeShaders.Count(shader => shader.Description.Stage == ShaderStages.Fragment) != 1)
        {
            throw new ArgumentException("A Metal graphics pipeline requires one vertex and one fragment shader.", nameof(description));
        }

        var resourceLayouts = (description.ResourceLayouts ?? [])
            .Select(GetNative<MetalResourceLayoutResource>)
            .ToArray();
        var resourceBindings = BuildResourceBindings(resourceLayouts);
        var compiledShaders = new List<MetalCompiledShader>(nativeShaders.Length);
        MTLRenderPipelineState pipelineState = default;
        MTLDepthStencilState depthStencilState = default;

        try
        {
            foreach (var shader in nativeShaders)
                compiledShaders.Add(_shaderCompiler.CrossCompile(_device, shader, resourceBindings));

            var vertex = compiledShaders[nativeShaders
                .Select((shader, index) => (shader, index))
                .Single(item => item.shader.Description.Stage == ShaderStages.Vertex).index];
            var fragment = compiledShaders[nativeShaders
                .Select((shader, index) => (shader, index))
                .Single(item => item.shader.Description.Stage == ShaderStages.Fragment).index];
            var vertexLayouts = description.ShaderSet.VertexLayouts ?? [];
            if (vertexLayouts.Length > MetalConversions.MaximumVertexBuffers)
                throw new NotSupportedException($"Metal supports at most {MetalConversions.MaximumVertexBuffers} vertex buffers in ElementEngine.");

            using var vertexDescriptor = CreateVertexDescriptor(vertexLayouts);
            using var pipelineDescriptor = new MTLRenderPipelineDescriptor
            {
                VertexFunction = vertex.Function,
                FragmentFunction = fragment.Function,
                VertexDescriptor = vertexDescriptor,
                InputPrimitiveTopology = description.PrimitiveTopology.ToMetalClass(),
                RasterSampleCount = 1,
            };

            ConfigurePipelineOutputs(pipelineDescriptor, description);

            NSError pipelineError = default;
            pipelineState = _device.NewRenderPipelineState(pipelineDescriptor, ref pipelineError);
            if (pipelineState.NativePtr == 0)
                throw new InvalidOperationException($"Metal pipeline creation failed: {DescribeError(pipelineError)}");

            using var depthDescriptor = new MTLDepthStencilDescriptor
            {
                DepthCompareFunction = description.DepthStencilState.DepthTestEnabled
                    ? description.DepthStencilState.DepthComparison.ToMetal()
                    : MTLCompareFunction.Always,
                IsDepthWriteEnabled = description.DepthStencilState.DepthWriteEnabled,
            };
            depthStencilState = _device.NewDepthStencilState(depthDescriptor);
            if (depthStencilState.NativePtr == 0)
                throw new InvalidOperationException("Failed to create the Metal depth-stencil state.");

            var pipeline = new MetalPipelineResource(
                pipelineState,
                depthStencilState,
                compiledShaders.Select(shader => shader.Library).ToArray(),
                compiledShaders.Select(shader => shader.Function).ToArray(),
                resourceLayouts,
                resourceBindings,
                description.Outputs,
                description.PrimitiveTopology.ToMetal(),
                description.RasterizerState.CullMode.ToMetal(),
                description.RasterizerState.DepthClipEnabled ? MTLDepthClipMode.Clip : MTLDepthClipMode.Clamp,
                description.RasterizerState.ScissorTestEnabled,
                checked((uint)vertexLayouts.Length));

            compiledShaders.Clear();
            return Track(pipeline);
        }
        catch (Exception creationException)
        {
            var teardown = new TeardownCollector(creationException);
            if (depthStencilState.NativePtr != 0)
                teardown.Run(depthStencilState.Dispose);
            if (pipelineState.NativePtr != 0)
                teardown.Run(pipelineState.Dispose);
            for (var index = compiledShaders.Count - 1; index >= 0; index--)
            {
                var shader = compiledShaders[index];
                teardown.Run(shader.Function.Dispose);
                teardown.Run(shader.Library.Dispose);
            }

            compiledShaders.Clear();
            teardown.ThrowIfFailed("Metal pipeline creation and cleanup both failed.");
            throw;
        }
    }

    private static MetalResourceBinding[] BuildResourceBindings(MetalResourceLayoutResource[] layouts)
    {
        var result = new List<MetalResourceBinding>();
        uint bufferIndex = 0;
        uint textureIndex = 0;
        uint samplerIndex = 0;

        for (var slot = 0; slot < layouts.Length; slot++)
        {
            var layout = layouts[slot];
            var elements = layout.Description.Elements ?? [];

            for (var elementIndex = 0; elementIndex < elements.Length; elementIndex++)
            {
                var element = elements[elementIndex];
                var nativeIndex = element.Kind switch
                {
                    ResourceKind.UniformBuffer => bufferIndex++,
                    ResourceKind.TextureReadOnly => textureIndex++,
                    ResourceKind.Sampler => samplerIndex++,
                    _ => throw new ArgumentOutOfRangeException(nameof(layouts)),
                };

                result.Add(new MetalResourceBinding(
                    checked((uint)slot),
                    checked((uint)elementIndex),
                    element.Kind,
                    element.Stages,
                    nativeIndex));
            }
        }

        if (bufferIndex > MetalConversions.VertexBufferStartIndex)
            throw new NotSupportedException($"A Metal pipeline can bind at most {MetalConversions.VertexBufferStartIndex} uniform buffers.");
        if (textureIndex > 128)
            throw new NotSupportedException("A Metal pipeline can bind at most 128 textures.");
        if (samplerIndex > 16)
            throw new NotSupportedException("A Metal pipeline can bind at most 16 samplers.");

        return result.ToArray();
    }

    private static MTLVertexDescriptor CreateVertexDescriptor(VertexLayoutDescription[] layouts)
    {
        var descriptor = new MTLVertexDescriptor();
        var attributeIndex = 0UL;

        try
        {
            for (var layoutIndex = 0; layoutIndex < layouts.Length; layoutIndex++)
            {
                var layout = layouts[layoutIndex];
                var elements = layout.Elements ?? [];
                var bufferIndex = MetalConversions.VertexBufferStartIndex + checked((uint)layoutIndex);
                var offset = 0u;

                foreach (var element in elements)
                {
                    var attribute = descriptor.Attributes.Object(attributeIndex);
                    attribute.Format = element.Format.ToMetal();
                    attribute.Offset = offset;
                    attribute.BufferIndex = bufferIndex;
                    offset = checked(offset + element.Format.GetVertexElementSize());
                    attributeIndex++;
                }

                var stride = layout.Stride == 0 ? offset : layout.Stride;
                if (stride < offset)
                    throw new ArgumentException($"Metal vertex layout {layoutIndex} has a stride smaller than its elements.", nameof(layouts));

                var nativeLayout = descriptor.Layouts.Object(bufferIndex);
                nativeLayout.Stride = stride;
                nativeLayout.StepFunction = MTLVertexStepFunction.PerVertex;
                nativeLayout.StepRate = 1;
            }

            return descriptor;
        }
        catch
        {
            descriptor.Dispose();
            throw;
        }
    }

    private void ConfigurePipelineOutputs(
        MTLRenderPipelineDescriptor descriptor,
        GraphicsPipelineDescription description)
    {
        var colorOutputs = description.Outputs.ColorAttachments ?? [];
        if (colorOutputs.Length == 0)
            throw new ArgumentException("A Metal graphics pipeline requires a color output.", nameof(description));
        if (colorOutputs.Length > 8)
            throw new NotSupportedException("Metal supports at most eight color outputs.");

        var blend = description.BlendState.Attachment;
        for (var index = 0; index < colorOutputs.Length; index++)
        {
            var attachment = descriptor.ColorAttachments.Object(checked((ulong)index));
            attachment.PixelFormat = colorOutputs[index].ToMetal(_device.IsDepth24Stencil8PixelFormatSupported);
            attachment.IsBlendingEnabled = blend.BlendEnabled;
            attachment.SourceRGBBlendFactor = blend.SourceColorFactor.ToMetal();
            attachment.DestinationRGBBlendFactor = blend.DestinationColorFactor.ToMetal();
            attachment.RgbBlendOperation = blend.ColorFunction.ToMetal();
            attachment.SourceAlphaBlendFactor = blend.SourceAlphaFactor.ToMetal();
            attachment.DestinationAlphaBlendFactor = blend.DestinationAlphaFactor.ToMetal();
            attachment.AlphaBlendOperation = blend.AlphaFunction.ToMetal();
            attachment.WriteMask = MTLColorWriteMask.All;
        }

        if (description.Outputs.DepthAttachment is { } depthFormat)
        {
            var metalDepthFormat = depthFormat.ToMetal(_device.IsDepth24Stencil8PixelFormatSupported);
            descriptor.DepthAttachmentPixelFormat = metalDepthFormat;
            if (depthFormat.HasStencil())
                descriptor.StencilAttachmentPixelFormat = metalDepthFormat;
        }
    }

    private static string DescribeError(NSError error)
    {
        if (error.NativePtr == 0)
            return "Metal did not provide an error description.";

        var description = error.LocalizedDescription;
        return description.NativePtr == 0 ? $"NSError {error.Code}" : description.ToString();
    }
}
