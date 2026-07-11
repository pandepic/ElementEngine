using System;

namespace ElementEngine
{
    public enum PlatformType
    {
        Unknown,
        Windows,
        MacOSX,
        Linux,
        iOS,
        Android,
    }

    public static class PlatformMapping
    {
        public static PlatformType Current
        {
            get
            {
                if (OperatingSystem.IsWindows())
                    return PlatformType.Windows;
                if (OperatingSystem.IsMacOS())
                    return PlatformType.MacOSX;
                if (OperatingSystem.IsLinux())
                    return PlatformType.Linux;
                if (OperatingSystem.IsIOS())
                    return PlatformType.iOS;
                if (OperatingSystem.IsAndroid())
                    return PlatformType.Android;

                return PlatformType.Unknown;
            }
        }
    }
}
