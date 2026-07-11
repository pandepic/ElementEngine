using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D3D12Api = Silk.NET.Direct3D12.D3D12;
using DirectXRange = Silk.NET.Direct3D12.Range;
using DXGIFormat = Silk.NET.DXGI.Format;
using DXGIApi = Silk.NET.DXGI.DXGI;

namespace ElementEngine;

internal unsafe sealed partial class DirectX12Backend : IGraphicsBackend
{
    private const int FrameCount = 3;
    private const uint ShaderResourceDescriptorCapacity = 16384;
    private const uint SamplerDescriptorCapacity = 2048;
    private const uint RenderTargetDescriptorCapacity = 4096;
    private const uint DepthDescriptorCapacity = 1024;
    private const ulong MaximumConstantBufferSize = 64 * 1024;
    private const uint DxgiUsageRenderTargetOutput = 0x20;
    private const uint DxgiMakeWindowAssociationNoAltEnter = 2;
    private const int DxgiErrorNotFound = unchecked((int)0x887A0002);

    private readonly object _sync = new();
    private readonly GameWindow _window;
    private readonly GraphicsDeviceOptions _options;
    private readonly D3D12Api _d3d12;
    private readonly DXGIApi _dxgi;
    private readonly AutoResetEvent _fenceEvent = new(false);
    private readonly nint[] _frameAllocators = new nint[FrameCount];
    private readonly ulong[] _frameFenceValues = new ulong[FrameCount];
    private readonly List<IDisposable>[] _frameTransients = new List<IDisposable>[FrameCount];
    private readonly DirectX12UploadArena[] _frameUploadArenas = new DirectX12UploadArena[FrameCount];
    private readonly nint[] _swapchainBuffers = new nint[FrameCount];
    private readonly ResourceStates[] _swapchainStates = new ResourceStates[FrameCount];
    private readonly HashSet<IDisposable> _liveResources = [];

    // Fence values are monotonic, so a resource deferred with the last signaled value
    // is safe to destroy once the fence has completed past it.
    private readonly List<(ulong FenceValue, IDisposable NativeResource)> _deferredDestructions = [];
    private DirectX12UploadArena _immediateUploadArena;

    private IDXGIFactory4* _factory;
    private IDXGIAdapter1* _adapter;
    private ID3D12Device* _device;
    private ID3D12CommandQueue* _queue;
    private IDXGISwapChain3* _swapchain;
    private ID3D12GraphicsCommandList* _commandList;
    private ID3D12CommandAllocator* _immediateAllocator;
    private ID3D12GraphicsCommandList* _immediateCommandList;
    private ID3D12Fence* _fence;
    private DirectX12DescriptorAllocator _shaderResourceDescriptors;
    private DirectX12DescriptorAllocator _samplerDescriptors;
    private DirectX12DescriptorAllocator _renderTargetDescriptors;
    private DirectX12DescriptorAllocator _depthDescriptors;
    private DirectX12DescriptorAllocation _swapchainRenderTargets;
    private DirectX12ShaderCompiler _shaderCompiler;
    private ulong _nextFenceValue = 1;
    private uint _frameIndex;
    private uint _swapchainWidth;
    private uint _swapchainHeight;
    private bool _disposed;

    public DirectX12Backend(GameWindow window, GraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DirectX 12 is only available on Windows.");
        if (window.NativeKind != NativeWindowKind.Win32 || window.NativeHandle == 0)
            throw new ArgumentException("DirectX 12 requires a valid Win32 window handle.", nameof(window));

        _window = window;
        _options = options;
        _d3d12 = D3D12Api.GetApi();
#pragma warning disable CS0618 // ElementEngine owns the Win32 window and selects native DXGI without Silk.NET.Windowing.
        _dxgi = DXGIApi.GetApi();
#pragma warning restore CS0618

        for (var index = 0; index < FrameCount; index++)
            _frameTransients[index] = [];

        try
        {
            EnableDebugLayer();
            CreateFactoryAndDevice();
            CreateCommandInfrastructure();
            CreateDescriptorAllocators();
            CreateSwapchain(
                checked((uint)Math.Max(1, window.FramebufferWidth)),
                checked((uint)Math.Max(1, window.FramebufferHeight)));
            _shaderCompiler = new DirectX12ShaderCompiler(options.Debug);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public GraphicsBackend BackendType => GraphicsBackend.DirectX12;
    public bool IsUvOriginTopLeft => true;
    public OutputDescription SwapchainOutputDescription => OutputDescription.Default;

    public object CreateBuffer(BufferDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var allocationSize = (description.Usage & BufferUsage.UniformBuffer) != 0
            ? Align(description.SizeInBytes, 256)
            : description.SizeInBytes;
        if ((description.Usage & BufferUsage.UniformBuffer) != 0 && allocationSize > MaximumConstantBufferSize)
        {
            throw new NotSupportedException(
                $"DirectX 12 constant buffers cannot exceed {MaximumConstantBufferSize} bytes after alignment.");
        }

        var resource = CreateBufferResource(allocationSize, HeapType.Default, ResourceStates.Common);
        return TrackResource(
            new DirectX12Buffer(resource, ResourceStates.Common, description.SizeInBytes, allocationSize, description.Usage));
    }

    public object CreateTexture(TextureDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateTextureDescription(description);

        var resourceDescription = CreateTextureResourceDescription(description);
        if ((description.Usage & TextureUsage.Staging) != 0)
            return TrackResource(CreateStagingTexture(description, resourceDescription));

        ID3D12Resource* resource = null;
        var heap = new HeapProperties(HeapType.Default);
        var initialState = ResourceStates.Common;
        ClearValue clearValue = default;
        ClearValue* clearValuePointer = null;

        if ((description.Usage & TextureUsage.DepthStencil) != 0)
        {
            clearValue = new ClearValue(
                DirectX12Conversions.ToDxgiFormat(description.Format),
                depthStencil: new DepthStencilValue(1f, 0));
            clearValuePointer = &clearValue;
        }

        var resourceId = ID3D12Resource.Guid;
        ThrowIfFailed(
            _device->CreateCommittedResource(
                &heap,
                HeapFlags.None,
                &resourceDescription,
                initialState,
                clearValuePointer,
                &resourceId,
                (void**)&resource),
            "create a texture resource");

        return TrackResource(new DirectX12Texture(resource, initialState, resourceDescription, description));
    }

    public object CreateTextureView(Texture texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);

        var nativeTexture = RequireTexture(texture);
        if (nativeTexture.IsStaging)
            throw new InvalidOperationException("Staging textures cannot have shader-resource views.");
        if ((texture.Usage & TextureUsage.Sampled) == 0)
            throw new ArgumentException("A DirectX 12 texture view requires Sampled usage.", nameof(texture));

        var descriptor = _shaderResourceDescriptors.Allocate(1);
        CreateShaderResourceView(nativeTexture, descriptor.CpuHandle);
        return TrackResource(new DirectX12TextureView(descriptor));
    }

    public object CreateSampler(SamplerDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var filter = description.Filter switch
        {
            SamplerFilter.Point => Filter.MinMagMipPoint,
            SamplerFilter.Linear => Filter.MinMagMipLinear,
            SamplerFilter.Anisotropic => Filter.Anisotropic,
            _ => throw new ArgumentOutOfRangeException(nameof(description)),
        };
        var descriptor = _samplerDescriptors.Allocate(1);
        var samplerDescription = new SamplerDesc(
            filter,
            TextureAddressMode.Wrap,
            TextureAddressMode.Wrap,
            TextureAddressMode.Wrap,
            0,
            Math.Clamp(description.MaximumAnisotropy, 1, 16),
            ComparisonFunc.Always,
            0,
            float.MaxValue);
        _device->CreateSampler(&samplerDescription, descriptor.CpuHandle);
        return TrackResource(new DirectX12Sampler(descriptor));
    }

    public object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(descriptions);

        var shaders = new object[descriptions.Length];

        try
        {
            for (var index = 0; index < descriptions.Length; index++)
                shaders[index] = TrackResource(_shaderCompiler.Compile(descriptions[index], options));

            return shaders;
        }
        catch
        {
            foreach (var shader in shaders)
            {
                if (shader is IDisposable disposable)
                    DestroyNativeResource(disposable);
            }
            throw;
        }
    }

    public object CreateResourceLayout(ResourceLayoutDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (description.Elements is null)
            throw new ArgumentException("A resource layout must provide elements.", nameof(description));

        foreach (var element in description.Elements)
        {
            if (element.Stages == ShaderStages.None ||
                (element.Stages & ~(ShaderStages.Vertex | ShaderStages.Fragment)) != 0)
            {
                throw new ArgumentException(
                    "DirectX 12 resource layout elements require at least one supported shader stage.",
                    nameof(description));
            }
        }

        return TrackResource(
            new DirectX12ResourceLayout(
                new ResourceLayoutDescription(description.Elements.ToArray())));
    }

    public object CreateResourceSet(ResourceSetDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var layout = RequireNative<DirectX12ResourceLayout>(description.Layout.NativeResource, "resource layout");
        if (description.BoundResources is null || description.BoundResources.Length != layout.Description.Elements.Length)
            throw new ArgumentException("The resource count does not match the DirectX 12 layout.", nameof(description));

        var nativeDescription = new ResourceSetDescription(
            description.Layout,
            description.BoundResources.ToArray());
        var resourceCount = 0u;
        var samplerCount = 0u;

        foreach (var element in layout.Description.Elements)
        {
            if (element.Kind == ResourceKind.Sampler)
                samplerCount++;
            else
                resourceCount++;
        }

        DirectX12DescriptorAllocation resources = null;
        DirectX12DescriptorAllocation samplers = null;
        var textures = new List<DirectX12Texture>();

        try
        {
            if (resourceCount > 0)
                resources = _shaderResourceDescriptors.Allocate(resourceCount);
            if (samplerCount > 0)
                samplers = _samplerDescriptors.Allocate(samplerCount);

            var resourceIndex = 0u;
            var samplerIndex = 0u;

            for (var index = 0; index < layout.Description.Elements.Length; index++)
            {
                var element = layout.Description.Elements[index];
                var boundResource = nativeDescription.BoundResources[index];

                switch (element.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        {
                            if (boundResource is not DeviceBuffer buffer)
                                throw new ArgumentException($"Resource {index} must be a device buffer.", nameof(description));
                            if ((buffer.Usage & BufferUsage.UniformBuffer) == 0)
                                throw new ArgumentException($"Resource {index} must have UniformBuffer usage.", nameof(description));

                            var nativeBuffer = RequireBuffer(buffer);
                            var view = new ConstantBufferViewDesc(
                                nativeBuffer.Resource->GetGPUVirtualAddress(),
                                checked((uint)nativeBuffer.AllocationSize));
                            _device->CreateConstantBufferView(&view, resources.GetCpuHandle(resourceIndex++));
                            break;
                        }
                    case ResourceKind.TextureReadOnly:
                        {
                            var destination = resources.GetCpuHandle(resourceIndex++);
                            switch (boundResource)
                            {
                                case Texture boundTexture:
                                    {
                                        var nativeTexture = RequireTexture(boundTexture);
                                        ValidateSampledTexture(nativeTexture, index, nameof(description));
                                        CreateShaderResourceView(nativeTexture, destination);
                                        textures.Add(nativeTexture);
                                        break;
                                    }
                                case TextureView boundView:
                                    {
                                        var nativeView = RequireNative<DirectX12TextureView>(boundView.NativeResource, "texture view");
                                        var nativeTexture = RequireTexture(boundView.Target);
                                        ValidateSampledTexture(nativeTexture, index, nameof(description));
                                        _device->CopyDescriptorsSimple(
                                            1,
                                            destination,
                                            nativeView.Descriptor.CpuHandle,
                                            DescriptorHeapType.CbvSrvUav);
                                        textures.Add(nativeTexture);
                                        break;
                                    }
                                default:
                                    throw new ArgumentException($"Resource {index} must be a texture or texture view.", nameof(description));
                            }

                            break;
                        }
                    case ResourceKind.Sampler:
                        {
                            if (boundResource is not Sampler sampler)
                                throw new ArgumentException($"Resource {index} must be a sampler.", nameof(description));

                            var nativeSampler = RequireNative<DirectX12Sampler>(sampler.NativeResource, "sampler");
                            _device->CopyDescriptorsSimple(
                                1,
                                samplers.GetCpuHandle(samplerIndex++),
                                nativeSampler.Descriptor.CpuHandle,
                                DescriptorHeapType.Sampler);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(description));
                }
            }

            return TrackResource(
                new DirectX12ResourceSet(nativeDescription, layout, textures.ToArray(), resources, samplers));
        }
        catch
        {
            resources?.Dispose();
            samplers?.Dispose();
            throw;
        }
    }

    public object CreateGraphicsPipeline(GraphicsPipelineDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return TrackResource(CreatePipeline(description));
    }

    public object CreateFramebuffer(FramebufferDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (description.ColorTargets is null || description.ColorTargets.Length == 0)
            throw new ArgumentException("A framebuffer requires color targets.", nameof(description));
        if (description.ColorTargets.Length > 8)
            throw new NotSupportedException("DirectX 12 supports at most eight simultaneous color targets.");

        var colorDescriptors = _renderTargetDescriptors.Allocate((uint)description.ColorTargets.Length);
        DirectX12DescriptorAllocation depthDescriptor = null;

        try
        {
            var colorTargets = new DirectX12Texture[description.ColorTargets.Length];
            var width = description.ColorTargets[0].Target.Width;
            var height = description.ColorTargets[0].Target.Height;

            for (var index = 0; index < description.ColorTargets.Length; index++)
            {
                var attachment = description.ColorTargets[index];
                var nativeTexture = RequireTexture(attachment.Target);
                ValidateFramebufferAttachment(attachment, width, height, TextureUsage.RenderTarget);
                colorTargets[index] = nativeTexture;
                CreateRenderTargetView(nativeTexture, attachment.ArrayLayer, colorDescriptors.GetCpuHandle((uint)index));
            }

            DirectX12Texture depthTarget = null;
            if (description.DepthTarget is { } depthAttachment)
            {
                ValidateFramebufferAttachment(depthAttachment, width, height, TextureUsage.DepthStencil);
                depthTarget = RequireTexture(depthAttachment.Target);
                depthDescriptor = _depthDescriptors.Allocate(1);
                CreateDepthStencilView(depthTarget, depthAttachment.ArrayLayer, depthDescriptor.CpuHandle);
            }

            return TrackResource(
                new DirectX12Framebuffer(
                    colorDescriptors,
                    depthDescriptor,
                    colorTargets,
                    depthTarget,
                    new OutputDescription(
                        description.ColorTargets.Select(attachment => attachment.Target.Format).ToArray(),
                        description.DepthTarget?.Target.Format),
                    width,
                    height));
        }
        catch
        {
            colorDescriptors.Dispose();
            depthDescriptor?.Dispose();
            throw;
        }
    }

    public void DestroyResource(GraphicsResource resource)
    {
        if (_disposed || resource.NativeResource is not IDisposable nativeResource)
            return;

        lock (_sync)
        {
            if (_disposed || !_liveResources.Contains(nativeResource))
                return;

            // Destruction defers until the fence passes the last signaled value, so
            // disposing resources never stalls the GPU.
            var lastSubmitted = _nextFenceValue - 1;
            if (_fence->GetCompletedValue() >= lastSubmitted)
                DestroyNativeResource(nativeResource);
            else
                _deferredDestructions.Add((lastSubmitted, nativeResource));
        }
    }

    private void DestroyDeferredResources()
    {
        if (_deferredDestructions.Count == 0)
            return;

        var completed = _fence->GetCompletedValue();
        var index = 0;
        while (index < _deferredDestructions.Count)
        {
            var (fenceValue, nativeResource) = _deferredDestructions[index];
            if (fenceValue > completed)
            {
                index++;
                continue;
            }

            _deferredDestructions.RemoveAt(index);
            DestroyNativeResource(nativeResource);
        }
    }

    public void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(buffer);
        if ((ulong)offset + (uint)data.Length > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(data));
        if (data.IsEmpty)
            return;

        lock (_sync)
            ExecuteImmediateBufferUpdate(RequireBuffer(buffer), offset, data);
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
        ArgumentNullException.ThrowIfNull(texture);
        var nativeTexture = RequireTexture(texture);
        ValidateTextureRegion(texture.Description, data, x, y, z, width, height, depth, mipLevel, arrayLayer);

        lock (_sync)
        {
            ExecuteImmediateTextureUpdate(nativeTexture, data, x, y, z, width, height, depth, mipLevel, arrayLayer);
        }
    }

    public MappedResource Map(Texture texture, MapMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);
        var nativeTexture = RequireTexture(texture);

        if (mode is not (MapMode.Read or MapMode.Write or MapMode.ReadWrite))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (!nativeTexture.IsStaging)
            throw new InvalidOperationException("Only staging textures can be mapped.");
        if (texture.MipLevels != 1 || texture.ArrayLayers != 1)
        {
            throw new NotSupportedException(
                "The mapping contract can represent only one DirectX 12 texture subresource.");
        }
        if (nativeTexture.MappedPointer != null)
            throw new InvalidOperationException("The texture is already mapped.");

        lock (_sync)
        {
            WaitForIdleCore();

            if (mode is MapMode.Read or MapMode.ReadWrite)
                SynchronizeStagingReadback(nativeTexture);
            if (mode == MapMode.ReadWrite)
                CopyStagingReadbackToUpload(nativeTexture);

            var resource = mode == MapMode.Read ? nativeTexture.StagingReadback : nativeTexture.StagingUpload;
            var readRange = mode == MapMode.Write
                ? new DirectXRange(0, 0)
                : new DirectXRange(0, (nuint)nativeTexture.StagingSize);
            void* data = null;
            ThrowIfFailed(resource->Map(0, &readRange, &data), "map a staging texture");
            nativeTexture.MappedMode = mode;
            nativeTexture.MappedPointer = data;

            var footprint = nativeTexture.StagingFootprints[0].Footprint;
            return new MappedResource(
                (nint)data,
                checked((uint)nativeTexture.StagingSize),
                footprint.RowPitch,
                checked(footprint.RowPitch * footprint.Height));
        }
    }

    public void Unmap(Texture texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);
        var nativeTexture = RequireTexture(texture);

        if (nativeTexture.MappedPointer == null || nativeTexture.MappedMode is null)
            throw new InvalidOperationException("The texture is not mapped.");

        var mode = nativeTexture.MappedMode.Value;
        var resource = mode == MapMode.Read ? nativeTexture.StagingReadback : nativeTexture.StagingUpload;
        var writtenRange = mode == MapMode.Read
            ? new DirectXRange(0, 0)
            : new DirectXRange(0, (nuint)nativeTexture.StagingSize);
        resource->Unmap(0, &writtenRange);
        nativeTexture.MappedPointer = null;
        nativeTexture.MappedMode = null;

        if (mode is MapMode.Write or MapMode.ReadWrite)
        {
            lock (_sync)
                SynchronizeStagingUpload(nativeTexture);
        }
    }

    public void Present()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            ThrowIfFailed(
                _swapchain->Present(_options.SyncToVerticalBlank ? 1u : 0u, 0),
                "present the DirectX 12 swapchain");
            _frameIndex = _swapchain->GetCurrentBackBufferIndex();
        }
    }

    public void ResizeSwapchain(uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (width == 0 || height == 0)
            return;

        lock (_sync)
        {
            if (width == _swapchainWidth && height == _swapchainHeight)
                return;

            WaitForIdleCore();
            ReleaseSwapchainBuffers();
            ThrowIfFailed(
                _swapchain->ResizeBuffers(FrameCount, width, height, DXGIFormat.FormatB8G8R8A8Unorm, 0),
                "resize the DirectX 12 swapchain");
            _swapchainWidth = width;
            _swapchainHeight = height;
            AcquireSwapchainBuffers();
            _frameIndex = _swapchain->GetCurrentBackBufferIndex();
        }
    }

    public void WaitForIdle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_sync)
            WaitForIdleCore();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_queue != null && _fence != null)
        {
            try
            {
                WaitForIdleCore();
            }
            catch
            {
            }
        }

        foreach (var transients in _frameTransients)
        {
            foreach (var transient in transients)
                transient.Dispose();
            transients.Clear();
        }

        // DestroyAllResources covers deferred natives too (they stay in _liveResources
        // until destroyed), so the deferred list only needs to be emptied.
        _deferredDestructions.Clear();

        for (var index = 0; index < _frameUploadArenas.Length; index++)
        {
            _frameUploadArenas[index]?.Dispose();
            _frameUploadArenas[index] = null;
        }

        _immediateUploadArena?.Dispose();
        _immediateUploadArena = null;

        DestroyAllResources();

        _shaderCompiler?.Dispose();
        _shaderCompiler = null;
        ReleaseSwapchainBuffers();
        _swapchainRenderTargets?.Dispose();
        _swapchainRenderTargets = null;

        _depthDescriptors?.Dispose();
        _renderTargetDescriptors?.Dispose();
        _samplerDescriptors?.Dispose();
        _shaderResourceDescriptors?.Dispose();

        if (_immediateCommandList != null)
        {
            _immediateCommandList->Release();
            _immediateCommandList = null;
        }
        if (_immediateAllocator != null)
        {
            _immediateAllocator->Release();
            _immediateAllocator = null;
        }
        if (_commandList != null)
        {
            _commandList->Release();
            _commandList = null;
        }

        for (var index = 0; index < _frameAllocators.Length; index++)
        {
            var allocator = (ID3D12CommandAllocator*)_frameAllocators[index];
            if (allocator != null)
                allocator->Release();
            _frameAllocators[index] = 0;
        }

        if (_fence != null)
        {
            _fence->Release();
            _fence = null;
        }
        if (_swapchain != null)
        {
            _swapchain->Release();
            _swapchain = null;
        }
        if (_queue != null)
        {
            _queue->Release();
            _queue = null;
        }
        if (_device != null)
        {
            _device->Release();
            _device = null;
        }
        if (_adapter != null)
        {
            _adapter->Release();
            _adapter = null;
        }
        if (_factory != null)
        {
            _factory->Release();
            _factory = null;
        }

        _fenceEvent.Dispose();
        _dxgi.Dispose();
        _d3d12.Dispose();
    }

    private void EnableDebugLayer()
    {
        if (!_options.Debug)
            return;

        ID3D12Debug* debug = null;
        var debugId = ID3D12Debug.Guid;
        ThrowIfFailed(_d3d12.GetDebugInterface(&debugId, (void**)&debug), "enable the DirectX 12 debug layer");

        try
        {
            debug->EnableDebugLayer();
        }
        finally
        {
            debug->Release();
        }
    }

    private void CreateFactoryAndDevice()
    {
        var factoryId = IDXGIFactory4.Guid;
        IDXGIFactory4* factory = null;
        ThrowIfFailed(
            _dxgi.CreateDXGIFactory2(_options.Debug ? DXGIApi.CreateFactoryDebug : 0u, &factoryId, (void**)&factory),
            "create the DXGI factory");
        _factory = factory;

        nuint bestDedicatedMemory = 0;

        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            IDXGIAdapter1* candidate = null;
            var result = _factory->EnumAdapters1(adapterIndex, &candidate);
            if (result == DxgiErrorNotFound)
                break;
            ThrowIfFailed(result, "enumerate DXGI adapters");

            AdapterDesc1 description;
            ThrowIfFailed(candidate->GetDesc1(&description), "query a DXGI adapter");
            if ((((AdapterFlag)description.Flags) & AdapterFlag.Software) != 0)
            {
                candidate->Release();
                continue;
            }

            ID3D12Device* testDevice = null;
            var deviceId = ID3D12Device.Guid;
            result = _d3d12.CreateDevice(
                (IUnknown*)candidate,
                D3DFeatureLevel.Level110,
                &deviceId,
                (void**)&testDevice);
            if (result < 0)
            {
                candidate->Release();
                continue;
            }

            testDevice->Release();

            if (_adapter != null && description.DedicatedVideoMemory <= bestDedicatedMemory)
            {
                candidate->Release();
                continue;
            }

            if (_adapter != null)
                _adapter->Release();
            _adapter = candidate;
            bestDedicatedMemory = description.DedicatedVideoMemory;
        }

        if (_adapter == null)
        {
            IDXGIAdapter1* warpAdapter = null;
            var adapterId = IDXGIAdapter1.Guid;
            ThrowIfFailed(
                _factory->EnumWarpAdapter(&adapterId, (void**)&warpAdapter),
                "select the DirectX 12 WARP adapter");
            _adapter = warpAdapter;
        }

        var selectedDeviceId = ID3D12Device.Guid;
        ID3D12Device* device = null;
        ThrowIfFailed(
            _d3d12.CreateDevice(
                (IUnknown*)_adapter,
                D3DFeatureLevel.Level110,
                &selectedDeviceId,
                (void**)&device),
            "create the DirectX 12 device");
        _device = device;
    }

    private void CreateCommandInfrastructure()
    {
        var queueDescription = new CommandQueueDesc(CommandListType.Direct, 0, CommandQueueFlags.None, 0);
        var queueId = ID3D12CommandQueue.Guid;
        ID3D12CommandQueue* queue = null;
        ThrowIfFailed(
            _device->CreateCommandQueue(&queueDescription, &queueId, (void**)&queue),
            "create the DirectX 12 command queue");
        _queue = queue;

        var allocatorId = ID3D12CommandAllocator.Guid;
        for (var index = 0; index < FrameCount; index++)
        {
            ID3D12CommandAllocator* allocator = null;
            ThrowIfFailed(
                _device->CreateCommandAllocator(CommandListType.Direct, &allocatorId, (void**)&allocator),
                "create a DirectX 12 frame command allocator");
            _frameAllocators[index] = (nint)allocator;
        }

        ID3D12CommandAllocator* immediateAllocator = null;
        ThrowIfFailed(
            _device->CreateCommandAllocator(CommandListType.Direct, &allocatorId, (void**)&immediateAllocator),
            "create the DirectX 12 upload command allocator");
        _immediateAllocator = immediateAllocator;

        var commandListId = ID3D12GraphicsCommandList.Guid;
        ID3D12GraphicsCommandList* commandList = null;
        ThrowIfFailed(
            _device->CreateCommandList(
                0,
                CommandListType.Direct,
                (ID3D12CommandAllocator*)_frameAllocators[0],
                null,
                &commandListId,
                (void**)&commandList),
            "create the DirectX 12 frame command list");
        _commandList = commandList;
        ThrowIfFailed(_commandList->Close(), "close the initial DirectX 12 frame command list");

        ID3D12GraphicsCommandList* immediateCommandList = null;
        ThrowIfFailed(
            _device->CreateCommandList(
                0,
                CommandListType.Direct,
                _immediateAllocator,
                null,
                &commandListId,
                (void**)&immediateCommandList),
            "create the DirectX 12 upload command list");
        _immediateCommandList = immediateCommandList;
        ThrowIfFailed(_immediateCommandList->Close(), "close the initial DirectX 12 upload command list");

        var fenceId = ID3D12Fence.Guid;
        ID3D12Fence* fence = null;
        ThrowIfFailed(_device->CreateFence(0, FenceFlags.None, &fenceId, (void**)&fence), "create a DirectX 12 fence");
        _fence = fence;
    }

    private void CreateDescriptorAllocators()
    {
        _shaderResourceDescriptors = new DirectX12DescriptorAllocator(
            _device,
            DescriptorHeapType.CbvSrvUav,
            ShaderResourceDescriptorCapacity,
            true);
        _samplerDescriptors = new DirectX12DescriptorAllocator(
            _device,
            DescriptorHeapType.Sampler,
            SamplerDescriptorCapacity,
            true);
        _renderTargetDescriptors = new DirectX12DescriptorAllocator(
            _device,
            DescriptorHeapType.Rtv,
            RenderTargetDescriptorCapacity,
            false);
        _depthDescriptors = new DirectX12DescriptorAllocator(
            _device,
            DescriptorHeapType.Dsv,
            DepthDescriptorCapacity,
            false);
    }

    private void CreateSwapchain(uint width, uint height)
    {
        var description = new SwapChainDesc1(
            width,
            height,
            DXGIFormat.FormatB8G8R8A8Unorm,
            false,
            new SampleDesc(1, 0),
            DxgiUsageRenderTargetOutput,
            FrameCount,
            Scaling.Stretch,
            SwapEffect.FlipDiscard,
            AlphaMode.Ignore,
            0);
        IDXGISwapChain1* swapchain = null;

        ThrowIfFailed(
            _factory->CreateSwapChainForHwnd(
                (IUnknown*)_queue,
                _window.NativeHandle,
                &description,
                null,
                null,
                &swapchain),
            "create the DirectX 12 swapchain");

        try
        {
            var swapchainId = IDXGISwapChain3.Guid;
            IDXGISwapChain3* swapchain3 = null;
            ThrowIfFailed(swapchain->QueryInterface(&swapchainId, (void**)&swapchain3), "query the DXGI swapchain interface");
            _swapchain = swapchain3;
        }
        finally
        {
            swapchain->Release();
        }

        ThrowIfFailed(
            _factory->MakeWindowAssociation(_window.NativeHandle, DxgiMakeWindowAssociationNoAltEnter),
            "disable DXGI automatic full screen handling");

        _swapchainWidth = width;
        _swapchainHeight = height;
        _swapchainRenderTargets = _renderTargetDescriptors.Allocate(FrameCount);
        AcquireSwapchainBuffers();
        _frameIndex = _swapchain->GetCurrentBackBufferIndex();
    }

    private void AcquireSwapchainBuffers()
    {
        var resourceId = ID3D12Resource.Guid;

        try
        {
            for (uint index = 0; index < FrameCount; index++)
            {
                ID3D12Resource* resource = null;
                ThrowIfFailed(_swapchain->GetBuffer(index, &resourceId, (void**)&resource), "acquire a swapchain buffer");
                _swapchainBuffers[index] = (nint)resource;
                _swapchainStates[index] = ResourceStates.Present;
                _device->CreateRenderTargetView(resource, null, _swapchainRenderTargets.GetCpuHandle(index));
            }
        }
        catch
        {
            ReleaseSwapchainBuffers();
            throw;
        }
    }

    private void ReleaseSwapchainBuffers()
    {
        for (var index = 0; index < _swapchainBuffers.Length; index++)
        {
            var resource = (ID3D12Resource*)_swapchainBuffers[index];
            if (resource != null)
                resource->Release();
            _swapchainBuffers[index] = 0;
            _swapchainStates[index] = ResourceStates.Present;
        }
    }

    private DirectX12Texture CreateStagingTexture(TextureDescription description, ResourceDesc resourceDescription)
    {
        var subresourceCount = checked(description.MipLevels * description.ArrayLayers);
        if (description.Type == TextureType.Texture3D)
            subresourceCount = description.MipLevels;

        var footprints = new PlacedSubresourceFootprint[subresourceCount];
        var rows = new uint[subresourceCount];
        var rowSizes = new ulong[subresourceCount];
        ulong totalSize = 0;

        fixed (PlacedSubresourceFootprint* footprintPointer = footprints)
        fixed (uint* rowPointer = rows)
        fixed (ulong* rowSizePointer = rowSizes)
        {
            _device->GetCopyableFootprints(
                &resourceDescription,
                0,
                subresourceCount,
                0,
                footprintPointer,
                rowPointer,
                rowSizePointer,
                &totalSize);
        }

        if (totalSize > uint.MaxValue)
            throw new NotSupportedException("Mapped textures larger than 4 GiB are not representable by MappedResource.");

        ID3D12Resource* textureResource = null;
        var defaultHeap = new HeapProperties(HeapType.Default);
        var resourceId = ID3D12Resource.Guid;
        ThrowIfFailed(
            _device->CreateCommittedResource(
                &defaultHeap,
                HeapFlags.None,
                &resourceDescription,
                ResourceStates.Common,
                null,
                &resourceId,
                (void**)&textureResource),
            "create a staging texture resource");

        ID3D12Resource* upload = null;
        ID3D12Resource* readback = null;

        try
        {
            upload = CreateBufferResource(totalSize, HeapType.Upload, ResourceStates.GenericRead);
            readback = CreateBufferResource(totalSize, HeapType.Readback, ResourceStates.CopyDest);
            return new DirectX12Texture(
                textureResource,
                ResourceStates.Common,
                upload,
                readback,
                resourceDescription,
                description,
                footprints,
                rows,
                totalSize);
        }
        catch
        {
            textureResource->Release();
            if (upload != null)
                upload->Release();
            if (readback != null)
                readback->Release();
            throw;
        }
    }

    private ID3D12Resource* CreateBufferResource(ulong size, HeapType heapType, ResourceStates initialState)
    {
        var heap = new HeapProperties(heapType);
        var description = new ResourceDesc(
            ResourceDimension.Buffer,
            0,
            Math.Max(1, size),
            1,
            1,
            1,
            DXGIFormat.FormatUnknown,
            new SampleDesc(1, 0),
            TextureLayout.LayoutRowMajor,
            ResourceFlags.None);
        ID3D12Resource* resource = null;
        var resourceId = ID3D12Resource.Guid;
        ThrowIfFailed(
            _device->CreateCommittedResource(
                &heap,
                HeapFlags.None,
                &description,
                initialState,
                null,
                &resourceId,
                (void**)&resource),
            $"create a {heapType} buffer resource");
        return resource;
    }

    private DirectX12UploadArena CreateUploadArena(ulong capacity)
    {
        var resource = CreateBufferResource(capacity, HeapType.Upload, ResourceStates.GenericRead);
        var emptyReadRange = new DirectXRange(0, 0);
        void* mapped = null;
        var mapResult = resource->Map(0, &emptyReadRange, &mapped);

        if (mapResult < 0)
        {
            resource->Release();
            ThrowIfFailed(mapResult, "map a DirectX 12 upload arena");
        }

        return new DirectX12UploadArena
        {
            Resource = resource,
            Mapped = (byte*)mapped,
            Capacity = capacity,
        };
    }

    // Suballocates staging space from a shared upload arena instead of creating a
    // committed resource per update; a full arena retires into the frame's transient
    // list (it is still referenced by copies recorded earlier this frame).
    private ulong AllocateUploadSpace(
        ref DirectX12UploadArena arena,
        ulong size,
        List<IDisposable> retireTo)
    {
        const ulong Alignment = 16;
        var offset = arena is null ? 0 : (arena.Offset + Alignment - 1) & ~(Alignment - 1);

        if (arena is null || offset + size > arena.Capacity)
        {
            var capacity = Math.Max(Math.Max(256 * 1024UL, size), (arena?.Capacity ?? 0) * 2);
            if (arena is not null)
                retireTo.Add(arena);

            arena = CreateUploadArena(capacity);
            offset = 0;
        }

        arena.Offset = offset + size;
        return offset;
    }

    // The immediate path waits for the GPU before recording, so one arena is reused
    // whole (offset 0) for each immediate buffer or texture upload.
    private DirectX12UploadArena EnsureImmediateUploadCapacity(ulong size)
    {
        if (_immediateUploadArena is not null && _immediateUploadArena.Capacity < size)
        {
            _immediateUploadArena.Dispose();
            _immediateUploadArena = null;
        }

        _immediateUploadArena ??= CreateUploadArena(Math.Max(64 * 1024UL, size));
        _immediateUploadArena.Offset = 0;
        return _immediateUploadArena;
    }

    private static ResourceDesc CreateTextureResourceDescription(TextureDescription description)
    {
        var flags = ResourceFlags.None;
        if ((description.Usage & TextureUsage.RenderTarget) != 0)
            flags |= ResourceFlags.AllowRenderTarget;
        if ((description.Usage & TextureUsage.DepthStencil) != 0)
            flags |= ResourceFlags.AllowDepthStencil;
        if ((description.Usage & TextureUsage.DepthStencil) != 0 && (description.Usage & TextureUsage.Sampled) == 0)
            flags |= ResourceFlags.DenyShaderResource;

        var format = DirectX12Conversions.ToDxgiFormat(description.Format);
        if ((description.Usage & TextureUsage.DepthStencil) != 0 && (description.Usage & TextureUsage.Sampled) != 0)
        {
            format = description.Format switch
            {
                PixelFormat.D24_UNorm_S8_UInt => DXGIFormat.FormatR24G8Typeless,
                PixelFormat.D32_Float_S8_UInt => DXGIFormat.FormatR32G8X24Typeless,
                _ => format,
            };
        }

        return new ResourceDesc(
            description.Type switch
            {
                TextureType.Texture1D => ResourceDimension.Texture1D,
                TextureType.Texture2D => ResourceDimension.Texture2D,
                TextureType.Texture3D => ResourceDimension.Texture3D,
                _ => throw new ArgumentOutOfRangeException(nameof(description)),
            },
            0,
            description.Width,
            description.Type == TextureType.Texture1D ? 1u : description.Height,
            checked((ushort)(description.Type == TextureType.Texture3D ? description.Depth : description.ArrayLayers)),
            checked((ushort)description.MipLevels),
            format,
            new SampleDesc(1, 0),
            TextureLayout.LayoutUnknown,
            flags);
    }

    private void CreateShaderResourceView(DirectX12Texture texture, CpuDescriptorHandle destination)
    {
        if (texture.IsStaging)
            throw new InvalidOperationException("Staging textures cannot be sampled.");

        var description = texture.Description;
        var format = description.Format switch
        {
            PixelFormat.D24_UNorm_S8_UInt => DXGIFormat.FormatR24UnormX8Typeless,
            PixelFormat.D32_Float_S8_UInt => DXGIFormat.FormatR32FloatX8X24Typeless,
            _ => DirectX12Conversions.ToDxgiFormat(description.Format),
        };
        ShaderResourceViewDesc view;

        if (description.Type == TextureType.Texture3D)
        {
            view = new ShaderResourceViewDesc(
                format,
                SrvDimension.Texture3D,
                DirectX12Conversions.DefaultShaderComponentMapping,
                texture3D: new Tex3DSrv(0, description.MipLevels, 0));
        }
        else if (description.ArrayLayers > 1)
        {
            view = description.Type == TextureType.Texture1D
                ? new ShaderResourceViewDesc(
                    format,
                    SrvDimension.Texture1Darray,
                    DirectX12Conversions.DefaultShaderComponentMapping,
                    texture1DArray: new Tex1DArraySrv(0, description.MipLevels, 0, description.ArrayLayers, 0))
                : new ShaderResourceViewDesc(
                    format,
                    SrvDimension.Texture2Darray,
                    DirectX12Conversions.DefaultShaderComponentMapping,
                    texture2DArray: new Tex2DArraySrv(0, description.MipLevels, 0, description.ArrayLayers, 0, 0));
        }
        else
        {
            view = description.Type == TextureType.Texture1D
                ? new ShaderResourceViewDesc(
                    format,
                    SrvDimension.Texture1D,
                    DirectX12Conversions.DefaultShaderComponentMapping,
                    texture1D: new Tex1DSrv(0, description.MipLevels, 0))
                : new ShaderResourceViewDesc(
                    format,
                    SrvDimension.Texture2D,
                    DirectX12Conversions.DefaultShaderComponentMapping,
                    texture2D: new Tex2DSrv(0, description.MipLevels, 0, 0));
        }

        _device->CreateShaderResourceView(texture.Resource, &view, destination);
    }

    private void CreateRenderTargetView(DirectX12Texture texture, uint arrayLayer, CpuDescriptorHandle destination)
    {
        var description = texture.Description;
        var format = DirectX12Conversions.ToDxgiFormat(description.Format);
        RenderTargetViewDesc view;

        if (description.Type == TextureType.Texture3D)
        {
            view = new RenderTargetViewDesc(
                format,
                RtvDimension.Texture3D,
                texture3D: new Tex3DRtv(0, arrayLayer, 1));
        }
        else if (description.ArrayLayers > 1)
        {
            view = description.Type == TextureType.Texture1D
                ? new RenderTargetViewDesc(
                    format,
                    RtvDimension.Texture1Darray,
                    texture1DArray: new Tex1DArrayRtv(0, arrayLayer, 1))
                : new RenderTargetViewDesc(
                    format,
                    RtvDimension.Texture2Darray,
                    texture2DArray: new Tex2DArrayRtv(0, arrayLayer, 1, 0));
        }
        else
        {
            view = description.Type == TextureType.Texture1D
                ? new RenderTargetViewDesc(format, RtvDimension.Texture1D, texture1D: new Tex1DRtv(0))
                : new RenderTargetViewDesc(format, RtvDimension.Texture2D, texture2D: new Tex2DRtv(0, 0));
        }

        _device->CreateRenderTargetView(texture.Resource, &view, destination);
    }

    private void CreateDepthStencilView(DirectX12Texture texture, uint arrayLayer, CpuDescriptorHandle destination)
    {
        var description = texture.Description;
        var format = DirectX12Conversions.ToDxgiFormat(description.Format);
        DepthStencilViewDesc view;

        if (description.Type == TextureType.Texture3D)
            throw new NotSupportedException("DirectX 12 does not support 3D depth-stencil views.");

        if (description.ArrayLayers > 1)
        {
            view = description.Type == TextureType.Texture1D
                ? new DepthStencilViewDesc(
                    format,
                    DsvDimension.Texture1Darray,
                    DsvFlags.None,
                    texture1DArray: new Tex1DArrayDsv(0, arrayLayer, 1))
                : new DepthStencilViewDesc(
                    format,
                    DsvDimension.Texture2Darray,
                    DsvFlags.None,
                    texture2DArray: new Tex2DArrayDsv(0, arrayLayer, 1));
        }
        else
        {
            view = description.Type == TextureType.Texture1D
                ? new DepthStencilViewDesc(format, DsvDimension.Texture1D, DsvFlags.None, texture1D: new Tex1DDsv(0))
                : new DepthStencilViewDesc(format, DsvDimension.Texture2D, DsvFlags.None, texture2D: new Tex2DDsv(0));
        }

        _device->CreateDepthStencilView(texture.Resource, &view, destination);
    }

    private static void ValidateTextureDescription(TextureDescription description)
    {
        if (description.Width == 0 || description.Height == 0 || description.Depth == 0 ||
            description.MipLevels == 0 || description.ArrayLayers == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(description), "Texture dimensions, mip levels, and array layers must be non-zero.");
        }
        if (description.Type == TextureType.Texture1D && (description.Height != 1 || description.Depth != 1))
            throw new ArgumentException("A 1D texture must have a height and depth of one.", nameof(description));
        if (description.Type == TextureType.Texture2D && description.Depth != 1)
            throw new ArgumentException("A 2D texture must have a depth of one.", nameof(description));
        if (description.Type == TextureType.Texture3D && description.ArrayLayers != 1)
            throw new ArgumentException("A 3D texture cannot have array layers.", nameof(description));
        if ((description.Usage & TextureUsage.Staging) != 0 && description.Usage != TextureUsage.Staging)
            throw new ArgumentException("A staging texture cannot also be sampled or used as an attachment.", nameof(description));

        var isDepthStencil = IsDepthStencilFormat(description.Format);
        if ((description.Usage & TextureUsage.DepthStencil) != 0 && !isDepthStencil)
            throw new ArgumentException("A depth-stencil texture requires a depth-stencil format.", nameof(description));
        if ((description.Usage & TextureUsage.RenderTarget) != 0 && isDepthStencil)
            throw new ArgumentException("A color render target requires a color format.", nameof(description));
        if (description.Type == TextureType.Texture3D && (description.Usage & TextureUsage.DepthStencil) != 0)
            throw new NotSupportedException("DirectX 12 does not support 3D depth-stencil textures.");
        if (isDepthStencil && (description.Usage & TextureUsage.Sampled) != 0 &&
            (description.Usage & TextureUsage.DepthStencil) == 0)
        {
            throw new ArgumentException(
                "A sampled DirectX 12 depth texture must also have DepthStencil usage.",
                nameof(description));
        }
        if (isDepthStencil && (description.Usage & TextureUsage.Staging) != 0)
        {
            throw new NotSupportedException(
                "The mapping contract cannot represent DirectX 12 depth and stencil planes in one staging texture.");
        }

        DirectX12Conversions.ToDxgiFormat(description.Format);
    }

    private static void ValidateFramebufferAttachment(
        FramebufferAttachmentDescription attachment,
        uint width,
        uint height,
        TextureUsage requiredUsage)
    {
        var texture = attachment.Target;
        ArgumentNullException.ThrowIfNull(texture);
        if ((texture.Usage & requiredUsage) == 0)
            throw new ArgumentException($"Framebuffer attachment {texture.Name ?? "<unnamed>"} is missing {requiredUsage} usage.");
        if (texture.Width != width || texture.Height != height)
            throw new ArgumentException("All framebuffer attachments must have matching dimensions.");
        var attachmentLayers = texture.Type == TextureType.Texture3D ? texture.Depth : texture.ArrayLayers;
        if (attachment.ArrayLayer >= attachmentLayers)
            throw new ArgumentOutOfRangeException(nameof(attachment), "The framebuffer array layer does not exist.");

        var isDepthStencil = IsDepthStencilFormat(texture.Format);
        if (requiredUsage == TextureUsage.RenderTarget && isDepthStencil)
            throw new ArgumentException("A color framebuffer attachment requires a color format.");
        if (requiredUsage == TextureUsage.DepthStencil && !isDepthStencil)
            throw new ArgumentException("A depth framebuffer attachment requires a depth-stencil format.");
    }

    private static void ValidateTextureRegion(
        TextureDescription description,
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
        if (width == 0 || height == 0 || depth == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture update dimensions must be non-zero.");
        if (IsDepthStencilFormat(description.Format))
        {
            throw new NotSupportedException(
                "DirectX 12 CPU updates cannot represent both planes of a packed depth-stencil texture.");
        }
        if (mipLevel >= description.MipLevels || arrayLayer >= description.ArrayLayers)
            throw new ArgumentOutOfRangeException(nameof(mipLevel));

        var mipWidth = Math.Max(1u, description.Width >> checked((int)mipLevel));
        var mipHeight = Math.Max(1u, description.Height >> checked((int)mipLevel));
        var mipDepth = Math.Max(1u, description.Depth >> checked((int)mipLevel));
        if ((ulong)x + width > mipWidth || (ulong)y + height > mipHeight || (ulong)z + depth > mipDepth)
            throw new ArgumentOutOfRangeException(nameof(width), "The update region exceeds the texture subresource.");

        var requiredBytes = checked((ulong)width * height * depth * DirectX12Conversions.GetBytesPerPixel(description.Format));
        if ((ulong)data.Length < requiredBytes)
            throw new ArgumentException("The texture update does not contain enough source data.", nameof(data));
    }

    private T RequireNative<T>(object nativeResource, string resourceName) where T : class, IDisposable
    {
        if (nativeResource is not T native || !_liveResources.Contains(native))
        {
            throw new InvalidOperationException(
                $"The {resourceName} is not a live resource owned by this DirectX 12 device.");
        }

        return native;
    }

    private DirectX12Buffer RequireBuffer(DeviceBuffer buffer)
        => RequireNative<DirectX12Buffer>(buffer.NativeResource, "buffer");

    private DirectX12Texture RequireTexture(Texture texture)
        => RequireNative<DirectX12Texture>(texture.NativeResource, "texture");

    private static bool IsDepthStencilFormat(PixelFormat format)
        => format is PixelFormat.D24_UNorm_S8_UInt or PixelFormat.D32_Float_S8_UInt;

    private static void ValidateSampledTexture(DirectX12Texture texture, int index, string parameterName)
    {
        if (texture.IsStaging || (texture.Description.Usage & TextureUsage.Sampled) == 0)
        {
            throw new ArgumentException(
                $"Resource {index} must be a non-staging texture with Sampled usage.",
                parameterName);
        }
    }

    private T TrackResource<T>(T resource) where T : class, IDisposable
    {
        lock (_sync)
        {
            if (_disposed)
            {
                resource.Dispose();
                throw new ObjectDisposedException(nameof(DirectX12Backend));
            }

            if (!_liveResources.Add(resource))
                throw new InvalidOperationException("The DirectX 12 resource is already tracked.");
        }

        return resource;
    }

    private void DestroyNativeResource(IDisposable resource)
    {
        if (!_liveResources.Remove(resource))
            return;

        resource.Dispose();
    }

    private void DestroyAllResources()
    {
        foreach (var resource in _liveResources.ToArray())
            DestroyNativeResource(resource);
    }

    private void WaitForIdleCore()
    {
        if (_queue == null || _fence == null)
            return;

        var value = SignalFence();
        WaitForFence(value);

        foreach (var transients in _frameTransients)
        {
            foreach (var transient in transients)
                transient.Dispose();
            transients.Clear();
        }

        DestroyDeferredResources();
    }

    private ulong SignalFence()
    {
        var value = _nextFenceValue++;
        ThrowIfFailed(_queue->Signal(_fence, value), "signal the DirectX 12 fence");
        return value;
    }

    private void WaitForFence(ulong value)
    {
        if (value == 0 || _fence->GetCompletedValue() >= value)
            return;

        var eventHandle = _fenceEvent.SafeWaitHandle.DangerousGetHandle();
        ThrowIfFailed(_fence->SetEventOnCompletion(value, (void*)eventHandle), "set a DirectX 12 fence event");
        _fenceEvent.WaitOne();
    }

    internal static void ThrowIfFailed(int result, string operation)
    {
        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Failed to {operation} (HRESULT 0x{result:X8}).",
                Marshal.GetExceptionForHR(result, new IntPtr(-1)));
        }
    }

    private static ulong Align(ulong value, ulong alignment)
        => checked((value + alignment - 1) & ~(alignment - 1));
}
