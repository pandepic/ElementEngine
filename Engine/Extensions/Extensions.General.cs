using System;
using System.ComponentModel;

namespace ElementEngine
{
    public static partial class Extensions
    {
        public static T ConvertTo<T>(this object value)
        {
            if (value is T t)
            {
                return t;
            }
            else
            {
                try
                {
                    //Handling Nullable types i.e, int?, double?, bool? .. etc
                    if (Nullable.GetUnderlyingType(typeof(T)) != null)
                    {
                        var conv = TypeDescriptor.GetConverter(typeof(T));
                        return (T)conv.ConvertFrom(value);
                    }
                    else
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                catch (Exception)
                {
                    return default;
                }
            }
        }
    }
}
