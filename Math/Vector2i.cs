using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ElementEngine
{
    public struct Vector2i
    {
        public int X { get; set; }
        public int Y { get; set; }

        public static Vector2i Zero = new Vector2i(0, 0);

        public Vector2i(int val)
        {
            X = val;
            Y = val;
        }

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2i(Vector2 v)
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

        public override bool Equals(object obj)
        {
            if (obj is Vector2i point)
                return point == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool operator ==(Vector2i p1, Vector2i p2) => p1.X == p2.X && p1.Y == p2.Y;
        public static bool operator !=(Vector2i p1, Vector2i p2) => !(p1 == p2);

        public static Vector2i operator +(Vector2i p1, Vector2i p2) => new Vector2i(p1.X + p2.X, p1.Y + p2.Y);
        public static Vector2i operator -(Vector2i p1, Vector2i p2) => new Vector2i(p1.X - p2.X, p1.Y - p2.Y);
        public static Vector2i operator *(Vector2i p1, Vector2i p2) => new Vector2i(p1.X * p2.X, p1.Y * p2.Y);
        public static Vector2i operator /(Vector2i p1, Vector2i p2) => new Vector2i(p1.X / p2.X, p1.Y / p2.Y);

        public static implicit operator Vector2i(Vector2 v) => new Vector2i(v);

    } // Point
}
