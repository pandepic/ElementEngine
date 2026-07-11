using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal abstract record GraphicsCommand;
internal sealed record SetFramebufferCommand(Framebuffer Framebuffer) : GraphicsCommand;
internal sealed record SetViewportCommand(uint Index, Viewport Viewport) : GraphicsCommand;
internal sealed record ClearColorTargetCommand(uint Index, RgbaFloat Color) : GraphicsCommand;
internal sealed record ClearDepthStencilCommand(float Depth, byte Stencil) : GraphicsCommand;
internal sealed record SetScissorRectCommand(uint Index, uint X, uint Y, uint Width, uint Height) : GraphicsCommand;
internal sealed record SetFullScissorRectCommand(uint Index) : GraphicsCommand;
internal sealed record UpdateBufferCommand(DeviceBuffer Buffer, uint Offset, ReadOnlyMemory<byte> Data) : GraphicsCommand;
internal sealed record CopyTextureCommand(Texture Source, Texture Destination) : GraphicsCommand;
internal sealed record SetVertexBufferCommand(uint Index, DeviceBuffer Buffer) : GraphicsCommand;
internal sealed record SetIndexBufferCommand(DeviceBuffer Buffer, IndexFormat Format) : GraphicsCommand;
internal sealed record SetPipelineCommand(Pipeline Pipeline) : GraphicsCommand;
internal sealed record SetGraphicsResourceSetCommand(uint Slot, ResourceSet ResourceSet) : GraphicsCommand;
internal sealed record DrawCommand(uint VertexCount) : GraphicsCommand;
internal sealed record DrawIndexedCommand(
    uint IndexCount,
    uint InstanceCount,
    uint IndexStart,
    int VertexOffset,
    uint InstanceStart) : GraphicsCommand;

public sealed class CommandList : IDisposable
{
    private bool _recording;
    private bool _disposed;
    private readonly List<GraphicsCommand> _commands = [];
    private byte[] _uploadArena = [];
    private int _uploadArenaOffset;

    internal IReadOnlyList<GraphicsCommand> Commands => _commands;

    public void Begin()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_recording)
            throw new InvalidOperationException("The command list is already recording.");

        _commands.Clear();
        _uploadArenaOffset = 0;
        _recording = true;
    }

    public void End()
    {
        RequireRecording();
        _recording = false;
    }

    public void SetFramebuffer(Framebuffer framebuffer)
    {
        ArgumentNullException.ThrowIfNull(framebuffer);
        Add(new SetFramebufferCommand(framebuffer));
    }

    public void SetViewport(uint index, Viewport viewport)
    {
        RequirePrimaryIndex(index);
        Add(new SetViewportCommand(index, viewport));
    }

    public void ClearColorTarget(uint index, RgbaFloat color) => Add(new ClearColorTargetCommand(index, color));

    public void ClearDepthStencil(float depth, byte stencil = 0)
    {
        if (!(depth >= 0f && depth <= 1f))
            throw new ArgumentOutOfRangeException(nameof(depth), "The clear depth must be between 0 and 1.");

        Add(new ClearDepthStencilCommand(depth, stencil));
    }

    public void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
    {
        RequirePrimaryIndex(index);
        Add(new SetScissorRectCommand(index, x, y, width, height));
    }

    public void SetFullScissorRect(uint index)
    {
        RequirePrimaryIndex(index);
        Add(new SetFullScissorRectCommand(index));
    }

    // Vulkan (without the multiViewport feature) and Metal expose one viewport/scissor
    // slot, so the portable contract validates here instead of crashing per backend.
    private static void RequirePrimaryIndex(uint index)
    {
        if (index != 0)
            throw new NotSupportedException("The graphics contract currently supports a single viewport and scissor rectangle (index 0).");
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, T value) where T : unmanaged
    {
        var values = MemoryMarshal.CreateReadOnlySpan(ref value, 1);
        AddUpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(values));
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, T[] values) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(values);
        AddUpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(values.AsSpan()));
    }

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, ReadOnlySpan<T> values) where T : unmanaged
        => AddUpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(values));

    public void UpdateBuffer<T>(DeviceBuffer buffer, uint offset, ref T source, uint sizeInBytes)
        where T : unmanaged
    {
        // A byref-backed span stays GC-tracked; a raw pointer from Unsafe.AsPointer would
        // dangle if the GC relocated the source (typically a heap array element) mid-copy.
        var data = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref source), checked((int)sizeInBytes));
        AddUpdateBuffer(buffer, offset, data);
    }

    public unsafe void UpdateBuffer(DeviceBuffer buffer, uint offset, nint source, uint sizeInBytes)
    {
        var data = new ReadOnlySpan<byte>(source.ToPointer(), checked((int)sizeInBytes));
        AddUpdateBuffer(buffer, offset, data);
    }

    public void CopyTexture(Texture source, Texture destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        Add(new CopyTextureCommand(source, destination));
    }

    public void SetVertexBuffer(uint index, DeviceBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Add(new SetVertexBufferCommand(index, buffer));
    }

    public void SetIndexBuffer(DeviceBuffer buffer, IndexFormat format)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Add(new SetIndexBufferCommand(buffer, format));
    }

    public void SetPipeline(Pipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        Add(new SetPipelineCommand(pipeline));
    }

    public void SetGraphicsResourceSet(uint slot, ResourceSet resourceSet)
    {
        ArgumentNullException.ThrowIfNull(resourceSet);
        Add(new SetGraphicsResourceSetCommand(slot, resourceSet));
    }

    public void Draw(uint vertexCount) => Add(new DrawCommand(vertexCount));

    public void DrawIndexed(uint indexCount)
        => DrawIndexed(indexCount, 1, 0, 0, 0);

    public void DrawIndexed(
        uint indexCount,
        uint instanceCount,
        uint indexStart,
        int vertexOffset,
        uint instanceStart)
        => Add(new DrawIndexedCommand(indexCount, instanceCount, indexStart, vertexOffset, instanceStart));

    public void Dispose()
    {
        _commands.Clear();
        _recording = false;
        _disposed = true;
    }

    internal void ValidateForSubmission(GraphicsDevice graphicsDevice)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_recording)
            throw new InvalidOperationException("End the command list before submitting it.");

        foreach (var command in _commands)
            graphicsDevice.ValidateCommandResources(command);
    }

    private void AddUpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if ((ulong)offset + (uint)data.Length > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(data), "The update exceeds the destination buffer.");

        Add(new UpdateBufferCommand(buffer, offset, CopyToUploadArena(data)));
    }

    // Payloads are staged in a reusable arena that Begin() resets, so per-frame buffer
    // updates allocate nothing steady-state. Backends copy the data into native staging
    // during Submit, after which the arena is free to be reused.
    private ReadOnlyMemory<byte> CopyToUploadArena(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return ReadOnlyMemory<byte>.Empty;

        if (_uploadArenaOffset + data.Length > _uploadArena.Length)
        {
            // Older payloads keep referencing the previous array, which stays valid.
            var newCapacity = Math.Max(Math.Max(4096, data.Length), _uploadArena.Length * 2);
            _uploadArena = new byte[newCapacity];
            _uploadArenaOffset = 0;
        }

        data.CopyTo(_uploadArena.AsSpan(_uploadArenaOffset));
        var payload = new ReadOnlyMemory<byte>(_uploadArena, _uploadArenaOffset, data.Length);
        _uploadArenaOffset += data.Length;
        return payload;
    }

    private void Add(GraphicsCommand command)
    {
        RequireRecording();
        _commands.Add(command);
    }

    private void RequireRecording()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_recording)
            throw new InvalidOperationException("Begin the command list before recording commands.");
    }
}
