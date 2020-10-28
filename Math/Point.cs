using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
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

        public static Point operator +(Point p1, Point p2) => new Point(p1.X + p2.X, p1.Y + p2.Y);
        public static Point operator -(Point p1, Point p2) => new Point(p1.X - p2.X, p1.Y - p2.Y);
        public static Point operator *(Point p1, Point p2) => new Point(p1.X * p2.X, p1.Y * p2.Y);
        public static Point operator /(Point p1, Point p2) => new Point(p1.X / p2.X, p1.Y / p2.Y);

    } // Point
}
