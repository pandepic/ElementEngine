using System;
using System.Runtime.Versioning;
using SharpMetal.Metal;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal interface IMetalResource : IDisposable
{
}

[SupportedOSPlatform("macos")]
internal sealed class MetalBufferResource : IMetalResource
{
    private bool _disposed;

    public MTLBuffer Buffer { get; }
    public BufferDescription Description { get; }
    public bool IsCpuVisible { get; }

    public MetalBufferResource(MTLBuffer buffer, BufferDescription description, bool isCpuVisible)
    {
        Buffer = buffer;
        Description = description;
        IsCpuVisible = isCpuVisible;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Buffer.Dispose();
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalTextureResource : IMetalResource
{
    private bool _disposed;

    public MTLTexture Texture { get; }
    public TextureDescription Description { get; }
    public MTLPixelFormat MetalFormat { get; }
    public MTLStorageMode StorageMode { get; }
    public MetalTextureMapping Mapping { get; set; }

    public MetalTextureResource(
        MTLTexture texture,
        TextureDescription description,
        MTLPixelFormat metalFormat,
        MTLStorageMode storageMode)
    {
        Texture = texture;
        Description = description;
        MetalFormat = metalFormat;
        StorageMode = storageMode;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var teardown = new TeardownCollector();

        var mapping = Mapping;
        Mapping = null;
        if (mapping is not null)
            teardown.Run(mapping.Dispose);
        teardown.Run(Texture.Dispose);

        teardown.ThrowIfFailed("One or more errors occurred while disposing a Metal texture.");
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalTextureMapping : IDisposable
{
    private bool _disposed;

    public nint Data { get; }
    public MapMode Mode { get; }
    public uint SizeInBytes { get; }
    public uint RowPitch { get; }
    public uint DepthPitch { get; }

    public MetalTextureMapping(nint data, MapMode mode, uint sizeInBytes, uint rowPitch, uint depthPitch)
    {
        Data = data;
        Mode = mode;
        SizeInBytes = sizeInBytes;
        RowPitch = rowPitch;
        DepthPitch = depthPitch;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        System.Runtime.InteropServices.Marshal.FreeHGlobal(Data);
        _disposed = true;
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalTextureViewResource : IMetalResource
{
    public MetalTextureResource Target { get; }

    public MetalTextureViewResource(MetalTextureResource target)
    {
        Target = target;
    }

    public void Dispose()
    {
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalSamplerResource : IMetalResource
{
    private bool _disposed;

    public MTLSamplerState Sampler { get; }

    public MetalSamplerResource(MTLSamplerState sampler)
    {
        Sampler = sampler;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Sampler.Dispose();
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalShaderResource : IMetalResource
{
    public ShaderDescription Description { get; }
    public byte[] Spirv { get; }
    public CrossCompileOptions Options { get; }

    public MetalShaderResource(ShaderDescription description, byte[] spirv, CrossCompileOptions options)
    {
        Description = description;
        Spirv = spirv;
        Options = options;
    }

    public void Dispose()
    {
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalResourceLayoutResource : IMetalResource
{
    public ResourceLayoutDescription Description { get; }

    public MetalResourceLayoutResource(ResourceLayoutDescription description)
    {
        Description = description;
    }

    public void Dispose()
    {
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalResourceSetResource : IMetalResource
{
    public MetalResourceLayoutResource Layout { get; }
    public object[] BoundResources { get; }

    public MetalResourceSetResource(MetalResourceLayoutResource layout, object[] boundResources)
    {
        Layout = layout;
        BoundResources = boundResources;
    }

    public void Dispose()
    {
    }
}

[SupportedOSPlatform("macos")]
internal readonly record struct MetalResourceBinding(
    uint Slot,
    uint Element,
    ResourceKind Kind,
    ShaderStages Stages,
    uint NativeIndex);

[SupportedOSPlatform("macos")]
internal sealed class MetalPipelineResource : IMetalResource
{
    private bool _disposed;

    public MTLRenderPipelineState PipelineState { get; }
    public MTLDepthStencilState DepthStencilState { get; }
    public MTLLibrary[] Libraries { get; }
    public MTLFunction[] Functions { get; }
    public MetalResourceLayoutResource[] ResourceLayouts { get; }
    public MetalResourceBinding[] ResourceBindings { get; }
    public OutputDescription Outputs { get; }
    public MTLPrimitiveType PrimitiveType { get; }
    public MTLCullMode CullMode { get; }
    public MTLDepthClipMode DepthClipMode { get; }
    public bool ScissorTestEnabled { get; }
    public uint VertexLayoutCount { get; }

    public MetalPipelineResource(
        MTLRenderPipelineState pipelineState,
        MTLDepthStencilState depthStencilState,
        MTLLibrary[] libraries,
        MTLFunction[] functions,
        MetalResourceLayoutResource[] resourceLayouts,
        MetalResourceBinding[] resourceBindings,
        OutputDescription outputs,
        MTLPrimitiveType primitiveType,
        MTLCullMode cullMode,
        MTLDepthClipMode depthClipMode,
        bool scissorTestEnabled,
        uint vertexLayoutCount)
    {
        PipelineState = pipelineState;
        DepthStencilState = depthStencilState;
        Libraries = libraries;
        Functions = functions;
        ResourceLayouts = resourceLayouts;
        ResourceBindings = resourceBindings;
        Outputs = outputs;
        PrimitiveType = primitiveType;
        CullMode = cullMode;
        DepthClipMode = depthClipMode;
        ScissorTestEnabled = scissorTestEnabled;
        VertexLayoutCount = vertexLayoutCount;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var teardown = new TeardownCollector();

        teardown.Run(DepthStencilState.Dispose);
        teardown.Run(PipelineState.Dispose);

        for (var index = Functions.Length - 1; index >= 0; index--)
            teardown.Run(Functions[index].Dispose);
        for (var index = Libraries.Length - 1; index >= 0; index--)
            teardown.Run(Libraries[index].Dispose);

        teardown.ThrowIfFailed("One or more errors occurred while disposing a Metal pipeline.");
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalFramebufferResource : IMetalResource
{
    public MetalTextureResource[] ColorTargets { get; }
    public uint[] ColorSlices { get; }
    public MetalTextureResource DepthTarget { get; }
    public uint DepthSlice { get; }
    public OutputDescription OutputDescription { get; }
    public uint Width { get; }
    public uint Height { get; }

    public MetalFramebufferResource(
        MetalTextureResource[] colorTargets,
        uint[] colorSlices,
        MetalTextureResource depthTarget,
        uint depthSlice,
        OutputDescription outputDescription,
        uint width,
        uint height)
    {
        ColorTargets = colorTargets;
        ColorSlices = colorSlices;
        DepthTarget = depthTarget;
        DepthSlice = depthSlice;
        OutputDescription = outputDescription;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
    }
}
