using System;

namespace ElementEngine;

internal static class NativeAudioOutput
{
    public static IAudioOutput Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsAudioOutput();
        if (OperatingSystem.IsLinux())
            return new LinuxAudioOutput();
        if (OperatingSystem.IsMacOS())
            return new MacOSAudioOutput();

        throw new PlatformNotSupportedException("Audio output is supported on Windows, Linux, and macOS.");
    }
}
