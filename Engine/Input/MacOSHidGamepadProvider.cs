using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal static class MacOSHidGamepadProvider
{
    // Keeps IOHID device ids away from the GameController provider's pointer-hash
    // ids, which land in the low positive range on macOS heaps.
    private const int DeviceIdBase = 0x40000000;

    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string IOKitLibrary = "/System/Library/Frameworks/IOKit.framework/IOKit";
    private const string GameControllerFramework = "/System/Library/Frameworks/GameController.framework/GameController";

    private const uint Utf8StringEncoding = 0x08000100;
    private const nint SignedInt32NumberType = 3;
    private const uint InputMiscElementType = 1;
    private const uint InputAxisElementType = 3;

    private static readonly nint CoreFoundationHandle = NativeLibrary.Load(CoreFoundationLibrary);
    private static readonly nint TypeDictionaryKeyCallbacks = NativeLibrary.GetExport(CoreFoundationHandle, "kCFTypeDictionaryKeyCallBacks");
    private static readonly nint TypeDictionaryValueCallbacks = NativeLibrary.GetExport(CoreFoundationHandle, "kCFTypeDictionaryValueCallBacks");
    private static readonly nint TypeArrayCallbacks = NativeLibrary.GetExport(CoreFoundationHandle, "kCFTypeArrayCallBacks");

    private static readonly nint GameControllerFrameworkHandle = NativeLibrary.Load(GameControllerFramework);
    private static readonly nint ControllerClass = objc_getClass("GCController");
    private static readonly nint RespondsToSelector = sel_registerName("respondsToSelector:");
    private static readonly nint SupportsHidDeviceSelector = sel_registerName("supportsHIDDevice:");

    private static readonly nint DeviceUsagePageKey = CreateNativeString("DeviceUsagePage");
    private static readonly nint DeviceUsageKey = CreateNativeString("DeviceUsage");
    private static readonly nint ProductKey = CreateNativeString("Product");
    private static readonly nint VendorIdKey = CreateNativeString("VendorID");
    private static readonly nint ProductIdKey = CreateNativeString("ProductID");
    private static readonly nint VersionNumberKey = CreateNativeString("VersionNumber");

    private static readonly Lazy<nint> ManagerInstance = new(CreateManager);

    public static IReadOnlyList<INativeGamepad> Enumerate()
    {
        // Without supportsHIDDevice: (macOS 11+) there is no reliable way to dedup
        // against GameController pads, so skip IOHID enumeration entirely rather
        // than risk reporting the same pad twice.
        if (ControllerClass == 0 || !SendBoolean(ControllerClass, RespondsToSelector, SupportsHidDeviceSelector))
            return Array.Empty<INativeGamepad>();

        var manager = ManagerInstance.Value;
        if (manager == 0)
            return Array.Empty<INativeGamepad>();

        var deviceSet = IOHIDManagerCopyDevices(manager);
        if (deviceSet == 0)
            return Array.Empty<INativeGamepad>();

        var devices = new List<INativeGamepad>();
        try
        {
            var count = (int)CFSetGetCount(deviceSet);
            if (count <= 0)
                return devices;

            var deviceRefs = new nint[count];
            CFSetGetValues(deviceSet, deviceRefs);

            foreach (var device in deviceRefs)
            {
                if (device == 0 || SendBoolean(ControllerClass, SupportsHidDeviceSelector, device))
                    continue;

                if (IOHidGamepad.TryOpen(device, out var gamepad))
                    devices.Add(gamepad);
            }

            return devices;
        }
        catch
        {
            foreach (var device in devices)
                device.Dispose();

            throw;
        }
        finally
        {
            CFRelease(deviceSet);
            GC.KeepAlive(GameControllerFrameworkHandle);
        }
    }

    private static nint CreateManager()
    {
        var manager = IOHIDManagerCreate(0, 0);
        if (manager == 0)
            return 0;

        var matching = CreateUsageMatchingArray();
        if (matching == 0)
        {
            CFRelease(manager);
            return 0;
        }

        IOHIDManagerSetDeviceMatchingMultiple(manager, matching);
        CFRelease(matching);
        return manager;
    }

    private static unsafe nint CreateUsageMatchingArray()
    {
        var joystick = CreateUsageMatchingDictionary(HidGamepadLayout.JoystickUsage);
        var gamepad = CreateUsageMatchingDictionary(HidGamepadLayout.GamepadUsage);
        if (joystick == 0 || gamepad == 0)
        {
            if (joystick != 0)
                CFRelease(joystick);
            if (gamepad != 0)
                CFRelease(gamepad);
            return 0;
        }

        var values = stackalloc nint[2] { joystick, gamepad };
        var array = CFArrayCreate(0, values, 2, TypeArrayCallbacks);
        CFRelease(joystick);
        CFRelease(gamepad);
        return array;
    }

    private static unsafe nint CreateUsageMatchingDictionary(int usage)
    {
        var usagePage = (int)HidGamepadLayout.GenericDesktopUsagePage;
        var usagePageNumber = CFNumberCreate(0, SignedInt32NumberType, &usagePage);
        var usageNumber = CFNumberCreate(0, SignedInt32NumberType, &usage);
        if (usagePageNumber == 0 || usageNumber == 0)
        {
            if (usagePageNumber != 0)
                CFRelease(usagePageNumber);
            if (usageNumber != 0)
                CFRelease(usageNumber);
            return 0;
        }

        var keys = stackalloc nint[2] { DeviceUsagePageKey, DeviceUsageKey };
        var values = stackalloc nint[2] { usagePageNumber, usageNumber };
        var dictionary = CFDictionaryCreate(0, keys, values, 2, TypeDictionaryKeyCallbacks, TypeDictionaryValueCallbacks);
        CFRelease(usagePageNumber);
        CFRelease(usageNumber);
        return dictionary;
    }

    private sealed class IOHidGamepad : INativeGamepad
    {
        private nint _device;
        private readonly nint[] _buttonElements;
        private readonly AxisElement[] _axisElements;
        private readonly HatElement? _hat;
        private readonly Dictionary<GamepadAxis, float> _axes = new();
        private readonly HashSet<GamepadButtonType> _buttons = new();
        private bool _disposed;

        public int DeviceId { get; }
        public string Name { get; }
        public GamepadDeviceIdentifier Identifier { get; }

        private IOHidGamepad(
            nint device,
            SortedDictionary<uint, nint> buttonElementsByUsage,
            Dictionary<ushort, ValueElement> axisElementsByUsage,
            ValueElement? hat)
        {
            DeviceId = CreateDeviceId(device);
            Name = GetStringProperty(device, ProductKey) ?? "HID Gamepad";
            Identifier = new GamepadDeviceIdentifier(
                Bus: 0x03,
                (ushort)GetNumberProperty(device, VendorIdKey),
                (ushort)GetNumberProperty(device, ProductIdKey),
                (ushort)GetNumberProperty(device, VersionNumberKey));

            _buttonElements = new nint[buttonElementsByUsage.Count];
            buttonElementsByUsage.Values.CopyTo(_buttonElements, 0);

            var layout = HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>(axisElementsByUsage.Keys));
            var axisElements = new List<AxisElement>(layout.Count);
            foreach (var (usage, axis) in layout)
            {
                var element = axisElementsByUsage[usage];
                axisElements.Add(new AxisElement(axis, element.Element, element.LogicalMin, element.LogicalMax));
            }

            _axisElements = axisElements.ToArray();
            if (hat is { } hatElement)
                _hat = new HatElement(hatElement.Element, hatElement.LogicalMin, hatElement.LogicalMax);

            _device = CFRetain(device);
            foreach (var element in _buttonElements)
                CFRetain(element);
            foreach (var axisElement in _axisElements)
                CFRetain(axisElement.Element);
            if (_hat is { } retainedHat)
                CFRetain(retainedHat.Element);
        }

        public static bool TryOpen(nint device, out INativeGamepad gamepad)
        {
            gamepad = null;

            if (IOHIDDeviceOpen(device, 0) != 0)
                return false;

            var elementArray = IOHIDDeviceCopyMatchingElements(device, 0, 0);
            if (elementArray == 0)
            {
                IOHIDDeviceClose(device, 0);
                return false;
            }

            try
            {
                var buttonElementsByUsage = new SortedDictionary<uint, nint>();
                var axisElementsByUsage = new Dictionary<ushort, ValueElement>();
                ValueElement? hat = null;

                var elementCount = (int)CFArrayGetCount(elementArray);
                for (var index = 0; index < elementCount; index++)
                {
                    var element = CFArrayGetValueAtIndex(elementArray, index);
                    if (element == 0)
                        continue;

                    var elementType = IOHIDElementGetType(element);
                    if (elementType is < InputMiscElementType or > InputAxisElementType)
                        continue;

                    var usagePage = IOHIDElementGetUsagePage(element);
                    var usage = IOHIDElementGetUsage(element);
                    if (usagePage == HidGamepadLayout.ButtonUsagePage)
                    {
                        buttonElementsByUsage.TryAdd(usage, element);
                        continue;
                    }

                    if (usagePage != HidGamepadLayout.GenericDesktopUsagePage)
                        continue;

                    if (usage == HidGamepadLayout.HatSwitchUsage)
                    {
                        hat ??= new ValueElement(
                            element,
                            IOHIDElementGetLogicalMin(element),
                            IOHIDElementGetLogicalMax(element));
                    }
                    else if (usage is >= HidGamepadLayout.AxisUsageX and <= HidGamepadLayout.AxisUsageRZ)
                    {
                        axisElementsByUsage.TryAdd(
                            (ushort)usage,
                            new ValueElement(
                                element,
                                IOHIDElementGetLogicalMin(element),
                                IOHIDElementGetLogicalMax(element)));
                    }
                }

                if (buttonElementsByUsage.Count == 0 && axisElementsByUsage.Count == 0)
                {
                    IOHIDDeviceClose(device, 0);
                    return false;
                }

                gamepad = new IOHidGamepad(device, buttonElementsByUsage, axisElementsByUsage, hat);
                return true;
            }
            catch
            {
                IOHIDDeviceClose(device, 0);
                throw;
            }
            finally
            {
                CFRelease(elementArray);
            }
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            if (_disposed)
            {
                state = default;
                return false;
            }

            _buttons.Clear();
            for (var index = 0; index < _buttonElements.Length; index++)
            {
                if (!TryGetElementValue(_buttonElements[index], out var value))
                {
                    state = default;
                    return false;
                }

                if (value == 0)
                    continue;

                var button = HidGamepadLayout.GetButtonForIndex(index);
                if (button != GamepadButtonType.Invalid)
                    _buttons.Add(button);
            }

            foreach (var axisElement in _axisElements)
            {
                if (!TryGetElementValue(axisElement.Element, out var value))
                {
                    state = default;
                    return false;
                }

                _axes[axisElement.Axis] = HidGamepadLayout.ScaleAxisValue(
                    axisElement.Axis,
                    value,
                    axisElement.LogicalMin,
                    axisElement.LogicalMax);
            }

            if (_hat is { } hat)
            {
                if (!TryGetElementValue(hat.Element, out var value))
                {
                    state = default;
                    return false;
                }

                HidGamepadLayout.AddHatButtons(
                    _buttons,
                    HidGamepadLayout.DecodeHatSwitch(value, hat.LogicalMin, hat.LogicalMax));
            }

            state = new NativeGamepadState(_axes, _buttons);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var element in _buttonElements)
                CFRelease(element);
            foreach (var axisElement in _axisElements)
                CFRelease(axisElement.Element);
            if (_hat is { } hat)
                CFRelease(hat.Element);

            IOHIDDeviceClose(_device, 0);
            CFRelease(_device);
            _device = 0;
        }

        private bool TryGetElementValue(nint element, out long value)
        {
            if (IOHIDDeviceGetValue(_device, element, out var valueRef) != 0 || valueRef == 0)
            {
                value = 0;
                return false;
            }

            value = IOHIDValueGetIntegerValue(valueRef);
            return true;
        }

        private static int CreateDeviceId(nint device)
        {
            var service = IOHIDDeviceGetService(device);
            ulong entryId = 0;
            if (service == 0 || IORegistryEntryGetRegistryEntryID(service, out entryId) != 0 || entryId == 0)
                entryId = (ulong)device;

            var folded = (int)(entryId ^ (entryId >> 32));
            return DeviceIdBase | (folded & 0x3FFFFFFF);
        }

        private static int GetNumberProperty(nint device, nint key)
        {
            var value = IOHIDDeviceGetProperty(device, key);
            if (value == 0 || CFGetTypeID(value) != CFNumberGetTypeID())
                return 0;

            var number = 0;
            unsafe
            {
                CFNumberGetValue(value, SignedInt32NumberType, &number);
            }

            return number;
        }

        private static string GetStringProperty(nint device, nint key)
        {
            var value = IOHIDDeviceGetProperty(device, key);
            if (value == 0 || CFGetTypeID(value) != CFStringGetTypeID())
                return null;

            var buffer = new byte[256];
            if (!CFStringGetCString(value, buffer, buffer.Length, Utf8StringEncoding))
                return null;

            var terminator = Array.IndexOf(buffer, (byte)0);
            var name = System.Text.Encoding.UTF8.GetString(buffer, 0, terminator < 0 ? buffer.Length : terminator).Trim();
            return name.Length == 0 ? null : name;
        }

        private readonly record struct ValueElement(nint Element, long LogicalMin, long LogicalMax);

        private readonly record struct AxisElement(GamepadAxis Axis, nint Element, long LogicalMin, long LogicalMax);

        private readonly record struct HatElement(nint Element, long LogicalMin, long LogicalMax);
    }

    private static nint CreateNativeString(string value) => CFStringCreateWithCString(0, value, Utf8StringEncoding);

    private static bool SendBoolean(nint target, nint selector, nint value)
        => objc_msgSend_bool_pointer(target, selector, value);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_pointer(nint target, nint selector, nint value);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFRetain(nint value);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(nint value);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFGetTypeID(nint value);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFNumberGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFStringGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFStringCreateWithCString(
        nint allocator,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
        uint encoding);

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool CFStringGetCString(nint value, [Out] byte[] buffer, nint bufferSize, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    private static extern unsafe nint CFNumberCreate(nint allocator, nint numberType, void* valuePointer);

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern unsafe bool CFNumberGetValue(nint number, nint numberType, void* valuePointer);

    [DllImport(CoreFoundationLibrary)]
    private static extern unsafe nint CFDictionaryCreate(
        nint allocator,
        nint* keys,
        nint* values,
        nint valueCount,
        nint keyCallbacks,
        nint valueCallbacks);

    [DllImport(CoreFoundationLibrary)]
    private static extern unsafe nint CFArrayCreate(nint allocator, nint* values, nint valueCount, nint callbacks);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetCount(nint array);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetValueAtIndex(nint array, nint index);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFSetGetCount(nint set);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFSetGetValues(nint set, [Out] nint[] values);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDManagerCreate(nint allocator, uint options);

    [DllImport(IOKitLibrary)]
    private static extern void IOHIDManagerSetDeviceMatchingMultiple(nint manager, nint matching);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDManagerCopyDevices(nint manager);

    [DllImport(IOKitLibrary)]
    private static extern int IOHIDDeviceOpen(nint device, uint options);

    [DllImport(IOKitLibrary)]
    private static extern int IOHIDDeviceClose(nint device, uint options);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDDeviceCopyMatchingElements(nint device, nint matching, uint options);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDDeviceGetProperty(nint device, nint key);

    [DllImport(IOKitLibrary)]
    private static extern int IOHIDDeviceGetValue(nint device, nint element, out nint value);

    [DllImport(IOKitLibrary)]
    private static extern uint IOHIDDeviceGetService(nint device);

    [DllImport(IOKitLibrary)]
    private static extern uint IOHIDElementGetType(nint element);

    [DllImport(IOKitLibrary)]
    private static extern uint IOHIDElementGetUsagePage(nint element);

    [DllImport(IOKitLibrary)]
    private static extern uint IOHIDElementGetUsage(nint element);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDElementGetLogicalMin(nint element);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDElementGetLogicalMax(nint element);

    [DllImport(IOKitLibrary)]
    private static extern nint IOHIDValueGetIntegerValue(nint value);

    [DllImport(IOKitLibrary)]
    private static extern int IORegistryEntryGetRegistryEntryID(uint entry, out ulong entryId);
}
