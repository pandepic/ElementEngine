using System.Runtime.InteropServices;
using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class NativeInteropTests
{
    [Fact]
    public void RandRMonitorInfoMatchesNativeLayout()
    {
        var expectedSize = IntPtr.Size == 8 ? 56 : 44;
        var expectedOutputsOffset = IntPtr.Size == 8 ? 48 : 40;

        Assert.Equal(expectedSize, Marshal.SizeOf<XRRMonitorInfo>());
        Assert.Equal(expectedOutputsOffset, Marshal.OffsetOf<XRRMonitorInfo>(nameof(XRRMonitorInfo.Outputs)).ToInt32());
    }

    [Fact]
    public void RandRModeSwitchStructuresMatchNativeLayout()
    {
        var is64Bit = IntPtr.Size == 8;

        Assert.Equal(is64Bit ? 64 : 32, Marshal.SizeOf<XRRScreenResources>());
        Assert.Equal(is64Bit ? 24 : 12, Marshal.OffsetOf<XRRScreenResources>(nameof(XRRScreenResources.Crtcs)).ToInt32());
        Assert.Equal(is64Bit ? 80 : 56, Marshal.SizeOf<XRRModeInfo>());
        Assert.Equal(is64Bit ? 72 : 52, Marshal.OffsetOf<XRRModeInfo>(nameof(XRRModeInfo.ModeFlags)).ToInt32());
        Assert.Equal(is64Bit ? 64 : 48, Marshal.SizeOf<XRRCrtcInfo>());
        Assert.Equal(is64Bit ? 40 : 32, Marshal.OffsetOf<XRRCrtcInfo>(nameof(XRRCrtcInfo.Outputs)).ToInt32());
        Assert.Equal(is64Bit ? 96 : 56, Marshal.SizeOf<XRROutputInfo>());
        Assert.Equal(is64Bit ? 88 : 52, Marshal.OffsetOf<XRROutputInfo>(nameof(XRROutputInfo.Modes)).ToInt32());
    }

    [Fact]
    public void TextInputStructuresMatchNativeLayout()
    {
        Assert.Equal(IntPtr.Size * 2, Marshal.SizeOf<CocoaNative.NativeRange>());
        Assert.Equal(32, Marshal.SizeOf<CocoaNative.NativeRect>());
    }

    [Fact]
    public void WaylandInteropStructuresMatchNativeLayout()
    {
        var is64Bit = IntPtr.Size == 8;

        Assert.Equal(8, Marshal.SizeOf<WaylandNative.PollFileDescriptor>());
        Assert.Equal(is64Bit ? 24 : 12, Marshal.SizeOf<WaylandNative.WlArray>());
        Assert.Equal(is64Bit ? 40 : 24, Marshal.SizeOf<WaylandNative.WlInterface>());
        Assert.Equal(is64Bit ? 24 : 12, Marshal.SizeOf<WaylandNative.WlMessage>());
        Assert.Equal(is64Bit ? 24 : 12, Marshal.SizeOf<WaylandNative.WlCursor>());
        Assert.Equal(20, Marshal.SizeOf<WaylandNative.WlCursorImage>());
    }

    [Fact]
    public void MacOSDisplayModeApisAreAvailable()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var coreGraphics = NativeLibrary.Load(
            "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics");

        try
        {
            string[] symbols =
            [
                "CGDisplayCopyDisplayMode",
                "CGDisplayCopyAllDisplayModes",
                "CGDisplaySetDisplayMode",
                "CGDisplayModeGetWidth",
                "CGDisplayModeGetHeight",
                "CGDisplayModeGetPixelWidth",
                "CGDisplayModeGetPixelHeight",
                "CGDisplayModeGetRefreshRate",
                "CGDisplayModeIsUsableForDesktopGUI",
                "CGWindowLevelForKey",
            ];

            foreach (var symbol in symbols)
                Assert.True(NativeLibrary.TryGetExport(coreGraphics, symbol, out _), $"Missing CoreGraphics symbol {symbol}.");
        }
        finally
        {
            NativeLibrary.Free(coreGraphics);
        }
    }
}
