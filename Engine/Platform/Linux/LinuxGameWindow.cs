using System;
using System.Collections.Generic;

namespace ElementEngine;

internal static class LinuxGameWindow
{
    private static readonly LinuxWindowBackend[] Backends =
    [
        new(
            NativeWindowKind.Wayland,
            static () => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) &&
                         !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")),
            static createInfo => new WaylandGameWindow(createInfo)),
        new(
            NativeWindowKind.X11,
            static () => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")),
            static createInfo => new X11GameWindow(createInfo)),
    ];

    public static GameWindow Create(WindowCreateInfo createInfo)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Linux windows can only be created on Linux.");

        var failures = new List<Exception>();

        foreach (var nativeKind in GetPreferredBackendOrder())
        {
            if (!TryGetBackend(nativeKind, out var backend) || !backend.IsAvailable())
                continue;

            try
            {
                var window = backend.Create(createInfo);
                if (window.NativeKind == backend.NativeKind)
                    return window;

                window.Dispose();
                throw new InvalidOperationException(
                    $"The {backend.NativeKind} backend created a {window.NativeKind} window.");
            }
            catch (Exception exception) when (
                exception is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or
                    NotSupportedException)
            {
                failures.Add(exception);
            }
        }

        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var display = Environment.GetEnvironmentVariable("DISPLAY");
        var message = string.IsNullOrWhiteSpace(waylandDisplay)
            ? $"No usable Linux window backend was found. DISPLAY is '{display ?? "<unset>"}'."
            : $"No usable Linux window backend was found. WAYLAND_DISPLAY is '{waylandDisplay}' and DISPLAY is '{display ?? "<unset>"}'.";

        throw failures.Count == 0
            ? new PlatformNotSupportedException(message)
            : new PlatformNotSupportedException(message, new AggregateException(failures));
    }

    private static IEnumerable<NativeWindowKind> GetPreferredBackendOrder()
    {
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        if (string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase))
            yield return NativeWindowKind.Wayland;

        yield return NativeWindowKind.X11;

        if (!string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase))
            yield return NativeWindowKind.Wayland;
    }

    private static bool TryGetBackend(NativeWindowKind nativeKind, out LinuxWindowBackend backend)
    {
        foreach (var candidate in Backends)
        {
            if (candidate.NativeKind != nativeKind)
                continue;

            backend = candidate;
            return true;
        }

        backend = default;
        return false;
    }

    private readonly record struct LinuxWindowBackend(
        NativeWindowKind NativeKind,
        Func<bool> IsAvailable,
        Func<WindowCreateInfo, GameWindow> Create);
}
