using System;
using System.Runtime.Versioning;
using SharpMetal.Metal;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal static class MetalConversions
{
    public const uint VertexBufferStartIndex = 24;
    public const uint MaximumVertexBuffers = 8;

    public static MTLPixelFormat ToMetal(this PixelFormat format, bool supportsDepth24Stencil8)
    {
        return format switch
        {
            PixelFormat.R8_UNorm => MTLPixelFormat.R8Unorm,
            PixelFormat.R8_SNorm => MTLPixelFormat.R8Snorm,
            PixelFormat.R8_UInt => MTLPixelFormat.R8Uint,
            PixelFormat.R8_SInt => MTLPixelFormat.R8Sint,
            PixelFormat.R16_UNorm => MTLPixelFormat.R16Unorm,
            PixelFormat.R16_SNorm => MTLPixelFormat.R16Snorm,
            PixelFormat.R16_UInt => MTLPixelFormat.R16Uint,
            PixelFormat.R16_SInt => MTLPixelFormat.R16Sint,
            PixelFormat.R16_Float => MTLPixelFormat.R16Float,
            PixelFormat.R32_UInt => MTLPixelFormat.R32Uint,
            PixelFormat.R32_SInt => MTLPixelFormat.R32Sint,
            PixelFormat.R32_Float => MTLPixelFormat.R32Float,
            PixelFormat.R8_G8_UNorm => MTLPixelFormat.RG8Unorm,
            PixelFormat.R8_G8_SNorm => MTLPixelFormat.RG8Snorm,
            PixelFormat.R8_G8_UInt => MTLPixelFormat.RG8Uint,
            PixelFormat.R8_G8_SInt => MTLPixelFormat.RG8Sint,
            PixelFormat.R16_G16_UNorm => MTLPixelFormat.RG16Unorm,
            PixelFormat.R16_G16_SNorm => MTLPixelFormat.RG16Snorm,
            PixelFormat.R16_G16_UInt => MTLPixelFormat.RG16Uint,
            PixelFormat.R16_G16_SInt => MTLPixelFormat.RG16Sint,
            PixelFormat.R16_G16_Float => MTLPixelFormat.RG16Float,
            PixelFormat.R32_G32_UInt => MTLPixelFormat.RG32Uint,
            PixelFormat.R32_G32_SInt => MTLPixelFormat.RG32Sint,
            PixelFormat.R32_G32_Float => MTLPixelFormat.RG32Float,
            PixelFormat.R8_G8_B8_A8_UNorm => MTLPixelFormat.RGBA8Unorm,
            PixelFormat.R8_G8_B8_A8_UNorm_SRgb => MTLPixelFormat.RGBA8UnormsRGB,
            PixelFormat.B8_G8_R8_A8_UNorm => MTLPixelFormat.BGRA8Unorm,
            PixelFormat.B8_G8_R8_A8_UNorm_SRgb => MTLPixelFormat.BGRA8UnormsRGB,
            PixelFormat.R8_G8_B8_A8_SNorm => MTLPixelFormat.RGBA8Snorm,
            PixelFormat.R8_G8_B8_A8_UInt => MTLPixelFormat.RGBA8Uint,
            PixelFormat.R8_G8_B8_A8_SInt => MTLPixelFormat.RGBA8Sint,
            PixelFormat.R16_G16_B16_A16_UNorm => MTLPixelFormat.RGBA16Unorm,
            PixelFormat.R16_G16_B16_A16_SNorm => MTLPixelFormat.RGBA16Snorm,
            PixelFormat.R16_G16_B16_A16_UInt => MTLPixelFormat.RGBA16Uint,
            PixelFormat.R16_G16_B16_A16_SInt => MTLPixelFormat.RGBA16Sint,
            PixelFormat.R16_G16_B16_A16_Float => MTLPixelFormat.RGBA16Float,
            PixelFormat.R32_G32_B32_A32_UInt => MTLPixelFormat.RGBA32Uint,
            PixelFormat.R32_G32_B32_A32_SInt => MTLPixelFormat.RGBA32Sint,
            PixelFormat.R32_G32_B32_A32_Float => MTLPixelFormat.RGBA32Float,
            PixelFormat.D24_UNorm_S8_UInt => supportsDepth24Stencil8
                ? MTLPixelFormat.Depth24UnormStencil8
                : MTLPixelFormat.Depth32FloatStencil8,
            PixelFormat.D32_Float_S8_UInt => MTLPixelFormat.Depth32FloatStencil8,
            PixelFormat.ETC2_R8_G8_B8_UNorm => MTLPixelFormat.ETC2RGB8,
            PixelFormat.ETC2_R8_G8_B8_A8_UNorm => MTLPixelFormat.EACRGBA8,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static MTLTextureType ToMetal(this TextureDescription description)
    {
        return description.Type switch
        {
            TextureType.Texture1D when description.ArrayLayers > 1 => MTLTextureType.Type1DArray,
            TextureType.Texture1D => MTLTextureType.Type1D,
            TextureType.Texture2D when description.ArrayLayers > 1 => MTLTextureType.Type2DArray,
            TextureType.Texture2D => MTLTextureType.Type2D,
            TextureType.Texture3D => MTLTextureType.Type3D,
            _ => throw new ArgumentOutOfRangeException(nameof(description)),
        };
    }

    public static MTLTextureUsage ToMetal(this TextureUsage usage)
    {
        var result = MTLTextureUsage.Unknown;

        if ((usage & TextureUsage.Sampled) != 0)
            result |= MTLTextureUsage.ShaderRead;
        if ((usage & (TextureUsage.RenderTarget | TextureUsage.DepthStencil)) != 0)
            result |= MTLTextureUsage.RenderTarget;

        return result;
    }

    public static MTLCompareFunction ToMetal(this ComparisonKind comparison)
    {
        return comparison switch
        {
            ComparisonKind.Never => MTLCompareFunction.Never,
            ComparisonKind.Less => MTLCompareFunction.Less,
            ComparisonKind.Equal => MTLCompareFunction.Equal,
            ComparisonKind.LessEqual => MTLCompareFunction.LessEqual,
            ComparisonKind.Greater => MTLCompareFunction.Greater,
            ComparisonKind.NotEqual => MTLCompareFunction.NotEqual,
            ComparisonKind.GreaterEqual => MTLCompareFunction.GreaterEqual,
            ComparisonKind.Always => MTLCompareFunction.Always,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison)),
        };
    }

    public static MTLBlendFactor ToMetal(this BlendFactor factor)
    {
        return factor switch
        {
            BlendFactor.Zero => MTLBlendFactor.Zero,
            BlendFactor.One => MTLBlendFactor.One,
            BlendFactor.SourceAlpha => MTLBlendFactor.SourceAlpha,
            BlendFactor.InverseSourceAlpha => MTLBlendFactor.OneMinusSourceAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor)),
        };
    }

    public static MTLBlendOperation ToMetal(this BlendFunction function)
    {
        return function switch
        {
            BlendFunction.Add => MTLBlendOperation.Add,
            BlendFunction.Subtract => MTLBlendOperation.Subtract,
            BlendFunction.ReverseSubtract => MTLBlendOperation.ReverseSubtract,
            BlendFunction.Minimum => MTLBlendOperation.Min,
            BlendFunction.Maximum => MTLBlendOperation.Max,
            _ => throw new ArgumentOutOfRangeException(nameof(function)),
        };
    }

    public static MTLCullMode ToMetal(this FaceCullMode mode)
    {
        return mode switch
        {
            FaceCullMode.None => MTLCullMode.None,
            FaceCullMode.Front => MTLCullMode.Front,
            FaceCullMode.Back => MTLCullMode.Back,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    public static MTLPrimitiveType ToMetal(this PrimitiveTopology topology)
    {
        return topology switch
        {
            PrimitiveTopology.TriangleList => MTLPrimitiveType.Triangle,
            PrimitiveTopology.TriangleStrip => MTLPrimitiveType.TriangleStrip,
            PrimitiveTopology.LineList => MTLPrimitiveType.Line,
            PrimitiveTopology.LineStrip => MTLPrimitiveType.LineStrip,
            _ => throw new ArgumentOutOfRangeException(nameof(topology)),
        };
    }

    public static MTLPrimitiveTopologyClass ToMetalClass(this PrimitiveTopology topology)
    {
        return topology switch
        {
            PrimitiveTopology.TriangleList or PrimitiveTopology.TriangleStrip => MTLPrimitiveTopologyClass.Triangle,
            PrimitiveTopology.LineList or PrimitiveTopology.LineStrip => MTLPrimitiveTopologyClass.Line,
            _ => throw new ArgumentOutOfRangeException(nameof(topology)),
        };
    }

    public static MTLVertexFormat ToMetal(this VertexElementFormat format)
    {
        return format switch
        {
            VertexElementFormat.Float2 => MTLVertexFormat.Float2,
            VertexElementFormat.Float3 => MTLVertexFormat.Float3,
            VertexElementFormat.Float4 => MTLVertexFormat.Float4,
            VertexElementFormat.Byte4_Norm => MTLVertexFormat.UChar4Normalized,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static uint GetVertexElementSize(this VertexElementFormat format)
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

    public static uint GetBytesPerPixel(this PixelFormat format)
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
            PixelFormat.ETC2_R8_G8_B8_UNorm => 8,
            PixelFormat.ETC2_R8_G8_B8_A8_UNorm => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    public static bool IsCompressed(this PixelFormat format) =>
        format is PixelFormat.ETC2_R8_G8_B8_UNorm or PixelFormat.ETC2_R8_G8_B8_A8_UNorm;

    public static bool HasStencil(this PixelFormat format) =>
        format is PixelFormat.D24_UNorm_S8_UInt or PixelFormat.D32_Float_S8_UInt;

    public static bool IsDepth(this PixelFormat format) => format.HasStencil();

    public static ulong AlignUp(ulong value, ulong alignment)
    {
        if (alignment <= 1)
            return value;

        return checked((value + alignment - 1) / alignment * alignment);
    }
}
