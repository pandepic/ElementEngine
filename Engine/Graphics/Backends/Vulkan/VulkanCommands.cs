using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkFramebuffer = Silk.NET.Vulkan.Framebuffer;

namespace ElementEngine;

internal sealed unsafe partial class VulkanGraphicsBackend
{
    public void Submit(IReadOnlyList<GraphicsCommand> commands)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(commands);

        lock (_sync)
        {
            var usesSwapchain = commands.Any(command =>
                command is SetFramebufferCommand { Framebuffer.IsSwapchain: true });
            var beginsSwapchainFrame = usesSwapchain && !_frameSubmitted;

            if (beginsSwapchainFrame && _swapchainDirty)
            {
                if (_window.FramebufferWidth <= 0 || _window.FramebufferHeight <= 0)
                    throw new InvalidOperationException("Cannot render a Vulkan swapchain frame while the window is minimized.");

                RecreateSwapchain((uint)_window.FramebufferWidth, (uint)_window.FramebufferHeight);
            }

            var frame = _frames[_currentFrame];
            WaitForFrame(frame);
            DestroyTransientBuffers(frame);
            frame.UploadArenaOffset = 0;

            if (beginsSwapchainFrame)
                AcquireSwapchainImage(frame);
            else if (usesSwapchain && !_frameAcquired)
                throw new InvalidOperationException("The active Vulkan swapchain frame has no acquired image.");
            else if (!_frameSubmitted)
                _frameAcquired = false;

            Check(
                _vk.ResetCommandPool(_device, frame.CommandPool, CommandPoolResetFlags.None),
                "reset a Vulkan frame command pool");

            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };
            var state = new VulkanCommandState
            {
                CommandBuffer = frame.CommandBuffer,
                Frame = frame,
                OriginalSwapchainLayout = usesSwapchain
                    ? _swapchainImageLayouts[_currentImage]
                    : ImageLayout.Undefined,
            };
            var queued = false;

            try
            {
                Check(_vk.BeginCommandBuffer(frame.CommandBuffer, &beginInfo), "begin a Vulkan frame command buffer");

                foreach (var command in commands)
                    RecordCommand(command, state);

                EndRenderPass(state);
                if (state.SwapchainTouched)
                    TransitionSwapchainImage(state.CommandBuffer, ImageLayout.PresentSrcKhr);

                Check(_vk.EndCommandBuffer(frame.CommandBuffer), "end a Vulkan frame command buffer");
                var frameFence = frame.InFlight;
                Check(_vk.ResetFences(_device, 1, &frameFence), "reset a Vulkan frame fence");

                var commandBuffer = frame.CommandBuffer;
                var waitSemaphore = frame.ImageAvailable;
                var signalSemaphore = frame.RenderFinished;
                var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;
                var submitInfo = new SubmitInfo
                {
                    SType = StructureType.SubmitInfo,
                    WaitSemaphoreCount = beginsSwapchainFrame ? 1u : 0u,
                    PWaitSemaphores = beginsSwapchainFrame ? &waitSemaphore : null,
                    PWaitDstStageMask = beginsSwapchainFrame ? &waitStage : null,
                    CommandBufferCount = 1,
                    PCommandBuffers = &commandBuffer,
                    SignalSemaphoreCount = beginsSwapchainFrame ? 1u : 0u,
                    PSignalSemaphores = beginsSwapchainFrame ? &signalSemaphore : null,
                };
                Check(_vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, frame.InFlight), "submit Vulkan graphics commands");
                frame.SubmittedSerial = ++_submittedSerial;
                queued = true;

                if (state.SwapchainTouched)
                {
                    _imageFences[_currentImage] = frame.InFlight;
                    _frameSubmitted = true;
                    if (!beginsSwapchainFrame)
                        _waitForFrameBeforePresent = true;
                }
            }
            catch (Exception exception)
            {
                if (!queued)
                {
                    RestoreRecordedLayouts(state);
                    DestroyTransientBuffers(frame);

                    if (beginsSwapchainFrame && _frameAcquired)
                    {
                        try
                        {
                            ReleaseAcquiredImage(frame, state.OriginalSwapchainLayout);
                        }
                        catch (Exception recoveryException)
                        {
                            throw new AggregateException(
                                "Vulkan command recording failed and the acquired swapchain image could not be released.",
                                exception,
                                recoveryException);
                        }
                    }
                }

                throw;
            }
        }
    }

    private void RecordCommand(GraphicsCommand command, VulkanCommandState state)
    {
        switch (command)
        {
            case SetFramebufferCommand setFramebuffer:
                SetFramebuffer(setFramebuffer.Framebuffer, state);
                break;
            case SetViewportCommand setViewport:
                SetViewport(setViewport.Index, setViewport.Viewport, state);
                break;
            case ClearColorTargetCommand clearColor:
                ClearColorTarget(clearColor.Index, clearColor.Color, state);
                break;
            case ClearDepthStencilCommand clearDepth:
                ClearDepthStencil(clearDepth.Depth, clearDepth.Stencil, state);
                break;
            case SetScissorRectCommand setScissor:
                SetScissorRect(
                    setScissor.Index,
                    setScissor.X,
                    setScissor.Y,
                    setScissor.Width,
                    setScissor.Height,
                    state);
                break;
            case SetFullScissorRectCommand fullScissor:
                RequireFramebuffer(state);
                SetScissorRect(fullScissor.Index, 0, 0, state.Extent.Width, state.Extent.Height, state);
                break;
            case UpdateBufferCommand updateBuffer:
                RecordBufferUpdate(updateBuffer.Buffer, updateBuffer.Offset, updateBuffer.Data.Span, state);
                break;
            case CopyTextureCommand copyTexture:
                RecordTextureCopy(copyTexture.Source, copyTexture.Destination, state);
                break;
            case SetVertexBufferCommand setVertexBuffer:
                SetVertexBuffer(setVertexBuffer.Index, setVertexBuffer.Buffer, state);
                break;
            case SetIndexBufferCommand setIndexBuffer:
                SetIndexBuffer(setIndexBuffer.Buffer, setIndexBuffer.Format, state);
                break;
            case SetPipelineCommand setPipeline:
                SetPipeline(setPipeline.Pipeline, state);
                break;
            case SetGraphicsResourceSetCommand setResourceSet:
                SetGraphicsResourceSet(setResourceSet.Slot, setResourceSet.ResourceSet, state);
                break;
            case DrawCommand draw:
                RequireDrawState(state, indexed: false);
                _vk.CmdDraw(state.CommandBuffer, draw.VertexCount, 1, 0, 0);
                break;
            case DrawIndexedCommand drawIndexed:
                RequireDrawState(state, indexed: true);
                _vk.CmdDrawIndexed(
                    state.CommandBuffer,
                    drawIndexed.IndexCount,
                    drawIndexed.InstanceCount,
                    drawIndexed.IndexStart,
                    drawIndexed.VertexOffset,
                    drawIndexed.InstanceStart);
                break;
            default:
                throw new NotSupportedException($"Unsupported Vulkan graphics command: {command.GetType().Name}.");
        }
    }

    private void SetFramebuffer(Framebuffer framebuffer, VulkanCommandState state)
    {
        ArgumentNullException.ThrowIfNull(framebuffer);
        EndRenderPass(state);

        if (framebuffer.IsSwapchain)
        {
            if (!_frameAcquired)
                throw new InvalidOperationException("The Vulkan swapchain image was not acquired for this submission.");

            state.IsSwapchain = true;
            state.SwapchainTouched = true;
            state.RenderPass = _swapchainRenderPass;
            state.Framebuffer = _swapchainFramebuffers[_currentImage];
            state.Extent = _swapchainExtent;
            state.Output = _swapchainOutputDescription;
            state.OffscreenFramebuffer = null;
        }
        else
        {
            var nativeFramebuffer = GetNative<VulkanFramebufferResource>(framebuffer);
            state.IsSwapchain = false;
            state.RenderPass = nativeFramebuffer.RenderPass;
            state.Framebuffer = nativeFramebuffer.Handle;
            state.Extent = nativeFramebuffer.Extent;
            state.Output = nativeFramebuffer.Output;
            state.OffscreenFramebuffer = nativeFramebuffer;
        }

        state.FramebufferSelected = true;
        if (state.Pipeline is not null)
            ValidatePipelineOutput(state.Pipeline, state.Output);
    }

    private void SetViewport(uint index, ElementEngine.Viewport viewport, VulkanCommandState state)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Vulkan pipelines currently expose one viewport.");
        if (!float.IsFinite(viewport.X) || !float.IsFinite(viewport.Y) ||
            !float.IsFinite(viewport.Width) || !float.IsFinite(viewport.Height) ||
            !float.IsFinite(viewport.MinDepth) || !float.IsFinite(viewport.MaxDepth) ||
            viewport.Width <= 0 || viewport.Height <= 0 || viewport.MinDepth < 0 ||
            viewport.MaxDepth > 1 || viewport.MinDepth > viewport.MaxDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(viewport));
        }

        var nativeViewport = VulkanConversions.ToViewport(viewport);
        _vk.CmdSetViewport(state.CommandBuffer, index, 1, &nativeViewport);
        state.ViewportSet = true;
    }

    private void ClearColorTarget(uint index, RgbaFloat color, VulkanCommandState state)
    {
        RequireFramebuffer(state);
        var colorAttachmentCount = state.Output.ColorAttachments?.Length ?? 0;
        if (index >= colorAttachmentCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        EnsureRenderPass(state);
        var attachment = new ClearAttachment
        {
            AspectMask = ImageAspectFlags.ColorBit,
            ColorAttachment = index,
        };
        attachment.ClearValue.Color.Float32_0 = color.R;
        attachment.ClearValue.Color.Float32_1 = color.G;
        attachment.ClearValue.Color.Float32_2 = color.B;
        attachment.ClearValue.Color.Float32_3 = color.A;
        var rectangle = new ClearRect
        {
            Rect = new Rect2D(new Offset2D(0, 0), state.Extent),
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        _vk.CmdClearAttachments(state.CommandBuffer, 1, &attachment, 1, &rectangle);
    }

    private void ClearDepthStencil(float depth, byte stencil, VulkanCommandState state)
    {
        RequireFramebuffer(state);
        if (state.Output.DepthAttachment is not { } depthFormat)
            throw new InvalidOperationException("The bound Vulkan framebuffer has no depth attachment to clear.");

        EnsureRenderPass(state);
        var attachment = new ClearAttachment
        {
            AspectMask = depthFormat.AspectMask(),
        };
        attachment.ClearValue.DepthStencil = new ClearDepthStencilValue(depth, stencil);
        var rectangle = new ClearRect
        {
            Rect = new Rect2D(new Offset2D(0, 0), state.Extent),
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        _vk.CmdClearAttachments(state.CommandBuffer, 1, &attachment, 1, &rectangle);
    }

    private void SetScissorRect(
        uint index,
        uint x,
        uint y,
        uint width,
        uint height,
        VulkanCommandState state)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Vulkan pipelines currently expose one scissor rectangle.");

        var rectangle = new Rect2D(
            new Offset2D(checked((int)x), checked((int)y)),
            new Extent2D(width, height));
        _vk.CmdSetScissor(state.CommandBuffer, index, 1, &rectangle);
        state.ScissorSet = true;
    }

    private void RecordBufferUpdate(
        DeviceBuffer buffer,
        uint offset,
        ReadOnlySpan<byte> data,
        VulkanCommandState state)
    {
        var destination = GetNative<VulkanBufferResource>(buffer);
        ValidateBufferRange(destination, offset, (uint)data.Length);
        if (data.IsEmpty)
            return;

        EndRenderPass(state);
        var arenaOffset = AllocateFrameUploadSpace(state.Frame, (ulong)data.Length, out var arena);
        data.CopyTo(new Span<byte>((void*)(arena.MappedAddress + checked((nint)arenaOffset)), data.Length));
        var copy = new BufferCopy
        {
            SrcOffset = arenaOffset,
            DstOffset = offset,
            Size = (uint)data.Length,
        };
        BufferTransferWriteBarrier(state.CommandBuffer, destination);
        _vk.CmdCopyBuffer(state.CommandBuffer, arena.Handle, destination.Handle, 1, &copy);
        BufferTransferBarrier(state.CommandBuffer, destination);
    }

    private void RecordTextureCopy(Texture source, Texture destination, VulkanCommandState state)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ValidateTextureCopy(source.Description, destination.Description);

        var nativeSource = GetNative<VulkanTextureResource>(source);
        var nativeDestination = GetNative<VulkanTextureResource>(destination);
        if (ReferenceEquals(nativeSource, nativeDestination))
            throw new ArgumentException("A Vulkan texture cannot be copied onto itself.", nameof(destination));

        EndRenderPass(state);
        TransitionTexture(state, nativeSource, ImageLayout.TransferSrcOptimal);
        TransitionTexture(state, nativeDestination, ImageLayout.TransferDstOptimal);

        var copyAspects = nativeSource.AspectMask == (ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit)
            ? new[] { ImageAspectFlags.DepthBit, ImageAspectFlags.StencilBit }
            : new[] { nativeSource.AspectMask };

        for (uint mipLevel = 0; mipLevel < source.MipLevels; mipLevel++)
        {
            var layerCount = source.Type == TextureType.Texture3D ? 1u : source.ArrayLayers;
            foreach (var aspect in copyAspects)
            {
                var copy = new ImageCopy
                {
                    SrcSubresource = new ImageSubresourceLayers
                    {
                        AspectMask = aspect,
                        MipLevel = mipLevel,
                        BaseArrayLayer = 0,
                        LayerCount = layerCount,
                    },
                    DstSubresource = new ImageSubresourceLayers
                    {
                        AspectMask = aspect,
                        MipLevel = mipLevel,
                        BaseArrayLayer = 0,
                        LayerCount = layerCount,
                    },
                    Extent = new Extent3D(
                        Math.Max(1u, source.Width >> (int)mipLevel),
                        Math.Max(1u, source.Height >> (int)mipLevel),
                        Math.Max(1u, source.Depth >> (int)mipLevel)),
                };
                _vk.CmdCopyImage(
                    state.CommandBuffer,
                    nativeSource.Handle,
                    ImageLayout.TransferSrcOptimal,
                    nativeDestination.Handle,
                    ImageLayout.TransferDstOptimal,
                    1,
                    &copy);
            }
        }

        TransitionTexture(state, nativeSource, GetRestingLayout(nativeSource));
        TransitionTexture(state, nativeDestination, GetRestingLayout(nativeDestination));
    }

    private void SetVertexBuffer(uint index, DeviceBuffer buffer, VulkanCommandState state)
    {
        if (index >= VulkanCommandState.MaximumBindings)
            throw new ArgumentOutOfRangeException(nameof(index));

        var nativeBuffer = GetNative<VulkanBufferResource>(buffer);
        if ((buffer.Usage & BufferUsage.VertexBuffer) == 0)
            throw new ArgumentException("A Vulkan vertex buffer requires VertexBuffer usage.", nameof(buffer));
        if (state.Pipeline is not null && index >= state.Pipeline.VertexBufferCount)
            throw new InvalidOperationException($"The active Vulkan pipeline has no vertex layout for slot {index}.");

        state.VertexBuffers[index] = nativeBuffer;
        var handle = nativeBuffer.Handle;
        ulong offset = 0;
        _vk.CmdBindVertexBuffers(state.CommandBuffer, index, 1, &handle, &offset);
    }

    private void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format, VulkanCommandState state)
    {
        var nativeBuffer = GetNative<VulkanBufferResource>(buffer);
        if ((buffer.Usage & BufferUsage.IndexBuffer) == 0)
            throw new ArgumentException("A Vulkan index buffer requires IndexBuffer usage.", nameof(buffer));

        state.IndexBuffer = nativeBuffer;
        state.IndexFormat = format;
        _vk.CmdBindIndexBuffer(
            state.CommandBuffer,
            nativeBuffer.Handle,
            0,
            format == IndexFormat.UInt16 ? IndexType.Uint16 : IndexType.Uint32);
    }

    private void SetPipeline(Pipeline pipeline, VulkanCommandState state)
    {
        var nativePipeline = GetNative<VulkanPipelineResource>(pipeline);
        if (state.FramebufferSelected)
            ValidatePipelineOutput(nativePipeline, state.Output);

        // Resource sets never survive a pipeline switch: DX12 and Metal cannot retain
        // them, so retaining layout-compatible sets here would let command streams that
        // draw fine on Vulkan throw on the other backends.
        Array.Clear(state.ResourceSets);

        state.Pipeline = nativePipeline;
        _vk.CmdBindPipeline(state.CommandBuffer, PipelineBindPoint.Graphics, nativePipeline.Handle);
    }

    private void SetGraphicsResourceSet(uint slot, ResourceSet resourceSet, VulkanCommandState state)
    {
        var pipeline = state.Pipeline ??
            throw new InvalidOperationException("Set a Vulkan graphics pipeline before binding resource sets.");
        if (slot >= pipeline.SetLayouts.Length || slot >= VulkanCommandState.MaximumBindings)
            throw new ArgumentOutOfRangeException(nameof(slot));

        var nativeSet = GetNative<VulkanResourceSetResource>(resourceSet);
        if (!ReferenceEquals(nativeSet.Layout, pipeline.SetLayouts[slot]))
            throw new ArgumentException("The Vulkan resource set layout does not match the active pipeline slot.", nameof(resourceSet));

        foreach (var texture in nativeSet.Textures)
        {
            if (IsCurrentFramebufferAttachment(texture, state))
            {
                throw new InvalidOperationException(
                    "A Vulkan framebuffer attachment cannot be sampled by the same render pass.");
            }
        }

        if (nativeSet.Textures.Any(texture => texture.Layout != ImageLayout.ShaderReadOnlyOptimal))
        {
            EndRenderPass(state);
            foreach (var texture in nativeSet.Textures)
                TransitionTexture(state, texture, ImageLayout.ShaderReadOnlyOptimal);
        }

        var descriptorSet = nativeSet.Handle;
        _vk.CmdBindDescriptorSets(
            state.CommandBuffer,
            PipelineBindPoint.Graphics,
            pipeline.Layout,
            slot,
            1,
            &descriptorSet,
            0,
            null);
        state.ResourceSets[slot] = nativeSet;
    }

    private void RequireDrawState(VulkanCommandState state, bool indexed)
    {
        RequireFramebuffer(state);
        var pipeline = state.Pipeline ?? throw new InvalidOperationException("Set a Vulkan pipeline before drawing.");
        ValidatePipelineOutput(pipeline, state.Output);

        if (!state.ViewportSet)
            throw new InvalidOperationException("Set a Vulkan viewport before drawing.");
        if (indexed && state.IndexBuffer is null)
            throw new InvalidOperationException("Bind a Vulkan index buffer before drawing indexed primitives.");

        for (var index = 0; index < pipeline.VertexBufferCount; index++)
        {
            if (state.VertexBuffers[index] is null)
                throw new InvalidOperationException($"Bind Vulkan vertex buffer slot {index} before drawing.");
        }

        for (var index = 0; index < pipeline.SetLayouts.Length; index++)
        {
            if (state.ResourceSets[index] is null)
                throw new InvalidOperationException($"Bind Vulkan resource set slot {index} before drawing.");
            if (state.ResourceSets[index].Textures.Any(texture => IsCurrentFramebufferAttachment(texture, state)))
            {
                throw new InvalidOperationException(
                    "A Vulkan framebuffer attachment cannot be sampled by the same render pass.");
            }
        }

        if (pipeline.ScissorTestEnabled)
        {
            if (!state.ScissorSet)
                throw new InvalidOperationException("Set a Vulkan scissor rectangle before drawing with scissor testing.");
        }
        else
        {
            SetScissorRect(0, 0, 0, state.Extent.Width, state.Extent.Height, state);
        }

        EnsureRenderPass(state);
    }

    private void EnsureRenderPass(VulkanCommandState state)
    {
        RequireFramebuffer(state);
        if (state.RenderPassActive)
            return;

        if (state.IsSwapchain)
        {
            TransitionSwapchainImage(state.CommandBuffer, ImageLayout.ColorAttachmentOptimal);
        }
        else
        {
            foreach (var colorTarget in state.OffscreenFramebuffer.ColorTargets)
                TransitionTexture(state, colorTarget, ImageLayout.ColorAttachmentOptimal);
            if (state.OffscreenFramebuffer.DepthTarget is not null)
                TransitionTexture(state, state.OffscreenFramebuffer.DepthTarget, ImageLayout.DepthStencilAttachmentOptimal);
        }

        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = state.RenderPass,
            Framebuffer = state.Framebuffer,
            RenderArea = new Rect2D(new Offset2D(0, 0), state.Extent),
            ClearValueCount = 0,
            PClearValues = null,
        };
        _vk.CmdBeginRenderPass(state.CommandBuffer, &beginInfo, SubpassContents.Inline);
        state.RenderPassActive = true;
    }

    private void EndRenderPass(VulkanCommandState state)
    {
        if (!state.RenderPassActive)
            return;

        _vk.CmdEndRenderPass(state.CommandBuffer);
        state.RenderPassActive = false;
    }

    private static void RequireFramebuffer(VulkanCommandState state)
    {
        if (!state.FramebufferSelected)
            throw new InvalidOperationException("Set a Vulkan framebuffer before issuing render commands.");
    }

    private void TransitionTexture(
        VulkanCommandState state,
        VulkanTextureResource texture,
        ImageLayout newLayout)
    {
        if (texture.Layout == newLayout)
            return;

        state.OriginalTextureLayouts.TryAdd(texture, texture.Layout);
        TransitionTexture(state.CommandBuffer, texture, newLayout);
    }

    private void TransitionSwapchainImage(CommandBuffer commandBuffer, ImageLayout newLayout)
    {
        var oldLayout = _swapchainImageLayouts[_currentImage];
        if (oldLayout == newLayout)
            return;

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = _swapchainImages[_currentImage],
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
        };
        GetLayoutSynchronization(oldLayout, out var sourceStage, out barrier.SrcAccessMask);
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
        _swapchainImageLayouts[_currentImage] = newLayout;
    }

    private void WaitForFrame(VulkanFrameContext frame)
    {
        var fence = frame.InFlight;
        Check(
            _vk.WaitForFences(_device, 1, &fence, true, ulong.MaxValue),
            "wait for a Vulkan frame fence");

        _completedSerial = Math.Max(_completedSerial, frame.SubmittedSerial);
        DestroyDeferredResources();
    }

    private void AcquireSwapchainImage(VulkanFrameContext frame)
    {
        while (true)
        {
            var result = _swapchainApi.AcquireNextImage(
                _device,
                _swapchain,
                ulong.MaxValue,
                frame.ImageAvailable,
                default,
                ref _currentImage);

            if (result == Result.ErrorOutOfDateKhr)
            {
                if (_window.FramebufferWidth <= 0 || _window.FramebufferHeight <= 0)
                    throw new InvalidOperationException("Cannot acquire a Vulkan swapchain image while the window is minimized.");

                RecreateSwapchain((uint)_window.FramebufferWidth, (uint)_window.FramebufferHeight);
                continue;
            }

            if (result == Result.SuboptimalKhr)
                _swapchainDirty = true;
            else
                Check(result, "acquire a Vulkan swapchain image");

            break;
        }

        var imageFence = _imageFences[_currentImage];
        if (imageFence.Handle != 0)
        {
            Check(
                _vk.WaitForFences(_device, 1, &imageFence, true, ulong.MaxValue),
                "wait for a Vulkan swapchain image fence");
        }

        _frameAcquired = true;
    }

    private void RestoreRecordedLayouts(VulkanCommandState state)
    {
        foreach (var (texture, layout) in state.OriginalTextureLayouts)
            texture.Layout = layout;

        if (state.SwapchainTouched)
            _swapchainImageLayouts[_currentImage] = state.OriginalSwapchainLayout;
    }

    private void ReleaseAcquiredImage(VulkanFrameContext frame, ImageLayout originalLayout)
    {
        Check(
            _vk.ResetCommandPool(_device, frame.CommandPool, CommandPoolResetFlags.None),
            "reset a failed Vulkan frame command pool");
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        Check(_vk.BeginCommandBuffer(frame.CommandBuffer, &beginInfo), "begin Vulkan image-release commands");
        _swapchainImageLayouts[_currentImage] = originalLayout;
        TransitionSwapchainImage(frame.CommandBuffer, ImageLayout.PresentSrcKhr);
        Check(_vk.EndCommandBuffer(frame.CommandBuffer), "end Vulkan image-release commands");

        var frameFence = frame.InFlight;
        Check(_vk.ResetFences(_device, 1, &frameFence), "reset a Vulkan image-release fence");
        var waitSemaphore = frame.ImageAvailable;
        var signalSemaphore = frame.RenderFinished;
        var waitStage = PipelineStageFlags.AllCommandsBit;
        var commandBuffer = frame.CommandBuffer;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore,
        };
        Check(_vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, frame.InFlight), "release a Vulkan swapchain image");
        _imageFences[_currentImage] = frame.InFlight;

        var swapchain = _swapchain;
        var imageIndex = _currentImage;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
        };
        var result = _swapchainApi.QueuePresent(_presentQueue, &presentInfo);
        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
            _swapchainDirty = true;
        else
            Check(result, "present a released Vulkan swapchain image");

        _frameAcquired = false;
        _frameSubmitted = false;
        _waitForFrameBeforePresent = false;
        _currentFrame = (_currentFrame + 1) % (uint)_frames.Length;
    }

    private bool IsCurrentFramebufferAttachment(VulkanTextureResource texture, VulkanCommandState state)
    {
        if (!state.FramebufferSelected || state.IsSwapchain || state.OffscreenFramebuffer is null)
            return false;

        return state.OffscreenFramebuffer.ColorTargets.Contains(texture) ||
               ReferenceEquals(state.OffscreenFramebuffer.DepthTarget, texture);
    }

    private static void ValidatePipelineOutput(VulkanPipelineResource pipeline, OutputDescription output)
    {
        if (!(pipeline.Output.ColorAttachments ?? []).SequenceEqual(output.ColorAttachments ?? []) ||
            pipeline.Output.DepthAttachment != output.DepthAttachment)
        {
            throw new InvalidOperationException("The Vulkan pipeline output does not match the active framebuffer.");
        }
    }

    private static void ValidateTextureCopy(TextureDescription source, TextureDescription destination)
    {
        if (source.Width != destination.Width || source.Height != destination.Height ||
            source.Depth != destination.Depth || source.MipLevels != destination.MipLevels ||
            source.ArrayLayers != destination.ArrayLayers || source.Format != destination.Format ||
            source.Type != destination.Type)
        {
            throw new ArgumentException("Vulkan texture copies require matching dimensions, subresources, formats, and types.");
        }
    }
}
