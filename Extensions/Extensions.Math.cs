﻿using SixLabors.ImageSharp;
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

        public static Vector2i ToVector2i(this Vector2 v)
        {
            return new Vector2i((int)v.X, (int)v.Y);
        }
    } // Extensions
}
