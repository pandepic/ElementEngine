using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class VulkanConversionTests
{
    [Theory]
    [InlineData(NativeWindowKind.X11, "VK_KHR_xlib_surface")]
    [InlineData(NativeWindowKind.Wayland, "VK_KHR_wayland_surface")]
    [InlineData(NativeWindowKind.Win32, "VK_KHR_win32_surface")]
    public void NativeWindowsSelectTheMatchingVulkanSurfaceExtension(
        NativeWindowKind nativeKind,
        string expected)
    {
        Assert.Equal(expected, VulkanGraphicsBackend.GetPlatformSurfaceExtension(nativeKind));
    }

    [Fact]
    public void ViewportPreservesTopLeftClipSpaceDirection()
    {
        var viewport = VulkanConversions.ToViewport(new Viewport(10, 20, 640, 480, 0.1f, 0.9f));

        Assert.Equal(10f, viewport.X);
        Assert.Equal(500f, viewport.Y);
        Assert.Equal(640f, viewport.Width);
        Assert.Equal(-480f, viewport.Height);
        Assert.Equal(0.1f, viewport.MinDepth);
        Assert.Equal(0.9f, viewport.MaxDepth);
    }
}
