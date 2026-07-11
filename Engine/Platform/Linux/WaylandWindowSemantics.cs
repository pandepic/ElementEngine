using System;
using System.Collections.Generic;

namespace ElementEngine;

internal static class WaylandWindowSemantics
{
    public const uint MaximizedState = 1;
    public const uint FullScreenState = 2;
    public const uint ActivatedState = 4;

    public static WindowState ResolveConfiguredState(
        WindowState requestedState,
        ReadOnlySpan<uint> compositorStates)
    {
        var maximized = false;
        var fullScreen = false;

        foreach (var state in compositorStates)
        {
            maximized |= state == MaximizedState;
            fullScreen |= state == FullScreenState;
        }

        if (fullScreen)
        {
            return requestedState == WindowState.FullScreen
                ? WindowState.FullScreen
                : WindowState.BorderlessFullScreen;
        }

        if (maximized)
            return WindowState.Maximized;

        return requestedState is WindowState.Hidden or WindowState.Minimized
            ? requestedState
            : WindowState.Normal;
    }

    public static int SelectScale(IEnumerable<int> enteredOutputScales)
    {
        ArgumentNullException.ThrowIfNull(enteredOutputScales);

        var scale = 1;
        foreach (var outputScale in enteredOutputScales)
            scale = Math.Max(scale, Math.Max(1, outputScale));

        return scale;
    }

    public static int ConvertRefreshRate(int refreshRateInMillihertz) =>
        refreshRateInMillihertz <= 0 ? 0 : (refreshRateInMillihertz + 500) / 1000;
}
