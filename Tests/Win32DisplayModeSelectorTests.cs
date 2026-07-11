using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class Win32DisplayModeSelectorTests
{
    [Theory]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.FullScreen, (int)Win32DisplayModeTransition.EnterExclusive)]
    [InlineData(WindowState.FullScreen, WindowState.BorderlessFullScreen, (int)Win32DisplayModeTransition.RestoreDesktop)]
    [InlineData(WindowState.FullScreen, WindowState.FullScreen, (int)Win32DisplayModeTransition.None)]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.BorderlessFullScreen, (int)Win32DisplayModeTransition.None)]
    public void FullScreenTransitionsSelectTheRequiredDisplayModeAction(
        WindowState current,
        WindowState target,
        int expected)
    {
        Assert.Equal((Win32DisplayModeTransition)expected, Win32FullScreenTransition.GetDisplayModeTransition(current, target));
    }

    [Fact]
    public void FullScreenTransitionsRejectWindowedStates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Win32FullScreenTransition.GetDisplayModeTransition(WindowState.Normal, WindowState.FullScreen));
    }

    [Fact]
    public void SelectsCurrentColorDepthAndRefreshRate()
    {
        Win32DisplayMode[] modes =
        [
            new(1920, 1080, 32, 144, 0),
            new(1920, 1080, 32, 60, 1),
            new(1920, 1080, 64, 60, 2),
        ];

        var found = Win32DisplayModeSelector.TrySelect(modes, 1920, 1080, 32, 60, out var selected);

        Assert.True(found);
        Assert.Equal(modes[1], selected);
    }

    [Fact]
    public void SelectsClosestRefreshRateAndPrefersHigherRateForATie()
    {
        Win32DisplayMode[] modes =
        [
            new(1280, 720, 32, 50, 0),
            new(1280, 720, 32, 70, 1),
        ];

        var found = Win32DisplayModeSelector.TrySelect(modes, 1280, 720, 32, 60, out var selected);

        Assert.True(found);
        Assert.Equal(70u, selected.RefreshRate);
    }

    [Fact]
    public void RejectsDifferentDimensionsAndLegacyColorDepths()
    {
        Win32DisplayMode[] modes =
        [
            new(1920, 1080, 16, 60, 0),
            new(2560, 1440, 32, 60, 1),
        ];

        var found = Win32DisplayModeSelector.TrySelect(modes, 1920, 1080, 32, 60, out _);

        Assert.False(found);
    }
}
