using System;
using System.Collections.Generic;

namespace ElementEngine;

internal readonly record struct X11CrtcBounds(
    nuint Id,
    int X,
    int Y,
    int Width,
    int Height);

internal readonly record struct X11DisplayMode(
    nuint Id,
    int Width,
    int Height,
    uint RefreshRateMillihertz);

internal enum X11ExclusiveModeAction
{
    None,
    Enter,
    Restore,
}

internal static class X11ExclusiveModeTransition
{
    public static X11ExclusiveModeAction GetAction(bool exclusiveModeActive, WindowState requestedState)
    {
        if (requestedState == WindowState.FullScreen)
            return exclusiveModeActive ? X11ExclusiveModeAction.None : X11ExclusiveModeAction.Enter;

        return exclusiveModeActive ? X11ExclusiveModeAction.Restore : X11ExclusiveModeAction.None;
    }

    public static WindowState GetStateAfterRestore(WindowState fullScreenState) =>
        fullScreenState == WindowState.FullScreen
            ? WindowState.BorderlessFullScreen
            : fullScreenState;
}

internal static class X11DisplayModeSelector
{
    public static bool TrySelectCrtc(
        IReadOnlyList<X11CrtcBounds> crtcs,
        int windowX,
        int windowY,
        int windowWidth,
        int windowHeight,
        out X11CrtcBounds selectedCrtc)
    {
        selectedCrtc = default;
        var found = false;
        decimal bestIntersectionArea = -1;
        var bestCenterDistance = decimal.MaxValue;

        foreach (var crtc in crtcs)
        {
            if (crtc.Width <= 0 || crtc.Height <= 0)
                continue;

            var intersectionArea = GetIntersectionArea(
                windowX,
                windowY,
                windowWidth,
                windowHeight,
                crtc.X,
                crtc.Y,
                crtc.Width,
                crtc.Height);
            var centerDistance = GetCenterDistanceSquared(
                windowX,
                windowY,
                windowWidth,
                windowHeight,
                crtc.X,
                crtc.Y,
                crtc.Width,
                crtc.Height);

            if (!found ||
                intersectionArea > bestIntersectionArea ||
                (intersectionArea == bestIntersectionArea && centerDistance < bestCenterDistance) ||
                (intersectionArea == bestIntersectionArea && centerDistance == bestCenterDistance && crtc.Id < selectedCrtc.Id))
            {
                selectedCrtc = crtc;
                bestIntersectionArea = intersectionArea;
                bestCenterDistance = centerDistance;
                found = true;
            }
        }

        return found;
    }

    public static bool TrySelectMode(
        IReadOnlyList<X11DisplayMode> modes,
        int width,
        int height,
        nuint currentModeId,
        uint currentRefreshRateMillihertz,
        out X11DisplayMode selectedMode)
    {
        selectedMode = default;
        var found = false;

        foreach (var mode in modes)
        {
            if (mode.Width != width || mode.Height != height)
                continue;

            if (!found || IsBetter(mode, selectedMode, currentModeId, currentRefreshRateMillihertz))
            {
                selectedMode = mode;
                found = true;
            }
        }

        return found;
    }

    private static bool IsBetter(
        X11DisplayMode candidate,
        X11DisplayMode currentBest,
        nuint currentModeId,
        uint currentRefreshRateMillihertz)
    {
        var candidateIsCurrentMode = candidate.Id == currentModeId;
        var bestIsCurrentMode = currentBest.Id == currentModeId;
        if (candidateIsCurrentMode != bestIsCurrentMode)
            return candidateIsCurrentMode;

        var candidateUsesCurrentRefreshRate = candidate.RefreshRateMillihertz == currentRefreshRateMillihertz;
        var bestUsesCurrentRefreshRate = currentBest.RefreshRateMillihertz == currentRefreshRateMillihertz;
        if (candidateUsesCurrentRefreshRate != bestUsesCurrentRefreshRate)
            return candidateUsesCurrentRefreshRate;

        var candidateRefreshDifference = Difference(candidate.RefreshRateMillihertz, currentRefreshRateMillihertz);
        var bestRefreshDifference = Difference(currentBest.RefreshRateMillihertz, currentRefreshRateMillihertz);
        if (candidateRefreshDifference != bestRefreshDifference)
            return candidateRefreshDifference < bestRefreshDifference;
        if (candidate.RefreshRateMillihertz != currentBest.RefreshRateMillihertz)
            return candidate.RefreshRateMillihertz > currentBest.RefreshRateMillihertz;

        return candidate.Id < currentBest.Id;
    }

    private static decimal GetIntersectionArea(
        int firstX,
        int firstY,
        int firstWidth,
        int firstHeight,
        int secondX,
        int secondY,
        int secondWidth,
        int secondHeight)
    {
        var left = Math.Max((decimal)firstX, secondX);
        var top = Math.Max((decimal)firstY, secondY);
        var right = Math.Min((decimal)firstX + Math.Max(0, firstWidth), (decimal)secondX + secondWidth);
        var bottom = Math.Min((decimal)firstY + Math.Max(0, firstHeight), (decimal)secondY + secondHeight);
        return Math.Max(0, right - left) * Math.Max(0, bottom - top);
    }

    private static decimal GetCenterDistanceSquared(
        int firstX,
        int firstY,
        int firstWidth,
        int firstHeight,
        int secondX,
        int secondY,
        int secondWidth,
        int secondHeight)
    {
        var horizontalDistance =
            (2m * firstX + Math.Max(0, firstWidth)) -
            (2m * secondX + secondWidth);
        var verticalDistance =
            (2m * firstY + Math.Max(0, firstHeight)) -
            (2m * secondY + secondHeight);
        return horizontalDistance * horizontalDistance + verticalDistance * verticalDistance;
    }

    private static uint Difference(uint left, uint right) =>
        left >= right ? left - right : right - left;
}
