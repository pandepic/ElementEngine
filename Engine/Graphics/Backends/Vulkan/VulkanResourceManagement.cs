using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace ElementEngine;

internal sealed unsafe partial class VulkanGraphicsBackend
{
    public object CreateBuffer(BufferDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var usage = BufferUsageFlags.TransferSrcBit | BufferUsageFlags.TransferDstBit;
        if ((description.Usage & BufferUsage.VertexBuffer) != 0)
            usage |= BufferUsageFlags.VertexBufferBit;
        if ((description.Usage & BufferUsage.IndexBuffer) != 0)
            usage |= BufferUsageFlags.IndexBufferBit;
        if ((description.Usage & BufferUsage.UniformBuffer) != 0)
            usage |= BufferUsageFlags.UniformBufferBit;

        var memoryProperties = (description.Usage & BufferUsage.Staging) != 0
            ? MemoryPropertyFlags.HostVisibleBit
            : MemoryPropertyFlags.DeviceLocalBit;
        var preferredProperties = memoryProperties == MemoryPropertyFlags.HostVisibleBit
            ? MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostCachedBit
            : MemoryPropertyFlags.None;
        var resource = CreateInternalBuffer(description.SizeInBytes, usage, memoryProperties, preferredProperties);
        TrackResource(resource);
        return resource;
    }

    public object CreateTexture(TextureDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateTextureDescription(description);

        var format = description.Format.ToVulkan();
        var isStaging = (description.Usage & TextureUsage.Staging) != 0;
        var tiling = isStaging ? ImageTiling.Linear : ImageTiling.Optimal;
        var usage = ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit;

        if ((description.Usage & TextureUsage.Sampled) != 0)
            usage |= ImageUsageFlags.SampledBit;
        if ((description.Usage & TextureUsage.RenderTarget) != 0)
            usage |= ImageUsageFlags.ColorAttachmentBit;
        if ((description.Usage & TextureUsage.DepthStencil) != 0)
            usage |= ImageUsageFlags.DepthStencilAttachmentBit;

        ValidateFormatSupport(format, tiling, usage);

        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            Flags = description.Type == TextureType.Texture3D &&
                    (description.Usage & (TextureUsage.RenderTarget | TextureUsage.DepthStencil)) != 0
                ? ImageCreateFlags.Create2DArrayCompatibleBit
                : ImageCreateFlags.None,
            ImageType = description.Type.ToVulkan(),
            Format = format,
            Extent = new Extent3D(description.Width, description.Height, description.Depth),
            MipLevels = description.MipLevels,
            ArrayLayers = description.ArrayLayers,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = tiling,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
        };

        Check(_vk.CreateImage(_device, &createInfo, null, out var image), "create a Vulkan image");
        _vk.GetImageMemoryRequirements(_device, image, out var requirements);
        var requiredMemory = isStaging ? MemoryPropertyFlags.HostVisibleBit : MemoryPropertyFlags.DeviceLocalBit;
        var preferredMemory = isStaging
            ? MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostCachedBit
            : MemoryPropertyFlags.None;
        var memoryType = FindMemoryType(requirements.MemoryTypeBits, requiredMemory, preferredMemory, out var actualProperties);
        var allocationInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryType,
        };

        try
        {
            Check(_vk.AllocateMemory(_device, &allocationInfo, null, out var memory), "allocate Vulkan image memory");

            try
            {
                Check(_vk.BindImageMemory(_device, image, memory, 0), "bind Vulkan image memory");
                var resource = new VulkanTextureResource
                {
                    Handle = image,
                    Memory = memory,
                    Description = description,
                    Format = format,
                    AspectMask = description.Format.AspectMask(),
                    MemoryProperties = actualProperties,
                };
                TrackResource(resource);
                return resource;
            }
            catch
            {
                _vk.FreeMemory(_device, memory, null);
                throw;
            }
        }
        catch
        {
            _vk.DestroyImage(_device, image, null);
            throw;
        }
    }

    public object CreateTextureView(Texture texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var nativeTexture = GetNative<VulkanTextureResource>(texture);
        var resource = new VulkanTextureViewResource
        {
            Handle = CreateImageView(
                nativeTexture.Handle,
                nativeTexture.Format,
                nativeTexture.AspectMask.SampledAspectMask(),
                nativeTexture.Description.ToViewType(),
                nativeTexture.Description.MipLevels,
                nativeTexture.Description.ArrayLayers),
            Texture = nativeTexture,
        };
        TrackResource(resource);
        return resource;
    }

    public object CreateSampler(SamplerDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var anisotropic = description.Filter == SamplerFilter.Anisotropic &&
                          _physicalDeviceFeatures.SamplerAnisotropy;

        var filter = description.Filter == SamplerFilter.Point ? Filter.Nearest : Filter.Linear;
        var createInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = filter,
            MinFilter = filter,
            MipmapMode = description.Filter == SamplerFilter.Point
                ? SamplerMipmapMode.Nearest
                : SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MipLodBias = 0,
            AnisotropyEnable = anisotropic,
            MaxAnisotropy = anisotropic
                ? Math.Clamp(description.MaximumAnisotropy, 1u, (uint)_physicalDeviceProperties.Limits.MaxSamplerAnisotropy)
                : 1,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MinLod = 0,
            MaxLod = Vk.LodClampNone,
            BorderColor = BorderColor.FloatTransparentBlack,
            UnnormalizedCoordinates = false,
        };
        Check(_vk.CreateSampler(_device, &createInfo, null, out var sampler), "create a Vulkan sampler");
        var resource = new VulkanSamplerResource { Handle = sampler };
        TrackResource(resource);
        return resource;
    }

    public object CreateFramebuffer(FramebufferDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (description.ColorTargets is null || description.ColorTargets.Length == 0)
            throw new ArgumentException("A Vulkan framebuffer requires at least one color target.", nameof(description));

        var firstTarget = description.ColorTargets[0].Target;
        var width = firstTarget.Width;
        var height = firstTarget.Height;
        var colorTargets = new VulkanTextureResource[description.ColorTargets.Length];
        var views = new ImageView[description.ColorTargets.Length + (description.DepthTarget.HasValue ? 1 : 0)];

        try
        {
            for (var index = 0; index < description.ColorTargets.Length; index++)
            {
                var attachment = description.ColorTargets[index];
                ValidateFramebufferAttachment(attachment, width, height, TextureUsage.RenderTarget);
                var texture = GetNative<VulkanTextureResource>(attachment.Target);
                colorTargets[index] = texture;
                views[index] = CreateAttachmentView(texture, attachment.ArrayLayer);
            }

            VulkanTextureResource depthTarget = null;
            if (description.DepthTarget is { } depthAttachment)
            {
                ValidateFramebufferAttachment(depthAttachment, width, height, TextureUsage.DepthStencil);
                depthTarget = GetNative<VulkanTextureResource>(depthAttachment.Target);
                views[^1] = CreateAttachmentView(depthTarget, depthAttachment.ArrayLayer);
            }

            var output = new OutputDescription(
                description.ColorTargets.Select(attachment => attachment.Target.Format).ToArray(),
                description.DepthTarget?.Target.Format);
            var renderPass = CreateRenderPass(output);

            try
            {
                fixed (ImageView* viewsPointer = views)
                {
                    var createInfo = new FramebufferCreateInfo
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = renderPass,
                        AttachmentCount = (uint)views.Length,
                        PAttachments = viewsPointer,
                        Width = width,
                        Height = height,
                        Layers = 1,
                    };
                    Check(_vk.CreateFramebuffer(_device, &createInfo, null, out var framebuffer), "create a Vulkan framebuffer");
                    var resource = new VulkanFramebufferResource
                    {
                        Handle = framebuffer,
                        RenderPass = renderPass,
                        Extent = new Extent2D(width, height),
                        Output = output,
                        ColorTargets = colorTargets,
                        DepthTarget = depthTarget,
                        OwnedViews = views,
                    };
                    TrackResource(resource);
                    return resource;
                }
            }
            catch
            {
                _vk.DestroyRenderPass(_device, renderPass, null);
                throw;
            }
        }
        catch
        {
            foreach (var view in views)
            {
                if (view.Handle != 0)
                    _vk.DestroyImageView(_device, view, null);
            }

            throw;
        }
    }

    public void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var destination = GetNative<VulkanBufferResource>(buffer);
        ValidateBufferRange(destination, offset, (uint)data.Length);
        if (data.IsEmpty)
            return;

        lock (_sync)
        {
            var staging = PrepareImmediateStaging(data);
            var dataLength = (uint)data.Length;
            ExecuteImmediate(commandBuffer =>
            {
                var copy = new BufferCopy
                {
                    SrcOffset = 0,
                    DstOffset = offset,
                    Size = dataLength,
                };
                BufferTransferWriteBarrier(commandBuffer, destination);
                _vk.CmdCopyBuffer(commandBuffer, staging.Handle, destination.Handle, 1, &copy);
                BufferTransferBarrier(commandBuffer, destination);
            });
        }
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        var destination = GetNative<VulkanTextureResource>(texture);
        ValidateTextureUpdate(destination, data, x, y, z, width, height, depth, mipLevel, arrayLayer);
        if (data.IsEmpty)
            return;

        lock (_sync)
        {
            var staging = PrepareImmediateStaging(data);
            ExecuteImmediate(commandBuffer =>
            {
                TransitionTexture(commandBuffer, destination, ImageLayout.TransferDstOptimal);
                var copy = new BufferImageCopy
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageSubresource = new ImageSubresourceLayers
                    {
                        AspectMask = destination.AspectMask,
                        MipLevel = mipLevel,
                        BaseArrayLayer = arrayLayer,
                        LayerCount = 1,
                    },
                    ImageOffset = new Offset3D((int)x, (int)y, (int)z),
                    ImageExtent = new Extent3D(width, height, depth),
                };
                _vk.CmdCopyBufferToImage(
                    commandBuffer,
                    staging.Handle,
                    destination.Handle,
                    ImageLayout.TransferDstOptimal,
                    1,
                    &copy);
                TransitionTexture(commandBuffer, destination, GetRestingLayout(destination));
            });
        }
    }

    public MappedResource Map(Texture texture, MapMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resource = GetNative<VulkanTextureResource>(texture);

        if ((resource.Description.Usage & TextureUsage.Staging) == 0)
            throw new InvalidOperationException("Only Vulkan staging textures can be mapped.");
        if (resource.Description.MipLevels != 1 || resource.Description.ArrayLayers != 1)
            throw new NotSupportedException("The mapping contract can represent only one Vulkan texture subresource.");
        if (resource.MappedAddress != 0)
            throw new InvalidOperationException("The Vulkan texture is already mapped.");

        lock (_sync)
        {
            Check(_vk.DeviceWaitIdle(_device), "wait before mapping a Vulkan texture");
            if (resource.Layout != ImageLayout.General)
            {
                ExecuteImmediate(commandBuffer => TransitionTexture(commandBuffer, resource, ImageLayout.General));
            }

            var subresource = new ImageSubresource
            {
                AspectMask = resource.AspectMask,
                MipLevel = 0,
                ArrayLayer = 0,
            };
            _vk.GetImageSubresourceLayout(_device, resource.Handle, &subresource, out var layout);
            void* mappedAddress;
            Check(_vk.MapMemory(_device, resource.Memory, 0, Vk.WholeSize, 0, &mappedAddress), "map Vulkan image memory");
            resource.MappedAddress = (nint)mappedAddress;

            if (mode is MapMode.Read or MapMode.ReadWrite &&
                (resource.MemoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
            {
                var range = new MappedMemoryRange
                {
                    SType = StructureType.MappedMemoryRange,
                    Memory = resource.Memory,
                    Offset = 0,
                    Size = Vk.WholeSize,
                };
                Check(_vk.InvalidateMappedMemoryRanges(_device, 1, &range), "invalidate mapped Vulkan image memory");
            }

            resource.MappedMode = mode;
            return new MappedResource(
                resource.MappedAddress + checked((nint)layout.Offset),
                checked((uint)layout.Size),
                checked((uint)layout.RowPitch),
                checked((uint)layout.DepthPitch));
        }
    }

    public void Unmap(Texture texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var resource = GetNative<VulkanTextureResource>(texture);
        if (resource.MappedAddress == 0)
            throw new InvalidOperationException("The Vulkan texture is not mapped.");

        lock (_sync)
        {
            if (resource.MappedMode is MapMode.Write or MapMode.ReadWrite &&
                (resource.MemoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
            {
                var range = new MappedMemoryRange
                {
                    SType = StructureType.MappedMemoryRange,
                    Memory = resource.Memory,
                    Offset = 0,
                    Size = Vk.WholeSize,
                };
                Check(_vk.FlushMappedMemoryRanges(_device, 1, &range), "flush mapped Vulkan image memory");
            }

            _vk.UnmapMemory(_device, resource.Memory);
            resource.MappedAddress = 0;
        }
    }

    public void DestroyResource(GraphicsResource resource)
    {
        if (_disposed || resource?.NativeResource is null)
            return;

        lock (_sync)
        {
            // Destruction defers until every submission in flight at this point has
            // signaled its fence, so disposing resources never stalls the GPU.
            if (_completedSerial >= _submittedSerial)
                DestroyNativeResource(resource.NativeResource);
            else
                _deferredDestructions.Add((_submittedSerial, resource.NativeResource));
        }
    }

    private void DestroyDeferredResources()
    {
        if (_deferredDestructions.Count == 0)
            return;

        var teardown = new TeardownCollector();
        var index = 0;
        while (index < _deferredDestructions.Count)
        {
            var (serial, nativeResource) = _deferredDestructions[index];
            if (serial > _completedSerial)
            {
                index++;
                continue;
            }

            _deferredDestructions.RemoveAt(index);
            teardown.Run(() => DestroyNativeResource(nativeResource));
        }

        teardown.ThrowIfFailed("One or more errors occurred while destroying deferred Vulkan resources.");
    }

    private VulkanBufferResource CreateInternalBuffer(
        ulong size,
        BufferUsageFlags usage,
        MemoryPropertyFlags requiredMemory,
        MemoryPropertyFlags preferredMemory)
    {
        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };
        Check(_vk.CreateBuffer(_device, &createInfo, null, out var buffer), "create a Vulkan buffer");
        _vk.GetBufferMemoryRequirements(_device, buffer, out var requirements);
        var memoryType = FindMemoryType(requirements.MemoryTypeBits, requiredMemory, preferredMemory, out var actualProperties);
        var allocationInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryType,
        };

        try
        {
            Check(_vk.AllocateMemory(_device, &allocationInfo, null, out var memory), "allocate Vulkan buffer memory");

            try
            {
                Check(_vk.BindBufferMemory(_device, buffer, memory, 0), "bind Vulkan buffer memory");
                return new VulkanBufferResource
                {
                    Handle = buffer,
                    Memory = memory,
                    Size = size,
                    Usage = usage,
                    MemoryProperties = actualProperties,
                };
            }
            catch
            {
                _vk.FreeMemory(_device, memory, null);
                throw;
            }
        }
        catch
        {
            _vk.DestroyBuffer(_device, buffer, null);
            throw;
        }
    }

    private VulkanBufferResource CreateUploadBuffer(ReadOnlySpan<byte> data)
    {
        var buffer = CreateInternalBuffer(
            (ulong)data.Length,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit,
            MemoryPropertyFlags.HostCoherentBit);

        try
        {
            void* mapped;
            Check(_vk.MapMemory(_device, buffer.Memory, 0, Vk.WholeSize, 0, &mapped), "map a Vulkan upload buffer");
            try
            {
                data.CopyTo(new Span<byte>(mapped, data.Length));
                if ((buffer.MemoryProperties & MemoryPropertyFlags.HostCoherentBit) == 0)
                {
                    var range = new MappedMemoryRange
                    {
                        SType = StructureType.MappedMemoryRange,
                        Memory = buffer.Memory,
                        Offset = 0,
                        Size = Vk.WholeSize,
                    };
                    Check(_vk.FlushMappedMemoryRanges(_device, 1, &range), "flush a Vulkan upload buffer");
                }
            }
            finally
            {
                _vk.UnmapMemory(_device, buffer.Memory);
            }

            return buffer;
        }
        catch
        {
            DestroyBuffer(buffer);
            throw;
        }
    }

    // The immediate update path fence-waits before returning, so one persistently
    // mapped staging buffer is reused across calls instead of allocating per update.
    private VulkanBufferResource PrepareImmediateStaging(ReadOnlySpan<byte> data)
    {
        var required = (ulong)data.Length;

        if (_immediateStaging is not null && _immediateStaging.Size < required)
        {
            DestroyBuffer(_immediateStaging);
            _immediateStaging = null;
        }

        if (_immediateStaging is null)
        {
            var staging = CreatePersistentlyMappedUploadBuffer(Math.Max(64 * 1024UL, required));
            _immediateStaging = staging;
        }

        data.CopyTo(new Span<byte>((void*)_immediateStaging.MappedAddress, data.Length));
        return _immediateStaging;
    }

    private VulkanBufferResource CreatePersistentlyMappedUploadBuffer(ulong capacity)
    {
        var buffer = CreateInternalBuffer(
            capacity,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        try
        {
            void* mapped;
            Check(_vk.MapMemory(_device, buffer.Memory, 0, Vk.WholeSize, 0, &mapped), "map a Vulkan upload buffer");
            buffer.MappedAddress = (nint)mapped;
            return buffer;
        }
        catch
        {
            DestroyBuffer(buffer);
            throw;
        }
    }

    // Suballocates from one host-visible arena per frame in flight; the arena resets
    // when its frame's fence is waited, so per-frame updates allocate no device memory.
    private ulong AllocateFrameUploadSpace(VulkanFrameContext frame, ulong size, out VulkanBufferResource arena)
    {
        const ulong Alignment = 16;
        var offset = (frame.UploadArenaOffset + Alignment - 1) & ~(Alignment - 1);

        if (frame.UploadArena is null || offset + size > frame.UploadArena.Size)
        {
            var capacity = Math.Max(Math.Max(256 * 1024UL, size), (frame.UploadArena?.Size ?? 0) * 2);

            // Commands recorded earlier this frame may reference the old arena, so it
            // retires through the frame's transient list, not immediate destruction.
            if (frame.UploadArena is not null)
                frame.TransientBuffers.Add(frame.UploadArena);

            frame.UploadArena = CreatePersistentlyMappedUploadBuffer(capacity);
            offset = 0;
        }

        frame.UploadArenaOffset = offset + size;
        arena = frame.UploadArena;
        return offset;
    }

    private ImageView CreateImageView(
        Silk.NET.Vulkan.Image image,
        Format format,
        ImageAspectFlags aspectMask,
        ImageViewType viewType,
        uint mipLevels,
        uint arrayLayers,
        uint baseArrayLayer = 0)
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = viewType,
            Format = format,
            Components = new ComponentMapping(
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity),
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = aspectMask,
                BaseMipLevel = 0,
                LevelCount = mipLevels,
                BaseArrayLayer = baseArrayLayer,
                LayerCount = arrayLayers,
            },
        };
        Check(_vk.CreateImageView(_device, &createInfo, null, out var view), "create a Vulkan image view");
        return view;
    }

    private ImageView CreateAttachmentView(VulkanTextureResource texture, uint arrayLayer) =>
        CreateImageView(
            texture.Handle,
            texture.Format,
            texture.AspectMask,
            texture.Description.Type switch
            {
                TextureType.Texture1D => ImageViewType.Type1D,
                TextureType.Texture3D => ImageViewType.Type2DArray,
                _ => ImageViewType.Type2D,
            },
            1,
            1,
            arrayLayer);

    private uint FindMemoryType(
        uint allowedTypes,
        MemoryPropertyFlags required,
        MemoryPropertyFlags preferred,
        out MemoryPropertyFlags actualProperties)
    {
        for (uint index = 0; index < _memoryProperties.MemoryTypeCount; index++)
        {
            var properties = _memoryProperties.MemoryTypes[(int)index].PropertyFlags;
            if ((allowedTypes & (1u << (int)index)) != 0 &&
                (properties & required) == required &&
                (properties & preferred) == preferred)
            {
                actualProperties = properties;
                return index;
            }
        }

        for (uint index = 0; index < _memoryProperties.MemoryTypeCount; index++)
        {
            var properties = _memoryProperties.MemoryTypes[(int)index].PropertyFlags;
            if ((allowedTypes & (1u << (int)index)) != 0 && (properties & required) == required)
            {
                actualProperties = properties;
                return index;
            }
        }

        throw new NotSupportedException($"No Vulkan memory type supports {required}.");
    }

    private void ExecuteImmediate(Action<CommandBuffer> record)
    {
        var allocateInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _transferCommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        Check(_vk.AllocateCommandBuffers(_device, &allocateInfo, out var commandBuffer), "allocate a Vulkan transfer command buffer");

        try
        {
            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };
            Check(_vk.BeginCommandBuffer(commandBuffer, &beginInfo), "begin a Vulkan transfer command buffer");
            record(commandBuffer);
            Check(_vk.EndCommandBuffer(commandBuffer), "end a Vulkan transfer command buffer");
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };

            // Wait on this submission's own fence rather than draining the whole queue;
            // vkQueueWaitIdle here stalled every in-flight frame per texture/buffer update.
            var fenceCreateInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
            Check(_vk.CreateFence(_device, &fenceCreateInfo, null, out var fence), "create a Vulkan transfer fence");

            try
            {
                Check(_vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, fence), "submit Vulkan transfer commands");
                var serial = ++_submittedSerial;
                Check(_vk.WaitForFences(_device, 1, &fence, true, ulong.MaxValue), "wait for Vulkan transfer commands");
                _completedSerial = Math.Max(_completedSerial, serial);
                DestroyDeferredResources();
            }
            finally
            {
                _vk.DestroyFence(_device, fence, null);
            }
        }
        finally
        {
            _vk.FreeCommandBuffers(_device, _transferCommandPool, 1, &commandBuffer);
        }
    }

    private void TransitionTexture(CommandBuffer commandBuffer, VulkanTextureResource texture, ImageLayout newLayout)
    {
        if (texture.Layout == newLayout)
            return;

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = texture.Layout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = texture.Handle,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = texture.AspectMask,
                BaseMipLevel = 0,
                LevelCount = texture.Description.MipLevels,
                BaseArrayLayer = 0,
                LayerCount = texture.Description.ArrayLayers,
            },
        };
        GetLayoutSynchronization(texture.Layout, out var sourceStage, out barrier.SrcAccessMask);
        GetLayoutSynchronization(newLayout, out var destinationStage, out barrier.DstAccessMask);
        _vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage,
            destinationStage,
            0,
            0,
            null,
            0,
            null,
            1,
            &barrier);
        texture.Layout = newLayout;
    }

    private static void GetLayoutSynchronization(
        ImageLayout layout,
        out PipelineStageFlags stage,
        out AccessFlags access)
    {
        switch (layout)
        {
            case ImageLayout.Undefined:
                stage = PipelineStageFlags.TopOfPipeBit;
                access = AccessFlags.None;
                break;
            case ImageLayout.TransferSrcOptimal:
                stage = PipelineStageFlags.TransferBit;
                access = AccessFlags.TransferReadBit;
                break;
            case ImageLayout.TransferDstOptimal:
                stage = PipelineStageFlags.TransferBit;
                access = AccessFlags.TransferWriteBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal:
                stage = PipelineStageFlags.VertexShaderBit | PipelineStageFlags.FragmentShaderBit;
                access = AccessFlags.ShaderReadBit;
                break;
            case ImageLayout.ColorAttachmentOptimal:
                stage = PipelineStageFlags.ColorAttachmentOutputBit;
                access = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit;
                break;
            case ImageLayout.DepthStencilAttachmentOptimal:
                stage = PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;
                access = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
                break;
            case ImageLayout.PresentSrcKhr:
                stage = PipelineStageFlags.BottomOfPipeBit;
                access = AccessFlags.None;
                break;
            default:
                stage = PipelineStageFlags.AllCommandsBit;
                access = AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit;
                break;
        }
    }

    private void BufferTransferBarrier(CommandBuffer commandBuffer, VulkanBufferResource buffer)
    {
        var barrier = new BufferMemoryBarrier
        {
            SType = StructureType.BufferMemoryBarrier,
            SrcAccessMask = AccessFlags.TransferWriteBit,
            DstAccessMask = AccessFlags.VertexAttributeReadBit | AccessFlags.IndexReadBit |
                            AccessFlags.UniformReadBit | AccessFlags.TransferReadBit,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Buffer = buffer.Handle,
            Offset = 0,
            Size = buffer.Size,
        };
        _vk.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.VertexInputBit | PipelineStageFlags.VertexShaderBit |
            PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.TransferBit,
            0,
            0,
            null,
            1,
            &barrier,
            0,
            null);
    }

    private void BufferTransferWriteBarrier(CommandBuffer commandBuffer, VulkanBufferResource buffer)
    {
        var barrier = new BufferMemoryBarrier
        {
            SType = StructureType.BufferMemoryBarrier,
            SrcAccessMask = AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit,
            DstAccessMask = AccessFlags.TransferWriteBit,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Buffer = buffer.Handle,
            Offset = 0,
            Size = buffer.Size,
        };
        _vk.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.AllCommandsBit,
            PipelineStageFlags.TransferBit,
            0,
            0,
            null,
            1,
            &barrier,
            0,
            null);
    }

    private static ImageLayout GetRestingLayout(VulkanTextureResource texture)
    {
        if ((texture.Description.Usage & TextureUsage.Staging) != 0)
            return ImageLayout.General;
        if ((texture.Description.Usage & TextureUsage.Sampled) != 0)
            return ImageLayout.ShaderReadOnlyOptimal;
        if ((texture.Description.Usage & TextureUsage.DepthStencil) != 0)
            return ImageLayout.DepthStencilAttachmentOptimal;
        if ((texture.Description.Usage & TextureUsage.RenderTarget) != 0)
            return ImageLayout.ColorAttachmentOptimal;
        return ImageLayout.General;
    }

    private void ValidateTextureDescription(TextureDescription description)
    {
        if (description.Width == 0 || description.Height == 0 || description.Depth == 0 ||
            description.MipLevels == 0 || description.ArrayLayers == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(description), "Vulkan texture dimensions and subresource counts must be positive.");
        }

        if (description.Type == TextureType.Texture1D && (description.Height != 1 || description.Depth != 1))
            throw new ArgumentException("Vulkan 1D textures must have height and depth equal to one.", nameof(description));
        if (description.Type == TextureType.Texture2D && description.Depth != 1)
            throw new ArgumentException("Vulkan 2D textures must have depth equal to one.", nameof(description));
        if (description.Type == TextureType.Texture3D && description.ArrayLayers != 1)
            throw new ArgumentException("Vulkan 3D textures cannot have array layers.", nameof(description));
        if ((description.Usage & TextureUsage.DepthStencil) != 0 && description.Format.AspectMask() == ImageAspectFlags.ColorBit)
            throw new ArgumentException("A Vulkan depth-stencil texture requires a depth-stencil format.", nameof(description));
        if ((description.Usage & TextureUsage.RenderTarget) != 0 && description.Format.AspectMask() != ImageAspectFlags.ColorBit)
            throw new ArgumentException("A Vulkan color render target requires a color format.", nameof(description));
        if ((description.Usage & TextureUsage.Staging) != 0 && VulkanConversions.IsCompressed(description.Format))
            throw new NotSupportedException("Vulkan linear staging images do not support compressed ElementEngine formats.");
        if ((description.Usage & TextureUsage.Staging) != 0 && description.Format.AspectMask() != ImageAspectFlags.ColorBit)
            throw new NotSupportedException("Vulkan staging textures do not support packed depth-stencil formats.");
    }

    private void ValidateFormatSupport(Format format, ImageTiling tiling, ImageUsageFlags usage)
    {
        _vk.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out var properties);
        var available = tiling == ImageTiling.Linear ? properties.LinearTilingFeatures : properties.OptimalTilingFeatures;
        var required = FormatFeatureFlags.TransferSrcBit | FormatFeatureFlags.TransferDstBit;

        if ((usage & ImageUsageFlags.SampledBit) != 0)
            required |= FormatFeatureFlags.SampledImageBit;
        if ((usage & ImageUsageFlags.ColorAttachmentBit) != 0)
            required |= FormatFeatureFlags.ColorAttachmentBit;
        if ((usage & ImageUsageFlags.DepthStencilAttachmentBit) != 0)
            required |= FormatFeatureFlags.DepthStencilAttachmentBit;

        if ((available & required) != required)
            throw new NotSupportedException($"The selected Vulkan device does not support {format} with {tiling} tiling and {usage} usage.");
    }

    private void ValidateTextureUpdate(
        VulkanTextureResource texture,
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
        if (texture.AspectMask != ImageAspectFlags.ColorBit)
            throw new NotSupportedException("Vulkan CPU texture updates do not support packed depth-stencil formats.");

        if (mipLevel >= texture.Description.MipLevels || arrayLayer >= texture.Description.ArrayLayers)
            throw new ArgumentOutOfRangeException(nameof(mipLevel), "The Vulkan texture subresource does not exist.");

        var mipWidth = Math.Max(1u, texture.Description.Width >> (int)mipLevel);
        var mipHeight = Math.Max(1u, texture.Description.Height >> (int)mipLevel);
        var mipDepth = Math.Max(1u, texture.Description.Depth >> (int)mipLevel);
        if (width == 0 || height == 0 || depth == 0 ||
            (ulong)x + width > mipWidth || (ulong)y + height > mipHeight || (ulong)z + depth > mipDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The Vulkan texture update exceeds the destination subresource.");
        }

        if (VulkanConversions.IsCompressed(texture.Description.Format) &&
            (x % 4 != 0 || y % 4 != 0 || (width % 4 != 0 && x + width != mipWidth) ||
             (height % 4 != 0 && y + height != mipHeight)))
        {
            throw new ArgumentException("Compressed Vulkan texture updates must align to 4x4 blocks except at image edges.");
        }

        var blockWidth = VulkanConversions.IsCompressed(texture.Description.Format) ? 4u : 1u;
        var blockHeight = VulkanConversions.IsCompressed(texture.Description.Format) ? 4u : 1u;
        var expectedSize = checked(
            ((ulong)width + blockWidth - 1) / blockWidth *
            (((ulong)height + blockHeight - 1) / blockHeight) *
            depth *
            texture.Description.Format.BytesPerBlock());

        if ((ulong)data.Length < expectedSize)
            throw new ArgumentException($"The texture update contains {data.Length} bytes but requires {expectedSize}.", nameof(data));
    }

    private static void ValidateFramebufferAttachment(
        FramebufferAttachmentDescription attachment,
        uint width,
        uint height,
        TextureUsage requiredUsage)
    {
        ArgumentNullException.ThrowIfNull(attachment.Target);
        if (attachment.Target.Width != width || attachment.Target.Height != height)
            throw new ArgumentException("All Vulkan framebuffer attachments must have the same dimensions.");
        if (attachment.ArrayLayer >= attachment.Target.ArrayLayers)
            throw new ArgumentOutOfRangeException(nameof(attachment), "The framebuffer array layer does not exist.");
        if ((attachment.Target.Usage & requiredUsage) == 0)
            throw new ArgumentException($"The Vulkan framebuffer attachment requires {requiredUsage} usage.");
    }

    private static void ValidateBufferRange(VulkanBufferResource buffer, uint offset, uint length)
    {
        if ((ulong)offset + length > buffer.Size)
            throw new ArgumentOutOfRangeException(nameof(length), "The Vulkan buffer operation exceeds the resource.");
    }

    private T GetNative<T>(GraphicsResource resource) where T : class
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (resource.NativeResource is not T native || !_liveResources.Contains(native))
            throw new ArgumentException($"The resource is not a live {typeof(T).Name} owned by this Vulkan device.", nameof(resource));
        return native;
    }

    private void TrackResource(object resource)
    {
        if (!_liveResources.Add(resource))
            throw new InvalidOperationException("The Vulkan resource is already tracked.");
    }

    private void DestroyAllResources(TeardownCollector teardown)
    {
        if (_device.Handle == 0)
        {
            _liveResources.Clear();
            return;
        }

        var order = new[]
        {
            typeof(VulkanResourceSetResource),
            typeof(VulkanFramebufferResource),
            typeof(VulkanPipelineResource),
            typeof(VulkanTextureViewResource),
            typeof(VulkanSamplerResource),
            typeof(VulkanShaderResource),
            typeof(VulkanResourceLayoutResource),
            typeof(VulkanTextureResource),
            typeof(VulkanBufferResource),
        };

        foreach (var type in order)
        {
            foreach (var resource in _liveResources.Where(type.IsInstanceOfType).ToArray())
                teardown.Run(() => DestroyNativeResource(resource));
        }

        foreach (var resource in _liveResources.ToArray())
            teardown.Run(() => DestroyNativeResource(resource));

        _liveResources.Clear();
    }

    private void DestroyNativeResource(object resource)
    {
        if (!_liveResources.Remove(resource))
            return;

        var teardown = new TeardownCollector();
        switch (resource)
        {
            case VulkanResourceSetResource resourceSet:
                {
                    var descriptorSet = resourceSet.Handle;
                    teardown.Run(() => FreeDescriptorSet(descriptorSet));
                    for (var index = resourceSet.OwnedViews.Length - 1; index >= 0; index--)
                    {
                        var view = resourceSet.OwnedViews[index];
                        teardown.Run(() => _vk.DestroyImageView(_device, view, null));
                    }
                    break;
                }
            case VulkanFramebufferResource framebuffer:
                teardown.Run(() => _vk.DestroyFramebuffer(_device, framebuffer.Handle, null));
                teardown.Run(() => _vk.DestroyRenderPass(_device, framebuffer.RenderPass, null));
                for (var index = framebuffer.OwnedViews.Length - 1; index >= 0; index--)
                {
                    var view = framebuffer.OwnedViews[index];
                    teardown.Run(() => _vk.DestroyImageView(_device, view, null));
                }
                break;
            case VulkanPipelineResource pipeline:
                teardown.Run(() => _vk.DestroyPipeline(_device, pipeline.Handle, null));
                teardown.Run(() => _vk.DestroyPipelineLayout(_device, pipeline.Layout, null));
                teardown.Run(() => _vk.DestroyRenderPass(_device, pipeline.CompatibilityRenderPass, null));
                break;
            case VulkanTextureViewResource view:
                teardown.Run(() => _vk.DestroyImageView(_device, view.Handle, null));
                break;
            case VulkanSamplerResource sampler:
                teardown.Run(() => _vk.DestroySampler(_device, sampler.Handle, null));
                break;
            case VulkanShaderResource shader:
                teardown.Run(() => _vk.DestroyShaderModule(_device, shader.Handle, null));
                break;
            case VulkanResourceLayoutResource layout:
                teardown.Run(() => _vk.DestroyDescriptorSetLayout(_device, layout.Handle, null));
                break;
            case VulkanTextureResource texture:
                if (texture.MappedAddress != 0)
                    teardown.Run(() => _vk.UnmapMemory(_device, texture.Memory));
                teardown.Run(() => _vk.DestroyImage(_device, texture.Handle, null));
                teardown.Run(() => _vk.FreeMemory(_device, texture.Memory, null));
                break;
            case VulkanBufferResource buffer:
                teardown.Run(() => DestroyBuffer(buffer));
                break;
            default:
                teardown.Run(() => throw new InvalidOperationException(
                    $"Unknown Vulkan resource type {resource.GetType().Name}."));
                break;
        }

        teardown.ThrowIfFailed($"One or more errors occurred while destroying {resource.GetType().Name}.");
    }

    private void FreeDescriptorSet(DescriptorSet descriptorSet)
    {
        Check(
            _vk.FreeDescriptorSets(_device, _descriptorPool, 1, &descriptorSet),
            "free a Vulkan descriptor set");
    }

    private void DestroyBuffer(VulkanBufferResource buffer)
    {
        var teardown = new TeardownCollector();
        if (buffer.Handle.Handle != 0)
            teardown.Run(() => _vk.DestroyBuffer(_device, buffer.Handle, null));
        if (buffer.Memory.Handle != 0)
            teardown.Run(() => _vk.FreeMemory(_device, buffer.Memory, null));
        teardown.ThrowIfFailed("One or more errors occurred while destroying a Vulkan buffer.");
    }

    private void DestroyTransientBuffers(VulkanFrameContext frame)
    {
        var teardown = new TeardownCollector();
        var buffers = frame.TransientBuffers.ToArray();
        frame.TransientBuffers.Clear();
        foreach (var buffer in buffers)
            teardown.Run(() => DestroyBuffer(buffer));
        teardown.ThrowIfFailed("One or more errors occurred while destroying transient Vulkan buffers.");
    }
}
