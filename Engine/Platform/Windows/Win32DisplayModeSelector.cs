using System;
using System.Collections.Generic;

namespace ElementEngine;

internal readonly record struct Win32DisplayMode(
    uint Width,
    uint Height,
    uint BitsPerPixel,
    uint RefreshRate,
    uint NativeIndex);

internal enum Win32DisplayModeTransition
{
    None,
    EnterExclusive,
    RestoreDesktop,
}

internal static class Win32FullScreenTransition
{
    public static Win32DisplayModeTransition GetDisplayModeTransition(WindowState current, WindowState target)
    {
        ValidateFullScreenState(current);
        ValidateFullScreenState(target);

        if (current == target)
            return Win32DisplayModeTransition.None;

        return target == WindowState.FullScreen
            ? Win32DisplayModeTransition.EnterExclusive
            : Win32DisplayModeTransition.RestoreDesktop;
    }

    private static void ValidateFullScreenState(WindowState state)
    {
        if (state is not WindowState.FullScreen and not WindowState.BorderlessFullScreen)
            throw new ArgumentOutOfRangeException(nameof(state), state, "A Win32 full-screen transition requires full-screen states.");
    }
}

internal static class Win32DisplayModeSelector
{
    public static bool TrySelect(
        IReadOnlyList<Win32DisplayMode> modes,
        uint width,
        uint height,
        uint currentBitsPerPixel,
        uint currentRefreshRate,
        out Win32DisplayMode selectedMode)
    {
        selectedMode = default;
        var found = false;

        foreach (var mode in modes)
        {
            if (mode.Width != width || mode.Height != height || mode.BitsPerPixel < 32)
                continue;

            if (!found || IsBetter(mode, selectedMode, currentBitsPerPixel, currentRefreshRate))
            {
                selectedMode = mode;
                found = true;
            }
        }

        return found;
    }

    private static bool IsBetter(
        Win32DisplayMode candidate,
        Win32DisplayMode currentBest,
        uint currentBitsPerPixel,
        uint currentRefreshRate)
    {
        var candidateUsesCurrentColorDepth = candidate.BitsPerPixel == currentBitsPerPixel;
        var bestUsesCurrentColorDepth = currentBest.BitsPerPixel == currentBitsPerPixel;
        if (candidateUsesCurrentColorDepth != bestUsesCurrentColorDepth)
            return candidateUsesCurrentColorDepth;

        var candidateUsesCurrentRefreshRate = candidate.RefreshRate == currentRefreshRate;
        var bestUsesCurrentRefreshRate = currentBest.RefreshRate == currentRefreshRate;
        if (candidateUsesCurrentRefreshRate != bestUsesCurrentRefreshRate)
            return candidateUsesCurrentRefreshRate;

        var candidateRefreshDifference = Difference(candidate.RefreshRate, currentRefreshRate);
        var bestRefreshDifference = Difference(currentBest.RefreshRate, currentRefreshRate);
        if (candidateRefreshDifference != bestRefreshDifference)
            return candidateRefreshDifference < bestRefreshDifference;

        if (candidate.BitsPerPixel != currentBest.BitsPerPixel)
            return candidate.BitsPerPixel > currentBest.BitsPerPixel;
        if (candidate.RefreshRate != currentBest.RefreshRate)
            return candidate.RefreshRate > currentBest.RefreshRate;

        return candidate.NativeIndex < currentBest.NativeIndex;
    }

    private static uint Difference(uint left, uint right)
        => left >= right ? left - right : right - left;
}
