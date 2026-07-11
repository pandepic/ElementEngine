using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using DXGIFormat = Silk.NET.DXGI.Format;

namespace ElementEngine;

internal unsafe sealed partial class DirectX12Backend
{
    private const int MaximumBindings = 16;
    private const int MaximumInputElements = 32;
    private const uint MaximumVertexStride = 2048;

    private DirectX12Pipeline CreatePipeline(GraphicsPipelineDescription description)
    {
        if (description.ResourceLayouts is null)
            throw new ArgumentException("A graphics pipeline must provide resource layouts.", nameof(description));
        if (description.ShaderSet.Shaders is null)
            throw new ArgumentException("A graphics pipeline must provide shaders.", nameof(description));
        if (description.Outputs.ColorAttachments is null || description.Outputs.ColorAttachments.Length == 0)
            throw new ArgumentException("A graphics pipeline must provide color outputs.", nameof(description));
        if (description.Outputs.ColorAttachments.Length > 8)
            throw new NotSupportedException("DirectX 12 supports at most eight color outputs.");
        if (description.ResourceLayouts.Length > MaximumBindings)
            throw new NotSupportedException($"DirectX 12 pipelines support at most {MaximumBindings} resource layouts.");
        if (description.ShaderSet.VertexLayouts?.Length > MaximumBindings)
            throw new NotSupportedException($"DirectX 12 pipelines support at most {MaximumBindings} vertex-buffer layouts.");

        foreach (var format in description.Outputs.ColorAttachments)
        {
            if (IsDepthStencilFormat(format))
                throw new ArgumentException("A DirectX 12 color output requires a color format.", nameof(description));
        }
        if (description.Outputs.DepthAttachment is { } depthFormat && !IsDepthStencilFormat(depthFormat))
            throw new ArgumentException("A DirectX 12 depth output requires a depth-stencil format.", nameof(description));
        if ((description.DepthStencilState.DepthTestEnabled || description.DepthStencilState.DepthWriteEnabled) &&
            !description.Outputs.DepthAttachment.HasValue)
        {
            throw new ArgumentException(
                "DirectX 12 depth testing or writing requires a depth output.",
                nameof(description));
        }

        var vertexShader = FindShader(description.ShaderSet.Shaders, ShaderStages.Vertex);
        var fragmentShader = FindShader(description.ShaderSet.Shaders, ShaderStages.Fragment);
        var resourceLayouts = new DirectX12ResourceLayout[description.ResourceLayouts.Length];
        for (var index = 0; index < resourceLayouts.Length; index++)
        {
            resourceLayouts[index] = RequireNative<DirectX12ResourceLayout>(
                description.ResourceLayouts[index].NativeResource,
                "resource layout");
        }

        var rootSignature = CreateRootSignature(resourceLayouts, out var rootBindings);

        try
        {
            return CreatePipelineState(
                description,
                vertexShader,
                fragmentShader,
                rootSignature,
                rootBindings,
                resourceLayouts);
        }
        catch
        {
            rootSignature->Release();
            throw;
        }
    }

    private ID3D12RootSignature* CreateRootSignature(
        DirectX12ResourceLayout[] layouts,
        out DirectX12RootBinding[] rootBindings)
    {
        var rangeCount = 0;
        foreach (var layout in layouts)
            rangeCount += layout.Description.Elements.Length;

        var ranges = stackalloc DescriptorRange[Math.Max(1, rangeCount)];
        var parameters = stackalloc RootParameter[Math.Max(1, layouts.Length * 2)];
        rootBindings = new DirectX12RootBinding[layouts.Length];
        var rangeOffset = 0;
        var parameterCount = 0;

        for (var slot = 0; slot < layouts.Length; slot++)
        {
            var layout = layouts[slot];
            var resourceRangeStart = rangeOffset;
            var resourceRangeCount = 0;
            var samplerRangeStart = rangeOffset;
            var samplerRangeCount = 0;
            var resourceStages = ShaderStages.None;
            var samplerStages = ShaderStages.None;

            for (var binding = 0; binding < layout.Description.Elements.Length; binding++)
            {
                var element = layout.Description.Elements[binding];
                if (element.Kind == ResourceKind.Sampler)
                    continue;

                ranges[rangeOffset++] = new DescriptorRange(
                    element.Kind == ResourceKind.UniformBuffer ? DescriptorRangeType.Cbv : DescriptorRangeType.Srv,
                    1,
                    (uint)binding,
                    (uint)slot,
                    (uint)resourceRangeCount);
                resourceRangeCount++;
                resourceStages |= element.Stages;
            }

            samplerRangeStart = rangeOffset;
            for (var binding = 0; binding < layout.Description.Elements.Length; binding++)
            {
                var element = layout.Description.Elements[binding];
                if (element.Kind != ResourceKind.Sampler)
                    continue;

                ranges[rangeOffset++] = new DescriptorRange(
                    DescriptorRangeType.Sampler,
                    1,
                    (uint)binding,
                    (uint)slot,
                    (uint)samplerRangeCount);
                samplerRangeCount++;
                samplerStages |= element.Stages;
            }

            var resourceParameter = -1;
            var samplerParameter = -1;

            if (resourceRangeCount > 0)
            {
                resourceParameter = parameterCount;
                parameters[parameterCount++] = new RootParameter(
                    parameterType: RootParameterType.TypeDescriptorTable,
                    shaderVisibility: DirectX12Conversions.ToShaderVisibility(resourceStages),
                    descriptorTable: new RootDescriptorTable(
                        (uint)resourceRangeCount,
                        ranges + resourceRangeStart));
            }

            if (samplerRangeCount > 0)
            {
                samplerParameter = parameterCount;
                parameters[parameterCount++] = new RootParameter(
                    parameterType: RootParameterType.TypeDescriptorTable,
                    shaderVisibility: DirectX12Conversions.ToShaderVisibility(samplerStages),
                    descriptorTable: new RootDescriptorTable(
                        (uint)samplerRangeCount,
                        ranges + samplerRangeStart));
            }

            rootBindings[slot] = new DirectX12RootBinding(resourceParameter, samplerParameter);
        }

        var rootDescription = new RootSignatureDesc(
            (uint)parameterCount,
            parameters,
            0,
            null,
            RootSignatureFlags.AllowInputAssemblerInputLayout);
        ID3D10Blob* serialized = null;
        ID3D10Blob* errors = null;

        var result = _d3d12.SerializeRootSignature(
            &rootDescription,
            D3DRootSignatureVersion.Version1,
            &serialized,
            &errors);

        try
        {
            if (result < 0 || serialized == null)
            {
                var details = errors == null
                    ? $"HRESULT 0x{result:X8}"
                    : Marshal.PtrToStringUTF8(
                        (nint)errors->GetBufferPointer(),
                        checked((int)errors->GetBufferSize()));
                throw new InvalidOperationException($"Failed to serialize the DirectX 12 root signature: {details}");
            }

            ID3D12RootSignature* rootSignature = null;
            var rootSignatureId = ID3D12RootSignature.Guid;
            ThrowIfFailed(
                _device->CreateRootSignature(
                    0,
                    serialized->GetBufferPointer(),
                    serialized->GetBufferSize(),
                    &rootSignatureId,
                    (void**)&rootSignature),
                "create the DirectX 12 root signature");
            return rootSignature;
        }
        finally
        {
            if (errors != null)
                errors->Release();
            if (serialized != null)
                serialized->Release();
        }
    }

    private DirectX12Pipeline CreatePipelineState(
        GraphicsPipelineDescription description,
        DirectX12Shader vertexShader,
        DirectX12Shader fragmentShader,
        ID3D12RootSignature* rootSignature,
        DirectX12RootBinding[] rootBindings,
        DirectX12ResourceLayout[] resourceLayouts)
    {
        var layouts = description.ShaderSet.VertexLayouts ?? [];
        var elementCount = 0;
        foreach (var layout in layouts)
            elementCount += layout.Elements?.Length ?? 0;
        if (elementCount > MaximumInputElements)
            throw new NotSupportedException($"DirectX 12 pipelines support at most {MaximumInputElements} vertex elements.");

        var inputElements = new InputElementDesc[Math.Max(1, elementCount)];
        var vertexStrides = new uint[layouts.Length];
        var semanticName = Encoding.ASCII.GetBytes("TEXCOORD\0");
        var inputIndex = 0;
        var shaderLocation = 0u;

        fixed (byte* semanticNamePointer = semanticName)
        fixed (InputElementDesc* inputElementPointer = inputElements)
        fixed (byte* vertexBytecode = vertexShader.Bytecode)
        fixed (byte* fragmentBytecode = fragmentShader.Bytecode)
        {
            for (var slot = 0; slot < layouts.Length; slot++)
            {
                var layout = layouts[slot];
                var offset = 0u;

                foreach (var element in layout.Elements ?? [])
                {
                    inputElements[inputIndex++] = new InputElementDesc(
                        semanticNamePointer,
                        shaderLocation++,
                        DirectX12Conversions.ToVertexFormat(element.Format),
                        (uint)slot,
                        offset,
                        InputClassification.PerVertexData,
                        0);
                    offset += DirectX12Conversions.GetVertexElementSize(element.Format);
                }

                vertexStrides[slot] = layout.Stride == 0 ? offset : layout.Stride;
                if (layout.Stride != 0 && layout.Stride < offset)
                    throw new ArgumentException($"Vertex layout {slot} has a stride smaller than its elements.", nameof(description));
                if (vertexStrides[slot] > MaximumVertexStride)
                {
                    throw new NotSupportedException(
                        $"DirectX 12 vertex layout {slot} exceeds the {MaximumVertexStride}-byte stride limit.");
                }
            }

            var blendAttachment = description.BlendState.Attachment;
            var nativeBlendAttachment = new RenderTargetBlendDesc(
                blendAttachment.BlendEnabled,
                false,
                DirectX12Conversions.ToBlend(blendAttachment.SourceColorFactor),
                DirectX12Conversions.ToBlend(blendAttachment.DestinationColorFactor),
                DirectX12Conversions.ToBlendOperation(blendAttachment.ColorFunction),
                DirectX12Conversions.ToBlend(blendAttachment.SourceAlphaFactor),
                DirectX12Conversions.ToBlend(blendAttachment.DestinationAlphaFactor),
                DirectX12Conversions.ToBlendOperation(blendAttachment.AlphaFunction),
                LogicOp.Noop,
                0x0F);
            var blend = new BlendDesc(false, false);
            for (var index = 0; index < description.Outputs.ColorAttachments.Length; index++)
                blend.RenderTarget[index] = nativeBlendAttachment;

            var rasterizer = new RasterizerDesc(
                FillMode.Solid,
                DirectX12Conversions.ToCullMode(description.RasterizerState.CullMode),
                false,
                0,
                0,
                0,
                description.RasterizerState.DepthClipEnabled,
                false,
                false,
                0,
                ConservativeRasterizationMode.Off);
            var hasDepthOutput = description.Outputs.DepthAttachment.HasValue;
            var depthEnabled =
                (description.DepthStencilState.DepthTestEnabled || description.DepthStencilState.DepthWriteEnabled) &&
                hasDepthOutput;
            var stencilOperation = new DepthStencilopDesc(
                StencilOp.Keep,
                StencilOp.Keep,
                StencilOp.Keep,
                ComparisonFunc.Always);
            var depthStencil = new DepthStencilDesc(
                depthEnabled,
                description.DepthStencilState.DepthWriteEnabled && depthEnabled
                    ? DepthWriteMask.All
                    : DepthWriteMask.Zero,
                description.DepthStencilState.DepthTestEnabled
                    ? DirectX12Conversions.ToComparison(description.DepthStencilState.DepthComparison)
                    : ComparisonFunc.Always,
                false,
                byte.MaxValue,
                byte.MaxValue,
                stencilOperation,
                stencilOperation);
            var pipelineDescription = new GraphicsPipelineStateDesc(
                rootSignature,
                new ShaderBytecode(vertexBytecode, (nuint)vertexShader.Bytecode.Length),
                new ShaderBytecode(fragmentBytecode, (nuint)fragmentShader.Bytecode.Length),
                blendState: blend,
                sampleMask: uint.MaxValue,
                rasterizerState: rasterizer,
                depthStencilState: depthStencil,
                inputLayout: new InputLayoutDesc(inputElementPointer, (uint)elementCount),
                iBStripCutValue: IndexBufferStripCutValue.ValueDisabled,
                primitiveTopologyType: DirectX12Conversions.ToPrimitiveTopologyType(description.PrimitiveTopology),
                numRenderTargets: (uint)description.Outputs.ColorAttachments.Length,
                dSVFormat: description.Outputs.DepthAttachment.HasValue
                    ? DirectX12Conversions.ToDxgiFormat(description.Outputs.DepthAttachment.Value)
                    : DXGIFormat.FormatUnknown,
                sampleDesc: new Silk.NET.DXGI.SampleDesc(1, 0),
                flags: PipelineStateFlags.None);

            for (var index = 0; index < description.Outputs.ColorAttachments.Length; index++)
            {
                pipelineDescription.RTVFormats[index] =
                    DirectX12Conversions.ToDxgiFormat(description.Outputs.ColorAttachments[index]);
            }

            ID3D12PipelineState* pipelineState = null;
            var pipelineStateId = ID3D12PipelineState.Guid;
            ThrowIfFailed(
                _device->CreateGraphicsPipelineState(
                    &pipelineDescription,
                    &pipelineStateId,
                    (void**)&pipelineState),
                "create the DirectX 12 graphics pipeline");

            return new DirectX12Pipeline(
                pipelineState,
                rootSignature,
                DirectX12Conversions.ToPrimitiveTopology(description.PrimitiveTopology),
                vertexStrides,
                rootBindings,
                resourceLayouts,
                new OutputDescription(
                    description.Outputs.ColorAttachments.ToArray(),
                    description.Outputs.DepthAttachment),
                description.RasterizerState.ScissorTestEnabled);
        }
    }

    private DirectX12Shader FindShader(Shader[] shaders, ShaderStages stage)
    {
        DirectX12Shader result = null;

        foreach (var shader in shaders)
        {
            if (shader.Description.Stage != stage)
                continue;
            if (result is not null)
                throw new ArgumentException($"A graphics pipeline cannot contain multiple {stage} shaders.", nameof(shaders));

            result = RequireNative<DirectX12Shader>(shader.NativeResource, "shader");
        }

        return result ?? throw new ArgumentException($"A graphics pipeline requires a {stage} shader.", nameof(shaders));
    }
}
