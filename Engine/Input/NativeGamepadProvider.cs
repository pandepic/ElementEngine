using System;
using System.Collections.Generic;

namespace ElementEngine;

internal static class NativeGamepadProvider
{
    public static IReadOnlyList<INativeGamepad> Enumerate(bool applyMappings = false)
    {
        if (OperatingSystem.IsWindows())
            return WindowsGamepadProvider.Enumerate();
        if (OperatingSystem.IsLinux())
            return LinuxGamepadProvider.Enumerate(applyMappings);
        if (OperatingSystem.IsMacOS())
            return MacOSGamepadProvider.Enumerate();

        return Array.Empty<INativeGamepad>();
    }
}
