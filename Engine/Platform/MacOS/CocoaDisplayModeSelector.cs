using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal readonly record struct CocoaDisplayMode(
    int Width,
    int Height,
    int PixelWidth,
    int PixelHeight,
    double RefreshRate,
    bool IsUsableForDesktopGui,
    int NativeIndex);

internal enum CocoaDisplayModeTransition
{
    None,
    EnterExclusive,
    RestoreDesktop,
}

internal static class CocoaFullScreenTransition
{
    public static CocoaDisplayModeTransition GetDisplayModeTransition(WindowState current, WindowState target)
    {
        if (current == target)
            return CocoaDisplayModeTransition.None;
        if (target == WindowState.FullScreen)
            return CocoaDisplayModeTransition.EnterExclusive;
        if (current == WindowState.FullScreen)
            return CocoaDisplayModeTransition.RestoreDesktop;

        return CocoaDisplayModeTransition.None;
    }
}

internal static class CocoaDisplayModeSelector
{
    public static bool TrySelect(
        IReadOnlyList<CocoaDisplayMode> modes,
        int width,
        int height,
        CocoaDisplayMode currentMode,
        out CocoaDisplayMode selectedMode)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        selectedMode = default;
        var found = false;

        foreach (var mode in modes)
        {
            if (!mode.IsUsableForDesktopGui || mode.Width != width || mode.Height != height)
                continue;

            if (!found || IsBetter(mode, selectedMode, currentMode, width, height))
            {
                selectedMode = mode;
                found = true;
            }
        }

        return found;
    }

    private static bool IsBetter(
        CocoaDisplayMode candidate,
        CocoaDisplayMode currentBest,
        CocoaDisplayMode currentMode,
        int targetWidth,
        int targetHeight)
    {
        var candidateScaleDifference = GetScaleDifference(candidate, currentMode, targetWidth, targetHeight);
        var bestScaleDifference = GetScaleDifference(currentBest, currentMode, targetWidth, targetHeight);
        if (candidateScaleDifference != bestScaleDifference)
            return candidateScaleDifference < bestScaleDifference;

        var candidateRefreshDifference = GetRefreshDifference(candidate.RefreshRate, currentMode.RefreshRate);
        var bestRefreshDifference = GetRefreshDifference(currentBest.RefreshRate, currentMode.RefreshRate);
        if (candidateRefreshDifference != bestRefreshDifference)
            return candidateRefreshDifference < bestRefreshDifference;

        if (candidate.RefreshRate != currentBest.RefreshRate)
            return candidate.RefreshRate > currentBest.RefreshRate;

        var candidatePixelCount = (long)candidate.PixelWidth * candidate.PixelHeight;
        var bestPixelCount = (long)currentBest.PixelWidth * currentBest.PixelHeight;
        if (candidatePixelCount != bestPixelCount)
            return candidatePixelCount > bestPixelCount;

        return candidate.NativeIndex < currentBest.NativeIndex;
    }

    private static double GetScaleDifference(
        CocoaDisplayMode mode,
        CocoaDisplayMode currentMode,
        int targetWidth,
        int targetHeight)
    {
        if (currentMode.Width <= 0 || currentMode.Height <= 0 ||
            currentMode.PixelWidth <= 0 || currentMode.PixelHeight <= 0)
        {
            return 0;
        }

        var expectedPixelWidth = targetWidth * (double)currentMode.PixelWidth / currentMode.Width;
        var expectedPixelHeight = targetHeight * (double)currentMode.PixelHeight / currentMode.Height;
        return Math.Abs(mode.PixelWidth - expectedPixelWidth) + Math.Abs(mode.PixelHeight - expectedPixelHeight);
    }

    private static double GetRefreshDifference(double refreshRate, double currentRefreshRate)
    {
        if (currentRefreshRate <= 0)
            return refreshRate <= 0 ? 0 : 1;
        if (refreshRate <= 0)
            return double.MaxValue;

        return Math.Abs(refreshRate - currentRefreshRate);
    }
}

internal sealed class CocoaDisplayModeSession : IDisposable
{
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private readonly uint _display;
    private nint _originalMode;

    private CocoaDisplayModeSession(uint display, nint originalMode)
    {
        _display = display;
        _originalMode = originalMode;
    }

    ~CocoaDisplayModeSession()
    {
        try
        {
            Restore();
        }
        catch (Exception exception)
        {
            Logging.Error(
                "Failed to restore a macOS display mode during finalization: {error}",
                exception.Message);
        }
    }

    public static CocoaDisplayModeSession Enter(uint display, int width, int height)
    {
        var originalMode = CGDisplayCopyDisplayMode(display);
        if (originalMode == 0)
            throw new InvalidOperationException($"Failed to query the current mode for display {display}.");

        var modeList = CGDisplayCopyAllDisplayModes(display, 0);
        if (modeList == 0)
        {
            CFRelease(originalMode);
            throw new InvalidOperationException($"Failed to enumerate modes for display {display}.");
        }

        try
        {
            var currentMode = ReadMode(originalMode, -1);
            var modeCount = checked((int)CFArrayGetCount(modeList));
            var modes = new CocoaDisplayMode[modeCount];

            for (var index = 0; index < modeCount; index++)
            {
                var nativeMode = CFArrayGetValueAtIndex(modeList, index);
                modes[index] = ReadMode(nativeMode, index);
            }

            if (!CocoaDisplayModeSelector.TrySelect(modes, width, height, currentMode, out var selectedMode))
            {
                throw new NotSupportedException(
                    $"Display {display} does not support a usable {width}x{height} full-screen mode.");
            }

            var selectedNativeMode = CFArrayGetValueAtIndex(modeList, selectedMode.NativeIndex);
            var setResult = CGDisplaySetDisplayMode(display, selectedNativeMode, 0);
            if (setResult != 0)
            {
                var setException = new InvalidOperationException(
                    $"Failed to set display {display} to {width}x{height}. CoreGraphics returned error {setResult}.");
                var restoreResult = CGDisplaySetDisplayMode(display, originalMode, 0);
                if (restoreResult != 0)
                {
                    throw new AggregateException(
                        "The macOS display mode change and its rollback both failed.",
                        setException,
                        new InvalidOperationException(
                            $"Failed to restore display {display}. CoreGraphics returned error {restoreResult}."));
                }

                throw setException;
            }

            var session = new CocoaDisplayModeSession(display, originalMode);
            originalMode = 0;
            return session;
        }
        finally
        {
            CFRelease(modeList);
            if (originalMode != 0)
                CFRelease(originalMode);
        }
    }

    public void Restore()
    {
        if (_originalMode == 0)
            return;

        var result = CGDisplaySetDisplayMode(_display, _originalMode, 0);
        if (result != 0)
        {
            throw new InvalidOperationException(
                $"Failed to restore display {_display}. CoreGraphics returned error {result}.");
        }

        CFRelease(_originalMode);
        _originalMode = 0;
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Restore();

    private static CocoaDisplayMode ReadMode(nint nativeMode, int nativeIndex) => new(
        checked((int)CGDisplayModeGetWidth(nativeMode)),
        checked((int)CGDisplayModeGetHeight(nativeMode)),
        checked((int)CGDisplayModeGetPixelWidth(nativeMode)),
        checked((int)CGDisplayModeGetPixelHeight(nativeMode)),
        CGDisplayModeGetRefreshRate(nativeMode),
        CGDisplayModeIsUsableForDesktopGUI(nativeMode),
        nativeIndex);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nint CGDisplayCopyDisplayMode(uint display);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nint CGDisplayCopyAllDisplayModes(uint display, nint options);

    [DllImport(CoreGraphicsLibrary)]
    private static extern int CGDisplaySetDisplayMode(uint display, nint mode, nint options);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nuint CGDisplayModeGetWidth(nint mode);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nuint CGDisplayModeGetHeight(nint mode);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nuint CGDisplayModeGetPixelWidth(nint mode);

    [DllImport(CoreGraphicsLibrary)]
    private static extern nuint CGDisplayModeGetPixelHeight(nint mode);

    [DllImport(CoreGraphicsLibrary)]
    private static extern double CGDisplayModeGetRefreshRate(nint mode);

    [DllImport(CoreGraphicsLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CGDisplayModeIsUsableForDesktopGUI(nint mode);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetCount(nint array);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetValueAtIndex(nint array, nint index);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(nint value);
}

internal static class CocoaDisplayNative
{
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const int MainMenuWindowLevelKey = 8;

    [DllImport(CoreGraphicsLibrary)]
    private static extern int CGWindowLevelForKey(int key);

    public static nint GetFullScreenWindowLevel() => CGWindowLevelForKey(MainMenuWindowLevelKey) + 1;
}
