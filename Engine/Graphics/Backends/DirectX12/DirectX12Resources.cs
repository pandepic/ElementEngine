using System;
using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace ElementEngine;

internal unsafe sealed class DirectX12DescriptorAllocator : IDisposable
{
    private readonly object _lock = new();
    private readonly uint _capacity;
    private readonly uint _increment;
    private readonly List<FreeDescriptorRange> _freeRanges = [];
    private ID3D12DescriptorHeap* _heap;
    private bool _disposed;

    public ID3D12DescriptorHeap* Heap => _heap;
    public DescriptorHeapType Type { get; }
    public bool ShaderVisible { get; }

    public DirectX12DescriptorAllocator(
        ID3D12Device* device,
        DescriptorHeapType type,
        uint capacity,
        bool shaderVisible)
    {
        Type = type;
        ShaderVisible = shaderVisible;
        _capacity = capacity;
        _increment = device->GetDescriptorHandleIncrementSize(type);

        var description = new DescriptorHeapDesc(
            type,
            capacity,
            shaderVisible ? DescriptorHeapFlags.ShaderVisible : DescriptorHeapFlags.None,
            0);
        var descriptorHeapId = ID3D12DescriptorHeap.Guid;
        ID3D12DescriptorHeap* heap = null;
        DirectX12Backend.ThrowIfFailed(
            device->CreateDescriptorHeap(&description, &descriptorHeapId, (void**)&heap),
            $"create the {type} descriptor heap");
        _heap = heap;
        _freeRanges.Add(new FreeDescriptorRange(0, capacity));
    }

    public DirectX12DescriptorAllocation Allocate(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        lock (_lock)
        {
            for (var i = 0; i < _freeRanges.Count; i++)
            {
                var freeRange = _freeRanges[i];
                if (freeRange.Count < count)
                    continue;

                var index = freeRange.Index;
                if (freeRange.Count == count)
                    _freeRanges.RemoveAt(i);
                else
                    _freeRanges[i] = new FreeDescriptorRange(index + count, freeRange.Count - count);

                return new DirectX12DescriptorAllocation(this, index, count, GetCpuHandle(index), GetGpuHandle(index));
            }
        }

        throw new InvalidOperationException($"The DirectX 12 {Type} descriptor heap is exhausted ({_capacity} descriptors).");
    }

    internal void Free(uint index, uint count)
    {
        lock (_lock)
        {
            var insertIndex = 0;
            while (insertIndex < _freeRanges.Count && _freeRanges[insertIndex].Index < index)
                insertIndex++;

            _freeRanges.Insert(insertIndex, new FreeDescriptorRange(index, count));

            for (var i = Math.Max(0, insertIndex - 1); i < _freeRanges.Count - 1;)
            {
                var current = _freeRanges[i];
                var next = _freeRanges[i + 1];
                if (current.Index + current.Count != next.Index)
                {
                    i++;
                    continue;
                }

                _freeRanges[i] = new FreeDescriptorRange(current.Index, current.Count + next.Count);
                _freeRanges.RemoveAt(i + 1);
            }
        }
    }

    public CpuDescriptorHandle GetCpuHandle(uint index)
    {
        var handle = _heap->GetCPUDescriptorHandleForHeapStart();
        handle.Ptr += (nuint)(index * _increment);
        return handle;
    }

    public GpuDescriptorHandle GetGpuHandle(uint index)
    {
        if (!ShaderVisible)
            return default;

        var handle = _heap->GetGPUDescriptorHandleForHeapStart();
        handle.Ptr += (ulong)index * _increment;
        return handle;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_heap != null)
        {
            _heap->Release();
            _heap = null;
        }

        _freeRanges.Clear();
        _disposed = true;
    }

    private readonly record struct FreeDescriptorRange(uint Index, uint Count);
}

internal sealed class DirectX12DescriptorAllocation : IDisposable
{
    private DirectX12DescriptorAllocator _owner;

    public uint Index { get; }
    public uint Count { get; }
    public CpuDescriptorHandle CpuHandle { get; }
    public GpuDescriptorHandle GpuHandle { get; }

    public DirectX12DescriptorAllocation(
        DirectX12DescriptorAllocator owner,
        uint index,
        uint count,
        CpuDescriptorHandle cpuHandle,
        GpuDescriptorHandle gpuHandle)
    {
        _owner = owner;
        Index = index;
        Count = count;
        CpuHandle = cpuHandle;
        GpuHandle = gpuHandle;
    }

    public CpuDescriptorHandle GetCpuHandle(uint offset)
    {
        if (offset >= Count)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return _owner.GetCpuHandle(Index + offset);
    }

    public GpuDescriptorHandle GetGpuHandle(uint offset)
    {
        if (offset >= Count)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return _owner.GetGpuHandle(Index + offset);
    }

    public void Dispose()
    {
        var owner = _owner;
        if (owner is null)
            return;

        _owner = null;
        owner.Free(Index, Count);
    }
}

internal unsafe sealed class DirectX12Buffer : IDisposable
{
    public ID3D12Resource* Resource;
    public ResourceStates State;
    public readonly uint LogicalSize;
    public readonly ulong AllocationSize;
    public readonly BufferUsage Usage;

    public DirectX12Buffer(
        ID3D12Resource* resource,
        ResourceStates state,
        uint logicalSize,
        ulong allocationSize,
        BufferUsage usage)
    {
        Resource = resource;
        State = state;
        LogicalSize = logicalSize;
        AllocationSize = allocationSize;
        Usage = usage;
    }

    public void Dispose()
    {
        if (Resource == null)
            return;

        Resource->Release();
        Resource = null;
    }
}

internal unsafe sealed class DirectX12Texture : IDisposable
{
    public ID3D12Resource* Resource;
    public ID3D12Resource* StagingUpload;
    public ID3D12Resource* StagingReadback;
    public ResourceStates State;
    public readonly ResourceDesc ResourceDescription;
    public readonly TextureDescription Description;
    public readonly PlacedSubresourceFootprint[] StagingFootprints;
    public readonly uint[] StagingRowCounts;
    public readonly ulong StagingSize;
    public MapMode? MappedMode;
    public void* MappedPointer;

    public bool IsStaging => (Description.Usage & TextureUsage.Staging) != 0;

    public DirectX12Texture(
        ID3D12Resource* resource,
        ResourceStates state,
        ResourceDesc resourceDescription,
        TextureDescription description)
    {
        Resource = resource;
        State = state;
        ResourceDescription = resourceDescription;
        Description = description;
    }

    public DirectX12Texture(
        ID3D12Resource* resource,
        ResourceStates state,
        ID3D12Resource* stagingUpload,
        ID3D12Resource* stagingReadback,
        ResourceDesc resourceDescription,
        TextureDescription description,
        PlacedSubresourceFootprint[] stagingFootprints,
        uint[] stagingRowCounts,
        ulong stagingSize)
    {
        Resource = resource;
        State = state;
        StagingUpload = stagingUpload;
        StagingReadback = stagingReadback;
        ResourceDescription = resourceDescription;
        Description = description;
        StagingFootprints = stagingFootprints;
        StagingRowCounts = stagingRowCounts;
        StagingSize = stagingSize;
    }

    public void Dispose()
    {
        if (MappedPointer != null)
        {
            var resource = MappedMode == MapMode.Read ? StagingReadback : StagingUpload;
            resource->Unmap(0, null);
            MappedPointer = null;
            MappedMode = null;
        }

        if (Resource != null)
        {
            Resource->Release();
            Resource = null;
        }

        if (StagingUpload != null)
        {
            StagingUpload->Release();
            StagingUpload = null;
        }

        if (StagingReadback != null)
        {
            StagingReadback->Release();
            StagingReadback = null;
        }
    }
}

internal sealed class DirectX12TextureView : IDisposable
{
    public DirectX12DescriptorAllocation Descriptor { get; }

    public DirectX12TextureView(DirectX12DescriptorAllocation descriptor)
    {
        Descriptor = descriptor;
    }

    public void Dispose() => Descriptor.Dispose();
}

internal sealed class DirectX12Sampler : IDisposable
{
    public DirectX12DescriptorAllocation Descriptor { get; }

    public DirectX12Sampler(DirectX12DescriptorAllocation descriptor)
    {
        Descriptor = descriptor;
    }

    public void Dispose() => Descriptor.Dispose();
}

internal sealed class DirectX12Shader : IDisposable
{
    public ShaderStages Stage { get; }
    public byte[] Bytecode { get; }

    public DirectX12Shader(ShaderStages stage, byte[] bytecode)
    {
        Stage = stage;
        Bytecode = bytecode;
    }

    public void Dispose()
    {
    }
}

internal sealed class DirectX12ResourceLayout : IDisposable
{
    public ResourceLayoutDescription Description { get; }

    public DirectX12ResourceLayout(ResourceLayoutDescription description)
    {
        Description = description;
    }

    public void Dispose()
    {
    }
}

internal sealed class DirectX12ResourceSet : IDisposable
{
    public DirectX12DescriptorAllocation ResourceDescriptors { get; }
    public DirectX12DescriptorAllocation SamplerDescriptors { get; }
    public DirectX12ResourceLayout Layout { get; }
    public DirectX12Texture[] Textures { get; }
    public ResourceSetDescription Description { get; }

    public DirectX12ResourceSet(
        ResourceSetDescription description,
        DirectX12ResourceLayout layout,
        DirectX12Texture[] textures,
        DirectX12DescriptorAllocation resourceDescriptors,
        DirectX12DescriptorAllocation samplerDescriptors)
    {
        Description = description;
        Layout = layout;
        Textures = textures;
        ResourceDescriptors = resourceDescriptors;
        SamplerDescriptors = samplerDescriptors;
    }

    public void Dispose()
    {
        ResourceDescriptors?.Dispose();
        SamplerDescriptors?.Dispose();
    }
}

internal readonly record struct DirectX12RootBinding(int ResourceParameter, int SamplerParameter);

internal unsafe sealed class DirectX12Pipeline : IDisposable
{
    public ID3D12PipelineState* PipelineState;
    public ID3D12RootSignature* RootSignature;
    public D3DPrimitiveTopology PrimitiveTopology { get; }
    public uint[] VertexStrides { get; }
    public DirectX12RootBinding[] RootBindings { get; }
    public DirectX12ResourceLayout[] ResourceLayouts { get; }
    public OutputDescription Output { get; }
    public bool ScissorEnabled { get; }

    public DirectX12Pipeline(
        ID3D12PipelineState* pipelineState,
        ID3D12RootSignature* rootSignature,
        D3DPrimitiveTopology primitiveTopology,
        uint[] vertexStrides,
        DirectX12RootBinding[] rootBindings,
        DirectX12ResourceLayout[] resourceLayouts,
        OutputDescription output,
        bool scissorEnabled)
    {
        PipelineState = pipelineState;
        RootSignature = rootSignature;
        PrimitiveTopology = primitiveTopology;
        VertexStrides = vertexStrides;
        RootBindings = rootBindings;
        ResourceLayouts = resourceLayouts;
        Output = output;
        ScissorEnabled = scissorEnabled;
    }

    public void Dispose()
    {
        if (PipelineState != null)
        {
            PipelineState->Release();
            PipelineState = null;
        }

        if (RootSignature != null)
        {
            RootSignature->Release();
            RootSignature = null;
        }
    }
}

internal unsafe sealed class DirectX12Framebuffer : IDisposable
{
    public DirectX12DescriptorAllocation ColorDescriptors { get; }
    public DirectX12DescriptorAllocation DepthDescriptor { get; }
    public DirectX12Texture[] ColorTargets { get; }
    public DirectX12Texture DepthTarget { get; }
    public OutputDescription Output { get; }
    public uint Width { get; }
    public uint Height { get; }

    public DirectX12Framebuffer(
        DirectX12DescriptorAllocation colorDescriptors,
        DirectX12DescriptorAllocation depthDescriptor,
        DirectX12Texture[] colorTargets,
        DirectX12Texture depthTarget,
        OutputDescription output,
        uint width,
        uint height)
    {
        ColorDescriptors = colorDescriptors;
        DepthDescriptor = depthDescriptor;
        ColorTargets = colorTargets;
        DepthTarget = depthTarget;
        Output = output;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        ColorDescriptors?.Dispose();
        DepthDescriptor?.Dispose();
    }
}

internal unsafe sealed class DirectX12TransientResource : IDisposable
{
    private ID3D12Resource* _resource;

    public DirectX12TransientResource(ID3D12Resource* resource)
    {
        _resource = resource;
    }

    public void Dispose()
    {
        if (_resource == null)
            return;

        _resource->Release();
        _resource = null;
    }
}

// A persistently mapped upload heap suballocated by offset; frame arenas reset when
// their frame's fence is waited, the immediate arena resets per immediate operation.
internal unsafe sealed class DirectX12UploadArena : IDisposable
{
    public ID3D12Resource* Resource;
    public byte* Mapped;
    public ulong Capacity;
    public ulong Offset;

    public void Dispose()
    {
        if (Resource == null)
            return;

        Resource->Unmap(0, null);
        Resource->Release();
        Resource = null;
        Mapped = null;
    }
}
