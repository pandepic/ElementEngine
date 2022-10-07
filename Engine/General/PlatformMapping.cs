using System.Collections.Generic;

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

    public unsafe static class PlatformMapping
    {
        public static Dictionary<string, PlatformType> Map = new()
        {
            {
                "Windows",
                PlatformType.Windows
            },

            {
                "Mac OS X",
                PlatformType.MacOSX
            },

            {
                "Linux",
                PlatformType.Linux
            },

            {
                "iOS",
                PlatformType.iOS
            },

            {
                "Android",
                PlatformType.Android
            },
        };

        internal static PlatformType GetPlatformTypeBySDLName(string sdlName)
        {
            if (Map.TryGetValue(sdlName, out var type))
                return type;

            return PlatformType.Unknown;
        }
    }
}
