using System;
using System.Collections.Generic;

namespace ElementEngine;

internal interface IGraphicsBackend : IDisposable
{
    GraphicsBackend BackendType { get; }
    bool IsUvOriginTopLeft { get; }
    OutputDescription SwapchainOutputDescription { get; }

    object CreateBuffer(BufferDescription description);
    object CreateTexture(TextureDescription description);
    object CreateTextureView(Texture texture);
    object CreateSampler(SamplerDescription description);
    object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options);
    object CreateResourceLayout(ResourceLayoutDescription description);
    object CreateResourceSet(ResourceSetDescription description);
    object CreateGraphicsPipeline(GraphicsPipelineDescription description);
    object CreateFramebuffer(FramebufferDescription description);
    void DestroyResource(GraphicsResource resource);

    void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data);
    void UpdateTexture(
        Texture texture,
        ReadOnlySpan<byte> data,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer);
    MappedResource Map(Texture texture, MapMode mode);
    void Unmap(Texture texture);

    void Submit(IReadOnlyList<GraphicsCommand> commands);
    void Present();
    void ResizeSwapchain(uint width, uint height);
    void WaitForIdle();
}

internal static class GraphicsBackendSelector
{
    public static GraphicsBackend GetDefault()
    {
        if (OperatingSystem.IsWindows())
            return GraphicsBackend.DirectX12;

        if (OperatingSystem.IsLinux())
            return GraphicsBackend.Vulkan;

        if (OperatingSystem.IsMacOS())
            return GraphicsBackend.Metal;

        throw new PlatformNotSupportedException("ElementEngine supports Windows, Linux, and macOS desktop platforms.");
    }
}
