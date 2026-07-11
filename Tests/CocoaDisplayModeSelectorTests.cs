using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class CocoaDisplayModeSelectorTests
{
    [Theory]
    [InlineData(WindowState.Normal, WindowState.FullScreen, (int)CocoaDisplayModeTransition.EnterExclusive)]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.FullScreen, (int)CocoaDisplayModeTransition.EnterExclusive)]
    [InlineData(WindowState.FullScreen, WindowState.Normal, (int)CocoaDisplayModeTransition.RestoreDesktop)]
    [InlineData(WindowState.FullScreen, WindowState.BorderlessFullScreen, (int)CocoaDisplayModeTransition.RestoreDesktop)]
    [InlineData(WindowState.Normal, WindowState.BorderlessFullScreen, (int)CocoaDisplayModeTransition.None)]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.Normal, (int)CocoaDisplayModeTransition.None)]
    [InlineData(WindowState.FullScreen, WindowState.FullScreen, (int)CocoaDisplayModeTransition.None)]
    public void FullScreenTransitionsSelectTheRequiredDisplayModeAction(
        WindowState current,
        WindowState target,
        int expected)
    {
        Assert.Equal(
            (CocoaDisplayModeTransition)expected,
            CocoaFullScreenTransition.GetDisplayModeTransition(current, target));
    }

    [Fact]
    public void SelectsExactDimensionsAndCurrentRefreshRate()
    {
        var currentMode = Mode(1920, 1080, 3840, 2160, 60, true, -1);
        CocoaDisplayMode[] modes =
        [
            Mode(1280, 720, 2560, 1440, 120, true, 0),
            Mode(1280, 720, 2560, 1440, 60, true, 1),
            Mode(1920, 1080, 3840, 2160, 60, true, 2),
        ];

        var found = CocoaDisplayModeSelector.TrySelect(modes, 1280, 720, currentMode, out var selected);

        Assert.True(found);
        Assert.Equal(modes[1], selected);
    }

    [Fact]
    public void PreservesCurrentPixelDensity()
    {
        var currentMode = Mode(1728, 1117, 3456, 2234, 60, true, -1);
        CocoaDisplayMode[] modes =
        [
            Mode(1280, 720, 1280, 720, 60, true, 0),
            Mode(1280, 720, 2560, 1440, 60, true, 1),
        ];

        var found = CocoaDisplayModeSelector.TrySelect(modes, 1280, 720, currentMode, out var selected);

        Assert.True(found);
        Assert.Equal(modes[1], selected);
    }

    [Fact]
    public void SelectsHigherRefreshRateWhenDifferencesAreEqual()
    {
        var currentMode = Mode(1920, 1080, 1920, 1080, 60, true, -1);
        CocoaDisplayMode[] modes =
        [
            Mode(1280, 720, 1280, 720, 50, true, 0),
            Mode(1280, 720, 1280, 720, 70, true, 1),
        ];

        var found = CocoaDisplayModeSelector.TrySelect(modes, 1280, 720, currentMode, out var selected);

        Assert.True(found);
        Assert.Equal(70, selected.RefreshRate);
    }

    [Fact]
    public void RejectsUnusableAndDifferentSizedModes()
    {
        var currentMode = Mode(1920, 1080, 1920, 1080, 60, true, -1);
        CocoaDisplayMode[] modes =
        [
            Mode(1280, 720, 1280, 720, 60, false, 0),
            Mode(1920, 1080, 1920, 1080, 60, true, 1),
        ];

        var found = CocoaDisplayModeSelector.TrySelect(modes, 1280, 720, currentMode, out _);

        Assert.False(found);
    }

    [Theory]
    [InlineData(0, 720, "width")]
    [InlineData(1280, 0, "height")]
    public void RejectsInvalidTargetDimensions(int width, int height, string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CocoaDisplayModeSelector.TrySelect([], width, height, default, out _));

        Assert.Equal(parameterName, exception.ParamName);
    }

    private static CocoaDisplayMode Mode(
        int width,
        int height,
        int pixelWidth,
        int pixelHeight,
        double refreshRate,
        bool usable,
        int nativeIndex) => new(
        width,
        height,
        pixelWidth,
        pixelHeight,
        refreshRate,
        usable,
        nativeIndex);
}
