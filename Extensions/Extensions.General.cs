using System;
using System.Collections.Generic;
using System.Text;

namespace PandaEngine
{
    public static partial class Extensions
    {
        public static T ToEnum<T>(this string str) where T : IConvertible
        {
            return (T)Enum.Parse(typeof(T), str);
        }
    }
}
