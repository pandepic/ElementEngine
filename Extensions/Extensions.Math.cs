using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public static partial class Extensions
    {
        public static float ToDegrees(this float f)
        {
            return f * PandaMath.ToDegrees;
        }

        public static float ToRadians(this float f)
        {
            return f * PandaMath.ToRadians;
        }

        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
    } // Extensions
}
