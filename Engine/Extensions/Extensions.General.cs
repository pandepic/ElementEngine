using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ElementEngine
{
    public static partial class Extensions
    {
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "Nullable<T> resolves through TypeDescriptor's intrinsic converters and " +
                "custom converters are rooted via TypeConverterAttribute's annotated constructor; " +
                "verified under NativeAOT and full trimming.")]
        public static T ConvertTo<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this object value)
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
