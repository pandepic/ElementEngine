using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ElementEngine;

[SupportedOSPlatform("windows")]
internal static class WindowsHidGamepadProvider
{
    // Kept well above the XInput provider's device ids 0-3 so the two never collide.
    private const int DeviceIdOffset = 0x1000;

    private const uint GenericRead = 0x80000000;
    private const uint FileShareReadWrite = 0x00000003;
    private const uint OpenExisting = 3;
    private const uint FileFlagOverlapped = 0x40000000;
    private const nint InvalidHandleValue = -1;
    private const int ErrorIoPending = 997;
    private const int ErrorIoIncomplete = 996;
    private const int HidpInput = 0;
    private const int HidpStatusSuccess = 0x00110000;
    private const int ConfigurationSuccess = 0;
    private const int ConfigurationBufferSmall = 26;
    private const uint PresentDeviceInterfacesFlag = 0;

    public static IReadOnlyList<INativeGamepad> Enumerate()
    {
        var devices = new List<INativeGamepad>();

        try
        {
            foreach (var path in GetHidInterfacePaths())
            {
                // "IG_" in the device path marks an XInput-owned interface (Microsoft's
                // documented heuristic); those pads belong to the XInput provider.
                if (path.Contains("IG_", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (HidGamepad.TryOpen(path, out var gamepad))
                    devices.Add(gamepad);
            }
        }
        catch
        {
            foreach (var device in devices)
                device.Dispose();

            throw;
        }

        return devices;
    }

    private static IReadOnlyList<string> GetHidInterfacePaths()
    {
        HidD_GetHidGuid(out var hidGuid);

        while (true)
        {
            if (CM_Get_Device_Interface_List_SizeW(out var length, ref hidGuid, null, PresentDeviceInterfacesFlag) != ConfigurationSuccess ||
                length <= 1)
            {
                return Array.Empty<string>();
            }

            var buffer = new char[length];
            var result = CM_Get_Device_Interface_ListW(ref hidGuid, null, buffer, length, PresentDeviceInterfacesFlag);
            if (result == ConfigurationBufferSmall)
                continue;
            if (result != ConfigurationSuccess)
                return Array.Empty<string>();

            return SplitDoubleNullTerminatedList(buffer);
        }
    }

    private static IReadOnlyList<string> SplitDoubleNullTerminatedList(char[] buffer)
    {
        var values = new List<string>();
        var start = 0;

        for (var index = 0; index < buffer.Length; index++)
        {
            if (buffer[index] != '\0')
                continue;

            if (index > start)
                values.Add(new string(buffer, start, index - start));

            start = index + 1;
        }

        return values;
    }

    private sealed unsafe class HidGamepad : INativeGamepad
    {
        private readonly nint _handle;
        private readonly nint _preparsedData;
        private readonly nint _reportBuffer;
        private readonly nint _overlapped;
        private readonly uint _reportLength;
        private readonly AxisBinding[] _axisBindings;
        private readonly HatBinding? _hat;
        private readonly Dictionary<ushort, int> _buttonIndicesByUsage = new();
        private readonly ushort[] _buttonUsageBuffer;
        private readonly Dictionary<GamepadAxis, float> _axes = new();
        private readonly HashSet<GamepadButtonType> _buttons = new();
        private bool _readPending;
        private bool _disposed;

        public int DeviceId { get; }
        public string Name { get; }
        public GamepadDeviceIdentifier Identifier { get; }

        private HidGamepad(nint handle, nint preparsedData, in HidpCaps caps, string path)
        {
            _handle = handle;
            _preparsedData = preparsedData;
            _reportLength = caps.InputReportByteLength;
            DeviceId = DeviceIdOffset + (GetStableHash(path) & 0x7FFFFF);

            var attributes = new HiddAttributes { Size = (uint)sizeof(HiddAttributes) };
            if (HidD_GetAttributes(handle, ref attributes))
            {
                Identifier = new GamepadDeviceIdentifier(
                    Bus: 0x03,
                    attributes.VendorId,
                    attributes.ProductId,
                    attributes.VersionNumber);
            }

            Name = ReadProductString(handle) ?? "HID Gamepad";

            ReadButtonCapabilities(caps);
            _axisBindings = ReadValueCapabilities(caps, out _hat);

            var maximumButtonUsages = HidP_MaxUsageListLength(HidpInput, HidGamepadLayout.ButtonUsagePage, preparsedData);
            _buttonUsageBuffer = new ushort[maximumButtonUsages];

            _reportBuffer = Marshal.AllocHGlobal((int)_reportLength);
            _overlapped = Marshal.AllocHGlobal(sizeof(NativeOverlapped));
        }

        public static bool TryOpen(string path, out INativeGamepad gamepad)
        {
            gamepad = null;
            var handle = CreateFileW(path, GenericRead, FileShareReadWrite, 0, OpenExisting, FileFlagOverlapped, 0);
            if (handle == InvalidHandleValue)
                return false;

            nint preparsedData = 0;
            try
            {
                if (!HidD_GetPreparsedData(handle, out preparsedData) ||
                    HidP_GetCaps(preparsedData, out var caps) != HidpStatusSuccess ||
                    caps.UsagePage != HidGamepadLayout.GenericDesktopUsagePage ||
                    (caps.Usage != HidGamepadLayout.JoystickUsage && caps.Usage != HidGamepadLayout.GamepadUsage) ||
                    caps.InputReportByteLength == 0)
                {
                    ReleaseNativeResources(handle, preparsedData);
                    return false;
                }

                gamepad = new HidGamepad(handle, preparsedData, caps, path);
                return true;
            }
            catch
            {
                ReleaseNativeResources(handle, preparsedData);
                throw;
            }
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            if (_disposed)
            {
                state = default;
                return false;
            }

            // Drain every queued report so the state reflects the newest one, then
            // leave a read pending for the next poll (mirrors the evdev provider).
            while (true)
            {
                var result = PumpRead();
                if (result == ReadResult.Failed)
                {
                    state = default;
                    return false;
                }

                if (result == ReadResult.Pending)
                    break;

                ProcessReport();
            }

            state = new NativeGamepadState(_axes, _buttons);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_readPending)
            {
                // Make sure the kernel is done writing to the report buffer before freeing it.
                CancelIoEx(_handle, _overlapped);
                GetOverlappedResult(_handle, (void*)_overlapped, out _, wait: true);
                _readPending = false;
            }

            ReleaseNativeResources(_handle, _preparsedData);
            Marshal.FreeHGlobal(_reportBuffer);
            Marshal.FreeHGlobal(_overlapped);
        }

        private enum ReadResult
        {
            Completed,
            Pending,
            Failed,
        }

        private ReadResult PumpRead()
        {
            if (!_readPending)
            {
                new Span<byte>((void*)_overlapped, sizeof(NativeOverlapped)).Clear();
                if (ReadFile(_handle, (void*)_reportBuffer, _reportLength, 0, (void*)_overlapped))
                    return ReadResult.Completed;

                if (Marshal.GetLastPInvokeError() != ErrorIoPending)
                    return ReadResult.Failed;

                _readPending = true;
            }

            if (GetOverlappedResult(_handle, (void*)_overlapped, out _, wait: false))
            {
                _readPending = false;
                return ReadResult.Completed;
            }

            if (Marshal.GetLastPInvokeError() == ErrorIoIncomplete)
                return ReadResult.Pending;

            _readPending = false;
            return ReadResult.Failed;
        }

        private void ProcessReport()
        {
            _buttons.Clear();

            if (_buttonUsageBuffer.Length > 0)
            {
                var usageCount = (uint)_buttonUsageBuffer.Length;
                var status = HidP_GetUsages(
                    HidpInput,
                    HidGamepadLayout.ButtonUsagePage,
                    0,
                    _buttonUsageBuffer,
                    ref usageCount,
                    _preparsedData,
                    (byte*)_reportBuffer,
                    _reportLength);

                if (status == HidpStatusSuccess)
                {
                    for (var index = 0; index < usageCount; index++)
                    {
                        if (!_buttonIndicesByUsage.TryGetValue(_buttonUsageBuffer[index], out var buttonIndex))
                            continue;

                        var button = HidGamepadLayout.GetButtonForIndex(buttonIndex);
                        if (button != GamepadButtonType.Invalid)
                            _buttons.Add(button);
                    }
                }
            }

            foreach (var binding in _axisBindings)
            {
                if (!TryGetUsageValue(binding.Usage, binding.LinkCollection, binding.LogicalMin, binding.BitSize, out var value))
                    continue;

                _axes[binding.Axis] = HidGamepadLayout.ScaleAxisValue(
                    binding.Axis,
                    value,
                    binding.LogicalMin,
                    binding.LogicalMax);
            }

            if (_hat is { } hat &&
                TryGetUsageValue(HidGamepadLayout.HatSwitchUsage, hat.LinkCollection, hat.LogicalMin, hat.BitSize, out var hatValue))
            {
                HidGamepadLayout.AddHatButtons(
                    _buttons,
                    HidGamepadLayout.DecodeHatSwitch(hatValue, hat.LogicalMin, hat.LogicalMax));
            }
        }

        private bool TryGetUsageValue(ushort usage, ushort linkCollection, int logicalMin, int bitSize, out long value)
        {
            var status = HidP_GetUsageValue(
                HidpInput,
                HidGamepadLayout.GenericDesktopUsagePage,
                linkCollection,
                usage,
                out var rawValue,
                _preparsedData,
                (byte*)_reportBuffer,
                _reportLength);

            if (status != HidpStatusSuccess)
            {
                value = 0;
                return false;
            }

            value = logicalMin < 0 ? HidGamepadLayout.SignExtend(rawValue, bitSize) : rawValue;
            return true;
        }

        private void ReadButtonCapabilities(in HidpCaps caps)
        {
            if (caps.NumberInputButtonCaps == 0)
                return;

            var buttonCaps = new HidpButtonCaps[caps.NumberInputButtonCaps];
            var buttonCapsLength = caps.NumberInputButtonCaps;
            if (HidP_GetButtonCaps(HidpInput, buttonCaps, ref buttonCapsLength, _preparsedData) != HidpStatusSuccess)
                return;

            var usages = new SortedSet<ushort>();
            for (var index = 0; index < buttonCapsLength; index++)
            {
                ref readonly var cap = ref buttonCaps[index];
                if (cap.UsagePage != HidGamepadLayout.ButtonUsagePage)
                    continue;

                int usageMax = cap.IsRange != 0 ? cap.UsageMax : cap.UsageMin;
                for (int usage = cap.UsageMin; usage <= usageMax; usage++)
                    usages.Add((ushort)usage);
            }

            foreach (var usage in usages)
                _buttonIndicesByUsage.Add(usage, _buttonIndicesByUsage.Count);
        }

        private AxisBinding[] ReadValueCapabilities(in HidpCaps caps, out HatBinding? hat)
        {
            hat = null;
            if (caps.NumberInputValueCaps == 0)
                return Array.Empty<AxisBinding>();

            var valueCaps = new HidpValueCaps[caps.NumberInputValueCaps];
            var valueCapsLength = caps.NumberInputValueCaps;
            if (HidP_GetValueCaps(HidpInput, valueCaps, ref valueCapsLength, _preparsedData) != HidpStatusSuccess)
                return Array.Empty<AxisBinding>();

            var valueInfoByUsage = new Dictionary<ushort, ValueInfo>();
            for (var index = 0; index < valueCapsLength; index++)
            {
                ref readonly var cap = ref valueCaps[index];
                if (cap.UsagePage != HidGamepadLayout.GenericDesktopUsagePage)
                    continue;

                var info = new ValueInfo(cap.LinkCollection, cap.LogicalMin, cap.LogicalMax, cap.BitSize);
                int usageMax = cap.IsRange != 0 ? cap.UsageMax : cap.UsageMin;
                for (int usage = cap.UsageMin; usage <= usageMax; usage++)
                {
                    if (usage == HidGamepadLayout.HatSwitchUsage)
                        hat ??= new HatBinding(cap.LinkCollection, cap.LogicalMin, cap.LogicalMax, cap.BitSize);
                    else if (usage is >= HidGamepadLayout.AxisUsageX and <= HidGamepadLayout.AxisUsageRZ)
                        valueInfoByUsage.TryAdd((ushort)usage, info);
                }
            }

            var layout = HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>(valueInfoByUsage.Keys));
            var bindings = new List<AxisBinding>(layout.Count);
            foreach (var (usage, axis) in layout)
            {
                var info = valueInfoByUsage[usage];
                bindings.Add(new AxisBinding(axis, usage, info.LinkCollection, info.LogicalMin, info.LogicalMax, info.BitSize));
            }

            return bindings.ToArray();
        }

        private static string ReadProductString(nint handle)
        {
            var buffer = new char[127];
            if (!HidD_GetProductString(handle, buffer, (uint)(buffer.Length * sizeof(char))))
                return null;

            var terminator = Array.IndexOf(buffer, '\0');
            var name = new string(buffer, 0, terminator < 0 ? buffer.Length : terminator).Trim();
            return name.Length == 0 ? null : name;
        }

        private static void ReleaseNativeResources(nint handle, nint preparsedData)
        {
            if (preparsedData != 0)
                HidD_FreePreparsedData(preparsedData);

            CloseHandle(handle);
        }

        // string.GetHashCode is randomized per process, so hash manually to keep
        // device ids stable across enumerations.
        private static int GetStableHash(string value)
        {
            var hash = 2166136261u;
            foreach (var character in value)
            {
                hash ^= char.ToUpperInvariant(character);
                hash *= 16777619u;
            }

            return (int)hash;
        }

        private readonly record struct ValueInfo(ushort LinkCollection, int LogicalMin, int LogicalMax, int BitSize);

        private readonly record struct AxisBinding(
            GamepadAxis Axis,
            ushort Usage,
            ushort LinkCollection,
            int LogicalMin,
            int LogicalMax,
            int BitSize);

        private readonly record struct HatBinding(ushort LinkCollection, int LogicalMin, int LogicalMax, int BitSize);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeOverlapped
    {
        public nint Internal;
        public nint InternalHigh;
        public uint OffsetLow;
        public uint OffsetHigh;
        public nint EventHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HiddAttributes
    {
        public uint Size;
        public ushort VendorId;
        public ushort ProductId;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        public fixed ushort Reserved[17];
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    // The trailing fields mirror the Range arm of the native union; for non-range
    // capabilities the single usage lands in UsageMin.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct HidpButtonCaps
    {
        public ushort UsagePage;
        public byte ReportId;
        public byte IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        public byte IsRange;
        public byte IsStringRange;
        public byte IsDesignatorRange;
        public byte IsAbsolute;
        public fixed uint Reserved[10];
        public ushort UsageMin;
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;
        public ushort DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct HidpValueCaps
    {
        public ushort UsagePage;
        public byte ReportId;
        public byte IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        public byte IsRange;
        public byte IsStringRange;
        public byte IsDesignatorRange;
        public byte IsAbsolute;
        public byte HasNull;
        public byte Reserved;
        public ushort BitSize;
        public ushort ReportCount;
        public fixed ushort Reserved2[5];
        public uint UnitsExp;
        public uint Units;
        public int LogicalMin;
        public int LogicalMax;
        public int PhysicalMin;
        public int PhysicalMax;
        public ushort UsageMin;
        public ushort UsageMax;
        public ushort StringMin;
        public ushort StringMax;
        public ushort DesignatorMin;
        public ushort DesignatorMax;
        public ushort DataIndexMin;
        public ushort DataIndexMax;
    }

    [DllImport("hid")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetAttributes(nint device, ref HiddAttributes attributes);

    [DllImport("hid", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetProductString(nint device, [Out] char[] buffer, uint bufferLength);

    [DllImport("hid")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetPreparsedData(nint device, out nint preparsedData);

    [DllImport("hid")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_FreePreparsedData(nint preparsedData);

    [DllImport("hid")]
    private static extern int HidP_GetCaps(nint preparsedData, out HidpCaps caps);

    [DllImport("hid")]
    private static extern int HidP_GetButtonCaps(
        int reportType,
        [Out] HidpButtonCaps[] buttonCaps,
        ref ushort buttonCapsLength,
        nint preparsedData);

    [DllImport("hid")]
    private static extern int HidP_GetValueCaps(
        int reportType,
        [Out] HidpValueCaps[] valueCaps,
        ref ushort valueCapsLength,
        nint preparsedData);

    [DllImport("hid")]
    private static extern uint HidP_MaxUsageListLength(int reportType, ushort usagePage, nint preparsedData);

    [DllImport("hid")]
    private static extern unsafe int HidP_GetUsages(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        [Out] ushort[] usageList,
        ref uint usageLength,
        nint preparsedData,
        byte* report,
        uint reportLength);

    [DllImport("hid")]
    private static extern unsafe int HidP_GetUsageValue(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        nint preparsedData,
        byte* report,
        uint reportLength);

    [DllImport("cfgmgr32", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_Interface_List_SizeW(
        out uint length,
        ref Guid interfaceClassGuid,
        string deviceId,
        uint flags);

    [DllImport("cfgmgr32", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_Device_Interface_ListW(
        ref Guid interfaceClassGuid,
        string deviceId,
        [Out] char[] buffer,
        uint bufferLength,
        uint flags);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateFileW(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool CloseHandle(nint handle);

    [DllImport("kernel32", SetLastError = true)]
    private static extern unsafe bool ReadFile(
        nint handle,
        void* buffer,
        uint bytesToRead,
        nint bytesRead,
        void* overlapped);

    [DllImport("kernel32", SetLastError = true)]
    private static extern unsafe bool GetOverlappedResult(
        nint handle,
        void* overlapped,
        out uint bytesTransferred,
        bool wait);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool CancelIoEx(nint handle, nint overlapped);
}
