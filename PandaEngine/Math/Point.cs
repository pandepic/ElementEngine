using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public static Point Zero = new Point(0, 0);

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point(Vector2 v)
        {
            X = (int)v.X;
            Y = (int)v.Y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", X, Y);
        }

    } // Point
}
