using System;
using Silk.NET.Vulkan;

namespace ElementEngine;

internal static class VulkanConversions
{
    internal static Silk.NET.Vulkan.Viewport ToViewport(ElementEngine.Viewport viewport) => new(
        viewport.X,
        viewport.Y + viewport.Height,
        viewport.Width,
        -viewport.Height,
        viewport.MinDepth,
        viewport.MaxDepth);

    public static Format ToVulkan(this PixelFormat format) => format switch
    {
        PixelFormat.R8_UNorm => Format.R8Unorm,
        PixelFormat.R8_SNorm => Format.R8SNorm,
        PixelFormat.R8_UInt => Format.R8Uint,
        PixelFormat.R8_SInt => Format.R8Sint,
        PixelFormat.R16_UNorm => Format.R16Unorm,
        PixelFormat.R16_SNorm => Format.R16SNorm,
        PixelFormat.R16_UInt => Format.R16Uint,
        PixelFormat.R16_SInt => Format.R16Sint,
        PixelFormat.R16_Float => Format.R16Sfloat,
        PixelFormat.R32_UInt => Format.R32Uint,
        PixelFormat.R32_SInt => Format.R32Sint,
        PixelFormat.R32_Float => Format.R32Sfloat,
        PixelFormat.R8_G8_UNorm => Format.R8G8Unorm,
        PixelFormat.R8_G8_SNorm => Format.R8G8SNorm,
        PixelFormat.R8_G8_UInt => Format.R8G8Uint,
        PixelFormat.R8_G8_SInt => Format.R8G8Sint,
        PixelFormat.R16_G16_UNorm => Format.R16G16Unorm,
        PixelFormat.R16_G16_SNorm => Format.R16G16SNorm,
        PixelFormat.R16_G16_UInt => Format.R16G16Uint,
        PixelFormat.R16_G16_SInt => Format.R16G16Sint,
        PixelFormat.R16_G16_Float => Format.R16G16Sfloat,
        PixelFormat.R32_G32_UInt => Format.R32G32Uint,
        PixelFormat.R32_G32_SInt => Format.R32G32Sint,
        PixelFormat.R32_G32_Float => Format.R32G32Sfloat,
        PixelFormat.R8_G8_B8_A8_UNorm => Format.R8G8B8A8Unorm,
        PixelFormat.R8_G8_B8_A8_UNorm_SRgb => Format.R8G8B8A8Srgb,
        PixelFormat.B8_G8_R8_A8_UNorm => Format.B8G8R8A8Unorm,
        PixelFormat.B8_G8_R8_A8_UNorm_SRgb => Format.B8G8R8A8Srgb,
        PixelFormat.R8_G8_B8_A8_SNorm => Format.R8G8B8A8SNorm,
        PixelFormat.R8_G8_B8_A8_UInt => Format.R8G8B8A8Uint,
        PixelFormat.R8_G8_B8_A8_SInt => Format.R8G8B8A8Sint,
        PixelFormat.R16_G16_B16_A16_UNorm => Format.R16G16B16A16Unorm,
        PixelFormat.R16_G16_B16_A16_SNorm => Format.R16G16B16A16SNorm,
        PixelFormat.R16_G16_B16_A16_UInt => Format.R16G16B16A16Uint,
        PixelFormat.R16_G16_B16_A16_SInt => Format.R16G16B16A16Sint,
        PixelFormat.R16_G16_B16_A16_Float => Format.R16G16B16A16Sfloat,
        PixelFormat.R32_G32_B32_A32_UInt => Format.R32G32B32A32Uint,
        PixelFormat.R32_G32_B32_A32_SInt => Format.R32G32B32A32Sint,
        PixelFormat.R32_G32_B32_A32_Float => Format.R32G32B32A32Sfloat,
        PixelFormat.D24_UNorm_S8_UInt => Format.D24UnormS8Uint,
        PixelFormat.D32_Float_S8_UInt => Format.D32SfloatS8Uint,
        PixelFormat.ETC2_R8_G8_B8_UNorm => Format.Etc2R8G8B8UnormBlock,
        PixelFormat.ETC2_R8_G8_B8_A8_UNorm => Format.Etc2R8G8B8A8UnormBlock,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported Vulkan pixel format."),
    };

    public static PixelFormat ToElement(this Format format) => format switch
    {
        Format.B8G8R8A8Unorm => PixelFormat.B8_G8_R8_A8_UNorm,
        Format.B8G8R8A8Srgb => PixelFormat.B8_G8_R8_A8_UNorm_SRgb,
        Format.R8G8B8A8Unorm => PixelFormat.R8_G8_B8_A8_UNorm,
        Format.R8G8B8A8Srgb => PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
        _ => throw new NotSupportedException($"The Vulkan swapchain format {format} has no ElementEngine equivalent."),
    };

    public static ImageAspectFlags AspectMask(this PixelFormat format) => format switch
    {
        PixelFormat.D24_UNorm_S8_UInt or PixelFormat.D32_Float_S8_UInt =>
            ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit,
        _ => ImageAspectFlags.ColorBit,
    };

    // Sampled image views must select exactly one aspect (VUID-VkDescriptorImageInfo-imageView-01976),
    // so depth-stencil textures are sampled through their depth aspect alone.
    public static ImageAspectFlags SampledAspectMask(this ImageAspectFlags aspectMask) =>
        (aspectMask & ImageAspectFlags.DepthBit) != 0 ? ImageAspectFlags.DepthBit : aspectMask;

    public static ImageType ToVulkan(this TextureType type) => type switch
    {
        TextureType.Texture1D => ImageType.Type1D,
        TextureType.Texture2D => ImageType.Type2D,
        TextureType.Texture3D => ImageType.Type3D,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static ImageViewType ToViewType(this TextureDescription description)
    {
        if (description.Type == TextureType.Texture3D)
            return ImageViewType.Type3D;
        if (description.ArrayLayers > 1)
            return description.Type == TextureType.Texture1D ? ImageViewType.Type1DArray : ImageViewType.Type2DArray;

        return description.Type == TextureType.Texture1D ? ImageViewType.Type1D : ImageViewType.Type2D;
    }

    public static Silk.NET.Vulkan.PrimitiveTopology ToVulkan(this PrimitiveTopology topology) => topology switch
    {
        PrimitiveTopology.TriangleList => Silk.NET.Vulkan.PrimitiveTopology.TriangleList,
        PrimitiveTopology.TriangleStrip => Silk.NET.Vulkan.PrimitiveTopology.TriangleStrip,
        PrimitiveTopology.LineList => Silk.NET.Vulkan.PrimitiveTopology.LineList,
        PrimitiveTopology.LineStrip => Silk.NET.Vulkan.PrimitiveTopology.LineStrip,
        _ => throw new ArgumentOutOfRangeException(nameof(topology)),
    };

    public static CullModeFlags ToVulkan(this FaceCullMode mode) => mode switch
    {
        FaceCullMode.None => CullModeFlags.None,
        FaceCullMode.Front => CullModeFlags.FrontBit,
        FaceCullMode.Back => CullModeFlags.BackBit,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static CompareOp ToVulkan(this ComparisonKind comparison) => comparison switch
    {
        ComparisonKind.Never => CompareOp.Never,
        ComparisonKind.Less => CompareOp.Less,
        ComparisonKind.Equal => CompareOp.Equal,
        ComparisonKind.LessEqual => CompareOp.LessOrEqual,
        ComparisonKind.Greater => CompareOp.Greater,
        ComparisonKind.NotEqual => CompareOp.NotEqual,
        ComparisonKind.GreaterEqual => CompareOp.GreaterOrEqual,
        ComparisonKind.Always => CompareOp.Always,
        _ => throw new ArgumentOutOfRangeException(nameof(comparison)),
    };

    public static Silk.NET.Vulkan.BlendFactor ToVulkan(this BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => Silk.NET.Vulkan.BlendFactor.Zero,
        BlendFactor.One => Silk.NET.Vulkan.BlendFactor.One,
        BlendFactor.SourceAlpha => Silk.NET.Vulkan.BlendFactor.SrcAlpha,
        BlendFactor.InverseSourceAlpha => Silk.NET.Vulkan.BlendFactor.OneMinusSrcAlpha,
        _ => throw new ArgumentOutOfRangeException(nameof(factor)),
    };

    public static BlendOp ToVulkan(this BlendFunction function) => function switch
    {
        BlendFunction.Add => BlendOp.Add,
        BlendFunction.Subtract => BlendOp.Subtract,
        BlendFunction.ReverseSubtract => BlendOp.ReverseSubtract,
        BlendFunction.Minimum => BlendOp.Min,
        BlendFunction.Maximum => BlendOp.Max,
        _ => throw new ArgumentOutOfRangeException(nameof(function)),
    };

    public static ShaderStageFlags ToVulkan(this ShaderStages stages)
    {
        var flags = ShaderStageFlags.None;
        if ((stages & ShaderStages.Vertex) != 0)
            flags |= ShaderStageFlags.VertexBit;
        if ((stages & ShaderStages.Fragment) != 0)
            flags |= ShaderStageFlags.FragmentBit;
        return flags;
    }

    public static DescriptorType ToVulkan(this ResourceKind kind) => kind switch
    {
        ResourceKind.UniformBuffer => DescriptorType.UniformBuffer,
        ResourceKind.TextureReadOnly => DescriptorType.SampledImage,
        ResourceKind.Sampler => DescriptorType.Sampler,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static VertexInputAttributeDescription ToVulkan(
        this VertexElementFormat format,
        uint location,
        uint binding,
        uint offset) => new()
        {
            Location = location,
            Binding = binding,
            Offset = offset,
            Format = format switch
            {
                VertexElementFormat.Float2 => Format.R32G32Sfloat,
                VertexElementFormat.Float3 => Format.R32G32B32Sfloat,
                VertexElementFormat.Float4 => Format.R32G32B32A32Sfloat,
                VertexElementFormat.Byte4_Norm => Format.R8G8B8A8Unorm,
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            },
        };

    public static uint SizeInBytes(this VertexElementFormat format) => format switch
    {
        VertexElementFormat.Float2 => 8,
        VertexElementFormat.Float3 => 12,
        VertexElementFormat.Float4 => 16,
        VertexElementFormat.Byte4_Norm => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static uint BytesPerBlock(this PixelFormat format) => format switch
    {
        PixelFormat.R8_UNorm or PixelFormat.R8_SNorm or PixelFormat.R8_UInt or PixelFormat.R8_SInt => 1,
        PixelFormat.R16_UNorm or PixelFormat.R16_SNorm or PixelFormat.R16_UInt or PixelFormat.R16_SInt or
        PixelFormat.R16_Float or PixelFormat.R8_G8_UNorm or PixelFormat.R8_G8_SNorm or PixelFormat.R8_G8_UInt or
        PixelFormat.R8_G8_SInt => 2,
        PixelFormat.R32_UInt or PixelFormat.R32_SInt or PixelFormat.R32_Float or PixelFormat.R16_G16_UNorm or
        PixelFormat.R16_G16_SNorm or PixelFormat.R16_G16_UInt or PixelFormat.R16_G16_SInt or
        PixelFormat.R16_G16_Float or PixelFormat.R8_G8_B8_A8_UNorm or PixelFormat.R8_G8_B8_A8_UNorm_SRgb or
        PixelFormat.B8_G8_R8_A8_UNorm or PixelFormat.B8_G8_R8_A8_UNorm_SRgb or PixelFormat.R8_G8_B8_A8_SNorm or
        PixelFormat.R8_G8_B8_A8_UInt or PixelFormat.R8_G8_B8_A8_SInt or PixelFormat.D24_UNorm_S8_UInt => 4,
        PixelFormat.R32_G32_UInt or PixelFormat.R32_G32_SInt or PixelFormat.R32_G32_Float or
        PixelFormat.R16_G16_B16_A16_UNorm or PixelFormat.R16_G16_B16_A16_SNorm or
        PixelFormat.R16_G16_B16_A16_UInt or PixelFormat.R16_G16_B16_A16_SInt or
        PixelFormat.R16_G16_B16_A16_Float or PixelFormat.D32_Float_S8_UInt => 8,
        PixelFormat.R32_G32_B32_A32_UInt or PixelFormat.R32_G32_B32_A32_SInt or
        PixelFormat.R32_G32_B32_A32_Float or PixelFormat.ETC2_R8_G8_B8_A8_UNorm => 16,
        PixelFormat.ETC2_R8_G8_B8_UNorm => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsCompressed(this PixelFormat format) =>
        format is PixelFormat.ETC2_R8_G8_B8_UNorm or PixelFormat.ETC2_R8_G8_B8_A8_UNorm;
}
