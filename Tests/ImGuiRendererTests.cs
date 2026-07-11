using ElementEngine.UI;
using System.Runtime.InteropServices;
using Xunit;

namespace ElementEngine.Tests;

public sealed class ImGuiRendererTests
{
    [Theory]
    [InlineData(Architecture.X86)]
    [InlineData(Architecture.X64)]
    public void WindowsSupportsPackagedImGuiArchitectures(Architecture architecture)
    {
        Assert.True(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.Windows, architecture));
    }

    [Theory]
    [InlineData(Architecture.Arm)]
    [InlineData(Architecture.Arm64)]
    public void WindowsRejectsArchitecturesWithoutPackagedImGuiLibrary(Architecture architecture)
    {
        Assert.False(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.Windows, architecture));
    }

    [Theory]
    [InlineData(Architecture.Arm)]
    [InlineData(Architecture.Arm64)]
    [InlineData(Architecture.X86)]
    public void LinuxRejectsArchitecturesWithoutPackagedImGuiLibrary(Architecture architecture)
    {
        Assert.False(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.Linux, architecture));
    }

    [Fact]
    public void LinuxSupportsPackagedX64ImGuiLibrary()
    {
        Assert.True(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.Linux, Architecture.X64));
    }

    [Theory]
    [InlineData(Architecture.X64)]
    [InlineData(Architecture.Arm64)]
    public void MacOSSupportsUniversalImGuiLibrary(Architecture architecture)
    {
        Assert.True(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.MacOS, architecture));
    }

    [Fact]
    public void UnsupportedOperatingSystemsAreRejected()
    {
        Assert.False(ImGuiRenderer.IsNativePlatformSupported(ImGuiNativePlatform.Unsupported, Architecture.X64));
    }
}
