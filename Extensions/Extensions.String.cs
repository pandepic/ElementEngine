using System;

namespace ElementEngine
{
    public static partial class Extensions
    {
        public static T ToEnum<T>(this string str) where T : IConvertible
        {
            return (T)Enum.Parse(typeof(T), str);
        }

        public static string ToTitleCase(this string str)
        {
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);
        }
    }
}
