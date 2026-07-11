using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal sealed class X11ExclusiveDisplayMode
{
    private const ushort Rotate90 = 1 << 1;
    private const ushort Rotate270 = 1 << 3;

    private readonly nint _display;
    private readonly nuint _rootWindow;
    private readonly CrtcConfiguration _originalConfiguration;
    private readonly bool _configurationChanged;
    private bool _restored;

    private X11ExclusiveDisplayMode(
        nint display,
        nuint rootWindow,
        CrtcConfiguration originalConfiguration,
        bool configurationChanged)
    {
        _display = display;
        _rootWindow = rootWindow;
        _originalConfiguration = originalConfiguration;
        _configurationChanged = configurationChanged;
    }

    public static X11ExclusiveDisplayMode Enter(
        nint display,
        nuint rootWindow,
        int windowX,
        int windowY,
        int windowWidth,
        int windowHeight,
        int targetWidth,
        int targetHeight)
    {
        if (display == 0)
            throw new ArgumentException("An open X11 display is required.", nameof(display));
        if (rootWindow == 0)
            throw new ArgumentException("An X11 root window is required.", nameof(rootWindow));
        if (targetWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetWidth), "Full-screen dimensions must be greater than zero.");
        if (targetHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetHeight), "Full-screen dimensions must be greater than zero.");

        try
        {
            var resourcesPointer = GetScreenResources(display, rootWindow, false);
            if (resourcesPointer == 0)
                throw new NotSupportedException("RandR screen resources are unavailable for the current X11 display.");

            try
            {
                var resources = Marshal.PtrToStructure<XRRScreenResources>(resourcesPointer);
                var snapshots = GetActiveCrtcs(display, resourcesPointer, resources);
                var bounds = new List<X11CrtcBounds>(snapshots.Count);
                foreach (var snapshot in snapshots)
                    bounds.Add(snapshot.Bounds);

                if (!X11DisplayModeSelector.TrySelectCrtc(
                        bounds,
                        windowX,
                        windowY,
                        windowWidth,
                        windowHeight,
                        out var selectedBounds))
                {
                    throw new NotSupportedException("No active RandR output is available for full-screen mode switching.");
                }

                var selectedCrtc = snapshots.Find(snapshot => snapshot.Bounds.Id == selectedBounds.Id);
                if (selectedCrtc is null)
                    throw new InvalidOperationException("The selected RandR output is no longer available.");

                var nativeModes = GetModes(resources);
                var supportedModeIds = GetSupportedModeIds(
                    display,
                    resourcesPointer,
                    selectedCrtc.Configuration.Outputs);
                var supportedModes = new List<X11DisplayMode>(supportedModeIds.Count);
                foreach (var modeId in supportedModeIds)
                {
                    if (!nativeModes.TryGetValue(modeId, out var mode))
                        continue;

                    var rotated = IsQuarterTurn(selectedCrtc.Configuration.Rotation);
                    var width = checked((int)(rotated ? mode.Height : mode.Width));
                    var height = checked((int)(rotated ? mode.Width : mode.Height));
                    supportedModes.Add(new X11DisplayMode(
                        mode.Id,
                        width,
                        height,
                        GetRefreshRateMillihertz(mode)));
                }

                nativeModes.TryGetValue(selectedCrtc.Configuration.Mode, out var currentMode);
                var currentRefreshRate = GetRefreshRateMillihertz(currentMode);
                if (!X11DisplayModeSelector.TrySelectMode(
                        supportedModes,
                        targetWidth,
                        targetHeight,
                        selectedCrtc.Configuration.Mode,
                        currentRefreshRate,
                        out var selectedMode))
                {
                    throw new NotSupportedException(
                        $"The selected X11 display does not support a {targetWidth}x{targetHeight} full-screen mode.");
                }

                var configurationChanged = selectedMode.Id != selectedCrtc.Configuration.Mode;
                var exclusiveMode = new X11ExclusiveDisplayMode(
                    display,
                    rootWindow,
                    selectedCrtc.Configuration,
                    configurationChanged);

                if (configurationChanged &&
                    !TryApplyConfiguration(
                        display,
                        rootWindow,
                        selectedCrtc.Configuration with { Mode = selectedMode.Id },
                        out var status))
                {
                    throw new InvalidOperationException(
                        $"RandR failed to apply {targetWidth}x{targetHeight} full-screen mode (status {status}).");
                }

                return exclusiveMode;
            }
            finally
            {
                X11Native.XRRFreeScreenResources(resourcesPointer);
            }
        }
        catch (DllNotFoundException exception)
        {
            throw new NotSupportedException("RandR is required for exclusive full-screen mode on X11.", exception);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw new NotSupportedException("RandR 1.2 or newer is required for exclusive full-screen mode on X11.", exception);
        }
    }

    public bool TryRestore(out int status)
    {
        status = X11Native.RandRSetConfigSuccess;
        if (_restored)
            return true;
        if (!_configurationChanged)
        {
            _restored = true;
            return true;
        }

        try
        {
            if (!TryApplyConfiguration(
                    _display,
                    _rootWindow,
                    _originalConfiguration,
                    out status))
            {
                return false;
            }

            _restored = true;
            return true;
        }
        catch (DllNotFoundException)
        {
            status = -1;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            status = -1;
            return false;
        }
    }

    private static List<CrtcSnapshot> GetActiveCrtcs(
        nint display,
        nint resourcesPointer,
        XRRScreenResources resources)
    {
        var snapshots = new List<CrtcSnapshot>(Math.Max(0, resources.CrtcCount));
        if (resources.CrtcCount <= 0 || resources.Crtcs == 0)
            return snapshots;

        for (var index = 0; index < resources.CrtcCount; index++)
        {
            var crtc = ReadId(resources.Crtcs, index);
            var crtcInfoPointer = X11Native.XRRGetCrtcInfo(display, resourcesPointer, crtc);
            if (crtcInfoPointer == 0)
                continue;

            try
            {
                var crtcInfo = Marshal.PtrToStructure<XRRCrtcInfo>(crtcInfoPointer);
                if (crtcInfo.Mode == 0 ||
                    crtcInfo.Width == 0 ||
                    crtcInfo.Height == 0 ||
                    crtcInfo.OutputCount <= 0 ||
                    crtcInfo.Outputs == 0)
                {
                    continue;
                }

                var outputs = ReadIds(crtcInfo.Outputs, crtcInfo.OutputCount);
                var bounds = new X11CrtcBounds(
                    crtc,
                    crtcInfo.X,
                    crtcInfo.Y,
                    checked((int)crtcInfo.Width),
                    checked((int)crtcInfo.Height));
                var configuration = new CrtcConfiguration(
                    crtc,
                    crtcInfo.X,
                    crtcInfo.Y,
                    crtcInfo.Mode,
                    crtcInfo.Rotation,
                    outputs);
                snapshots.Add(new CrtcSnapshot(bounds, configuration));
            }
            finally
            {
                X11Native.XRRFreeCrtcInfo(crtcInfoPointer);
            }
        }

        return snapshots;
    }

    private static Dictionary<nuint, XRRModeInfo> GetModes(XRRScreenResources resources)
    {
        var modes = new Dictionary<nuint, XRRModeInfo>(Math.Max(0, resources.ModeCount));
        if (resources.ModeCount <= 0 || resources.Modes == 0)
            return modes;

        var modeSize = Marshal.SizeOf<XRRModeInfo>();
        for (var index = 0; index < resources.ModeCount; index++)
        {
            var mode = Marshal.PtrToStructure<XRRModeInfo>(
                resources.Modes + checked(index * modeSize));
            if (mode.Id != 0)
                modes[mode.Id] = mode;
        }

        return modes;
    }

    private static HashSet<nuint> GetSupportedModeIds(
        nint display,
        nint resourcesPointer,
        nuint[] outputs)
    {
        HashSet<nuint> supportedModes = null;

        foreach (var output in outputs)
        {
            var outputInfoPointer = X11Native.XRRGetOutputInfo(display, resourcesPointer, output);
            if (outputInfoPointer == 0)
                throw new InvalidOperationException($"RandR output {output} is no longer available.");

            try
            {
                var outputInfo = Marshal.PtrToStructure<XRROutputInfo>(outputInfoPointer);
                var outputModes = outputInfo.ModeCount > 0 && outputInfo.Modes != 0
                    ? new HashSet<nuint>(ReadIds(outputInfo.Modes, outputInfo.ModeCount))
                    : [];

                if (supportedModes is null)
                    supportedModes = outputModes;
                else
                    supportedModes.IntersectWith(outputModes);
            }
            finally
            {
                X11Native.XRRFreeOutputInfo(outputInfoPointer);
            }
        }

        return supportedModes ?? [];
    }

    private static bool TryApplyConfiguration(
        nint display,
        nuint rootWindow,
        CrtcConfiguration configuration,
        out int status)
    {
        status = -1;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var resourcesPointer = GetScreenResources(display, rootWindow, attempt > 0);
            if (resourcesPointer == 0)
                continue;

            try
            {
                status = X11Native.XRRSetCrtcConfig(
                    display,
                    resourcesPointer,
                    configuration.Crtc,
                    X11Native.CurrentTime,
                    configuration.X,
                    configuration.Y,
                    configuration.Mode,
                    configuration.Rotation,
                    configuration.Outputs,
                    configuration.Outputs.Length);
                if (status == X11Native.RandRSetConfigSuccess)
                {
                    X11Native.XSync(display, X11Native.False);
                    return true;
                }
            }
            finally
            {
                X11Native.XRRFreeScreenResources(resourcesPointer);
            }
        }

        return false;
    }

    private static nint GetScreenResources(nint display, nuint rootWindow, bool refresh)
    {
        if (refresh)
            return X11Native.XRRGetScreenResources(display, rootWindow);

        try
        {
            var resources = X11Native.XRRGetScreenResourcesCurrent(display, rootWindow);
            return resources != 0
                ? resources
                : X11Native.XRRGetScreenResources(display, rootWindow);
        }
        catch (EntryPointNotFoundException)
        {
            return X11Native.XRRGetScreenResources(display, rootWindow);
        }
    }

    private static nuint[] ReadIds(nint values, int count)
    {
        var ids = new nuint[count];
        for (var index = 0; index < count; index++)
            ids[index] = ReadId(values, index);

        return ids;
    }

    private static nuint ReadId(nint values, int index) =>
        unchecked((nuint)Marshal.ReadIntPtr(values, checked(index * nint.Size)));

    private static uint GetRefreshRateMillihertz(XRRModeInfo mode)
    {
        if (mode.DotClock == 0 || mode.HorizontalTotal == 0 || mode.VerticalTotal == 0)
            return 0;

        var refreshRate = (double)mode.DotClock / mode.HorizontalTotal / mode.VerticalTotal;
        if ((mode.ModeFlags & X11Native.RandRModeFlagInterlace) != 0)
            refreshRate *= 2;
        if ((mode.ModeFlags & X11Native.RandRModeFlagDoubleScan) != 0)
            refreshRate /= 2;

        return checked((uint)Math.Round(refreshRate * 1000, MidpointRounding.AwayFromZero));
    }

    private static bool IsQuarterTurn(ushort rotation) =>
        (rotation & (Rotate90 | Rotate270)) != 0;

    private sealed record CrtcSnapshot(
        X11CrtcBounds Bounds,
        CrtcConfiguration Configuration);

    private sealed record CrtcConfiguration(
        nuint Crtc,
        int X,
        int Y,
        nuint Mode,
        ushort Rotation,
        nuint[] Outputs);
}
