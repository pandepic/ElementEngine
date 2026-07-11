using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpMetal.Foundation;
using SharpMetal.Metal;
using SharpMetal.QuartzCore;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MetalGraphicsBackend : IGraphicsBackend
{
    private MTLDevice _device;
    private MTLCommandQueue _commandQueue;
    private CAMetalLayer _layer;
    private MetalShaderCompiler _shaderCompiler;
    private readonly GraphicsDeviceOptions _options;
    private readonly HashSet<IMetalResource> _resources = [];
    private PendingMetalFrame _pendingFrame;
    private readonly List<PendingMetalFrame> _flushedFrames = [];
    private uint _swapchainWidth;
    private uint _swapchainHeight;
    private bool _disposed;

    public GraphicsBackend BackendType => GraphicsBackend.Metal;
    public bool IsUvOriginTopLeft => true;
    public OutputDescription SwapchainOutputDescription { get; } =
        new([PixelFormat.B8_G8_R8_A8_UNorm]);

    public MetalGraphicsBackend(GameWindow window, GraphicsDeviceOptions options)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("The Metal backend can only run on macOS.");
        ArgumentNullException.ThrowIfNull(window);
        if (window.NativeKind != NativeWindowKind.Cocoa || window.NativeDisplayHandle == 0)
            throw new ArgumentException("Metal requires an ElementEngine Cocoa window with a CAMetalLayer.", nameof(window));

        _options = options;
        _swapchainWidth = checked((uint)Math.Max(1, window.FramebufferWidth));
        _swapchainHeight = checked((uint)Math.Max(1, window.FramebufferHeight));

        _device = MTLDevice.CreateSystemDefaultDevice();
        if (_device.NativePtr == 0)
            throw new InvalidOperationException("Metal did not return a system graphics device.");

        try
        {
            _commandQueue = _device.NewCommandQueue();
            if (_commandQueue.NativePtr == 0)
                throw new InvalidOperationException("Failed to create the Metal command queue.");

            _layer = new CAMetalLayer(window.NativeDisplayHandle)
            {
                Device = _device,
                PixelFormat = MTLPixelFormat.BGRA8Unorm,
                FramebufferOnly = true,
                DisplaySyncEnabled = options.SyncToVerticalBlank,
                AllowsNextDrawableTimeout = true,
                MaximumDrawableCount = 3,
            };
            _shaderCompiler = new MetalShaderCompiler(options.Debug);
        }
        catch (Exception creationException)
        {
            var teardown = new TeardownCollector(creationException);
            _layer = default;

            var commandQueue = _commandQueue;
            _commandQueue = default;
            if (commandQueue.NativePtr != 0)
                teardown.Run(commandQueue.Dispose);

            var device = _device;
            _device = default;
            if (device.NativePtr != 0)
                teardown.Run(device.Dispose);

            teardown.ThrowIfFailed("Metal backend creation and cleanup both failed.");
            throw;
        }
    }

    public object CreateBuffer(BufferDescription description)
    {
        ThrowIfDisposed();

        var cpuVisible = (description.Usage & BufferUsage.Staging) != 0;
        var resourceOptions = cpuVisible
            ? MTLResourceOptions.ResourceStorageModeShared
            : MTLResourceOptions.ResourceStorageModePrivate;
        var buffer = _device.NewBuffer(description.SizeInBytes, resourceOptions);
        if (buffer.NativePtr == 0)
            throw new InvalidOperationException($"Failed to allocate a {description.SizeInBytes}-byte Metal buffer.");

        return Track(new MetalBufferResource(buffer, description, cpuVisible));
    }

    public object CreateTexture(TextureDescription description)
    {
        ThrowIfDisposed();
        ValidateTextureDescription(description);

        var staging = (description.Usage & TextureUsage.Staging) != 0;
        var storageMode = staging
            ? (_device.HasUnifiedMemory ? MTLStorageMode.Shared : MTLStorageMode.Managed)
            : MTLStorageMode.Private;
        var metalFormat = description.Format.ToMetal(_device.IsDepth24Stencil8PixelFormatSupported);

        using var descriptor = new MTLTextureDescriptor
        {
            TextureType = description.ToMetal(),
            PixelFormat = metalFormat,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            MipmapLevelCount = description.MipLevels,
            ArrayLength = description.ArrayLayers,
            SampleCount = 1,
            Usage = description.Usage.ToMetal(),
            StorageMode = storageMode,
            ResourceOptions = storageMode switch
            {
                MTLStorageMode.Shared => MTLResourceOptions.ResourceStorageModeShared,
                MTLStorageMode.Managed => MTLResourceOptions.ResourceStorageModeManaged,
                _ => MTLResourceOptions.ResourceStorageModePrivate,
            },
            AllowGPUOptimizedContents = !staging,
        };

        var texture = _device.NewTexture(descriptor);
        if (texture.NativePtr == 0)
            throw new NotSupportedException($"The Metal device could not create the requested {description.Format} texture.");

        return Track(new MetalTextureResource(texture, description, metalFormat, storageMode));
    }

    public object CreateTextureView(Texture texture)
    {
        ThrowIfDisposed();
        return Track(new MetalTextureViewResource(GetNative<MetalTextureResource>(texture)));
    }

    public object CreateSampler(SamplerDescription description)
    {
        ThrowIfDisposed();

        var linear = description.Filter is SamplerFilter.Linear or SamplerFilter.Anisotropic;
        using var descriptor = new MTLSamplerDescriptor
        {
            MinFilter = linear ? MTLSamplerMinMagFilter.Linear : MTLSamplerMinMagFilter.Nearest,
            MagFilter = linear ? MTLSamplerMinMagFilter.Linear : MTLSamplerMinMagFilter.Nearest,
            MipFilter = linear ? MTLSamplerMipFilter.Linear : MTLSamplerMipFilter.Nearest,
            MaxAnisotropy = description.Filter == SamplerFilter.Anisotropic
                ? Math.Max(1u, description.MaximumAnisotropy)
                : 1,
            SAddressMode = MTLSamplerAddressMode.Repeat,
            TAddressMode = MTLSamplerAddressMode.Repeat,
            RAddressMode = MTLSamplerAddressMode.Repeat,
            NormalizedCoordinates = true,
        };
        var sampler = _device.NewSamplerState(descriptor);
        if (sampler.NativePtr == 0)
            throw new InvalidOperationException("Failed to create a Metal sampler.");

        return Track(new MetalSamplerResource(sampler));
    }

    public object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(descriptions);

        var results = new object[descriptions.Length];
        var created = new List<MetalShaderResource>(descriptions.Length);

        try
        {
            for (var index = 0; index < descriptions.Length; index++)
            {
                var description = descriptions[index];
                var spirv = IsSpirv(description.ShaderBytes)
                    ? description.ShaderBytes.ToArray()
                    : _shaderCompiler.CompileGlsl(description);
                var shader = Track(new MetalShaderResource(description, spirv, options));
                created.Add(shader);
                results[index] = shader;
            }

            return results;
        }
        catch (Exception creationException)
        {
            var teardown = new TeardownCollector(creationException);
            foreach (var shader in created)
                teardown.Run(() => DestroyNativeResource(shader));
            teardown.ThrowIfFailed("Metal shader creation and cleanup both failed.");
            throw;
        }
    }

    public object CreateResourceLayout(ResourceLayoutDescription description)
    {
        ThrowIfDisposed();
        var elements = description.Elements ?? [];

        for (var index = 0; index < elements.Length; index++)
        {
            if (elements[index].Stages == ShaderStages.None)
                throw new ArgumentException($"Metal resource layout element {index} requires a shader stage.", nameof(description));
        }

        return Track(new MetalResourceLayoutResource(description));
    }

    public object CreateResourceSet(ResourceSetDescription description)
    {
        ThrowIfDisposed();
        var layout = GetNative<MetalResourceLayoutResource>(description.Layout);
        var elements = layout.Description.Elements ?? [];
        var resources = description.BoundResources ?? [];

        if (elements.Length != resources.Length)
            throw new ArgumentException("The Metal resource count does not match its layout.", nameof(description));

        for (var index = 0; index < elements.Length; index++)
            ValidateBoundResource(elements[index].Kind, resources[index], index);

        return Track(new MetalResourceSetResource(layout, resources.ToArray()));
    }

    public object CreateFramebuffer(FramebufferDescription description)
    {
        ThrowIfDisposed();
        var attachments = description.ColorTargets ?? [];
        if (attachments.Length == 0)
            throw new ArgumentException("A Metal framebuffer requires a color attachment.", nameof(description));
        if (attachments.Length > 8)
            throw new NotSupportedException("Metal supports at most eight color attachments.");

        var colors = new MetalTextureResource[attachments.Length];
        var slices = new uint[attachments.Length];
        uint width = 0;
        uint height = 0;

        for (var index = 0; index < attachments.Length; index++)
        {
            var attachment = attachments[index];
            var target = GetNative<MetalTextureResource>(attachment.Target);
            ValidateFramebufferAttachment(target, attachment.ArrayLayer, false, index, ref width, ref height);
            colors[index] = target;
            slices[index] = attachment.ArrayLayer;
        }

        MetalTextureResource depth = null;
        uint depthSlice = 0;
        if (description.DepthTarget is { } depthAttachment)
        {
            depth = GetNative<MetalTextureResource>(depthAttachment.Target);
            depthSlice = depthAttachment.ArrayLayer;
            ValidateFramebufferAttachment(depth, depthSlice, true, 0, ref width, ref height);
        }

        var output = new OutputDescription(
            attachments.Select(attachment => attachment.Target.Format).ToArray(),
            description.DepthTarget?.Target.Format);
        return Track(new MetalFramebufferResource(colors, slices, depth, depthSlice, output, width, height));
    }

    public void DestroyResource(GraphicsResource resource)
    {
        if (_disposed || resource.NativeResource is not IMetalResource native)
            return;

        DestroyNativeResource(native);
    }

    public void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        var native = GetNative<MetalBufferResource>(buffer);
        if ((ulong)offset + (ulong)data.Length > native.Description.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(data), "The update exceeds the Metal buffer.");
        if (data.IsEmpty)
            return;

        CompletePendingFrameBeforeImmediateUpdate(native.IsCpuVisible);

        if (native.IsCpuVisible)
        {
            // Cpu-visible buffers use shared storage, which needs no didModifyRange:
            // (that call is managed-storage-only and asserts under Metal API validation).
            data.CopyTo(new Span<byte>((void*)(native.Buffer.Contents + checked((int)offset)), data.Length));
            return;
        }

        using var pool = new NSAutoreleasePool();
        var commandBuffer = CreateCommandBuffer();
        var upload = CreateUploadBuffer(data);
        var blit = commandBuffer.BlitCommandEncoder();
        EnsureNative(blit.NativePtr, "create a Metal buffer upload encoder");
        blit.CopyFromBuffer(upload, 0, native.Buffer, offset, (ulong)data.Length);
        blit.EndEncoding();
        commandBuffer.Commit();
        upload.Dispose();
        CheckCompletedCommandBuffer(commandBuffer, wait: _options.Debug);
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
        ThrowIfDisposed();
        var native = GetNative<MetalTextureResource>(texture);
        if (native.Description.Format.IsDepth())
            throw new NotSupportedException("Metal CPU texture updates do not support packed depth-stencil formats.");

        ValidateTextureRegion(native.Description, x, y, z, width, height, depth, mipLevel, arrayLayer);
        var layout = GetTextureDataLayout(native.Description.Format, width, height, depth);
        if ((ulong)data.Length < layout.RequiredSize)
            throw new ArgumentException($"The texture update requires {layout.RequiredSize} bytes but received {data.Length}.", nameof(data));

        CompletePendingFrameBeforeImmediateUpdate(native.StorageMode != MTLStorageMode.Private);

        var region = new MTLRegion
        {
            origin = new MTLOrigin { x = x, y = y, z = z },
            size = new MTLSize { width = width, height = height, depth = depth },
        };

        if (native.StorageMode != MTLStorageMode.Private)
        {
            fixed (byte* source = data)
            {
                native.Texture.ReplaceRegion(
                    region,
                    mipLevel,
                    arrayLayer,
                    (nint)source,
                    layout.RowPitch,
                    layout.DepthPitch);
            }
            return;
        }

        var alignment = Math.Max(1UL, _device.MinimumLinearTextureAlignmentForPixelFormat(native.MetalFormat));
        var alignedRowPitch = MetalConversions.AlignUp(layout.RowPitch, alignment);
        var alignedDepthPitch = checked(alignedRowPitch * layout.RowCount);
        var uploadSize = checked(alignedDepthPitch * depth);

        using var pool = new NSAutoreleasePool();
        var upload = _device.NewBuffer(uploadSize, MTLResourceOptions.ResourceStorageModeShared);
        EnsureNative(upload.NativePtr, "allocate a Metal texture upload buffer");
        CopyTextureDataToUpload(data, upload.Contents, layout, alignedRowPitch, alignedDepthPitch, depth);

        var commandBuffer = CreateCommandBuffer();
        var blit = commandBuffer.BlitCommandEncoder();
        EnsureNative(blit.NativePtr, "create a Metal texture upload encoder");
        blit.CopyFromBuffer(
            upload,
            0,
            alignedRowPitch,
            alignedDepthPitch,
            region.size,
            native.Texture,
            arrayLayer,
            mipLevel,
            region.origin);
        blit.EndEncoding();
        commandBuffer.Commit();
        upload.Dispose();
        CheckCompletedCommandBuffer(commandBuffer, wait: _options.Debug);
    }

    public MappedResource Map(Texture texture, MapMode mode)
    {
        ThrowIfDisposed();
        var native = GetNative<MetalTextureResource>(texture);

        if ((native.Description.Usage & TextureUsage.Staging) == 0 || native.StorageMode == MTLStorageMode.Private)
            throw new InvalidOperationException("Only Metal staging textures can be mapped.");
        if (native.Description.ArrayLayers != 1 || native.Description.MipLevels != 1)
            throw new NotSupportedException("Mapping Metal texture arrays or mip chains is not supported by the current graphics contract.");
        if (native.Mapping is not null)
            throw new InvalidOperationException("The Metal texture is already mapped.");

        WaitForIdle();
        if (native.StorageMode == MTLStorageMode.Managed)
            SynchronizeTextureForCpu(native);

        var layout = GetTextureDataLayout(
            native.Description.Format,
            native.Description.Width,
            native.Description.Height,
            native.Description.Depth);
        if (layout.RequiredSize > int.MaxValue)
            throw new NotSupportedException("The Metal staging texture is too large to map.");

        var data = Marshal.AllocHGlobal(checked((int)layout.RequiredSize));
        var mapping = new MetalTextureMapping(
            data,
            mode,
            checked((uint)layout.RequiredSize),
            checked((uint)layout.RowPitch),
            checked((uint)layout.DepthPitch));

        try
        {
            // Populate the block for every mode (Unmap uploads the full region, so a
            // Write mapping must start from the current contents, not raw heap memory).
            var region = new MTLRegion
            {
                origin = default,
                size = new MTLSize
                {
                    width = native.Description.Width,
                    height = native.Description.Height,
                    depth = native.Description.Depth,
                },
            };
            native.Texture.GetBytes(data, layout.RowPitch, layout.DepthPitch, region, 0, 0);

            native.Mapping = mapping;
            return new MappedResource(data, mapping.SizeInBytes, mapping.RowPitch, mapping.DepthPitch);
        }
        catch
        {
            mapping.Dispose();
            throw;
        }
    }

    public void Unmap(Texture texture)
    {
        ThrowIfDisposed();
        var native = GetNative<MetalTextureResource>(texture);
        var mapping = native.Mapping ?? throw new InvalidOperationException("The Metal texture is not mapped.");

        try
        {
            if (mapping.Mode is MapMode.Write or MapMode.ReadWrite)
            {
                var region = new MTLRegion
                {
                    origin = default,
                    size = new MTLSize
                    {
                        width = native.Description.Width,
                        height = native.Description.Height,
                        depth = native.Description.Depth,
                    },
                };
                native.Texture.ReplaceRegion(
                    region,
                    0,
                    0,
                    mapping.Data,
                    mapping.RowPitch,
                    mapping.DepthPitch);
            }
        }
        finally
        {
            native.Mapping = null;
            mapping.Dispose();
        }
    }

    public void ResizeSwapchain(uint width, uint height)
    {
        ThrowIfDisposed();
        if (width == 0 || height == 0)
            return;

        if (_pendingFrame is not null)
            Present();

        _swapchainWidth = width;
        _swapchainHeight = height;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var teardown = new TeardownCollector();

        teardown.Run(PresentPendingFrame);
        if (_commandQueue.NativePtr != 0)
            teardown.Run(WaitForIdleCore);

        var resources = _resources.Reverse().ToArray();
        _resources.Clear();
        foreach (var resource in resources)
            teardown.Run(resource.Dispose);

        var shaderCompiler = _shaderCompiler;
        _shaderCompiler = null;
        if (shaderCompiler is not null)
            teardown.Run(shaderCompiler.Dispose);

        _layer = default;

        var commandQueue = _commandQueue;
        _commandQueue = default;
        if (commandQueue.NativePtr != 0)
            teardown.Run(commandQueue.Dispose);

        var device = _device;
        _device = default;
        if (device.NativePtr != 0)
            teardown.Run(device.Dispose);

        teardown.ThrowIfFailed("One or more errors occurred while disposing the Metal backend.");
    }

    private void CompletePendingFrameBeforeImmediateUpdate(bool usesCpuVisibleResource)
    {
        if (_pendingFrame is not null)
            FlushPendingFrameWithoutPresent();

        // Cpu-visible writes race any in-flight GPU work (including a frame already
        // presented this window), so drain the queue before touching shared memory.
        if (usesCpuVisibleResource || _options.Debug)
            WaitForIdleCore();
    }

    private T Track<T>(T resource) where T : class, IMetalResource
    {
        _resources.Add(resource);
        return resource;
    }

    private void DestroyNativeResource(IMetalResource resource)
    {
        if (_resources.Remove(resource))
            resource.Dispose();
    }

    private T GetNative<T>(GraphicsResource resource) where T : class, IMetalResource
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (resource.NativeResource is not T native || !_resources.Contains(native))
        {
            throw new ObjectDisposedException(
                resource.GetType().Name,
                "The graphics resource is disposed or belongs to another backend.");
        }

        return native;
    }

    private void ValidateBoundResource(ResourceKind kind, object resource, int index)
    {
        switch (kind)
        {
            case ResourceKind.UniformBuffer when resource is DeviceBuffer buffer:
                if ((buffer.Usage & BufferUsage.UniformBuffer) == 0)
                    throw new ArgumentException($"Metal binding {index} requires a uniform buffer.");
                GetNative<MetalBufferResource>(buffer);
                break;
            case ResourceKind.TextureReadOnly when resource is Texture texture:
                ValidateSampledTexture(GetNative<MetalTextureResource>(texture), index);
                break;
            case ResourceKind.TextureReadOnly when resource is TextureView view:
                ValidateSampledTexture(GetNative<MetalTextureViewResource>(view).Target, index);
                break;
            case ResourceKind.Sampler when resource is Sampler sampler:
                GetNative<MetalSamplerResource>(sampler);
                break;
            default:
                throw new ArgumentException($"Metal binding {index} is not compatible with {kind}.");
        }
    }

    private static void ValidateSampledTexture(MetalTextureResource texture, int index)
    {
        if ((texture.Description.Usage & TextureUsage.Sampled) == 0)
            throw new ArgumentException($"Metal binding {index} requires a texture with sampled usage.");
    }

    private static void ValidateFramebufferAttachment(
        MetalTextureResource texture,
        uint slice,
        bool depth,
        int index,
        ref uint width,
        ref uint height)
    {
        var requiredUsage = depth ? TextureUsage.DepthStencil : TextureUsage.RenderTarget;
        if ((texture.Description.Usage & requiredUsage) == 0)
            throw new ArgumentException($"Metal framebuffer attachment {index} is missing {requiredUsage} usage.");
        if (texture.Description.Format.IsDepth() != depth)
            throw new ArgumentException($"Metal framebuffer attachment {index} has an incompatible pixel format.");
        var attachmentSlices = texture.Description.Type == TextureType.Texture3D
            ? texture.Description.Depth
            : texture.Description.ArrayLayers;
        if (slice >= attachmentSlices)
            throw new ArgumentOutOfRangeException(nameof(slice), $"Metal framebuffer attachment {index} has no array layer {slice}.");

        if (width == 0)
        {
            width = texture.Description.Width;
            height = texture.Description.Height;
        }
        else if (width != texture.Description.Width || height != texture.Description.Height)
        {
            throw new ArgumentException("All Metal framebuffer attachments must have identical dimensions.");
        }
    }

    private static void ValidateTextureDescription(TextureDescription description)
    {
        if (description.MipLevels == 0 || description.ArrayLayers == 0)
            throw new ArgumentOutOfRangeException(nameof(description), "Metal textures require at least one mip level and array layer.");
        if (description.Type == TextureType.Texture1D && (description.Height != 1 || description.Depth != 1))
            throw new ArgumentException("Metal 1D textures require height and depth of one.", nameof(description));
        if (description.Type == TextureType.Texture2D && description.Depth != 1)
            throw new ArgumentException("Metal 2D textures require depth of one.", nameof(description));
        if (description.Type == TextureType.Texture3D && description.ArrayLayers != 1)
            throw new ArgumentException("Metal 3D textures cannot have array layers.", nameof(description));
        if ((description.Usage & TextureUsage.DepthStencil) != 0 && !description.Format.IsDepth())
            throw new ArgumentException("A Metal depth texture requires a depth-stencil format.", nameof(description));
        if ((description.Usage & TextureUsage.DepthStencil) == 0 && description.Format.IsDepth())
            throw new ArgumentException("A Metal depth-stencil format requires depth-stencil usage.", nameof(description));
        if ((description.Usage & TextureUsage.Staging) != 0 && description.Format.IsDepth())
            throw new NotSupportedException("Metal staging textures do not support packed depth-stencil formats.");

        var maximumMipLevels = 1u + (uint)Math.Floor(Math.Log2(Math.Max(description.Width, Math.Max(description.Height, description.Depth))));
        if (description.MipLevels > maximumMipLevels)
            throw new ArgumentOutOfRangeException(nameof(description), "The Metal texture has more mip levels than its dimensions allow.");
    }

    private static void ValidateTextureRegion(
        TextureDescription description,
        uint x,
        uint y,
        uint z,
        uint width,
        uint height,
        uint depth,
        uint mipLevel,
        uint arrayLayer)
    {
        if (mipLevel >= description.MipLevels || arrayLayer >= description.ArrayLayers)
            throw new ArgumentOutOfRangeException(nameof(mipLevel), "The Metal texture subresource is out of range.");
        if (width == 0 || height == 0 || depth == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Metal texture update dimensions must be nonzero.");

        var mipWidth = Math.Max(1u, description.Width >> checked((int)mipLevel));
        var mipHeight = Math.Max(1u, description.Height >> checked((int)mipLevel));
        var mipDepth = Math.Max(1u, description.Depth >> checked((int)mipLevel));
        if ((ulong)x + width > mipWidth || (ulong)y + height > mipHeight || (ulong)z + depth > mipDepth)
            throw new ArgumentOutOfRangeException(nameof(width), "The Metal texture update exceeds its mip dimensions.");

        if (MetalConversions.IsCompressed(description.Format) &&
            ((x & 3) != 0 || (y & 3) != 0 ||
             ((width & 3) != 0 && x + width != mipWidth) ||
             ((height & 3) != 0 && y + height != mipHeight)))
        {
            throw new ArgumentException(
                "Compressed Metal texture updates must align to four-texel blocks except at mip edges.");
        }
    }

    private static MetalTextureDataLayout GetTextureDataLayout(PixelFormat format, uint width, uint height, uint depth)
    {
        var compressed = MetalConversions.IsCompressed(format);
        var rowCount = compressed ? Math.Max(1u, (height + 3) / 4) : height;
        var columnCount = compressed ? Math.Max(1u, (width + 3) / 4) : width;
        var rowPitch = checked((ulong)columnCount * format.GetBytesPerPixel());
        var depthPitch = checked(rowPitch * rowCount);
        return new MetalTextureDataLayout(rowPitch, rowCount, depthPitch, checked(depthPitch * depth));
    }

    private static void CopyTextureDataToUpload(
        ReadOnlySpan<byte> source,
        nint destination,
        MetalTextureDataLayout layout,
        ulong destinationRowPitch,
        ulong destinationDepthPitch,
        uint depth)
    {
        var sourceOffset = 0UL;

        for (var depthIndex = 0u; depthIndex < depth; depthIndex++)
        {
            for (var row = 0u; row < layout.RowCount; row++)
            {
                var destinationOffset = checked(depthIndex * destinationDepthPitch + row * destinationRowPitch);
                var rowSource = source.Slice(checked((int)sourceOffset), checked((int)layout.RowPitch));
                rowSource.CopyTo(new Span<byte>((void*)(destination + checked((nint)destinationOffset)), rowSource.Length));
                sourceOffset += layout.RowPitch;
            }
        }
    }

    private MTLBuffer CreateUploadBuffer(ReadOnlySpan<byte> data)
    {
        var upload = _device.NewBuffer((ulong)data.Length, MTLResourceOptions.ResourceStorageModeShared);
        EnsureNative(upload.NativePtr, "allocate a Metal upload buffer");
        data.CopyTo(new Span<byte>((void*)upload.Contents, data.Length));
        return upload;
    }

    private MTLCommandBuffer CreateCommandBuffer()
    {
        var commandBuffer = _commandQueue.CommandBuffer();
        EnsureNative(commandBuffer.NativePtr, "create a Metal command buffer");
        return commandBuffer;
    }

    private void SynchronizeTextureForCpu(MetalTextureResource texture)
    {
        using var pool = new NSAutoreleasePool();
        var commandBuffer = CreateCommandBuffer();
        var blit = commandBuffer.BlitCommandEncoder();
        EnsureNative(blit.NativePtr, "create a Metal texture synchronization encoder");

        for (var mipLevel = 0u; mipLevel < texture.Description.MipLevels; mipLevel++)
        {
            for (var arrayLayer = 0u; arrayLayer < texture.Description.ArrayLayers; arrayLayer++)
                blit.SynchronizeTexture(texture.Texture, arrayLayer, mipLevel);
        }

        blit.EndEncoding();
        commandBuffer.Commit();
        CheckCompletedCommandBuffer(commandBuffer, true);
    }

    private static bool IsSpirv(byte[] bytes) =>
        bytes is { Length: >= 4 } &&
        bytes[0] == 0x03 && bytes[1] == 0x02 && bytes[2] == 0x23 && bytes[3] == 0x07;

    private static void EnsureNative(nint nativePointer, string operation)
    {
        if (nativePointer == 0)
            throw new InvalidOperationException($"Failed to {operation}.");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

[SupportedOSPlatform("macos")]
internal readonly record struct MetalTextureDataLayout(
    ulong RowPitch,
    uint RowCount,
    ulong DepthPitch,
    ulong RequiredSize);
