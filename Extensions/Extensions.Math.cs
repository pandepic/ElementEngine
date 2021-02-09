using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine
{
    public static partial class Extensions
    {
        public static float ToDegrees(this float f)
        {
            return f * MathHelper.ToDegrees;
        }

        public static float ToRadians(this float f)
        {
            return f * MathHelper.ToRadians;
        }

        public static double ToDegrees(this double f)
        {
            return f * MathHelper.ToDegrees;
        }

        public static double ToRadians(this double f)
        {
            return f * MathHelper.ToRadians;
        }

        public static Vector2I ToVector2I(this Vector2 v)
        {
            return new Vector2I((int)v.X, (int)v.Y);
        }

        public static Vector3I ToVector3I(this Vector3 v)
        {
            return new Vector3I((int)v.X, (int)v.Y, (int)v.Z);
        }

        public static float GetDistance(this Vector2 vec1, Vector2 vec2)
        {
            return MathF.Sqrt(MathF.Pow(vec2.X - vec1.X, 2) + MathF.Pow(vec2.Y - vec1.Y, 2));
        }
    } // Extensions
}
