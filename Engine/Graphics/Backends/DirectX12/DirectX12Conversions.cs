using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using DxgiFormat = Silk.NET.DXGI.Format;

namespace ElementEngine;

internal static class DirectX12Conversions
{
    public const uint DefaultShaderComponentMapping = 5768;

    public static DxgiFormat ToDxgiFormat(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.R8_UNorm => DxgiFormat.FormatR8Unorm,
            PixelFormat.R8_SNorm => DxgiFormat.FormatR8SNorm,
            PixelFormat.R8_UInt => DxgiFormat.FormatR8Uint,
            PixelFormat.R8_SInt => DxgiFormat.FormatR8Sint,
            PixelFormat.R16_UNorm => DxgiFormat.FormatR16Unorm,
            PixelFormat.R16_SNorm => DxgiFormat.FormatR16SNorm,
            PixelFormat.R16_UInt => DxgiFormat.FormatR16Uint,
            PixelFormat.R16_SInt => DxgiFormat.FormatR16Sint,
            PixelFormat.R16_Float => DxgiFormat.FormatR16Float,
            PixelFormat.R32_UInt => DxgiFormat.FormatR32Uint,
            PixelFormat.R32_SInt => DxgiFormat.FormatR32Sint,
            PixelFormat.R32_Float => DxgiFormat.FormatR32Float,
            PixelFormat.R8_G8_UNorm => DxgiFormat.FormatR8G8Unorm,
            PixelFormat.R8_G8_SNorm => DxgiFormat.FormatR8G8SNorm,
            PixelFormat.R8_G8_UInt => DxgiFormat.FormatR8G8Uint,
            PixelFormat.R8_G8_SInt => DxgiFormat.FormatR8G8Sint,
            PixelFormat.R16_G16_UNorm => DxgiFormat.FormatR16G16Unorm,
            PixelFormat.R16_G16_SNorm => DxgiFormat.FormatR16G16SNorm,
            PixelFormat.R16_G16_UInt => DxgiFormat.FormatR16G16Uint,
            PixelFormat.R16_G16_SInt => DxgiFormat.FormatR16G16Sint,
            PixelFormat.R16_G16_Float => DxgiFormat.FormatR16G16Float,
            PixelFormat.R32_G32_UInt => DxgiFormat.FormatR32G32Uint,
            PixelFormat.R32_G32_SInt => DxgiFormat.FormatR32G32Sint,
            PixelFormat.R32_G32_Float => DxgiFormat.FormatR32G32Float,
            PixelFormat.R8_G8_B8_A8_UNorm => DxgiFormat.FormatR8G8B8A8Unorm,
            PixelFormat.R8_G8_B8_A8_UNorm_SRgb => DxgiFormat.FormatR8G8B8A8UnormSrgb,
            PixelFormat.B8_G8_R8_A8_UNorm => DxgiFormat.FormatB8G8R8A8Unorm,
            PixelFormat.B8_G8_R8_A8_UNorm_SRgb => DxgiFormat.FormatB8G8R8A8UnormSrgb,
            PixelFormat.R8_G8_B8_A8_SNorm => DxgiFormat.FormatR8G8B8A8SNorm,
            PixelFormat.R8_G8_B8_A8_UInt => DxgiFormat.FormatR8G8B8A8Uint,
            PixelFormat.R8_G8_B8_A8_SInt => DxgiFormat.FormatR8G8B8A8Sint,
            PixelFormat.R16_G16_B16_A16_UNorm => DxgiFormat.FormatR16G16B16A16Unorm,
            PixelFormat.R16_G16_B16_A16_SNorm => DxgiFormat.FormatR16G16B16A16SNorm,
            PixelFormat.R16_G16_B16_A16_UInt => DxgiFormat.FormatR16G16B16A16Uint,
            PixelFormat.R16_G16_B16_A16_SInt => DxgiFormat.FormatR16G16B16A16Sint,
            PixelFormat.R16_G16_B16_A16_Float => DxgiFormat.FormatR16G16B16A16Float,
            PixelFormat.R32_G32_B32_A32_UInt => DxgiFormat.FormatR32G32B32A32Uint,
            PixelFormat.R32_G32_B32_A32_SInt => DxgiFormat.FormatR32G32B32A32Sint,
            PixelFormat.R32_G32_B32_A32_Float => DxgiFormat.FormatR32G32B32A32Float,
            PixelFormat.D24_UNorm_S8_UInt => DxgiFormat.FormatD24UnormS8Uint,
            PixelFormat.D32_Float_S8_UInt => DxgiFormat.FormatD32FloatS8X24Uint,
            PixelFormat.ETC2_R8_G8_B8_UNorm or PixelFormat.ETC2_R8_G8_B8_A8_UNorm
                => throw new NotSupportedException($"DirectX 12 does not support the {format} texture format."),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static uint GetBytesPerPixel(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.R8_UNorm or PixelFormat.R8_SNorm or PixelFormat.R8_UInt or PixelFormat.R8_SInt => 1,
            PixelFormat.R16_UNorm or PixelFormat.R16_SNorm or PixelFormat.R16_UInt or PixelFormat.R16_SInt or
                PixelFormat.R16_Float or PixelFormat.R8_G8_UNorm or PixelFormat.R8_G8_SNorm or
                PixelFormat.R8_G8_UInt or PixelFormat.R8_G8_SInt => 2,
            PixelFormat.R32_UInt or PixelFormat.R32_SInt or PixelFormat.R32_Float or
                PixelFormat.R16_G16_UNorm or PixelFormat.R16_G16_SNorm or PixelFormat.R16_G16_UInt or
                PixelFormat.R16_G16_SInt or PixelFormat.R16_G16_Float or PixelFormat.R8_G8_B8_A8_UNorm or
                PixelFormat.R8_G8_B8_A8_UNorm_SRgb or PixelFormat.B8_G8_R8_A8_UNorm or
                PixelFormat.B8_G8_R8_A8_UNorm_SRgb or PixelFormat.R8_G8_B8_A8_SNorm or
                PixelFormat.R8_G8_B8_A8_UInt or PixelFormat.R8_G8_B8_A8_SInt or
                PixelFormat.D24_UNorm_S8_UInt => 4,
            PixelFormat.R32_G32_UInt or PixelFormat.R32_G32_SInt or PixelFormat.R32_G32_Float or
                PixelFormat.R16_G16_B16_A16_UNorm or PixelFormat.R16_G16_B16_A16_SNorm or
                PixelFormat.R16_G16_B16_A16_UInt or PixelFormat.R16_G16_B16_A16_SInt or
                PixelFormat.R16_G16_B16_A16_Float or PixelFormat.D32_Float_S8_UInt => 8,
            PixelFormat.R32_G32_B32_A32_UInt or PixelFormat.R32_G32_B32_A32_SInt or
                PixelFormat.R32_G32_B32_A32_Float => 16,
            PixelFormat.ETC2_R8_G8_B8_UNorm or PixelFormat.ETC2_R8_G8_B8_A8_UNorm
                => throw new NotSupportedException($"DirectX 12 does not support the {format} texture format."),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static Silk.NET.Direct3D12.ComparisonFunc ToComparison(ComparisonKind comparison)
    {
        return comparison switch
        {
            ComparisonKind.Never => ComparisonFunc.Never,
            ComparisonKind.Less => ComparisonFunc.Less,
            ComparisonKind.Equal => ComparisonFunc.Equal,
            ComparisonKind.LessEqual => ComparisonFunc.LessEqual,
            ComparisonKind.Greater => ComparisonFunc.Greater,
            ComparisonKind.NotEqual => ComparisonFunc.NotEqual,
            ComparisonKind.GreaterEqual => ComparisonFunc.GreaterEqual,
            ComparisonKind.Always => ComparisonFunc.Always,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison)),
        };
    }

    public static Silk.NET.Direct3D12.Blend ToBlend(BlendFactor factor)
    {
        return factor switch
        {
            BlendFactor.Zero => Blend.Zero,
            BlendFactor.One => Blend.One,
            BlendFactor.SourceAlpha => Blend.SrcAlpha,
            BlendFactor.InverseSourceAlpha => Blend.InvSrcAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor)),
        };
    }

    public static Silk.NET.Direct3D12.BlendOp ToBlendOperation(BlendFunction function)
    {
        return function switch
        {
            BlendFunction.Add => BlendOp.Add,
            BlendFunction.Subtract => BlendOp.Subtract,
            BlendFunction.ReverseSubtract => BlendOp.RevSubtract,
            BlendFunction.Minimum => BlendOp.Min,
            BlendFunction.Maximum => BlendOp.Max,
            _ => throw new ArgumentOutOfRangeException(nameof(function)),
        };
    }

    public static Silk.NET.Direct3D12.CullMode ToCullMode(FaceCullMode mode)
    {
        return mode switch
        {
            FaceCullMode.None => CullMode.None,
            FaceCullMode.Front => CullMode.Front,
            FaceCullMode.Back => CullMode.Back,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    public static D3DPrimitiveTopology ToPrimitiveTopology(PrimitiveTopology topology)
    {
        return topology switch
        {
            PrimitiveTopology.TriangleList => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist,
            PrimitiveTopology.TriangleStrip => D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglestrip,
            PrimitiveTopology.LineList => D3DPrimitiveTopology.D3DPrimitiveTopologyLinelist,
            PrimitiveTopology.LineStrip => D3DPrimitiveTopology.D3DPrimitiveTopologyLinestrip,
            _ => throw new ArgumentOutOfRangeException(nameof(topology)),
        };
    }

    public static PrimitiveTopologyType ToPrimitiveTopologyType(PrimitiveTopology topology)
    {
        return topology switch
        {
            PrimitiveTopology.TriangleList or PrimitiveTopology.TriangleStrip => PrimitiveTopologyType.Triangle,
            PrimitiveTopology.LineList or PrimitiveTopology.LineStrip => PrimitiveTopologyType.Line,
            _ => throw new ArgumentOutOfRangeException(nameof(topology)),
        };
    }

    public static DxgiFormat ToVertexFormat(VertexElementFormat format)
    {
        return format switch
        {
            VertexElementFormat.Float2 => DxgiFormat.FormatR32G32Float,
            VertexElementFormat.Float3 => DxgiFormat.FormatR32G32B32Float,
            VertexElementFormat.Float4 => DxgiFormat.FormatR32G32B32A32Float,
            VertexElementFormat.Byte4_Norm => DxgiFormat.FormatR8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static uint GetVertexElementSize(VertexElementFormat format)
    {
        return format switch
        {
            VertexElementFormat.Float2 => 8,
            VertexElementFormat.Float3 => 12,
            VertexElementFormat.Float4 => 16,
            VertexElementFormat.Byte4_Norm => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static ShaderVisibility ToShaderVisibility(ShaderStages stages)
    {
        return stages switch
        {
            ShaderStages.Vertex => ShaderVisibility.Vertex,
            ShaderStages.Fragment => ShaderVisibility.Pixel,
            _ => ShaderVisibility.All,
        };
    }
}
