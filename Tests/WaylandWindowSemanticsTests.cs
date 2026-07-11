using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class WaylandWindowSemanticsTests
{
    [Theory]
    [InlineData(WindowState.FullScreen, WindowState.FullScreen)]
    [InlineData(WindowState.BorderlessFullScreen, WindowState.BorderlessFullScreen)]
    [InlineData(WindowState.Normal, WindowState.BorderlessFullScreen)]
    public void FullScreenConfigurePreservesTheRequestedSemanticState(
        WindowState requested,
        WindowState expected)
    {
        var actual = WaylandWindowSemantics.ResolveConfiguredState(
            requested,
            [WaylandWindowSemantics.FullScreenState]);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaximizedConfigureWinsOverAStaleNormalRequest()
    {
        var actual = WaylandWindowSemantics.ResolveConfiguredState(
            WindowState.Normal,
            [WaylandWindowSemantics.MaximizedState, WaylandWindowSemantics.ActivatedState]);

        Assert.Equal(WindowState.Maximized, actual);
    }

    [Theory]
    [InlineData(WindowState.Hidden)]
    [InlineData(WindowState.Minimized)]
    public void UnconfiguredVisibilityStatesAreNotInventedByAnEmptyConfigure(WindowState state)
    {
        Assert.Equal(state, WaylandWindowSemantics.ResolveConfiguredState(state, []));
    }

    [Fact]
    public void HighestEnteredOutputScaleControlsTheFramebufferScale()
    {
        Assert.Equal(3, WaylandWindowSemantics.SelectScale([1, 3, 2]));
        Assert.Equal(1, WaylandWindowSemantics.SelectScale([]));
        Assert.Equal(1, WaylandWindowSemantics.SelectScale([0]));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(59_940, 60)]
    [InlineData(143_500, 144)]
    public void OutputRefreshIsRoundedFromMillihertz(int millihertz, int expected)
    {
        Assert.Equal(expected, WaylandWindowSemantics.ConvertRefreshRate(millihertz));
    }
}
