using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public static partial class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextDouble(this Random rng, double min, double max)
        {
            return min + rng.NextDouble() * (max - min);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat(this Random rng)
        {
            return (float)rng.NextDouble();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat(this Random rng, float min, float max)
        {
            return (float)(min + rng.NextDouble() * (max - min));
        }
    }
}
