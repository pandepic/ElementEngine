using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using SharpMetal.Foundation;
using SharpMetal.Metal;
using SharpMetal.QuartzCore;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MetalGraphicsBackend
{
    public void Submit(IReadOnlyList<GraphicsCommand> commands)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commands);

        var inheritedDrawable = default(CAMetalDrawable);

        if (_pendingFrame is not null)
        {
            // Follow-up work must execute after the pending frame, so commit its
            // enqueued command buffer without presenting.
            FlushPendingFrameWithoutPresent();

            if (CommandsUseSwapchain(commands))
            {
                // Additional swapchain passes render into the frame's existing drawable;
                // the flushed frame stays alive (it owns the drawable) until Present.
                var flushed = _pendingFrame;
                _pendingFrame = null;
                _flushedFrames.Add(flushed);
                inheritedDrawable = flushed.Drawable;
            }
        }

        var frame = new PendingMetalFrame(new NSAutoreleasePool(), CreateCommandBuffer());
        var state = new MetalCommandState(frame)
        {
            Drawable = inheritedDrawable,
        };

        try
        {
            foreach (var command in commands)
                EncodeCommand(state, command);

            if (state.PendingClearColors.Count > 0 || state.PendingClearDepth.HasValue)
                EnsureRenderEncoder(state, applyPipeline: false);
            EndRenderPass(state);

            if (state.UsesSwapchain)
            {
                frame.Drawable = state.Drawable;
                frame.CommandBuffer.Enqueue();
                _pendingFrame = frame;
                return;
            }

            frame.CommandBuffer.Commit();
            CheckCompletedCommandBuffer(frame.CommandBuffer, _options.Debug);
            frame.ReleaseAfterCommit();
        }
        catch (Exception submissionException)
        {
            var teardown = new TeardownCollector(submissionException);
            teardown.Run(() => EndRenderPass(state));
            teardown.Run(frame.ReleaseWithoutCommit);
            teardown.ThrowIfFailed("Metal command submission and cleanup both failed.");
            throw;
        }
    }

    public void Present()
    {
        ThrowIfDisposed();
        PresentPendingFrame();
    }

    private static bool CommandsUseSwapchain(IReadOnlyList<GraphicsCommand> commands)
    {
        for (var index = 0; index < commands.Count; index++)
        {
            if (commands[index] is SetFramebufferCommand { Framebuffer.IsSwapchain: true })
                return true;
        }

        return false;
    }

    private void FlushPendingFrameWithoutPresent()
    {
        var frame = _pendingFrame;
        if (frame is null || frame.Committed)
            return;

        frame.CommandBuffer.Commit();
        frame.Committed = true;
        CheckCompletedCommandBuffer(frame.CommandBuffer, _options.Debug);
    }

    private void PresentPendingFrame()
    {
        var frame = _pendingFrame;
        _pendingFrame = null;

        if (frame is null && _flushedFrames.Count > 0)
        {
            // A later swapchain submission failed after flushing; present the drawable
            // from the last flushed frame so it is not abandoned.
            frame = _flushedFrames[^1];
            _flushedFrames.RemoveAt(_flushedFrames.Count - 1);
        }

        if (frame is null)
            return;

        var teardown = new TeardownCollector();
        teardown.Run(() =>
        {
            if (frame.Drawable.NativePtr == 0)
                throw new InvalidOperationException("The pending Metal frame has no drawable.");

            if (frame.Committed)
            {
                // The frame was flushed for a follow-up submission; present its drawable
                // from a fresh command buffer that executes after the flushed work.
                var presentBuffer = CreateCommandBuffer();
                presentBuffer.PresentDrawable(frame.Drawable);
                presentBuffer.Commit();
                CheckCompletedCommandBuffer(presentBuffer, _options.Debug);
                return;
            }

            frame.CommandBuffer.PresentDrawable(frame.Drawable);
            frame.CommandBuffer.Commit();
            CheckCompletedCommandBuffer(frame.CommandBuffer, _options.Debug);
        });
        teardown.Run(frame.ReleaseAfterCommit);

        var flushedFrames = _flushedFrames.ToArray();
        _flushedFrames.Clear();
        foreach (var flushedFrame in flushedFrames)
            teardown.Run(flushedFrame.ReleaseAfterCommit);

        teardown.ThrowIfFailed("Metal frame presentation and cleanup both failed.");
    }

    public void WaitForIdle()
    {
        ThrowIfDisposed();

        // Flush (but do not present) the pending frame so waiting stays side-effect
        // free like the other backends; SwapBuffers still presents the drawable later.
        if (_pendingFrame is not null)
            FlushPendingFrameWithoutPresent();

        WaitForIdleCore();
    }

    private void WaitForIdleCore()
    {
        using var pool = new NSAutoreleasePool();
        var commandBuffer = CreateCommandBuffer();
        commandBuffer.Commit();
        CheckCompletedCommandBuffer(commandBuffer, true);
    }

    private void EncodeCommand(MetalCommandState state, GraphicsCommand command)
    {
        switch (command)
        {
            case SetFramebufferCommand setFramebuffer:
                FlushPendingClears(state);
                state.Framebuffer = setFramebuffer.Framebuffer;
                state.PendingClearColors.Clear();
                state.PendingClearDepth = null;
                break;
            case SetViewportCommand setViewport:
                RequirePrimaryIndex(setViewport.Index, "viewport");
                state.Viewport = setViewport.Viewport;
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyViewport(state);
                break;
            case ClearColorTargetCommand clear:
                RequireFramebuffer(state);
                ValidateColorAttachmentIndex(state.Framebuffer, clear.Index);
                EndRenderPass(state);
                state.PendingClearColors[clear.Index] = clear.Color;
                break;
            case ClearDepthStencilCommand clearDepth:
                RequireFramebuffer(state);
                RequireDepthAttachment(state.Framebuffer);
                EndRenderPass(state);
                state.PendingClearDepth = clearDepth.Depth;
                state.PendingClearStencil = clearDepth.Stencil;
                break;
            case SetScissorRectCommand setScissor:
                RequirePrimaryIndex(setScissor.Index, "scissor rectangle");
                state.Scissor = new MTLScissorRect
                {
                    x = setScissor.X,
                    y = setScissor.Y,
                    width = setScissor.Width,
                    height = setScissor.Height,
                };
                state.ScissorIsFull = false;
                state.ScissorIsEmpty = setScissor.Width == 0 || setScissor.Height == 0;
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyScissor(state);
                break;
            case SetFullScissorRectCommand setFullScissor:
                RequirePrimaryIndex(setFullScissor.Index, "scissor rectangle");
                state.ScissorIsFull = true;
                state.ScissorIsEmpty = false;
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyScissor(state);
                break;
            case UpdateBufferCommand update:
                FlushPendingClears(state);
                EncodeBufferUpdate(state, update);
                break;
            case CopyTextureCommand copy:
                FlushPendingClears(state);
                EncodeTextureCopy(state, copy.Source, copy.Destination);
                break;
            case SetVertexBufferCommand setVertex:
                if ((setVertex.Buffer.Usage & BufferUsage.VertexBuffer) == 0)
                    throw new ArgumentException("A Metal vertex buffer requires VertexBuffer usage.", nameof(setVertex));
                if (setVertex.Index >= MetalConversions.MaximumVertexBuffers)
                    throw new ArgumentOutOfRangeException(nameof(setVertex), "The Metal vertex buffer index is out of range.");

                state.VertexBuffers[setVertex.Index] = GetNative<MetalBufferResource>(setVertex.Buffer);
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyVertexBuffer(state, setVertex.Index);
                break;
            case SetIndexBufferCommand setIndex:
                if ((setIndex.Buffer.Usage & BufferUsage.IndexBuffer) == 0)
                    throw new ArgumentException("A Metal index buffer requires IndexBuffer usage.", nameof(setIndex));

                state.IndexBuffer = GetNative<MetalBufferResource>(setIndex.Buffer);
                state.IndexFormat = setIndex.Format;
                break;
            case SetPipelineCommand setPipeline:
                state.Pipeline = GetNative<MetalPipelineResource>(setPipeline.Pipeline);
                state.ResourceSets.Clear();
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyPipelineAndResources(state);
                break;
            case SetGraphicsResourceSetCommand setResources:
                if (state.Pipeline is null)
                    throw new InvalidOperationException("Set a Metal pipeline before binding a graphics resource set.");

                var resourceSet = GetNative<MetalResourceSetResource>(setResources.ResourceSet);
                if (setResources.Slot >= state.Pipeline.ResourceLayouts.Length ||
                    !ReferenceEquals(resourceSet.Layout, state.Pipeline.ResourceLayouts[setResources.Slot]))
                {
                    throw new InvalidOperationException(
                        $"Metal resource set {setResources.Slot} does not match the active pipeline layout.");
                }

                state.ResourceSets[setResources.Slot] = resourceSet;
                if (state.RenderEncoder.NativePtr != 0)
                    ApplyResourceSet(state, setResources.Slot);
                break;
            case DrawCommand draw:
                EncodeDraw(state, draw.VertexCount);
                break;
            case DrawIndexedCommand drawIndexed:
                EncodeDrawIndexed(state, drawIndexed);
                break;
            default:
                throw new NotSupportedException($"The Metal backend does not support {command.GetType().Name}.");
        }
    }

    private void EncodeDraw(MetalCommandState state, uint vertexCount)
    {
        if (vertexCount == 0)
            return;

        ValidateDrawState(state, indexed: false);
        if (state.Pipeline.ScissorTestEnabled && state.ScissorIsEmpty)
            return;

        EnsureRenderEncoder(state);
        state.RenderEncoder.DrawPrimitives(state.Pipeline.PrimitiveType, 0, vertexCount);
    }

    private void EncodeDrawIndexed(MetalCommandState state, DrawIndexedCommand draw)
    {
        if (draw.IndexCount == 0 || draw.InstanceCount == 0)
            return;

        ValidateDrawState(state, indexed: true);
        if (state.Pipeline.ScissorTestEnabled && state.ScissorIsEmpty)
            return;

        EnsureRenderEncoder(state);
        var indexSize = state.IndexFormat == IndexFormat.UInt16 ? 2UL : 4UL;
        var indexType = state.IndexFormat == IndexFormat.UInt16 ? MTLIndexType.UInt16 : MTLIndexType.UInt32;
        var offset = checked(draw.IndexStart * indexSize);
        if (offset + checked(draw.IndexCount * indexSize) > state.IndexBuffer.Description.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(draw), "The indexed Metal draw exceeds its index buffer.");

        state.RenderEncoder.DrawIndexedPrimitives(
            state.Pipeline.PrimitiveType,
            draw.IndexCount,
            indexType,
            state.IndexBuffer.Buffer,
            offset,
            draw.InstanceCount,
            draw.VertexOffset,
            draw.InstanceStart);
    }

    private void EnsureRenderEncoder(MetalCommandState state, bool applyPipeline = true)
    {
        if (state.RenderEncoder.NativePtr != 0)
            return;

        RequireFramebuffer(state);
        using var descriptor = CreateRenderPassDescriptor(state);
        state.RenderEncoder = state.Frame.CommandBuffer.RenderCommandEncoder(descriptor);
        EnsureNative(state.RenderEncoder.NativePtr, "create a Metal render command encoder");
        state.PendingClearColors.Clear();
        state.PendingClearDepth = null;
        ApplyViewport(state);
        if (applyPipeline)
            ApplyPipelineAndResources(state);
    }

    private MTLRenderPassDescriptor CreateRenderPassDescriptor(MetalCommandState state)
    {
        var descriptor = new MTLRenderPassDescriptor();
        var (width, height) = GetFramebufferDimensions(state.Framebuffer);
        descriptor.RenderTargetWidth = width;
        descriptor.RenderTargetHeight = height;

        if (state.Framebuffer.IsSwapchain)
        {
            if (state.Drawable.NativePtr == 0)
                state.Drawable = _layer.NextDrawable;
            EnsureNative(state.Drawable.NativePtr, "acquire the next Metal drawable");
            state.UsesSwapchain = true;
            ConfigureColorAttachment(
                descriptor.ColorAttachments.Object(0),
                state.Drawable.Texture,
                0,
                false,
                state.PendingClearColors.GetValueOrDefault(0u),
                state.PendingClearColors.ContainsKey(0u));
            return descriptor;
        }

        var framebuffer = GetNative<MetalFramebufferResource>(state.Framebuffer);
        for (var index = 0; index < framebuffer.ColorTargets.Length; index++)
        {
            var attachmentIndex = checked((uint)index);
            ConfigureColorAttachment(
                descriptor.ColorAttachments.Object(attachmentIndex),
                framebuffer.ColorTargets[index].Texture,
                framebuffer.ColorSlices[index],
                framebuffer.ColorTargets[index].Description.Type == TextureType.Texture3D,
                state.PendingClearColors.GetValueOrDefault(attachmentIndex),
                state.PendingClearColors.ContainsKey(attachmentIndex));
        }

        if (framebuffer.DepthTarget is not null)
        {
            var depthAttachment = descriptor.DepthAttachment;
            depthAttachment.Texture = framebuffer.DepthTarget.Texture;
            if (framebuffer.DepthTarget.Description.Type == TextureType.Texture3D)
                depthAttachment.DepthPlane = framebuffer.DepthSlice;
            else
                depthAttachment.Slice = framebuffer.DepthSlice;
            depthAttachment.Level = 0;
            if (state.PendingClearDepth is { } clearDepth)
            {
                depthAttachment.LoadAction = MTLLoadAction.Clear;
                depthAttachment.ClearDepth = clearDepth;
            }
            else
            {
                depthAttachment.LoadAction = MTLLoadAction.Load;
            }
            depthAttachment.StoreAction = MTLStoreAction.Store;

            if (framebuffer.DepthTarget.Description.Format.HasStencil())
            {
                var stencilAttachment = descriptor.StencilAttachment;
                stencilAttachment.Texture = framebuffer.DepthTarget.Texture;
                if (framebuffer.DepthTarget.Description.Type == TextureType.Texture3D)
                    stencilAttachment.DepthPlane = framebuffer.DepthSlice;
                else
                    stencilAttachment.Slice = framebuffer.DepthSlice;
                stencilAttachment.Level = 0;
                if (state.PendingClearDepth.HasValue)
                {
                    stencilAttachment.LoadAction = MTLLoadAction.Clear;
                    stencilAttachment.ClearStencil = state.PendingClearStencil;
                }
                else
                {
                    stencilAttachment.LoadAction = MTLLoadAction.Load;
                }
                stencilAttachment.StoreAction = MTLStoreAction.Store;
            }
        }

        return descriptor;
    }

    private static void ConfigureColorAttachment(
        MTLRenderPassColorAttachmentDescriptor attachment,
        MTLTexture texture,
        uint slice,
        bool is3D,
        RgbaFloat clearColor,
        bool clear)
    {
        attachment.Texture = texture;
        if (is3D)
            attachment.DepthPlane = slice;
        else
            attachment.Slice = slice;
        attachment.Level = 0;
        attachment.LoadAction = clear ? MTLLoadAction.Clear : MTLLoadAction.Load;
        attachment.StoreAction = MTLStoreAction.Store;
        if (clear)
        {
            attachment.ClearColor = new MTLClearColor
            {
                red = clearColor.R,
                green = clearColor.G,
                blue = clearColor.B,
                alpha = clearColor.A,
            };
        }
    }

    private static void EndRenderPass(MetalCommandState state)
    {
        if (state.RenderEncoder.NativePtr == 0)
            return;

        state.RenderEncoder.EndEncoding();
        state.RenderEncoder = default;
    }

    private void FlushPendingClears(MetalCommandState state)
    {
        if (state.PendingClearColors.Count > 0 || state.PendingClearDepth.HasValue)
            EnsureRenderEncoder(state, applyPipeline: false);

        EndRenderPass(state);
    }

    private void ApplyPipelineAndResources(MetalCommandState state)
    {
        if (state.RenderEncoder.NativePtr == 0 || state.Pipeline is null)
            return;

        ValidatePipelineOutputs(state.Pipeline.Outputs, GetFramebufferOutput(state.Framebuffer));
        state.RenderEncoder.SetRenderPipelineState(state.Pipeline.PipelineState);
        state.RenderEncoder.SetDepthStencilState(state.Pipeline.DepthStencilState);
        state.RenderEncoder.SetCullMode(state.Pipeline.CullMode);
        state.RenderEncoder.SetDepthClipMode(state.Pipeline.DepthClipMode);
        state.RenderEncoder.SetFrontFacingWinding(MTLWinding.Clockwise);

        foreach (var vertexBuffer in state.VertexBuffers)
            ApplyVertexBuffer(state, vertexBuffer.Key);
        foreach (var slot in state.ResourceSets.Keys)
            ApplyResourceSet(state, slot);

        ApplyScissor(state);
    }

    private static void ApplyVertexBuffer(MetalCommandState state, uint index)
    {
        if (state.RenderEncoder.NativePtr == 0 || !state.VertexBuffers.TryGetValue(index, out var buffer))
            return;
        if (index >= MetalConversions.MaximumVertexBuffers)
            throw new ArgumentOutOfRangeException(nameof(index), "The Metal vertex buffer index is out of range.");

        state.RenderEncoder.SetVertexBuffer(
            buffer.Buffer,
            0,
            MetalConversions.VertexBufferStartIndex + index);
    }

    private void ApplyViewport(MetalCommandState state)
    {
        if (state.RenderEncoder.NativePtr == 0 || state.Framebuffer is null)
            return;

        var (width, height) = GetFramebufferDimensions(state.Framebuffer);
        var viewport = state.Viewport ?? new Viewport(0, 0, width, height, 0, 1);
        state.RenderEncoder.SetViewport(new MTLViewport
        {
            originX = viewport.X,
            originY = viewport.Y,
            width = viewport.Width,
            height = viewport.Height,
            znear = viewport.MinDepth,
            zfar = viewport.MaxDepth,
        });
    }

    private void ApplyScissor(MetalCommandState state)
    {
        if (state.RenderEncoder.NativePtr == 0 || state.Framebuffer is null || state.Pipeline is null)
            return;

        var (width, height) = GetFramebufferDimensions(state.Framebuffer);
        MTLScissorRect scissor;
        if (!state.Pipeline.ScissorTestEnabled || state.ScissorIsFull)
        {
            scissor = new MTLScissorRect { width = width, height = height };
        }
        else if (state.ScissorIsEmpty)
        {
            return;
        }
        else
        {
            scissor = state.Scissor;
            if (scissor.x >= width || scissor.y >= height)
            {
                state.ScissorIsEmpty = true;
                return;
            }

            scissor.width = Math.Min(scissor.width, width - scissor.x);
            scissor.height = Math.Min(scissor.height, height - scissor.y);
        }

        state.RenderEncoder.SetScissorRect(scissor);
    }

    private void ApplyResourceSet(MetalCommandState state, uint slot)
    {
        if (state.RenderEncoder.NativePtr == 0 || state.Pipeline is null ||
            !state.ResourceSets.TryGetValue(slot, out var resourceSet))
        {
            return;
        }

        if (slot >= state.Pipeline.ResourceLayouts.Length ||
            !ReferenceEquals(resourceSet.Layout, state.Pipeline.ResourceLayouts[slot]))
        {
            throw new InvalidOperationException($"Metal resource set {slot} does not match the active pipeline layout.");
        }

        foreach (var binding in state.Pipeline.ResourceBindings)
        {
            if (binding.Slot != slot)
                continue;
            if (binding.Element >= resourceSet.BoundResources.Length)
                throw new InvalidOperationException($"Metal resource set {slot} does not match the active pipeline layout.");

            var resource = resourceSet.BoundResources[binding.Element];
            switch (binding.Kind)
            {
                case ResourceKind.UniformBuffer:
                    BindBuffer(state.RenderEncoder, GetNative<MetalBufferResource>((DeviceBuffer)resource), binding);
                    break;
                case ResourceKind.TextureReadOnly:
                    BindTexture(state.RenderEncoder, GetMetalTexture(resource), binding);
                    break;
                case ResourceKind.Sampler:
                    BindSampler(state.RenderEncoder, GetNative<MetalSamplerResource>((Sampler)resource), binding);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binding));
            }
        }
    }

    private static void BindBuffer(
        MTLRenderCommandEncoder encoder,
        MetalBufferResource buffer,
        MetalResourceBinding binding)
    {
        if ((binding.Stages & ShaderStages.Vertex) != 0)
            encoder.SetVertexBuffer(buffer.Buffer, 0, binding.NativeIndex);
        if ((binding.Stages & ShaderStages.Fragment) != 0)
            encoder.SetFragmentBuffer(buffer.Buffer, 0, binding.NativeIndex);
    }

    private static void BindTexture(
        MTLRenderCommandEncoder encoder,
        MTLTexture texture,
        MetalResourceBinding binding)
    {
        if ((binding.Stages & ShaderStages.Vertex) != 0)
            encoder.SetVertexTexture(texture, binding.NativeIndex);
        if ((binding.Stages & ShaderStages.Fragment) != 0)
            encoder.SetFragmentTexture(texture, binding.NativeIndex);
    }

    private static void BindSampler(
        MTLRenderCommandEncoder encoder,
        MetalSamplerResource sampler,
        MetalResourceBinding binding)
    {
        if ((binding.Stages & ShaderStages.Vertex) != 0)
            encoder.SetVertexSamplerState(sampler.Sampler, binding.NativeIndex);
        if ((binding.Stages & ShaderStages.Fragment) != 0)
            encoder.SetFragmentSamplerState(sampler.Sampler, binding.NativeIndex);
    }

    private MTLTexture GetMetalTexture(object resource)
    {
        return resource switch
        {
            Texture texture => GetNative<MetalTextureResource>(texture).Texture,
            TextureView view => GetNative<MetalTextureViewResource>(view).Target.Texture,
            _ => throw new ArgumentException("A Metal texture binding requires a texture or texture view."),
        };
    }

    private void EncodeBufferUpdate(MetalCommandState state, UpdateBufferCommand command)
    {
        var destination = GetNative<MetalBufferResource>(command.Buffer);
        var length = (ulong)command.Data.Length;
        if ((ulong)command.Offset + length > destination.Description.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(command), "The update exceeds the Metal buffer.");
        if (length == 0)
            return;

        // Always copy on the GPU timeline (even for cpu-visible buffers) so updates apply
        // in command order; an immediate memcpy would make earlier draws see later data.
        var (arena, arenaOffset) = AllocateUploadSpace(state.Frame, length);
        command.Data.Span.CopyTo(new Span<byte>((void*)(arena.Contents + checked((nint)arenaOffset)), command.Data.Length));
        var blit = state.Frame.CommandBuffer.BlitCommandEncoder();
        EnsureNative(blit.NativePtr, "create a Metal command-list upload encoder");
        blit.CopyFromBuffer(arena, arenaOffset, destination.Buffer, command.Offset, length);
        blit.EndEncoding();
    }

    // Suballocates staging space from one shared arena per submission instead of
    // allocating an MTLBuffer per update; the arena is released with its frame and
    // Metal keeps it alive until the GPU has consumed the committed command buffer.
    private (MTLBuffer Arena, ulong Offset) AllocateUploadSpace(PendingMetalFrame frame, ulong size)
    {
        const ulong Alignment = 16;
        var offset = (frame.UploadArenaOffset + Alignment - 1) & ~(Alignment - 1);

        if (frame.UploadArena.NativePtr == 0 || offset + size > frame.UploadArenaCapacity)
        {
            var capacity = Math.Max(Math.Max(256 * 1024UL, size), frame.UploadArenaCapacity * 2);
            var arena = _device.NewBuffer(capacity, MTLResourceOptions.ResourceStorageModeShared);
            EnsureNative(arena.NativePtr, "create a Metal upload arena");
            frame.Uploads.Add(arena);
            frame.UploadArena = arena;
            frame.UploadArenaCapacity = capacity;
            offset = 0;
        }

        frame.UploadArenaOffset = offset + size;
        return (frame.UploadArena, offset);
    }

    private void EncodeTextureCopy(MetalCommandState state, Texture source, Texture destination)
    {
        var sourceTexture = GetNative<MetalTextureResource>(source);
        var destinationTexture = GetNative<MetalTextureResource>(destination);
        if (ReferenceEquals(sourceTexture, destinationTexture))
            throw new ArgumentException("A Metal texture cannot be copied onto itself.", nameof(destination));
        if (!AreCopyCompatible(sourceTexture.Description, destinationTexture.Description))
            throw new ArgumentException("Metal texture copies require matching dimensions, mip levels, array layers, formats, and texture types.");

        var blit = state.Frame.CommandBuffer.BlitCommandEncoder();
        EnsureNative(blit.NativePtr, "create a Metal texture copy encoder");

        for (var mipLevel = 0u; mipLevel < sourceTexture.Description.MipLevels; mipLevel++)
        {
            var width = Math.Max(1u, sourceTexture.Description.Width >> checked((int)mipLevel));
            var height = Math.Max(1u, sourceTexture.Description.Height >> checked((int)mipLevel));
            var depth = Math.Max(1u, sourceTexture.Description.Depth >> checked((int)mipLevel));
            var size = new MTLSize { width = width, height = height, depth = depth };

            for (var arrayLayer = 0u; arrayLayer < sourceTexture.Description.ArrayLayers; arrayLayer++)
            {
                blit.CopyFromTexture(
                    sourceTexture.Texture,
                    arrayLayer,
                    mipLevel,
                    default,
                    size,
                    destinationTexture.Texture,
                    arrayLayer,
                    mipLevel,
                    default);
            }
        }

        blit.EndEncoding();
    }

    private static bool AreCopyCompatible(TextureDescription source, TextureDescription destination) =>
        source.Width == destination.Width &&
        source.Height == destination.Height &&
        source.Depth == destination.Depth &&
        source.MipLevels == destination.MipLevels &&
        source.ArrayLayers == destination.ArrayLayers &&
        source.Format == destination.Format &&
        source.Type == destination.Type;

    private void ValidateDrawState(MetalCommandState state, bool indexed)
    {
        RequireFramebuffer(state);
        if (state.Pipeline is null)
            throw new InvalidOperationException("Set a Metal pipeline before drawing.");
        if (indexed && state.IndexBuffer is null)
            throw new InvalidOperationException("Set a Metal index buffer before drawing indexed geometry.");

        ValidatePipelineOutputs(state.Pipeline.Outputs, GetFramebufferOutput(state.Framebuffer));
        for (var index = 0u; index < state.Pipeline.VertexLayoutCount; index++)
        {
            if (!state.VertexBuffers.ContainsKey(index))
                throw new InvalidOperationException($"Set Metal vertex buffer {index} before drawing.");
        }

        for (var slot = 0u; slot < state.Pipeline.ResourceLayouts.Length; slot++)
        {
            if (!state.ResourceSets.TryGetValue(slot, out var resourceSet))
                throw new InvalidOperationException($"Set Metal graphics resource set {slot} before drawing.");
            if (!ReferenceEquals(resourceSet.Layout, state.Pipeline.ResourceLayouts[slot]))
                throw new InvalidOperationException($"Metal resource set {slot} does not match the active pipeline layout.");
        }

        ValidateFramebufferSamplingHazards(state);
    }

    private void ValidateFramebufferSamplingHazards(MetalCommandState state)
    {
        if (state.Framebuffer.IsSwapchain)
            return;

        var framebuffer = GetNative<MetalFramebufferResource>(state.Framebuffer);
        foreach (var resourceSet in state.ResourceSets.Values)
        {
            foreach (var resource in resourceSet.BoundResources)
            {
                var texture = resource switch
                {
                    Texture textureResource => GetNative<MetalTextureResource>(textureResource),
                    TextureView view => GetNative<MetalTextureViewResource>(view).Target,
                    _ => null,
                };

                if (texture is not null &&
                    (framebuffer.ColorTargets.Contains(texture) || ReferenceEquals(framebuffer.DepthTarget, texture)))
                {
                    throw new InvalidOperationException(
                        "A Metal framebuffer attachment cannot be sampled by the same render pass.");
                }
            }
        }
    }

    private static void ValidatePipelineOutputs(OutputDescription pipeline, OutputDescription framebuffer)
    {
        var pipelineColors = pipeline.ColorAttachments ?? [];
        var framebufferColors = framebuffer.ColorAttachments ?? [];
        if (pipelineColors.Length != framebufferColors.Length ||
            !pipelineColors.SequenceEqual(framebufferColors) ||
            pipeline.DepthAttachment != framebuffer.DepthAttachment)
        {
            throw new InvalidOperationException("The Metal pipeline outputs do not match the active framebuffer.");
        }
    }

    private OutputDescription GetFramebufferOutput(Framebuffer framebuffer)
    {
        return framebuffer.IsSwapchain
            ? SwapchainOutputDescription
            : GetNative<MetalFramebufferResource>(framebuffer).OutputDescription;
    }

    private (ulong Width, ulong Height) GetFramebufferDimensions(Framebuffer framebuffer)
    {
        if (framebuffer.IsSwapchain)
            return (_swapchainWidth, _swapchainHeight);

        var native = GetNative<MetalFramebufferResource>(framebuffer);
        return (native.Width, native.Height);
    }

    private void ValidateColorAttachmentIndex(Framebuffer framebuffer, uint index)
    {
        var colorCount = framebuffer.IsSwapchain
            ? 1
            : GetNative<MetalFramebufferResource>(framebuffer).ColorTargets.Length;
        if (index >= colorCount)
            throw new ArgumentOutOfRangeException(nameof(index), "The Metal color attachment index is out of range.");
    }

    private void RequireDepthAttachment(Framebuffer framebuffer)
    {
        if (framebuffer.IsSwapchain || GetNative<MetalFramebufferResource>(framebuffer).DepthTarget is null)
            throw new InvalidOperationException("The bound Metal framebuffer has no depth attachment to clear.");
    }

    private static void RequireFramebuffer(MetalCommandState state)
    {
        if (state.Framebuffer is null)
            throw new InvalidOperationException("Set a Metal framebuffer before issuing render commands.");
    }

    private static void RequirePrimaryIndex(uint index, string stateName)
    {
        if (index != 0)
            throw new NotSupportedException($"ElementEngine's Metal backend currently supports one {stateName}.");
    }

    private static void CheckCompletedCommandBuffer(MTLCommandBuffer commandBuffer, bool wait)
    {
        if (!wait)
            return;

        commandBuffer.WaitUntilCompleted();
        if (commandBuffer.Status != MTLCommandBufferStatus.Error)
            return;

        var error = commandBuffer.Error;
        throw new InvalidOperationException($"Metal command execution failed: {DescribeError(error)}");
    }
}

[SupportedOSPlatform("macos")]
internal sealed class MetalCommandState
{
    public PendingMetalFrame Frame { get; }
    public Framebuffer Framebuffer { get; set; }
    public MTLRenderCommandEncoder RenderEncoder { get; set; }
    public CAMetalDrawable Drawable { get; set; }
    public MetalPipelineResource Pipeline { get; set; }
    public MetalBufferResource IndexBuffer { get; set; }
    public IndexFormat IndexFormat { get; set; }
    public Viewport? Viewport { get; set; }
    public MTLScissorRect Scissor { get; set; }
    public bool ScissorIsFull { get; set; } = true;
    public bool ScissorIsEmpty { get; set; }
    public bool UsesSwapchain { get; set; }
    public Dictionary<uint, RgbaFloat> PendingClearColors { get; } = [];
    public float? PendingClearDepth { get; set; }
    public byte PendingClearStencil { get; set; }
    public Dictionary<uint, MetalBufferResource> VertexBuffers { get; } = [];
    public Dictionary<uint, MetalResourceSetResource> ResourceSets { get; } = [];

    public MetalCommandState(PendingMetalFrame frame)
    {
        Frame = frame;
    }

}

[SupportedOSPlatform("macos")]
internal sealed class PendingMetalFrame
{
    private bool _released;

    public NSAutoreleasePool Pool { get; }
    public MTLCommandBuffer CommandBuffer { get; }
    public CAMetalDrawable Drawable { get; set; }
    public bool Committed { get; set; }
    public List<MTLBuffer> Uploads { get; } = [];
    public MTLBuffer UploadArena { get; set; }
    public ulong UploadArenaCapacity { get; set; }
    public ulong UploadArenaOffset { get; set; }

    public PendingMetalFrame(NSAutoreleasePool pool, MTLCommandBuffer commandBuffer)
    {
        Pool = pool;
        CommandBuffer = commandBuffer;
    }

    public void ReleaseAfterCommit() => Release();

    public void ReleaseWithoutCommit() => Release();

    private void Release()
    {
        if (_released)
            return;

        _released = true;
        var teardown = new TeardownCollector();

        var uploads = Uploads.ToArray();
        Uploads.Clear();
        for (var index = uploads.Length - 1; index >= 0; index--)
            teardown.Run(uploads[index].Dispose);

        teardown.Run(Pool.Dispose);
        teardown.ThrowIfFailed("One or more errors occurred while releasing a pending Metal frame.");
    }
}
