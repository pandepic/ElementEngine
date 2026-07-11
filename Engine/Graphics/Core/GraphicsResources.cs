using System;

namespace ElementEngine;

public abstract class GraphicsResource : IDisposable
{
    private bool _disposed;

    public GraphicsDevice GraphicsDevice { get; }
    public string Name { get; set; }
    internal object NativeResource { get; set; }
    internal bool IsDisposed => _disposed;

    protected GraphicsResource(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            GraphicsDevice.DestroyResource(this);
        }
        finally
        {
            NativeResource = null;
            GC.SuppressFinalize(this);
        }
    }
}

public sealed class DeviceBuffer : GraphicsResource
{
    public uint SizeInBytes => Description.SizeInBytes;
    public BufferUsage Usage => Description.Usage;
    public BufferDescription Description { get; }

    internal DeviceBuffer(GraphicsDevice graphicsDevice, BufferDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class Texture : GraphicsResource
{
    public TextureDescription Description { get; }
    public uint Width => Description.Width;
    public uint Height => Description.Height;
    public uint Depth => Description.Depth;
    public uint MipLevels => Description.MipLevels;
    public uint ArrayLayers => Description.ArrayLayers;
    public PixelFormat Format => Description.Format;
    public TextureUsage Usage => Description.Usage;
    public TextureType Type => Description.Type;

    internal Texture(GraphicsDevice graphicsDevice, TextureDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class TextureView : GraphicsResource
{
    public Texture Target { get; }

    internal TextureView(GraphicsDevice graphicsDevice, Texture target)
        : base(graphicsDevice)
    {
        Target = target;
    }
}

public enum SamplerFilter
{
    Point,
    Linear,
    Anisotropic,
}

public readonly record struct SamplerDescription(SamplerFilter Filter, uint MaximumAnisotropy = 1);

public sealed class Sampler : GraphicsResource
{
    public SamplerDescription Description { get; }

    internal Sampler(GraphicsDevice graphicsDevice, SamplerDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class Shader : GraphicsResource
{
    public ShaderDescription Description { get; }

    internal Shader(GraphicsDevice graphicsDevice, ShaderDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class ResourceLayout : GraphicsResource
{
    public ResourceLayoutDescription Description { get; }

    internal ResourceLayout(GraphicsDevice graphicsDevice, ResourceLayoutDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class ResourceSet : GraphicsResource
{
    public ResourceSetDescription Description { get; }

    internal ResourceSet(GraphicsDevice graphicsDevice, ResourceSetDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class Pipeline : GraphicsResource
{
    public GraphicsPipelineDescription Description { get; }

    internal Pipeline(GraphicsDevice graphicsDevice, GraphicsPipelineDescription description)
        : base(graphicsDevice)
    {
        Description = description;
    }
}

public sealed class Framebuffer : GraphicsResource
{
    public FramebufferDescription Description { get; }
    public OutputDescription OutputDescription { get; }
    internal bool IsSwapchain { get; }

    internal Framebuffer(
        GraphicsDevice graphicsDevice,
        FramebufferDescription description,
        OutputDescription outputDescription,
        bool isSwapchain = false)
        : base(graphicsDevice)
    {
        Description = description;
        OutputDescription = outputDescription;
        IsSwapchain = isSwapchain;
    }
}
