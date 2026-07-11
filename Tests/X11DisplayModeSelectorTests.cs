using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class X11DisplayModeSelectorTests
{
    [Theory]
    [InlineData(false, WindowState.FullScreen, (int)X11ExclusiveModeAction.Enter)]
    [InlineData(true, WindowState.FullScreen, (int)X11ExclusiveModeAction.None)]
    [InlineData(false, WindowState.BorderlessFullScreen, (int)X11ExclusiveModeAction.None)]
    [InlineData(true, WindowState.BorderlessFullScreen, (int)X11ExclusiveModeAction.Restore)]
    [InlineData(true, WindowState.Normal, (int)X11ExclusiveModeAction.Restore)]
    public void SelectsExclusiveModeAction(
        bool exclusiveModeActive,
        WindowState requestedState,
        int expectedAction)
    {
        Assert.Equal(
            (X11ExclusiveModeAction)expectedAction,
            X11ExclusiveModeTransition.GetAction(exclusiveModeActive, requestedState));
    }

    [Theory]
    [InlineData(WindowState.FullScreen, WindowState.BorderlessFullScreen)]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.BorderlessFullScreen)]
    public void TracksStateAfterExclusiveModeIsRestored(
        WindowState currentState,
        WindowState expectedState)
    {
        Assert.Equal(expectedState, X11ExclusiveModeTransition.GetStateAfterRestore(currentState));
    }

    [Fact]
    public void SelectsCrtcWithLargestWindowIntersection()
    {
        X11CrtcBounds[] crtcs =
        [
            new(1, 0, 0, 1920, 1080),
            new(2, 1920, 0, 2560, 1440),
        ];

        var found = X11DisplayModeSelector.TrySelectCrtc(
            crtcs,
            1700,
            100,
            1000,
            800,
            out var selected);

        Assert.True(found);
        Assert.Equal(crtcs[1], selected);
    }

    [Fact]
    public void SelectsNearestCrtcForOffscreenWindow()
    {
        X11CrtcBounds[] crtcs =
        [
            new(1, -1920, 0, 1920, 1080),
            new(2, 0, 0, 1920, 1080),
        ];

        var found = X11DisplayModeSelector.TrySelectCrtc(
            crtcs,
            3000,
            200,
            800,
            600,
            out var selected);

        Assert.True(found);
        Assert.Equal(crtcs[1], selected);
    }

    [Fact]
    public void SelectsStableCrtcForAnExactTie()
    {
        X11CrtcBounds[] crtcs =
        [
            new(20, 1000, 0, 1000, 1000),
            new(10, -1000, 0, 1000, 1000),
        ];

        var found = X11DisplayModeSelector.TrySelectCrtc(
            crtcs,
            400,
            400,
            200,
            200,
            out var selected);

        Assert.True(found);
        Assert.Equal((nuint)10, selected.Id);
    }

    [Fact]
    public void SelectsCrtcWithoutOverflowAtExtremeCoordinates()
    {
        X11CrtcBounds[] crtcs =
        [
            new(1, int.MinValue, int.MinValue, int.MaxValue, int.MaxValue),
            new(2, 0, 0, int.MaxValue, int.MaxValue),
        ];

        var found = X11DisplayModeSelector.TrySelectCrtc(
            crtcs,
            int.MinValue,
            int.MinValue,
            int.MaxValue,
            int.MaxValue,
            out var selected);

        Assert.True(found);
        Assert.Equal(crtcs[0], selected);
    }

    [Fact]
    public void SelectsCurrentModeWhenItsDimensionsMatch()
    {
        X11DisplayMode[] modes =
        [
            new(10, 1920, 1080, 60_000),
            new(20, 1920, 1080, 144_000),
        ];

        var found = X11DisplayModeSelector.TrySelectMode(
            modes,
            1920,
            1080,
            10,
            144_000,
            out var selected);

        Assert.True(found);
        Assert.Equal(modes[0], selected);
    }

    [Fact]
    public void SelectsClosestRefreshRateAndPrefersHigherRateForATie()
    {
        X11DisplayMode[] modes =
        [
            new(10, 1280, 720, 50_000),
            new(20, 1280, 720, 70_000),
        ];

        var found = X11DisplayModeSelector.TrySelectMode(
            modes,
            1280,
            720,
            30,
            60_000,
            out var selected);

        Assert.True(found);
        Assert.Equal(70_000u, selected.RefreshRateMillihertz);
    }

    [Fact]
    public void RejectsModesWithDifferentDimensions()
    {
        X11DisplayMode[] modes =
        [
            new(10, 1920, 1080, 60_000),
            new(20, 1280, 720, 60_000),
        ];

        var found = X11DisplayModeSelector.TrySelectMode(
            modes,
            2560,
            1440,
            10,
            60_000,
            out _);

        Assert.False(found);
    }
}
