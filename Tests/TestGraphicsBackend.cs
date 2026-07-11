using System.Runtime.InteropServices;
using ElementEngine;

namespace ElementEngine.Tests;

internal sealed class TestGraphicsBackend : IGraphicsBackend
{
    private readonly Dictionary<Texture, GCHandle> _textureMappings = [];

    public GraphicsBackend BackendType => GraphicsBackend.Metal;
    public bool IsUvOriginTopLeft => true;
    public OutputDescription SwapchainOutputDescription { get; set; } = OutputDescription.Default;
    public List<GraphicsResource> DestroyedResources { get; } = [];
    public IReadOnlyList<GraphicsCommand> SubmittedCommands { get; private set; } = [];
    public TextureUpdate? LastTextureUpdate { get; private set; }
    public List<(uint Width, uint Height)> ResizeCalls { get; } = [];
    public int MapCalls { get; private set; }
    public int UnmapCalls { get; private set; }
    public bool DisposeCalled { get; private set; }
    public Func<ShaderDescription[], object[]>? CreateShadersOverride { get; set; }
    public byte[]? MappedTextureData { get; set; }
    public uint? MappedTextureRowPitch { get; set; }
    public uint? MappedTextureDepthPitch { get; set; }

    public object CreateBuffer(BufferDescription description) => new();
    public object CreateTexture(TextureDescription description) => new();
    public object CreateTextureView(Texture texture) => new();
    public object CreateSampler(SamplerDescription description) => new();

    public object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options)
        => CreateShadersOverride?.Invoke(descriptions) ?? descriptions.Select(_ => new object()).ToArray();

    public object CreateResourceLayout(ResourceLayoutDescription description) => new();
    public object CreateResourceSet(ResourceSetDescription description) => new();
    public object CreateGraphicsPipeline(GraphicsPipelineDescription description) => new();
    public object CreateFramebuffer(FramebufferDescription description) => new();

    public void DestroyResource(GraphicsResource resource) => DestroyedResources.Add(resource);

    public void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
    }

    public void UpdateTexture(
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
        LastTextureUpdate = new TextureUpdate(
            texture,
            data.ToArray(),
            x,
            y,
            z,
            width,
            height,
            depth,
            mipLevel,
            arrayLayer);
    }

    public MappedResource Map(Texture texture, MapMode mode)
    {
        MapCalls++;
        var data = MappedTextureData?.ToArray() ??
            new byte[checked((int)(texture.Width * texture.Height * 4))];
        var rowPitch = MappedTextureRowPitch ?? texture.Width * 4;
        var depthPitch = MappedTextureDepthPitch ?? (uint)data.Length;
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        _textureMappings.Add(texture, handle);
        return new MappedResource(
            handle.AddrOfPinnedObject(),
            (uint)data.Length,
            rowPitch,
            depthPitch);
    }

    public void Unmap(Texture texture)
    {
        UnmapCalls++;
        if (!_textureMappings.Remove(texture, out var mapping))
            return;

        mapping.Free();
    }

    public void Submit(IReadOnlyList<GraphicsCommand> commands)
    {
        ValidateCommandStream(commands);
        SubmittedCommands = commands.ToArray();
    }

    // Mirrors the draw-state validation every real backend performs at submit time, so
    // command streams that pass tests here do not throw on Vulkan, DirectX 12, or Metal.
    private static void ValidateCommandStream(IReadOnlyList<GraphicsCommand> commands)
    {
        var hasFramebuffer = false;
        var viewportSet = false;
        var hasIndexBuffer = false;
        Pipeline? pipeline = null;
        var boundResourceSets = new HashSet<uint>();

        foreach (var command in commands)
        {
            switch (command)
            {
                case SetFramebufferCommand:
                    hasFramebuffer = true;
                    break;
                case SetViewportCommand:
                    viewportSet = true;
                    break;
                case ClearColorTargetCommand or ClearDepthStencilCommand when !hasFramebuffer:
                    throw new InvalidOperationException("Set a framebuffer before clearing attachments.");
                case SetPipelineCommand setPipeline:
                    pipeline = setPipeline.Pipeline;
                    boundResourceSets.Clear();
                    break;
                case SetGraphicsResourceSetCommand setResourceSet:
                    if (pipeline is null)
                        throw new InvalidOperationException("Set a pipeline before binding resource sets.");
                    boundResourceSets.Add(setResourceSet.Slot);
                    break;
                case SetIndexBufferCommand:
                    hasIndexBuffer = true;
                    break;
                case DrawCommand:
                    RequireDrawState(hasFramebuffer, viewportSet, pipeline, boundResourceSets);
                    break;
                case DrawIndexedCommand:
                    RequireDrawState(hasFramebuffer, viewportSet, pipeline, boundResourceSets);
                    if (!hasIndexBuffer)
                        throw new InvalidOperationException("Set an index buffer before indexed drawing.");
                    break;
            }
        }
    }

    private static void RequireDrawState(
        bool hasFramebuffer,
        bool viewportSet,
        Pipeline? pipeline,
        HashSet<uint> boundResourceSets)
    {
        if (!hasFramebuffer)
            throw new InvalidOperationException("Set a framebuffer before drawing.");
        if (pipeline is null)
            throw new InvalidOperationException("Set a pipeline before drawing.");
        if (!viewportSet)
            throw new InvalidOperationException("Set viewport 0 before drawing.");

        var layoutCount = pipeline.Description.ResourceLayouts?.Length ?? 0;
        for (var slot = 0u; slot < layoutCount; slot++)
        {
            if (!boundResourceSets.Contains(slot))
                throw new InvalidOperationException($"Set resource set slot {slot} before drawing.");
        }
    }
    public void Present() { }
    public void ResizeSwapchain(uint width, uint height) => ResizeCalls.Add((width, height));
    public void WaitForIdle() { }

    public void Dispose()
    {
        DisposeCalled = true;
        foreach (var mapping in _textureMappings.Values)
        {
            if (mapping.IsAllocated)
                mapping.Free();
        }

        _textureMappings.Clear();
    }
}

internal sealed record TextureUpdate(
    Texture Texture,
    byte[] Data,
    uint X,
    uint Y,
    uint Z,
    uint Width,
    uint Height,
    uint Depth,
    uint MipLevel,
    uint ArrayLayer);
