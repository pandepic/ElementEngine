using System;

namespace ElementEngine;

internal static class NativeWindowFactory
{
    public static GameWindow Create(WindowCreateInfo createInfo)
    {
        if (OperatingSystem.IsWindows())
            return new Win32GameWindow(createInfo);

        if (OperatingSystem.IsLinux())
            return LinuxGameWindow.Create(createInfo);

        if (OperatingSystem.IsMacOS())
            return new CocoaGameWindow(createInfo);

        throw new PlatformNotSupportedException("ElementEngine supports Windows, Linux, and macOS desktop platforms.");
    }
}
