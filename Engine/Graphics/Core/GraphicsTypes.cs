using System;
using System.Runtime.InteropServices;

namespace ElementEngine;

public enum GraphicsBackend
{
    DirectX12,
    Vulkan,
    Metal,
}

public readonly record struct GraphicsDeviceOptions(bool Debug = false, bool SyncToVerticalBlank = false);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RgbaByte : IEquatable<RgbaByte>
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public static RgbaByte White => new(255, 255, 255, 255);
    public static RgbaByte Black => new(0, 0, 0, 255);
    public static RgbaByte Clear => new(0, 0, 0, 0);

    public RgbaByte(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public readonly bool Equals(RgbaByte other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override readonly bool Equals(object obj) => obj is RgbaByte other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public static bool operator ==(RgbaByte left, RgbaByte right) => left.Equals(right);
    public static bool operator !=(RgbaByte left, RgbaByte right) => !left.Equals(right);
}

[StructLayout(LayoutKind.Sequential)]
public struct RgbaFloat : IEquatable<RgbaFloat>
{
    public float R;
    public float G;
    public float B;
    public float A;

    public static RgbaFloat White => new(1f, 1f, 1f, 1f);
    public static RgbaFloat Black => new(0f, 0f, 0f, 1f);
    public static RgbaFloat Clear => new(0f, 0f, 0f, 0f);

    public RgbaFloat(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public readonly bool Equals(RgbaFloat other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override readonly bool Equals(object obj) => obj is RgbaFloat other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public static bool operator ==(RgbaFloat left, RgbaFloat right) => left.Equals(right);
    public static bool operator !=(RgbaFloat left, RgbaFloat right) => !left.Equals(right);
}

public readonly record struct Viewport(float X, float Y, float Width, float Height, float MinDepth, float MaxDepth);

[Flags]
public enum BufferUsage
{
    None = 0,
    VertexBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    UniformBuffer = 1 << 2,
    Staging = 1 << 3,
    Dynamic = 1 << 4,
}

public readonly record struct BufferDescription(uint SizeInBytes, BufferUsage Usage);

[Flags]
public enum TextureUsage
{
    None = 0,
    Sampled = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    Staging = 1 << 3,
}

public enum TextureType
{
    Texture1D,
    Texture2D,
    Texture3D,
}

public enum PixelFormat
{
    R8_UNorm,
    R8_SNorm,
    R8_UInt,
    R8_SInt,
    R16_UNorm,
    R16_SNorm,
    R16_UInt,
    R16_SInt,
    R16_Float,
    R32_UInt,
    R32_SInt,
    R32_Float,
    R8_G8_UNorm,
    R8_G8_SNorm,
    R8_G8_UInt,
    R8_G8_SInt,
    R16_G16_UNorm,
    R16_G16_SNorm,
    R16_G16_UInt,
    R16_G16_SInt,
    R16_G16_Float,
    R32_G32_UInt,
    R32_G32_SInt,
    R32_G32_Float,
    R8_G8_B8_A8_UNorm,
    R8_G8_B8_A8_UNorm_SRgb,
    B8_G8_R8_A8_UNorm,
    B8_G8_R8_A8_UNorm_SRgb,
    R8_G8_B8_A8_SNorm,
    R8_G8_B8_A8_UInt,
    R8_G8_B8_A8_SInt,
    R16_G16_B16_A16_UNorm,
    R16_G16_B16_A16_SNorm,
    R16_G16_B16_A16_UInt,
    R16_G16_B16_A16_SInt,
    R16_G16_B16_A16_Float,
    R32_G32_B32_A32_UInt,
    R32_G32_B32_A32_SInt,
    R32_G32_B32_A32_Float,
    D24_UNorm_S8_UInt,
    D32_Float_S8_UInt,
    ETC2_R8_G8_B8_UNorm,
    ETC2_R8_G8_B8_A8_UNorm,
}

public readonly record struct TextureDescription(
    uint Width,
    uint Height,
    uint Depth,
    uint MipLevels,
    uint ArrayLayers,
    PixelFormat Format,
    TextureUsage Usage,
    TextureType Type);

public enum IndexFormat
{
    UInt16,
    UInt32,
}

public enum PrimitiveTopology
{
    TriangleList,
    TriangleStrip,
    LineList,
    LineStrip,
}

public enum FaceCullMode
{
    None,
    Front,
    Back,
}

public enum ComparisonKind
{
    Never,
    Less,
    Equal,
    LessEqual,
    Greater,
    NotEqual,
    GreaterEqual,
    Always,
}

[Flags]
public enum ShaderStages
{
    None = 0,
    Vertex = 1 << 0,
    Fragment = 1 << 1,
}

public enum ResourceKind
{
    UniformBuffer,
    TextureReadOnly,
    Sampler,
}

public enum VertexElementSemantic
{
    Position,
    TextureCoordinate,
    Color,
}

public enum VertexElementFormat
{
    Float2,
    Float3,
    Float4,
    Byte4_Norm,
}

public readonly record struct VertexElementDescription(
    string Name,
    VertexElementSemantic Semantic,
    VertexElementFormat Format);

public readonly record struct VertexLayoutDescription
{
    public VertexElementDescription[] Elements { get; init; }
    public uint Stride { get; init; }

    public VertexLayoutDescription(params VertexElementDescription[] elements)
    {
        Elements = elements;
        Stride = 0;
    }
}

public readonly record struct ShaderDescription(ShaderStages Stage, byte[] ShaderBytes, string EntryPoint);

public readonly record struct ShaderSetDescription
{
    public VertexLayoutDescription[] VertexLayouts { get; init; }
    public Shader[] Shaders { get; init; }

    public ShaderSetDescription(VertexLayoutDescription[] vertexLayouts, Shader[] shaders)
    {
        VertexLayouts = vertexLayouts;
        Shaders = shaders;
    }
}

public readonly record struct ResourceLayoutElementDescription(
    string Name,
    ResourceKind Kind,
    ShaderStages Stages);

public readonly record struct ResourceLayoutDescription
{
    public ResourceLayoutElementDescription[] Elements { get; init; }

    public ResourceLayoutDescription(params ResourceLayoutElementDescription[] elements)
    {
        Elements = elements;
    }
}

public readonly record struct ResourceSetDescription
{
    public ResourceLayout Layout { get; init; }
    public object[] BoundResources { get; init; }

    public ResourceSetDescription(ResourceLayout layout, params object[] boundResources)
    {
        Layout = layout;
        BoundResources = boundResources;
    }
}

public readonly record struct FramebufferAttachmentDescription(Texture Target, uint ArrayLayer);

public struct FramebufferDescription
{
    public FramebufferAttachmentDescription? DepthTarget { get; set; }
    public FramebufferAttachmentDescription[] ColorTargets { get; set; }
}

public readonly record struct OutputDescription(PixelFormat[] ColorAttachments, PixelFormat? DepthAttachment = null)
{
    public static OutputDescription Default => new([PixelFormat.B8_G8_R8_A8_UNorm]);
}

public enum BlendFactor
{
    Zero,
    One,
    SourceAlpha,
    InverseSourceAlpha,
}

public enum BlendFunction
{
    Add,
    Subtract,
    ReverseSubtract,
    Minimum,
    Maximum,
}

public readonly record struct BlendAttachmentDescription(
    bool BlendEnabled,
    BlendFactor SourceColorFactor,
    BlendFactor DestinationColorFactor,
    BlendFunction ColorFunction,
    BlendFactor SourceAlphaFactor,
    BlendFactor DestinationAlphaFactor,
    BlendFunction AlphaFunction);

public readonly record struct BlendStateDescription
{
    public BlendAttachmentDescription Attachment { get; init; }

    public static BlendStateDescription SingleAlphaBlend => new()
    {
        Attachment = new BlendAttachmentDescription(
            true,
            BlendFactor.SourceAlpha,
            BlendFactor.InverseSourceAlpha,
            BlendFunction.Add,
            BlendFactor.One,
            BlendFactor.InverseSourceAlpha,
            BlendFunction.Add),
    };
}

public readonly record struct DepthStencilStateDescription
{
    public bool DepthTestEnabled { get; init; }
    public bool DepthWriteEnabled { get; init; }
    public ComparisonKind DepthComparison { get; init; }

    public DepthStencilStateDescription(
        bool depthTestEnabled,
        bool depthWriteEnabled,
        ComparisonKind depthComparison)
    {
        DepthTestEnabled = depthTestEnabled;
        DepthWriteEnabled = depthWriteEnabled;
        DepthComparison = depthComparison;
    }
}

public struct RasterizerStateDescription
{
    public bool DepthClipEnabled { get; set; }
    public FaceCullMode CullMode { get; set; }
    public bool ScissorTestEnabled { get; set; }
}

public struct GraphicsPipelineDescription
{
    public BlendStateDescription BlendState { get; set; }
    public DepthStencilStateDescription DepthStencilState { get; set; }
    public RasterizerStateDescription RasterizerState { get; set; }
    public PrimitiveTopology PrimitiveTopology { get; set; }
    public ShaderSetDescription ShaderSet { get; set; }
    public ResourceLayout[] ResourceLayouts { get; set; }
    public OutputDescription Outputs { get; set; }
}

public enum MapMode
{
    Read,
    Write,
    ReadWrite,
}

public readonly record struct MappedResource(nint Data, uint SizeInBytes, uint RowPitch, uint DepthPitch);

public readonly record struct MappedResourceView<T>(MappedResource MappedResource) where T : unmanaged
{
    public uint SizeInBytes => MappedResource.SizeInBytes;
}

public readonly record struct CrossCompileOptions
{
    public bool FixClipSpaceZ { get; init; }
    public bool InvertVertexOutputY { get; init; }

    public CrossCompileOptions(bool fixClipSpaceZ, bool invertVertexOutputY)
    {
        FixClipSpaceZ = fixClipSpaceZ;
        InvertVertexOutputY = invertVertexOutputY;
    }
}
