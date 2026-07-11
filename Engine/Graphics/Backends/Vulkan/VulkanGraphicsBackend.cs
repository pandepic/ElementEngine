using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkImage = Silk.NET.Vulkan.Image;

namespace ElementEngine;

internal sealed unsafe partial class VulkanGraphicsBackend : IGraphicsBackend
{
    private const int FramesInFlight = 2;
    private const string SurfaceExtension = "VK_KHR_surface";
    private const string XlibSurfaceExtension = "VK_KHR_xlib_surface";
    private const string WaylandSurfaceExtension = "VK_KHR_wayland_surface";
    private const string Win32SurfaceExtension = "VK_KHR_win32_surface";
    private const string SwapchainExtension = "VK_KHR_swapchain";
    private const string ValidationLayer = "VK_LAYER_KHRONOS_validation";

    private readonly object _sync = new();
    private readonly GameWindow _window;
    private readonly GraphicsDeviceOptions _options;
    private readonly HashSet<object> _liveResources = [];
    private Vk _vk;

    private Instance _instance;
    private KhrSurface _surfaceApi;
    private SurfaceKHR _surface;
    private PhysicalDevice _physicalDevice;
    private PhysicalDeviceProperties _physicalDeviceProperties;
    private PhysicalDeviceFeatures _physicalDeviceFeatures;
    private PhysicalDeviceMemoryProperties _memoryProperties;
    private Device _device;
    private KhrSwapchain _swapchainApi;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    private uint _graphicsQueueFamily;
    private uint _presentQueueFamily;
    private CommandPool _transferCommandPool;
    private DescriptorPool _descriptorPool;
    private VulkanFrameContext[] _frames = [];
    private Fence[] _imageFences = [];

    private SwapchainKHR _swapchain;
    private Format _swapchainFormat;
    private Extent2D _swapchainExtent;
    private VkImage[] _swapchainImages = [];
    private ImageView[] _swapchainImageViews = [];
    private Silk.NET.Vulkan.Framebuffer[] _swapchainFramebuffers = [];
    private ImageLayout[] _swapchainImageLayouts = [];
    private RenderPass _swapchainRenderPass;
    private OutputDescription _swapchainOutputDescription;
    private uint _currentFrame;
    private uint _currentImage;
    private bool _frameAcquired;
    private bool _frameSubmitted;
    private bool _waitForFrameBeforePresent;
    private bool _swapchainDirty;

    // Queue submissions get monotonically increasing serials; fence signals respect
    // submission order, so once a submission's fence is observed every earlier serial
    // has also completed and its deferred destructions are safe to run.
    private ulong _submittedSerial;
    private ulong _completedSerial;
    private readonly List<(ulong Serial, object NativeResource)> _deferredDestructions = [];
    private VulkanBufferResource _immediateStaging;
    private bool _disposed;

    public VulkanGraphicsBackend(GameWindow window, GraphicsDeviceOptions options)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.NativeKind is not (NativeWindowKind.X11 or NativeWindowKind.Wayland or NativeWindowKind.Win32))
            throw new PlatformNotSupportedException($"Vulkan does not support {window.NativeKind} windows in this build.");
        if (window.NativeHandle == 0)
            throw new ArgumentException("The window does not have a native handle.", nameof(window));
        if (window.NativeKind == NativeWindowKind.X11 && window.NativeDisplayHandle == 0)
            throw new ArgumentException("The X11 window does not expose its Display pointer.", nameof(window));
        if (window.NativeKind == NativeWindowKind.Wayland && window.NativeDisplayHandle == 0)
            throw new ArgumentException("The Wayland window does not expose its wl_display pointer.", nameof(window));

        _window = window;
        _options = options;
        _vk = Vk.GetApi();

        try
        {
            CreateInstance();
            LoadSurfaceApi();
            CreateSurface();
            SelectPhysicalDevice();
            CreateLogicalDevice();
            CreateCommandInfrastructure();
            CreateDescriptorPool();
            CreateSwapchain((uint)Math.Max(1, window.FramebufferWidth), (uint)Math.Max(1, window.FramebufferHeight));
            CreateFrameContexts();
        }
        catch (Exception creationException)
        {
            _disposed = true;
            var teardown = new TeardownCollector(creationException);
            DisposeCore(teardown);
            teardown.ThrowIfFailed("Vulkan backend creation and cleanup both failed.");
            throw;
        }
    }

    public GraphicsBackend BackendType => GraphicsBackend.Vulkan;

    public bool IsUvOriginTopLeft => true;

    public OutputDescription SwapchainOutputDescription => _swapchainOutputDescription;

    public void WaitForIdle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Check(_vk.DeviceWaitIdle(_device), "wait for the Vulkan device to become idle");

        lock (_sync)
        {
            _completedSerial = _submittedSerial;
            DestroyDeferredResources();
        }
    }

    public void ResizeSwapchain(uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (width == 0 || height == 0)
            return;

        lock (_sync)
        {
            RecreateSwapchain(width, height);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var teardown = new TeardownCollector();
        DisposeCore(teardown);
        teardown.ThrowIfFailed("One or more errors occurred while disposing the Vulkan backend.");
    }

    private void DisposeCore(TeardownCollector teardown)
    {
        if (_vk is null)
            return;

        if (_device.Handle != 0)
            teardown.Run(() => Check(_vk.DeviceWaitIdle(_device), "wait for the Vulkan device to become idle"));

        // DestroyAllResources covers deferred natives too (they stay in _liveResources
        // until destroyed), so the deferred list only needs to be emptied.
        _deferredDestructions.Clear();
        var immediateStaging = _immediateStaging;
        _immediateStaging = null;
        if (immediateStaging is not null && _device.Handle != 0)
            teardown.Run(() => DestroyBuffer(immediateStaging));

        DestroyAllResources(teardown);
        teardown.Run(DestroyFrameContexts);
        teardown.Run(DestroySwapchain);

        var descriptorPool = _descriptorPool;
        _descriptorPool = default;
        if (descriptorPool.Handle != 0 && _device.Handle != 0)
        {
            teardown.Run(() => _vk.DestroyDescriptorPool(_device, descriptorPool, null));
        }

        var transferCommandPool = _transferCommandPool;
        _transferCommandPool = default;
        if (transferCommandPool.Handle != 0 && _device.Handle != 0)
        {
            teardown.Run(() => _vk.DestroyCommandPool(_device, transferCommandPool, null));
        }

        var swapchainApi = _swapchainApi;
        _swapchainApi = null;
        if (swapchainApi is not null)
            teardown.Run(swapchainApi.Dispose);

        var device = _device;
        _device = default;
        if (device.Handle != 0)
        {
            teardown.Run(() => _vk.DestroyDevice(device, null));
        }

        var surfaceApi = _surfaceApi;
        _surfaceApi = null;
        var surface = _surface;
        _surface = default;
        if (surface.Handle != 0 && surfaceApi is not null)
        {
            teardown.Run(() => surfaceApi.DestroySurface(_instance, surface, null));
        }

        if (surfaceApi is not null)
            teardown.Run(surfaceApi.Dispose);

        var instance = _instance;
        _instance = default;
        if (instance.Handle != 0)
        {
            teardown.Run(() => _vk.DestroyInstance(instance, null));
        }

        _physicalDevice = default;
        _physicalDeviceProperties = default;
        _physicalDeviceFeatures = default;
        _memoryProperties = default;
        _graphicsQueue = default;
        _presentQueue = default;
        _graphicsQueueFamily = 0;
        _presentQueueFamily = 0;
        _currentFrame = 0;
        _currentImage = 0;
        _frameAcquired = false;
        _frameSubmitted = false;
        _waitForFrameBeforePresent = false;
        _swapchainDirty = false;

        var vk = _vk;
        _vk = null;
        teardown.Run(vk.Dispose);
    }

    private void CreateInstance()
    {
        var requiredExtensions = new List<string>
        {
            SurfaceExtension,
            GetPlatformSurfaceExtension(_window.NativeKind),
        };

        ValidateInstanceExtensions(requiredExtensions);

        var layers = new List<string>();
        if (_options.Debug)
        {
            if (!IsInstanceLayerAvailable(ValidationLayer))
                throw new InvalidOperationException($"Vulkan debug mode requires the {ValidationLayer} layer.");

            layers.Add(ValidationLayer);
        }

        var applicationName = AllocateUtf8("ElementEngine");
        var engineName = AllocateUtf8("ElementEngine");
        var extensionNames = AllocateUtf8Array(requiredExtensions);
        var layerNames = AllocateUtf8Array(layers);

        try
        {
            var applicationInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = applicationName,
                ApplicationVersion = Vk.MakeVersion(1, 0, 0),
                PEngineName = engineName,
                EngineVersion = Vk.MakeVersion(1, 0, 0),
                ApiVersion = Vk.Version11,
            };

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &applicationInfo,
                EnabledExtensionCount = (uint)requiredExtensions.Count,
                PpEnabledExtensionNames = extensionNames,
                EnabledLayerCount = (uint)layers.Count,
                PpEnabledLayerNames = layerNames,
            };

            Check(_vk.CreateInstance(&createInfo, null, out _instance), "create the Vulkan instance");
        }
        finally
        {
            FreeUtf8(applicationName);
            FreeUtf8(engineName);
            FreeUtf8Array(extensionNames, requiredExtensions.Count);
            FreeUtf8Array(layerNames, layers.Count);
        }
    }

    internal static string GetPlatformSurfaceExtension(NativeWindowKind nativeKind) => nativeKind switch
    {
        NativeWindowKind.X11 => XlibSurfaceExtension,
        NativeWindowKind.Wayland => WaylandSurfaceExtension,
        NativeWindowKind.Win32 => Win32SurfaceExtension,
        _ => throw new PlatformNotSupportedException($"Unsupported Vulkan window kind: {nativeKind}."),
    };

    private void LoadSurfaceApi()
    {
        if (!_vk.TryGetInstanceExtension(_instance, out _surfaceApi))
            throw new InvalidOperationException("The Vulkan surface extension could not be loaded.");
    }

    private void CreateSurface()
    {
        switch (_window.NativeKind)
        {
            case NativeWindowKind.X11:
                {
                    if (!_vk.TryGetInstanceExtension(_instance, out KhrXlibSurface xlibSurface))
                        throw new InvalidOperationException("The Vulkan Xlib surface extension could not be loaded.");

                    using (xlibSurface)
                    {
                        var createInfo = new XlibSurfaceCreateInfoKHR
                        {
                            SType = StructureType.XlibSurfaceCreateInfoKhr,
                            Dpy = (nint*)_window.NativeDisplayHandle,
                            Window = _window.NativeHandle,
                        };

                        Check(
                            xlibSurface.CreateXlibSurface(_instance, &createInfo, null, out _surface),
                            "create the Vulkan Xlib surface");
                    }

                    break;
                }
            case NativeWindowKind.Win32:
                {
                    if (!_vk.TryGetInstanceExtension(_instance, out KhrWin32Surface win32Surface))
                        throw new InvalidOperationException("The Vulkan Win32 surface extension could not be loaded.");

                    using (win32Surface)
                    {
                        var createInfo = new Win32SurfaceCreateInfoKHR
                        {
                            SType = StructureType.Win32SurfaceCreateInfoKhr,
                            Hinstance = GetModuleHandleW(null),
                            Hwnd = _window.NativeHandle,
                        };

                        Check(
                            win32Surface.CreateWin32Surface(_instance, &createInfo, null, out _surface),
                            "create the Vulkan Win32 surface");
                    }

                    break;
                }
            case NativeWindowKind.Wayland:
                {
                    if (!_vk.TryGetInstanceExtension(_instance, out KhrWaylandSurface waylandSurface))
                        throw new InvalidOperationException("The Vulkan Wayland surface extension could not be loaded.");

                    using (waylandSurface)
                    {
                        var createInfo = new WaylandSurfaceCreateInfoKHR
                        {
                            SType = StructureType.WaylandSurfaceCreateInfoKhr,
                            Display = (nint*)_window.NativeDisplayHandle,
                            Surface = (nint*)_window.NativeHandle,
                        };

                        Check(
                            waylandSurface.CreateWaylandSurface(_instance, &createInfo, null, out _surface),
                            "create the Vulkan Wayland surface");
                    }

                    break;
                }
            default:
                throw new PlatformNotSupportedException($"Unsupported Vulkan window kind: {_window.NativeKind}.");
        }
    }

    private void SelectPhysicalDevice()
    {
        uint deviceCount = 0;
        Check(_vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null), "enumerate Vulkan physical devices");
        if (deviceCount == 0)
            throw new InvalidOperationException("No Vulkan physical devices were found.");

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* devicesPointer = devices)
        {
            Check(
                _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, devicesPointer),
                "enumerate Vulkan physical devices");
        }

        var bestScore = int.MinValue;
        foreach (var device in devices)
        {
            _vk.GetPhysicalDeviceProperties(device, out var properties);
            if (properties.ApiVersion < Vk.Version11)
                continue;

            if (!TryGetQueueFamilies(device, out var graphicsFamily, out var presentFamily) ||
                !IsDeviceExtensionAvailable(device, SwapchainExtension) ||
                !HasSwapchainSupport(device))
            {
                continue;
            }

            var score = properties.DeviceType switch
            {
                PhysicalDeviceType.DiscreteGpu => 10_000,
                PhysicalDeviceType.IntegratedGpu => 5_000,
                PhysicalDeviceType.VirtualGpu => 1_000,
                PhysicalDeviceType.Cpu => 100,
                _ => 0,
            };

            score += checked((int)Math.Min(properties.Limits.MaxImageDimension2D, 4096));
            if (score <= bestScore)
                continue;

            bestScore = score;
            _physicalDevice = device;
            _physicalDeviceProperties = properties;
            _graphicsQueueFamily = graphicsFamily;
            _presentQueueFamily = presentFamily;
        }

        if (_physicalDevice.Handle == 0)
            throw new InvalidOperationException("No Vulkan 1.1 device supports graphics and presentation for this window.");

        _vk.GetPhysicalDeviceFeatures(_physicalDevice, out _physicalDeviceFeatures);
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out _memoryProperties);
    }

    private void CreateLogicalDevice()
    {
        var queueFamilies = _graphicsQueueFamily == _presentQueueFamily
            ? new[] { _graphicsQueueFamily }
            : new[] { _graphicsQueueFamily, _presentQueueFamily };
        var queueCreateInfos = stackalloc DeviceQueueCreateInfo[queueFamilies.Length];
        var priority = 1f;

        for (var index = 0; index < queueFamilies.Length; index++)
        {
            queueCreateInfos[index] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamilies[index],
                QueueCount = 1,
                PQueuePriorities = &priority,
            };
        }

        var enabledFeatures = new PhysicalDeviceFeatures
        {
            SamplerAnisotropy = _physicalDeviceFeatures.SamplerAnisotropy,
            DepthClamp = _physicalDeviceFeatures.DepthClamp,
        };
        var extensionName = AllocateUtf8(SwapchainExtension);

        try
        {
            var createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)queueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,
                EnabledExtensionCount = 1,
                PpEnabledExtensionNames = &extensionName,
                PEnabledFeatures = &enabledFeatures,
            };

            Check(_vk.CreateDevice(_physicalDevice, &createInfo, null, out _device), "create the Vulkan device");
        }
        finally
        {
            FreeUtf8(extensionName);
        }

        _vk.GetDeviceQueue(_device, _graphicsQueueFamily, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_device, _presentQueueFamily, 0, out _presentQueue);

        if (!_vk.TryGetDeviceExtension(_instance, _device, out _swapchainApi))
            throw new InvalidOperationException("The Vulkan swapchain extension could not be loaded.");
    }

    private void CreateCommandInfrastructure()
    {
        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamily,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
        };

        Check(_vk.CreateCommandPool(_device, &createInfo, null, out _transferCommandPool), "create the Vulkan transfer command pool");
    }

    private void CreateDescriptorPool()
    {
        var poolSizes = stackalloc DescriptorPoolSize[3]
        {
            new(DescriptorType.UniformBuffer, 4096),
            new(DescriptorType.SampledImage, 4096),
            new(DescriptorType.Sampler, 4096),
        };
        var createInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            MaxSets = 4096,
            PoolSizeCount = 3,
            PPoolSizes = poolSizes,
        };

        Check(_vk.CreateDescriptorPool(_device, &createInfo, null, out _descriptorPool), "create the Vulkan descriptor pool");
    }

    private void CreateFrameContexts()
    {
        _frames = new VulkanFrameContext[FramesInFlight];

        for (var index = 0; index < _frames.Length; index++)
        {
            CommandPool commandPool = default;
            Semaphore imageAvailable = default;
            Semaphore renderFinished = default;
            Fence fence = default;

            try
            {
                var poolInfo = new CommandPoolCreateInfo
                {
                    SType = StructureType.CommandPoolCreateInfo,
                    QueueFamilyIndex = _graphicsQueueFamily,
                    Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
                };
                Check(_vk.CreateCommandPool(_device, &poolInfo, null, out commandPool), "create a Vulkan frame command pool");

                var allocateInfo = new CommandBufferAllocateInfo
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    CommandPool = commandPool,
                    Level = CommandBufferLevel.Primary,
                    CommandBufferCount = 1,
                };
                Check(_vk.AllocateCommandBuffers(_device, &allocateInfo, out var commandBuffer), "allocate a Vulkan frame command buffer");

                var semaphoreInfo = new SemaphoreCreateInfo
                {
                    SType = StructureType.SemaphoreCreateInfo,
                };
                Check(_vk.CreateSemaphore(_device, &semaphoreInfo, null, out imageAvailable), "create a Vulkan image semaphore");
                Check(_vk.CreateSemaphore(_device, &semaphoreInfo, null, out renderFinished), "create a Vulkan render semaphore");

                var fenceInfo = new FenceCreateInfo
                {
                    SType = StructureType.FenceCreateInfo,
                    Flags = FenceCreateFlags.SignaledBit,
                };
                Check(_vk.CreateFence(_device, &fenceInfo, null, out fence), "create a Vulkan frame fence");

                _frames[index] = new VulkanFrameContext
                {
                    CommandPool = commandPool,
                    CommandBuffer = commandBuffer,
                    ImageAvailable = imageAvailable,
                    RenderFinished = renderFinished,
                    InFlight = fence,
                };
            }
            catch
            {
                if (fence.Handle != 0)
                    _vk.DestroyFence(_device, fence, null);
                if (renderFinished.Handle != 0)
                    _vk.DestroySemaphore(_device, renderFinished, null);
                if (imageAvailable.Handle != 0)
                    _vk.DestroySemaphore(_device, imageAvailable, null);
                if (commandPool.Handle != 0)
                    _vk.DestroyCommandPool(_device, commandPool, null);
                throw;
            }
        }
    }

    private void DestroyFrameContexts()
    {
        var frames = _frames;
        _frames = [];
        var teardown = new TeardownCollector();

        foreach (var frame in frames)
        {
            if (frame is null)
                continue;

            if (_device.Handle == 0)
            {
                frame.TransientBuffers.Clear();
                continue;
            }

            teardown.Run(() => DestroyTransientBuffers(frame));
            if (frame.UploadArena is { } uploadArena)
            {
                frame.UploadArena = null;
                teardown.Run(() => DestroyBuffer(uploadArena));
            }
            if (frame.InFlight.Handle != 0)
                teardown.Run(() => _vk.DestroyFence(_device, frame.InFlight, null));
            if (frame.RenderFinished.Handle != 0)
                teardown.Run(() => _vk.DestroySemaphore(_device, frame.RenderFinished, null));
            if (frame.ImageAvailable.Handle != 0)
                teardown.Run(() => _vk.DestroySemaphore(_device, frame.ImageAvailable, null));
            if (frame.CommandPool.Handle != 0)
                teardown.Run(() => _vk.DestroyCommandPool(_device, frame.CommandPool, null));
        }

        teardown.ThrowIfFailed("One or more errors occurred while destroying Vulkan frame contexts.");
    }

    private bool TryGetQueueFamilies(PhysicalDevice device, out uint graphicsFamily, out uint presentFamily)
    {
        graphicsFamily = uint.MaxValue;
        presentFamily = uint.MaxValue;
        uint familyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref familyCount, null);
        var properties = new QueueFamilyProperties[familyCount];

        fixed (QueueFamilyProperties* propertiesPointer = properties)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref familyCount, propertiesPointer);
        }

        for (uint index = 0; index < familyCount; index++)
        {
            if (properties[index].QueueCount > 0 &&
                (properties[index].QueueFlags & QueueFlags.GraphicsBit) != 0 &&
                graphicsFamily == uint.MaxValue)
            {
                graphicsFamily = index;
            }

            _surfaceApi.GetPhysicalDeviceSurfaceSupport(device, index, _surface, out var presentSupported);
            if (properties[index].QueueCount > 0 && presentSupported && presentFamily == uint.MaxValue)
                presentFamily = index;
        }

        return graphicsFamily != uint.MaxValue && presentFamily != uint.MaxValue;
    }

    private bool HasSwapchainSupport(PhysicalDevice device)
    {
        uint formatCount = 0;
        uint presentModeCount = 0;
        _surfaceApi.GetPhysicalDeviceSurfaceFormats(device, _surface, ref formatCount, null);
        _surfaceApi.GetPhysicalDeviceSurfacePresentModes(device, _surface, ref presentModeCount, null);
        return formatCount > 0 && presentModeCount > 0;
    }

    private bool IsDeviceExtensionAvailable(PhysicalDevice device, string extensionName)
    {
        uint count = 0;
        Check(
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, null),
            "enumerate Vulkan device extensions");
        var properties = new ExtensionProperties[count];

        fixed (ExtensionProperties* propertiesPointer = properties)
        {
            Check(
                _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, propertiesPointer),
                "enumerate Vulkan device extensions");
        }

        return properties.Any(property => GetExtensionName(property) == extensionName);
    }

    private void ValidateInstanceExtensions(IEnumerable<string> requiredExtensions)
    {
        uint count = 0;
        Check(_vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, null), "enumerate Vulkan instance extensions");
        var properties = new ExtensionProperties[count];

        fixed (ExtensionProperties* propertiesPointer = properties)
        {
            Check(
                _vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, propertiesPointer),
                "enumerate Vulkan instance extensions");
        }

        var available = properties.Select(GetExtensionName).ToHashSet(StringComparer.Ordinal);
        foreach (var extension in requiredExtensions)
        {
            if (!available.Contains(extension))
                throw new InvalidOperationException($"The Vulkan instance extension {extension} is unavailable.");
        }
    }

    private bool IsInstanceLayerAvailable(string layerName)
    {
        uint count = 0;
        Check(_vk.EnumerateInstanceLayerProperties(ref count, null), "enumerate Vulkan instance layers");
        var properties = new LayerProperties[count];

        fixed (LayerProperties* propertiesPointer = properties)
        {
            Check(_vk.EnumerateInstanceLayerProperties(ref count, propertiesPointer), "enumerate Vulkan instance layers");
        }

        foreach (var property in properties)
        {
            if (Marshal.PtrToStringUTF8((nint)property.LayerName) == layerName)
                return true;
        }

        return false;
    }

    private static string GetExtensionName(ExtensionProperties property)
        => Marshal.PtrToStringUTF8((nint)property.ExtensionName) ?? string.Empty;

    private static byte* AllocateUtf8(string value) => (byte*)Marshal.StringToCoTaskMemUTF8(value);

    private static byte** AllocateUtf8Array(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
            return null;

        var result = (byte**)NativeMemory.Alloc((nuint)values.Count, (nuint)sizeof(byte*));
        for (var index = 0; index < values.Count; index++)
            result[index] = AllocateUtf8(values[index]);
        return result;
    }

    private static void FreeUtf8(byte* value)
    {
        if (value != null)
            Marshal.FreeCoTaskMem((nint)value);
    }

    private static void FreeUtf8Array(byte** values, int count)
    {
        if (values == null)
            return;

        for (var index = 0; index < count; index++)
            FreeUtf8(values[index]);
        NativeMemory.Free(values);
    }

    private static void Check(Result result, string operation)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"Failed to {operation}: {result}.");
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandleW(string moduleName);
}
