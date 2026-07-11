using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace ElementEngine;

public sealed class GraphicsDevice : IDisposable
{
    private bool _disposed;

    internal IGraphicsBackend Backend { get; }
    public GraphicsBackend BackendType => Backend.BackendType;
    public bool IsUvOriginTopLeft => Backend.IsUvOriginTopLeft;
    public ResourceFactory ResourceFactory { get; }
    public Framebuffer SwapchainFramebuffer { get; }
    public Sampler PointSampler { get; }
    public Sampler LinearSampler { get; }
    public Sampler Aniso4xSampler { get; }

    internal GraphicsDevice(IGraphicsBackend backend)
    {
        Backend = backend;
        ResourceFactory = new ResourceFactory(this);
        SwapchainFramebuffer = new Framebuffer(
            this,
            default,
            backend.SwapchainOutputDescription,
            isSwapchain: true);
        PointSampler = ResourceFactory.CreateSampler(new SamplerDescription(SamplerFilter.Point));
        LinearSampler = ResourceFactory.CreateSampler(new SamplerDescription(SamplerFilter.Linear));
        Aniso4xSampler = ResourceFactory.CreateSampler(new SamplerDescription(SamplerFilter.Anisotropic, 4));
    }

    public static GraphicsDevice Create(
        GameWindow window,
        GraphicsDeviceOptions options,
        GraphicsBackend? backend = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        var renderBackend = RenderBackendFactory.Create(
            window,
            options,
            backend ?? GraphicsBackendSelector.GetDefault());

        try
        {
            return new GraphicsDevice(renderBackend);
        }
        catch
        {
            renderBackend.Dispose();
            throw;
        }
    }

    public void SubmitCommands(CommandList commandList)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(commandList);
        commandList.ValidateForSubmission(this);
        Backend.Submit(commandList.Commands);
    }

    public void SwapBuffers()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Backend.Present();
    }

    public void ResizeMainWindow(uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (width == 0 || height == 0)
            return;

        Backend.ResizeSwapchain(width, height);
    }

    public void WaitForIdle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Backend.WaitForIdle();
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, T value) where T : unmanaged
    {
        var values = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        UpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(values));
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, T[] values) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(values);
        UpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(values.AsSpan()));
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, ref T source, uint sizeInBytes)
        where T : unmanaged
    {
        // A byref-backed span stays GC-tracked; a raw pointer from Unsafe.AsPointer would
        // dangle if the GC relocated the source (typically a heap array element) mid-copy.
        UpdateBuffer(buffer, offset, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref source), checked((int)sizeInBytes)));
    }

    public void UpdateTexture<T>(
        Texture texture,
        T[] data,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        UpdateTexture(texture, MemoryMarshal.AsBytes(data.AsSpan()), x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    public unsafe void UpdateTexture(
        Texture texture,
        IntPtr source,
        uint sizeInBytes,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer)
    {
        UpdateTexture(
            texture,
            new ReadOnlySpan<byte>(source.ToPointer(), checked((int)sizeInBytes)),
            x,
            y,
            z,
            width,
            height,
            depth,
            mipLevel,
            arrayLayer);
    }

    public MappedResourceView<T> Map<T>(Texture texture, MapMode mode) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateResource(texture, nameof(texture));

        if (!texture.Usage.HasFlag(TextureUsage.Staging))
            throw new ArgumentException("Only staging textures can be mapped.", nameof(texture));

        return new MappedResourceView<T>(Backend.Map(texture, mode));
    }

    public void Unmap(Texture texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateResource(texture, nameof(texture));
        Backend.Unmap(texture);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var exceptions = new List<Exception>();
        CaptureException(exceptions, Backend.WaitForIdle);
        CaptureException(exceptions, Aniso4xSampler.Dispose);
        CaptureException(exceptions, LinearSampler.Dispose);
        CaptureException(exceptions, PointSampler.Dispose);
        CaptureException(exceptions, SwapchainFramebuffer.Dispose);
        CaptureException(exceptions, Backend.Dispose);
        _disposed = true;

        if (exceptions.Count == 1)
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

        if (exceptions.Count > 1)
            throw new AggregateException("One or more errors occurred while disposing the graphics device.", exceptions);
    }

    internal void DestroyResource(GraphicsResource resource)
    {
        if (!_disposed && resource.NativeResource is not null)
            Backend.DestroyResource(resource);
    }

    internal void ValidateResource(GraphicsResource resource, string parameterName)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(resource, parameterName);

        if (!ReferenceEquals(resource.GraphicsDevice, this))
            throw new ArgumentException("The resource belongs to a different graphics device.", parameterName);

        ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
    }

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    internal void ValidateCommandResources(GraphicsCommand command)
    {
        switch (command)
        {
            case SetFramebufferCommand setFramebuffer:
                ValidateFramebuffer(setFramebuffer.Framebuffer);
                break;
            case UpdateBufferCommand updateBuffer:
                ValidateResource(updateBuffer.Buffer, nameof(updateBuffer.Buffer));
                break;
            case CopyTextureCommand copyTexture:
                ValidateResource(copyTexture.Source, nameof(copyTexture.Source));
                ValidateResource(copyTexture.Destination, nameof(copyTexture.Destination));
                break;
            case SetVertexBufferCommand setVertexBuffer:
                ValidateResource(setVertexBuffer.Buffer, nameof(setVertexBuffer.Buffer));
                break;
            case SetIndexBufferCommand setIndexBuffer:
                ValidateResource(setIndexBuffer.Buffer, nameof(setIndexBuffer.Buffer));
                break;
            case SetPipelineCommand setPipeline:
                ValidateResource(setPipeline.Pipeline, nameof(setPipeline.Pipeline));
                break;
            case SetGraphicsResourceSetCommand setResourceSet:
                ValidateResourceSet(setResourceSet.ResourceSet);
                break;
        }
    }

    private void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateResource(buffer, nameof(buffer));

        if ((ulong)offset + (uint)data.Length > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(data), "The update exceeds the destination buffer.");

        Backend.UpdateBuffer(buffer, offset, data);
    }

    private void UpdateTexture(
        Texture texture,
        ReadOnlySpan<byte> data,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateResource(texture, nameof(texture));

        if (width == 0 || height == 0 || depth == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture update dimensions must be greater than zero.");

        if (mipLevel >= texture.MipLevels || arrayLayer >= texture.ArrayLayers)
            throw new ArgumentOutOfRangeException(nameof(mipLevel), "The texture subresource is out of range.");

        var mipWidth = Math.Max(1u, texture.Width >> checked((int)mipLevel));
        var mipHeight = Math.Max(1u, texture.Height >> checked((int)mipLevel));
        var mipDepth = Math.Max(1u, texture.Depth >> checked((int)mipLevel));
        if ((ulong)x + width > mipWidth ||
            (ulong)y + height > mipHeight ||
            (ulong)z + depth > mipDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The texture update exceeds the destination subresource.");
        }

        if (data.IsEmpty)
            throw new ArgumentException("Texture update data must not be empty.", nameof(data));

        Backend.UpdateTexture(texture, data, x, y, z, width, height, depth, mipLevel, arrayLayer);
    }

    private void ValidateFramebuffer(Framebuffer framebuffer)
    {
        ValidateResource(framebuffer, nameof(framebuffer));

        if (framebuffer.IsSwapchain)
            return;

        foreach (var attachment in framebuffer.Description.ColorTargets ?? [])
            ValidateResource(attachment.Target, nameof(framebuffer));

        if (framebuffer.Description.DepthTarget is { } depthTarget)
            ValidateResource(depthTarget.Target, nameof(framebuffer));
    }

    private void ValidateResourceSet(ResourceSet resourceSet)
    {
        ValidateResource(resourceSet, nameof(resourceSet));

        foreach (var boundResource in resourceSet.Description.BoundResources ?? [])
        {
            if (boundResource is GraphicsResource resource)
            {
                ValidateResource(resource, nameof(resourceSet));
                if (resource is TextureView view)
                    ValidateResource(view.Target, nameof(resourceSet));
            }
        }
    }

    private static void CaptureException(List<Exception> exceptions, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
}

internal static class RenderBackendFactory
{
    public static IGraphicsBackend Create(
        GameWindow window,
        GraphicsDeviceOptions options,
        GraphicsBackend backend)
    {
        return backend switch
        {
            GraphicsBackend.DirectX12 when OperatingSystem.IsWindows() =>
                new DirectX12Backend(window, options),
            GraphicsBackend.Vulkan when OperatingSystem.IsWindows() || OperatingSystem.IsLinux() =>
                new VulkanGraphicsBackend(window, options),
            GraphicsBackend.Metal when OperatingSystem.IsMacOS() =>
                new MetalGraphicsBackend(window, options),
            GraphicsBackend.DirectX12 =>
                throw new PlatformNotSupportedException("DirectX 12 is supported only on Windows."),
            GraphicsBackend.Vulkan =>
                throw new PlatformNotSupportedException("Vulkan is supported on Windows and Linux."),
            GraphicsBackend.Metal =>
                throw new PlatformNotSupportedException("Metal is supported only on macOS."),
            _ => throw new ArgumentOutOfRangeException(nameof(backend)),
        };
    }
}
