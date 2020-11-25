using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ElementEngine
{
    public struct Vector2I
    {
        public int X { get; set; }
        public int Y { get; set; }

        public static Vector2I Zero = new Vector2I(0, 0);

        public Vector2I(int val)
        {
            X = val;
            Y = val;
        }

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2I(Vector2 v)
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
            if (obj is Vector2I point)
                return point == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool operator ==(Vector2I p1, Vector2I p2) => p1.X == p2.X && p1.Y == p2.Y;
        public static bool operator !=(Vector2I p1, Vector2I p2) => !(p1 == p2);

        public static Vector2I operator +(Vector2I p1, Vector2I p2) => new Vector2I(p1.X + p2.X, p1.Y + p2.Y);
        public static Vector2I operator -(Vector2I p1, Vector2I p2) => new Vector2I(p1.X - p2.X, p1.Y - p2.Y);
        public static Vector2I operator *(Vector2I p1, Vector2I p2) => new Vector2I(p1.X * p2.X, p1.Y * p2.Y);
        public static Vector2I operator /(Vector2I p1, Vector2I p2) => new Vector2I(p1.X / p2.X, p1.Y / p2.Y);

        public static implicit operator Vector2I(Vector2 v) => new Vector2I(v);
        public static implicit operator Vector2(Vector2I v) => new Vector2(v.X, v.Y);

    } // Point
}
