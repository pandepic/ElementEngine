using System;
using System.Collections.Generic;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;
using DirectXBox = Silk.NET.Direct3D12.Box;
using DirectXRange = Silk.NET.Direct3D12.Range;
using DirectXViewport = Silk.NET.Direct3D12.Viewport;

namespace ElementEngine;

internal unsafe sealed partial class DirectX12Backend
{
    public void Submit(IReadOnlyList<GraphicsCommand> commands)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(commands);

        lock (_sync)
        {
            var frame = checked((int)_frameIndex);
            WaitForFence(_frameFenceValues[frame]);

            foreach (var transient in _frameTransients[frame])
                transient.Dispose();
            _frameTransients[frame].Clear();

            if (_frameUploadArenas[frame] is { } frameArena)
                frameArena.Offset = 0;
            DestroyDeferredResources();

            var allocator = (ID3D12CommandAllocator*)_frameAllocators[frame];
            ThrowIfFailed(allocator->Reset(), "reset a DirectX 12 frame command allocator");
            ThrowIfFailed(_commandList->Reset(allocator, null), "reset the DirectX 12 frame command list");

            var descriptorHeaps = stackalloc ID3D12DescriptorHeap*[2]
            {
                _shaderResourceDescriptors.Heap,
                _samplerDescriptors.Heap,
            };
            _commandList->SetDescriptorHeaps(2, descriptorHeaps);
            var state = new DirectX12RecordingState();
            var executed = false;

            try
            {
                foreach (var command in commands)
                    RecordCommand(_commandList, command, state, _frameTransients[frame], ref _frameUploadArenas[frame]);

                TransitionSwapchainToPresent(_commandList, state);
                ThrowIfFailed(_commandList->Close(), "close the DirectX 12 frame command list");
                ExecuteCommandList(_commandList);
                executed = true;
                _frameFenceValues[frame] = SignalFence();
            }
            catch
            {
                _commandList->Close();
                if (!executed)
                {
                    RestoreRecordedStates(state);
                    foreach (var transient in _frameTransients[frame])
                        transient.Dispose();
                    _frameTransients[frame].Clear();
                }
                throw;
            }
        }
    }

    private void RecordCommand(
        ID3D12GraphicsCommandList* commandList,
        GraphicsCommand command,
        DirectX12RecordingState state,
        List<IDisposable> transients,
        ref DirectX12UploadArena uploadArena)
    {
        switch (command)
        {
            case SetFramebufferCommand setFramebuffer:
                SetFramebuffer(commandList, setFramebuffer.Framebuffer, state);
                break;
            case SetViewportCommand setViewport:
                SetViewport(commandList, setViewport.Index, setViewport.Viewport, state);
                break;
            case ClearColorTargetCommand clearColor:
                ClearColor(commandList, clearColor.Index, clearColor.Color, state);
                break;
            case ClearDepthStencilCommand clearDepthStencil:
                ClearDepthStencil(commandList, clearDepthStencil.Depth, clearDepthStencil.Stencil, state);
                break;
            case SetScissorRectCommand setScissor:
                SetScissor(
                    commandList,
                    setScissor.Index,
                    setScissor.X,
                    setScissor.Y,
                    setScissor.Width,
                    setScissor.Height,
                    state);
                break;
            case SetFullScissorRectCommand fullScissor:
                RequireFramebuffer(state);
                SetScissor(commandList, fullScissor.Index, 0, 0, state.FramebufferWidth, state.FramebufferHeight, state);
                break;
            case UpdateBufferCommand updateBuffer:
                RecordBufferUpdate(
                    commandList,
                    RequireBuffer(updateBuffer.Buffer),
                    updateBuffer.Offset,
                    updateBuffer.Data.Span,
                    transients,
                    ref uploadArena,
                    state);
                break;
            case CopyTextureCommand copyTexture:
                RecordTextureCopy(commandList, copyTexture.Source, copyTexture.Destination, state);
                break;
            case SetVertexBufferCommand setVertexBuffer:
                if (setVertexBuffer.Index >= state.VertexBuffers.Length)
                    throw new ArgumentOutOfRangeException(nameof(setVertexBuffer.Index));
                state.VertexBuffers[setVertexBuffer.Index] = RequireBuffer(setVertexBuffer.Buffer);
                BindVertexBuffer(commandList, setVertexBuffer.Index, state);
                break;
            case SetIndexBufferCommand setIndexBuffer:
                state.IndexBuffer = RequireBuffer(setIndexBuffer.Buffer);
                state.IndexFormat = setIndexBuffer.Format;
                BindIndexBuffer(commandList, state);
                break;
            case SetPipelineCommand setPipeline:
                var pipeline = RequireNative<DirectX12Pipeline>(setPipeline.Pipeline.NativeResource, "pipeline");
                if (state.HasFramebuffer)
                    ValidatePipelineOutput(pipeline, state.Output);

                // Drop stale bindings beyond the new pipeline's layout; Vulkan and Metal
                // ignore them, so throwing here would break portable command streams.
                for (var slot = pipeline.VertexStrides.Length; slot < state.VertexBuffers.Length; slot++)
                    state.VertexBuffers[slot] = null;

                state.Pipeline = pipeline;
                Array.Clear(state.ResourceSets);
                commandList->SetPipelineState(state.Pipeline.PipelineState);
                commandList->SetGraphicsRootSignature(state.Pipeline.RootSignature);
                commandList->IASetPrimitiveTopology(state.Pipeline.PrimitiveTopology);
                BindAllVertexBuffers(commandList, state);
                if (state.IndexBuffer is not null)
                    BindIndexBuffer(commandList, state);
                if (state.HasFramebuffer && !state.Pipeline.ScissorEnabled)
                    RecordAndForceFullScissor(commandList, state);
                else if (state.Pipeline.ScissorEnabled)
                    ApplyScissors(commandList, state);
                break;
            case SetGraphicsResourceSetCommand setResourceSet:
                BindResourceSet(commandList, setResourceSet.Slot, setResourceSet.ResourceSet, state);
                break;
            case DrawCommand draw:
                RequireDrawState(state, false);
                commandList->DrawInstanced(draw.VertexCount, 1, 0, 0);
                break;
            case DrawIndexedCommand drawIndexed:
                RequireDrawState(state, true);
                commandList->DrawIndexedInstanced(
                    drawIndexed.IndexCount,
                    drawIndexed.InstanceCount,
                    drawIndexed.IndexStart,
                    drawIndexed.VertexOffset,
                    drawIndexed.InstanceStart);
                break;
            default:
                throw new NotSupportedException($"Unsupported DirectX 12 graphics command: {command.GetType().Name}.");
        }
    }

    private void SetFramebuffer(
        ID3D12GraphicsCommandList* commandList,
        Framebuffer framebuffer,
        DirectX12RecordingState state)
    {
        if (framebuffer.IsSwapchain)
        {
            TransitionSwapchain(commandList, ResourceStates.RenderTarget, state);
            var renderTarget = _swapchainRenderTargets.GetCpuHandle(_frameIndex);
            commandList->OMSetRenderTargets(1, &renderTarget, true, null);
            state.SwapchainRenderTarget = renderTarget;
            state.Framebuffer = null;
            state.IsSwapchainFramebuffer = true;
            state.FramebufferWidth = _swapchainWidth;
            state.FramebufferHeight = _swapchainHeight;
            state.Output = OutputDescription.Default;
        }
        else
        {
            var nativeFramebuffer = RequireNative<DirectX12Framebuffer>(framebuffer.NativeResource, "framebuffer");
            foreach (var colorTarget in nativeFramebuffer.ColorTargets)
            {
                EnsureTextureLive(colorTarget, "framebuffer color target");
                TransitionTexture(commandList, colorTarget, ResourceStates.RenderTarget, state);
            }
            if (nativeFramebuffer.DepthTarget is not null)
            {
                EnsureTextureLive(nativeFramebuffer.DepthTarget, "framebuffer depth target");
                TransitionTexture(commandList, nativeFramebuffer.DepthTarget, ResourceStates.DepthWrite, state);
            }

            var colorHandle = nativeFramebuffer.ColorDescriptors.CpuHandle;
            if (nativeFramebuffer.DepthDescriptor is null)
            {
                commandList->OMSetRenderTargets(
                    (uint)nativeFramebuffer.ColorTargets.Length,
                    &colorHandle,
                    true,
                    null);
            }
            else
            {
                var depthHandle = nativeFramebuffer.DepthDescriptor.CpuHandle;
                commandList->OMSetRenderTargets(
                    (uint)nativeFramebuffer.ColorTargets.Length,
                    &colorHandle,
                    true,
                    &depthHandle);
            }

            state.Framebuffer = nativeFramebuffer;
            state.IsSwapchainFramebuffer = false;
            state.FramebufferWidth = nativeFramebuffer.Width;
            state.FramebufferHeight = nativeFramebuffer.Height;
            state.Output = nativeFramebuffer.Output;
        }

        state.HasFramebuffer = true;
        if (state.Pipeline is not null)
            ValidatePipelineOutput(state.Pipeline, state.Output);

        foreach (var resourceSet in state.ResourceSets)
        {
            if (resourceSet is not null &&
                Array.Exists(resourceSet.Textures, texture => IsCurrentFramebufferAttachment(texture, state)))
            {
                throw new InvalidOperationException(
                    "A DirectX 12 framebuffer attachment cannot be sampled by the same draw state.");
            }
        }

        if (state.Pipeline is not null && !state.Pipeline.ScissorEnabled)
            RecordAndForceFullScissor(commandList, state);
    }

    private static void SetViewport(
        ID3D12GraphicsCommandList* commandList,
        uint index,
        Viewport viewport,
        DirectX12RecordingState state)
    {
        if (index >= state.Viewports.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (!float.IsFinite(viewport.X) || !float.IsFinite(viewport.Y) ||
            !float.IsFinite(viewport.Width) || !float.IsFinite(viewport.Height) ||
            !float.IsFinite(viewport.MinDepth) || !float.IsFinite(viewport.MaxDepth) ||
            viewport.Width <= 0 || viewport.Height <= 0 || viewport.MinDepth < 0 ||
            viewport.MaxDepth > 1 || viewport.MinDepth > viewport.MaxDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(viewport));
        }

        state.Viewports[index] = new DirectXViewport(
            viewport.X,
            viewport.Y,
            viewport.Width,
            viewport.Height,
            viewport.MinDepth,
            viewport.MaxDepth);
        state.ViewportSet[index] = true;
        state.ViewportCount = Math.Max(state.ViewportCount, checked((int)index + 1));

        for (var viewportIndex = 0; viewportIndex < state.ViewportCount; viewportIndex++)
        {
            if (!state.ViewportSet[viewportIndex])
                throw new InvalidOperationException("Viewport indices must be configured without gaps.");
        }

        fixed (DirectXViewport* viewports = state.Viewports)
            commandList->RSSetViewports((uint)state.ViewportCount, viewports);
    }

    private static void ClearColor(
        ID3D12GraphicsCommandList* commandList,
        uint index,
        RgbaFloat color,
        DirectX12RecordingState state)
    {
        RequireFramebuffer(state);
        var colorValues = stackalloc float[4] { color.R, color.G, color.B, color.A };
        CpuDescriptorHandle handle;

        if (state.IsSwapchainFramebuffer)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            handle = state.SwapchainRenderTarget;
        }
        else
        {
            if (index >= state.Framebuffer.ColorTargets.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            handle = state.Framebuffer.ColorDescriptors.GetCpuHandle(index);
        }

        commandList->ClearRenderTargetView(handle, colorValues, 0, null);
    }

    private static void ClearDepthStencil(
        ID3D12GraphicsCommandList* commandList,
        float depth,
        byte stencil,
        DirectX12RecordingState state)
    {
        RequireFramebuffer(state);
        if (state.IsSwapchainFramebuffer || state.Framebuffer.DepthDescriptor is null)
            throw new InvalidOperationException("The bound DirectX 12 framebuffer has no depth attachment to clear.");

        // The engine's depth formats all pair depth with stencil, so clear both aspects.
        commandList->ClearDepthStencilView(
            state.Framebuffer.DepthDescriptor.CpuHandle,
            ClearFlags.Depth | ClearFlags.Stencil,
            depth,
            stencil,
            0,
            null);
    }

    private static void SetScissor(
        ID3D12GraphicsCommandList* commandList,
        uint index,
        uint x,
        uint y,
        uint width,
        uint height,
        DirectX12RecordingState state)
    {
        if (index >= state.Scissors.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        state.Scissors[index] = new Box2D<int>(
            checked((int)x),
            checked((int)y),
            checked((int)((ulong)x + width)),
            checked((int)((ulong)y + height)));
        state.ScissorSet[index] = true;
        state.ScissorCount = Math.Max(state.ScissorCount, checked((int)index + 1));

        for (var scissorIndex = 0; scissorIndex < state.ScissorCount; scissorIndex++)
        {
            if (!state.ScissorSet[scissorIndex])
                throw new InvalidOperationException("Scissor indices must be configured without gaps.");
        }

        // D3D12 always honors the bound scissor rects, so while a scissor-disabled
        // pipeline is active only record the rects; they apply when scissor testing
        // is enabled again, matching Vulkan and Metal.
        if (state.Pipeline is null || state.Pipeline.ScissorEnabled)
            ApplyScissors(commandList, state);
    }

    private static void ApplyScissors(ID3D12GraphicsCommandList* commandList, DirectX12RecordingState state)
    {
        if (state.ScissorCount == 0)
            return;

        fixed (Box2D<int>* scissors = state.Scissors)
            commandList->RSSetScissorRects((uint)state.ScissorCount, scissors);
    }

    private static void ForceFullScissor(ID3D12GraphicsCommandList* commandList, DirectX12RecordingState state)
    {
        var full = new Box2D<int>(0, 0, checked((int)state.FramebufferWidth), checked((int)state.FramebufferHeight));
        var rects = stackalloc Box2D<int>[state.Scissors.Length];
        for (var index = 0; index < state.Scissors.Length; index++)
            rects[index] = full;

        commandList->RSSetScissorRects((uint)state.Scissors.Length, rects);
    }

    private static void RecordAndForceFullScissor(ID3D12GraphicsCommandList* commandList, DirectX12RecordingState state)
    {
        SetScissor(commandList, 0, 0, 0, state.FramebufferWidth, state.FramebufferHeight, state);
        ForceFullScissor(commandList, state);
    }

    private void BindVertexBuffer(
        ID3D12GraphicsCommandList* commandList,
        uint slot,
        DirectX12RecordingState state)
    {
        if (state.Pipeline is null || state.VertexBuffers[slot] is not { } buffer)
            return;
        if (slot >= state.Pipeline.VertexStrides.Length)
            throw new InvalidOperationException($"The active pipeline has no vertex layout for slot {slot}.");
        if ((buffer.Usage & BufferUsage.VertexBuffer) == 0)
            throw new InvalidOperationException("A buffer bound as a vertex buffer must have VertexBuffer usage.");

        TransitionBuffer(commandList, buffer, ResourceStates.VertexAndConstantBuffer, state);
        var view = new VertexBufferView(
            buffer.Resource->GetGPUVirtualAddress(),
            buffer.LogicalSize,
            state.Pipeline.VertexStrides[slot]);
        commandList->IASetVertexBuffers(slot, 1, &view);
    }

    private void BindAllVertexBuffers(ID3D12GraphicsCommandList* commandList, DirectX12RecordingState state)
    {
        for (uint slot = 0; slot < state.VertexBuffers.Length; slot++)
        {
            if (state.VertexBuffers[slot] is not null)
                BindVertexBuffer(commandList, slot, state);
        }
    }

    private void BindIndexBuffer(ID3D12GraphicsCommandList* commandList, DirectX12RecordingState state)
    {
        var buffer = state.IndexBuffer ?? throw new InvalidOperationException("No index buffer is bound.");
        if ((buffer.Usage & BufferUsage.IndexBuffer) == 0)
            throw new InvalidOperationException("A buffer bound as an index buffer must have IndexBuffer usage.");

        TransitionBuffer(commandList, buffer, ResourceStates.IndexBuffer, state);
        var view = new IndexBufferView(
            buffer.Resource->GetGPUVirtualAddress(),
            buffer.LogicalSize,
            state.IndexFormat switch
            {
                IndexFormat.UInt16 => Silk.NET.DXGI.Format.FormatR16Uint,
                IndexFormat.UInt32 => Silk.NET.DXGI.Format.FormatR32Uint,
                _ => throw new ArgumentOutOfRangeException(nameof(state.IndexFormat)),
            });
        commandList->IASetIndexBuffer(&view);
    }

    private void BindResourceSet(
        ID3D12GraphicsCommandList* commandList,
        uint slot,
        ResourceSet resourceSet,
        DirectX12RecordingState state)
    {
        var pipeline = state.Pipeline ?? throw new InvalidOperationException("Set a pipeline before binding resource sets.");
        if (slot >= pipeline.RootBindings.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));

        var nativeSet = RequireNative<DirectX12ResourceSet>(resourceSet.NativeResource, "resource set");
        var binding = pipeline.RootBindings[slot];
        if (!ReferenceEquals(nativeSet.Layout, pipeline.ResourceLayouts[slot]))
        {
            throw new ArgumentException(
                "The DirectX 12 resource set layout does not match the active pipeline slot.",
                nameof(resourceSet));
        }

        foreach (var texture in nativeSet.Textures)
        {
            EnsureTextureLive(texture, "resource-set texture");
            if (IsCurrentFramebufferAttachment(texture, state))
            {
                throw new InvalidOperationException(
                    "A DirectX 12 framebuffer attachment cannot be sampled by the same draw state.");
            }
        }

        for (var index = 0; index < nativeSet.Layout.Description.Elements.Length; index++)
        {
            var element = nativeSet.Layout.Description.Elements[index];
            var resource = nativeSet.Description.BoundResources[index];

            if (element.Kind == ResourceKind.UniformBuffer)
            {
                TransitionBuffer(
                    commandList,
                    RequireBuffer((DeviceBuffer)resource),
                    ResourceStates.VertexAndConstantBuffer,
                    state);
            }
            else if (element.Kind == ResourceKind.TextureReadOnly)
            {
                var texture = resource switch
                {
                    Texture directTexture => RequireTexture(directTexture),
                    TextureView textureView => RequireTexture(textureView.Target),
                    _ => throw new InvalidOperationException("The resource set contains an invalid texture resource."),
                };
                TransitionTexture(commandList, texture, ResourceStates.AllShaderResource, state);
            }
        }

        if (binding.ResourceParameter >= 0)
        {
            if (nativeSet.ResourceDescriptors is null)
                throw new InvalidOperationException("The resource set is missing its shader-resource descriptors.");
            commandList->SetGraphicsRootDescriptorTable(
                (uint)binding.ResourceParameter,
                nativeSet.ResourceDescriptors.GpuHandle);
        }

        if (binding.SamplerParameter >= 0)
        {
            if (nativeSet.SamplerDescriptors is null)
                throw new InvalidOperationException("The resource set is missing its sampler descriptors.");
            commandList->SetGraphicsRootDescriptorTable(
                (uint)binding.SamplerParameter,
                nativeSet.SamplerDescriptors.GpuHandle);
        }

        state.ResourceSets[slot] = nativeSet;
    }

    private void RecordBufferUpdate(
        ID3D12GraphicsCommandList* commandList,
        DirectX12Buffer destination,
        uint offset,
        ReadOnlySpan<byte> data,
        List<IDisposable> transients,
        ref DirectX12UploadArena uploadArena,
        DirectX12RecordingState state = null)
    {
        if ((ulong)offset + (uint)data.Length > destination.LogicalSize)
            throw new ArgumentOutOfRangeException(nameof(data), "The update exceeds the DirectX 12 buffer.");
        if (data.IsEmpty)
            return;

        var arenaOffset = AllocateUploadSpace(ref uploadArena, (ulong)data.Length, transients);
        data.CopyTo(new Span<byte>(uploadArena.Mapped + arenaOffset, data.Length));
        TransitionBuffer(commandList, destination, ResourceStates.CopyDest, state);
        commandList->CopyBufferRegion(destination.Resource, offset, uploadArena.Resource, arenaOffset, (ulong)data.Length);
        TransitionBuffer(commandList, destination, GetDefaultBufferState(destination), state);
    }

    private void RecordTextureCopy(
        ID3D12GraphicsCommandList* commandList,
        Texture source,
        Texture destination,
        DirectX12RecordingState state)
    {
        ValidateCopyCompatibility(source.Description, destination.Description);
        var nativeSource = RequireTexture(source);
        var nativeDestination = RequireTexture(destination);
        if (ReferenceEquals(nativeSource, nativeDestination))
            throw new ArgumentException("A DirectX 12 texture cannot be copied onto itself.", nameof(destination));

        TransitionTexture(commandList, nativeSource, ResourceStates.CopySource, state);
        TransitionTexture(commandList, nativeDestination, ResourceStates.CopyDest, state);
        commandList->CopyResource(nativeDestination.Resource, nativeSource.Resource);
        TransitionTexture(commandList, nativeSource, GetDefaultTextureState(nativeSource), state);
        TransitionTexture(commandList, nativeDestination, GetDefaultTextureState(nativeDestination), state);
    }

    private void ExecuteImmediateBufferUpdate(DirectX12Buffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        var recording = false;
        var originalState = buffer.State;

        try
        {
            BeginImmediateCommands();
            recording = true;
            var upload = EnsureImmediateUploadCapacity((ulong)data.Length);
            data.CopyTo(new Span<byte>(upload.Mapped, data.Length));
            TransitionBuffer(_immediateCommandList, buffer, ResourceStates.CopyDest);
            _immediateCommandList->CopyBufferRegion(buffer.Resource, offset, upload.Resource, 0, (ulong)data.Length);
            TransitionBuffer(_immediateCommandList, buffer, GetDefaultBufferState(buffer));
            EndImmediateCommands();
            recording = false;
        }
        catch
        {
            if (recording)
            {
                buffer.State = originalState;
                _immediateCommandList->Close();
            }
            throw;
        }
    }

    private void ExecuteImmediateTextureUpdate(
        DirectX12Texture texture,
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
        var format = DirectX12Conversions.ToDxgiFormat(texture.Description.Format);
        var bytesPerPixel = DirectX12Conversions.GetBytesPerPixel(texture.Description.Format);
        var sourceRowBytes = checked(width * bytesPerPixel);
        var rowPitch = checked((uint)Align(sourceRowBytes, 256));
        var uploadSize = Align(checked((ulong)rowPitch * height * depth), 512);
        var recording = false;
        var originalState = texture.State;

        try
        {
            BeginImmediateCommands();
            recording = true;
            var upload = EnsureImmediateUploadCapacity(uploadSize);
            WriteUploadTextureData(upload.Resource, data, sourceRowBytes, rowPitch, height, depth, 0);
            TransitionTexture(_immediateCommandList, texture, ResourceStates.CopyDest);
            var sourceLocation = new TextureCopyLocation(
                upload.Resource,
                TextureCopyType.PlacedFootprint,
                placedFootprint: new PlacedSubresourceFootprint(
                    0,
                    new SubresourceFootprint(format, width, height, depth, rowPitch)));
            var subresource = CalculateSubresource(texture.Description, mipLevel, arrayLayer);
            var destinationLocation = new TextureCopyLocation(
                texture.Resource,
                TextureCopyType.SubresourceIndex,
                subresourceIndex: subresource);
            _immediateCommandList->CopyTextureRegion(
                &destinationLocation,
                x,
                y,
                z,
                &sourceLocation,
                null);
            TransitionTexture(_immediateCommandList, texture, GetDefaultTextureState(texture));
            EndImmediateCommands();
            recording = false;
        }
        catch
        {
            if (recording)
            {
                texture.State = originalState;
                _immediateCommandList->Close();
            }
            throw;
        }
    }

    private void SynchronizeStagingReadback(DirectX12Texture texture)
        => SynchronizeStagingTexture(texture, uploadToTexture: false);

    private void SynchronizeStagingUpload(DirectX12Texture texture)
        => SynchronizeStagingTexture(texture, uploadToTexture: true);

    private void SynchronizeStagingTexture(DirectX12Texture texture, bool uploadToTexture)
    {
        var originalState = texture.State;
        var recording = false;

        try
        {
            BeginImmediateCommands();
            recording = true;
            TransitionTexture(
                _immediateCommandList,
                texture,
                uploadToTexture ? ResourceStates.CopyDest : ResourceStates.CopySource);

            for (uint subresource = 0; subresource < texture.StagingFootprints.Length; subresource++)
            {
                var textureLocation = new TextureCopyLocation(
                    texture.Resource,
                    TextureCopyType.SubresourceIndex,
                    subresourceIndex: subresource);
                var bufferLocation = new TextureCopyLocation(
                    uploadToTexture ? texture.StagingUpload : texture.StagingReadback,
                    TextureCopyType.PlacedFootprint,
                    placedFootprint: texture.StagingFootprints[subresource]);

                if (uploadToTexture)
                {
                    _immediateCommandList->CopyTextureRegion(
                        &textureLocation,
                        0,
                        0,
                        0,
                        &bufferLocation,
                        null);
                }
                else
                {
                    _immediateCommandList->CopyTextureRegion(
                        &bufferLocation,
                        0,
                        0,
                        0,
                        &textureLocation,
                        null);
                }
            }

            TransitionTexture(_immediateCommandList, texture, GetDefaultTextureState(texture));
            EndImmediateCommands();
            recording = false;
        }
        catch
        {
            if (recording)
            {
                texture.State = originalState;
                _immediateCommandList->Close();
            }
            throw;
        }
    }

    private static void WriteUploadTextureData(
        ID3D12Resource* upload,
        ReadOnlySpan<byte> data,
        uint sourceRowBytes,
        uint destinationRowPitch,
        uint height,
        uint depth,
        ulong destinationOffset,
        uint destinationSliceHeight = 0)
    {
        destinationSliceHeight = destinationSliceHeight == 0 ? height : destinationSliceHeight;
        var emptyReadRange = new DirectXRange(0, 0);
        void* mapped = null;
        ThrowIfFailed(upload->Map(0, &emptyReadRange, &mapped), "map a texture upload buffer");

        try
        {
            fixed (byte* source = data)
            {
                for (uint slice = 0; slice < depth; slice++)
                {
                    for (uint row = 0; row < height; row++)
                    {
                        var sourceOffset = checked(((ulong)slice * height + row) * sourceRowBytes);
                        var targetOffset = checked(
                            destinationOffset +
                            (ulong)slice * destinationSliceHeight * destinationRowPitch +
                            (ulong)row * destinationRowPitch);
                        Buffer.MemoryCopy(
                            source + sourceOffset,
                            (byte*)mapped + targetOffset,
                            sourceRowBytes,
                            sourceRowBytes);
                    }
                }
            }
        }
        finally
        {
            var writtenRange = new DirectXRange(
                (nuint)destinationOffset,
                (nuint)checked(destinationOffset + (ulong)destinationRowPitch * destinationSliceHeight * depth));
            upload->Unmap(0, &writtenRange);
        }
    }

    private static void CopyToUploadBuffer(ID3D12Resource* upload, ReadOnlySpan<byte> data)
    {
        var emptyReadRange = new DirectXRange(0, 0);
        void* destination = null;
        ThrowIfFailed(upload->Map(0, &emptyReadRange, &destination), "map a buffer upload resource");

        try
        {
            data.CopyTo(new Span<byte>(destination, data.Length));
        }
        finally
        {
            var writtenRange = new DirectXRange(0, (nuint)data.Length);
            upload->Unmap(0, &writtenRange);
        }
    }

    private void CopyStagingReadbackToUpload(DirectX12Texture texture)
    {
        var readRange = new DirectXRange(0, (nuint)texture.StagingSize);
        var noReadRange = new DirectXRange(0, 0);
        void* source = null;
        void* destination = null;
        ThrowIfFailed(texture.StagingReadback->Map(0, &readRange, &source), "map a staging readback resource");

        try
        {
            ThrowIfFailed(texture.StagingUpload->Map(0, &noReadRange, &destination), "map a staging upload resource");
            try
            {
                Buffer.MemoryCopy(source, destination, texture.StagingSize, texture.StagingSize);
            }
            finally
            {
                var writtenRange = new DirectXRange(0, (nuint)texture.StagingSize);
                texture.StagingUpload->Unmap(0, &writtenRange);
            }
        }
        finally
        {
            var noWriteRange = new DirectXRange(0, 0);
            texture.StagingReadback->Unmap(0, &noWriteRange);
        }
    }

    private void BeginImmediateCommands()
    {
        WaitForIdleCore();
        ThrowIfFailed(_immediateAllocator->Reset(), "reset the DirectX 12 upload command allocator");
        ThrowIfFailed(
            _immediateCommandList->Reset(_immediateAllocator, null),
            "reset the DirectX 12 upload command list");
    }

    private void EndImmediateCommands()
    {
        ThrowIfFailed(_immediateCommandList->Close(), "close the DirectX 12 upload command list");
        ExecuteCommandList(_immediateCommandList);
        WaitForFence(SignalFence());
    }

    private void ExecuteCommandList(ID3D12GraphicsCommandList* commandList)
    {
        var list = (ID3D12CommandList*)commandList;
        _queue->ExecuteCommandLists(1, &list);
    }

    private static void TransitionBuffer(
        ID3D12GraphicsCommandList* commandList,
        DirectX12Buffer buffer,
        ResourceStates destinationState,
        DirectX12RecordingState recordingState = null)
    {
        if (buffer.State == destinationState)
            return;

        recordingState?.OriginalBufferStates.TryAdd(buffer, buffer.State);

        var barrier = new ResourceBarrier(
            ResourceBarrierType.Transition,
            ResourceBarrierFlags.None,
            transition: new ResourceTransitionBarrier(
                buffer.Resource,
                uint.MaxValue,
                buffer.State,
                destinationState));
        commandList->ResourceBarrier(1, &barrier);
        buffer.State = destinationState;
    }

    private static void TransitionTexture(
        ID3D12GraphicsCommandList* commandList,
        DirectX12Texture texture,
        ResourceStates destinationState,
        DirectX12RecordingState recordingState = null)
    {
        if (texture.State == destinationState)
            return;

        recordingState?.OriginalTextureStates.TryAdd(texture, texture.State);

        var barrier = new ResourceBarrier(
            ResourceBarrierType.Transition,
            ResourceBarrierFlags.None,
            transition: new ResourceTransitionBarrier(
                texture.Resource,
                uint.MaxValue,
                texture.State,
                destinationState));
        commandList->ResourceBarrier(1, &barrier);
        texture.State = destinationState;
    }

    private void TransitionSwapchain(
        ID3D12GraphicsCommandList* commandList,
        ResourceStates destinationState,
        DirectX12RecordingState recordingState)
    {
        var index = checked((int)_frameIndex);
        var state = _swapchainStates[index];
        if (state == destinationState)
            return;

        if (!recordingState.SwapchainStateCaptured)
        {
            recordingState.SwapchainStateCaptured = true;
            recordingState.OriginalSwapchainState = state;
        }

        var barrier = new ResourceBarrier(
            ResourceBarrierType.Transition,
            ResourceBarrierFlags.None,
            transition: new ResourceTransitionBarrier(
                (ID3D12Resource*)_swapchainBuffers[index],
                uint.MaxValue,
                state,
                destinationState));
        commandList->ResourceBarrier(1, &barrier);
        _swapchainStates[index] = destinationState;
    }

    private void TransitionSwapchainToPresent(
        ID3D12GraphicsCommandList* commandList,
        DirectX12RecordingState recordingState)
    {
        if (_swapchainStates[_frameIndex] != ResourceStates.Present)
            TransitionSwapchain(commandList, ResourceStates.Present, recordingState);
    }

    private static ResourceStates GetDefaultBufferState(DirectX12Buffer buffer)
    {
        var state = ResourceStates.Common;
        if ((buffer.Usage & (BufferUsage.VertexBuffer | BufferUsage.UniformBuffer)) != 0)
            state |= ResourceStates.VertexAndConstantBuffer;
        if ((buffer.Usage & BufferUsage.IndexBuffer) != 0)
            state |= ResourceStates.IndexBuffer;
        return state;
    }

    private static ResourceStates GetDefaultTextureState(DirectX12Texture texture)
    {
        if ((texture.Description.Usage & TextureUsage.Sampled) != 0)
            return ResourceStates.AllShaderResource;
        if ((texture.Description.Usage & TextureUsage.DepthStencil) != 0)
            return ResourceStates.DepthWrite;
        if ((texture.Description.Usage & TextureUsage.RenderTarget) != 0)
            return ResourceStates.RenderTarget;
        return ResourceStates.Common;
    }

    private static uint CalculateSubresource(TextureDescription description, uint mipLevel, uint arrayLayer)
    {
        return description.Type == TextureType.Texture3D
            ? mipLevel
            : checked(mipLevel + arrayLayer * description.MipLevels);
    }

    private static void ValidateCopyCompatibility(TextureDescription source, TextureDescription destination)
    {
        if (source.Width != destination.Width || source.Height != destination.Height ||
            source.Depth != destination.Depth || source.MipLevels != destination.MipLevels ||
            source.ArrayLayers != destination.ArrayLayers || source.Format != destination.Format ||
            source.Type != destination.Type)
        {
            throw new ArgumentException("Texture copies require matching dimensions, mip levels, array layers, type, and format.");
        }
    }

    private static void RequireFramebuffer(DirectX12RecordingState state)
    {
        if (!state.HasFramebuffer)
            throw new InvalidOperationException("Set a framebuffer before issuing render commands.");
    }

    private void RequireDrawState(DirectX12RecordingState state, bool indexed)
    {
        RequireFramebuffer(state);
        var pipeline = state.Pipeline ?? throw new InvalidOperationException("Set a pipeline before drawing.");
        ValidatePipelineOutput(pipeline, state.Output);
        if (!state.ViewportSet[0])
            throw new InvalidOperationException("Set DirectX 12 viewport 0 before drawing.");
        if (indexed && state.IndexBuffer is null)
            throw new InvalidOperationException("Set an index buffer before indexed drawing.");

        for (var slot = 0; slot < pipeline.VertexStrides.Length; slot++)
        {
            if (state.VertexBuffers[slot] is null)
                throw new InvalidOperationException($"Set DirectX 12 vertex buffer slot {slot} before drawing.");
        }

        for (var slot = 0; slot < pipeline.ResourceLayouts.Length; slot++)
        {
            if (state.ResourceSets[slot] is null)
                throw new InvalidOperationException($"Set DirectX 12 resource set slot {slot} before drawing.");
        }

        if (pipeline.ScissorEnabled && !state.ScissorSet[0])
            throw new InvalidOperationException("Set DirectX 12 scissor rectangle 0 before drawing with scissor testing.");
    }

    private void RestoreRecordedStates(DirectX12RecordingState state)
    {
        foreach (var (buffer, resourceState) in state.OriginalBufferStates)
            buffer.State = resourceState;
        foreach (var (texture, resourceState) in state.OriginalTextureStates)
            texture.State = resourceState;
        if (state.SwapchainStateCaptured)
            _swapchainStates[_frameIndex] = state.OriginalSwapchainState;
    }

    private static void ValidatePipelineOutput(DirectX12Pipeline pipeline, OutputDescription output)
    {
        var pipelineColors = pipeline.Output.ColorAttachments ?? [];
        var framebufferColors = output.ColorAttachments ?? [];
        if (pipelineColors.Length != framebufferColors.Length ||
            pipeline.Output.DepthAttachment != output.DepthAttachment)
        {
            throw new InvalidOperationException("The DirectX 12 pipeline output does not match the active framebuffer.");
        }

        for (var index = 0; index < pipelineColors.Length; index++)
        {
            if (pipelineColors[index] != framebufferColors[index])
            {
                throw new InvalidOperationException(
                    "The DirectX 12 pipeline output does not match the active framebuffer.");
            }
        }
    }

    private bool IsCurrentFramebufferAttachment(DirectX12Texture texture, DirectX12RecordingState state)
    {
        if (!state.HasFramebuffer || state.IsSwapchainFramebuffer || state.Framebuffer is null)
            return false;

        foreach (var colorTarget in state.Framebuffer.ColorTargets)
        {
            if (ReferenceEquals(texture, colorTarget))
                return true;
        }

        return ReferenceEquals(texture, state.Framebuffer.DepthTarget);
    }

    private void EnsureTextureLive(DirectX12Texture texture, string resourceName)
    {
        if (texture.Resource == null || !_liveResources.Contains(texture))
            throw new InvalidOperationException($"The {resourceName} is no longer live on this DirectX 12 device.");
    }

    private sealed class DirectX12RecordingState
    {
        public readonly DirectX12Buffer[] VertexBuffers = new DirectX12Buffer[16];
        public readonly DirectX12ResourceSet[] ResourceSets = new DirectX12ResourceSet[16];
        public readonly DirectXViewport[] Viewports = new DirectXViewport[16];
        public readonly bool[] ViewportSet = new bool[16];
        public readonly Box2D<int>[] Scissors = new Box2D<int>[16];
        public readonly bool[] ScissorSet = new bool[16];
        public readonly Dictionary<DirectX12Buffer, ResourceStates> OriginalBufferStates = [];
        public readonly Dictionary<DirectX12Texture, ResourceStates> OriginalTextureStates = [];
        public DirectX12Buffer IndexBuffer;
        public IndexFormat IndexFormat;
        public DirectX12Pipeline Pipeline;
        public DirectX12Framebuffer Framebuffer;
        public bool IsSwapchainFramebuffer;
        public bool HasFramebuffer;
        public uint FramebufferWidth;
        public uint FramebufferHeight;
        public OutputDescription Output;
        public int ViewportCount;
        public int ScissorCount;
        public CpuDescriptorHandle SwapchainRenderTarget;
        public bool SwapchainStateCaptured;
        public ResourceStates OriginalSwapchainState;
    }
}
