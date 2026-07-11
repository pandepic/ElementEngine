using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal static class NativeDisplay
{
    public static DisplayMode GetCurrentDisplayMode(int displayIndex)
    {
        if (displayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(displayIndex));

        if (OperatingSystem.IsWindows())
            return GetWindowsDisplayMode(displayIndex);
        if (OperatingSystem.IsLinux())
            return GetLinuxDisplayMode(displayIndex);
        if (OperatingSystem.IsMacOS())
            return GetMacOSDisplayMode(displayIndex);

        throw new PlatformNotSupportedException("ElementEngine supports Windows, Linux, and macOS desktop platforms.");
    }

    private static DisplayMode GetLinuxDisplayMode(int displayIndex)
    {
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var x11Display = Environment.GetEnvironmentVariable("DISPLAY");
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        var preferWayland = !string.IsNullOrWhiteSpace(waylandDisplay) &&
                            (string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase) ||
                             string.IsNullOrWhiteSpace(x11Display));

        if (preferWayland)
        {
            try
            {
                using var window = new WaylandGameWindow(new WindowCreateInfo(
                    0,
                    0,
                    1,
                    1,
                    WindowState.Hidden,
                    "ElementEngine display query"));
                return window.GetCurrentDisplayMode(displayIndex);
            }
            catch (Exception exception) when (
                !string.IsNullOrWhiteSpace(x11Display) &&
                exception is DllNotFoundException or EntryPointNotFoundException or
                    InvalidOperationException or NotSupportedException or PlatformNotSupportedException)
            {
            }
        }

        return GetX11DisplayMode(displayIndex);
    }

    private static DisplayMode GetWindowsDisplayMode(int displayIndex)
    {
        var devices = new List<DisplayDevice>();
        for (uint deviceIndex = 0; ; deviceIndex++)
        {
            var device = new DisplayDevice
            {
                Size = (uint)Marshal.SizeOf<DisplayDevice>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceId = string.Empty,
                DeviceKey = string.Empty,
            };

            if (!EnumDisplayDevices(null, deviceIndex, ref device, 0))
                break;

            if ((device.StateFlags & DisplayDeviceAttachedToDesktop) == 0 ||
                (device.StateFlags & DisplayDeviceMirroringDriver) != 0)
            {
                continue;
            }

            if ((device.StateFlags & DisplayDevicePrimaryDevice) != 0)
                devices.Insert(0, device);
            else
                devices.Add(device);
        }

        if (displayIndex >= devices.Count)
        {
            if (displayIndex != 0 || devices.Count > 0)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");
        }

        var mode = new DevMode
        {
            DeviceName = string.Empty,
            FormName = string.Empty,
            Size = (ushort)Marshal.SizeOf<DevMode>(),
        };

        var deviceName = devices.Count == 0 ? null : devices[displayIndex].DeviceName;
        if (EnumDisplaySettings(deviceName, CurrentSettings, ref mode))
            return new DisplayMode((int)mode.PelsWidth, (int)mode.PelsHeight, (int)mode.DisplayFrequency);

        if (devices.Count > 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to query display {displayIndex}.");

        return new DisplayMode(GetSystemMetrics(0), GetSystemMetrics(1), 0);
    }

    private static DisplayMode GetX11DisplayMode(int displayIndex)
    {
        X11Native.EnsureThreadSupport();
        var display = X11Native.XOpenDisplay(0);
        if (display == 0)
            throw new InvalidOperationException("Unable to connect to an X11 display.");

        try
        {
            var displayModes = X11Native.GetDisplayModes(display);
            if (displayIndex >= displayModes.Count)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

            var mode = displayModes[displayIndex];
            return displayIndex == 0
                ? mode with
                {
                    RefreshRate = X11Native.GetRefreshRate(display, X11Native.XDefaultScreen(display)),
                }
                : mode;
        }
        finally
        {
            X11Native.XCloseDisplay(display);
        }
    }

    private static unsafe DisplayMode GetMacOSDisplayMode(int displayIndex)
    {
        var result = CGGetActiveDisplayList(0, 0, out var displayCount);
        if (result != 0)
            throw new InvalidOperationException($"CGGetActiveDisplayList failed with error {result}.");

        if (displayCount == 0)
        {
            if (displayIndex != 0)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

            return GetMacOSDisplayMode(CGMainDisplayID());
        }

        var displays = new uint[displayCount];
        fixed (uint* displayPointer = displays)
        {
            result = CGGetActiveDisplayList(displayCount, (nint)displayPointer, out displayCount);
        }

        if (result != 0)
            throw new InvalidOperationException($"CGGetActiveDisplayList failed with error {result}.");
        if (displayIndex >= displayCount)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

        var mainDisplay = CGMainDisplayID();
        var mainDisplayIndex = Array.IndexOf(displays, mainDisplay, 0, checked((int)displayCount));
        if (mainDisplayIndex > 0)
            (displays[0], displays[mainDisplayIndex]) = (displays[mainDisplayIndex], displays[0]);

        return GetMacOSDisplayMode(displays[displayIndex]);
    }

    private static DisplayMode GetMacOSDisplayMode(uint display)
    {
        var mode = CGDisplayCopyDisplayMode(display);
        var refreshRate = mode == 0 ? 0 : (int)Math.Round(CGDisplayModeGetRefreshRate(mode));

        // CGDisplayPixelsWide/High return points on Retina scaled modes; the mode's pixel
        // size matches what CocoaGameWindow reports (frame * backingScaleFactor).
        var width = mode == 0 ? CGDisplayPixelsWide(display) : CGDisplayModeGetPixelWidth(mode);
        var height = mode == 0 ? CGDisplayPixelsHigh(display) : CGDisplayModeGetPixelHeight(mode);

        if (mode != 0)
            CFRelease(mode);

        return new DisplayMode(checked((int)width), checked((int)height), refreshRate);
    }

    private const int CurrentSettings = -1;
    private const uint DisplayDeviceAttachedToDesktop = 0x00000001;
    private const uint DisplayDevicePrimaryDevice = 0x00000004;
    private const uint DisplayDeviceMirroringDriver = 0x00000008;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;
        public ushort LogPixels;
        public uint BitsPerPel;
        public uint PelsWidth;
        public uint PelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint ICMMethod;
        public uint ICMIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;
    }

    [DllImport("user32", EntryPoint = "EnumDisplaySettingsW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode mode);

    [DllImport("user32", EntryPoint = "EnumDisplayDevicesW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string device,
        uint deviceIndex,
        ref DisplayDevice displayDevice,
        uint flags);

    [DllImport("user32")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern uint CGMainDisplayID();

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern int CGGetActiveDisplayList(
        uint maximumDisplays,
        nint activeDisplays,
        out uint displayCount);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGDisplayPixelsWide(uint display);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGDisplayPixelsHigh(uint display);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGDisplayCopyDisplayMode(uint display);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern double CGDisplayModeGetRefreshRate(nint mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGDisplayModeGetPixelWidth(nint mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGDisplayModeGetPixelHeight(nint mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint value);
}
