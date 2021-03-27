using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ElementEngine
{
    public struct Vector2I
    {
        public int X;
        public int Y;

        public static Vector2I Zero = new Vector2I(0, 0);

        public Vector2I(int val)
        {
            X = val;
            Y = val;
        }

        public Vector2I(float val)
        {
            X = (int)val;
            Y = (int)val;
        }

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2I(float x, float y)
        {
            X = (int)x;
            Y = (int)y;
        }

        public Vector2I(double x, double y)
        {
            X = (int)x;
            Y = (int)y;
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

        public float GetDistance(Vector2I vec)
        {
            return ToVector2().GetDistance(vec.ToVector2());
        }

        public static float GetDistance(Vector2I vec1, Vector2I vec2)
        {
            return vec1.GetDistance(vec2);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", X, Y);
        }

        public static Vector2I FromString(string str)
        {
            var split = str.Trim().Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new Vector2I(int.Parse(split[0]), int.Parse(split[1]));
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
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Vector2I p1, Vector2I p2) => p1.X == p2.X && p1.Y == p2.Y;
        public static bool operator !=(Vector2I p1, Vector2I p2) => !(p1 == p2);

        public static Vector2I operator +(Vector2I p1, Vector2I p2) => new Vector2I(p1.X + p2.X, p1.Y + p2.Y);
        public static Vector2I operator -(Vector2I p1, Vector2I p2) => new Vector2I(p1.X - p2.X, p1.Y - p2.Y);
        public static Vector2I operator *(Vector2I p1, Vector2I p2) => new Vector2I(p1.X * p2.X, p1.Y * p2.Y);
        public static Vector2I operator /(Vector2I p1, Vector2I p2) => new Vector2I(p1.X / p2.X, p1.Y / p2.Y);
        public static Vector2I operator %(Vector2I p1, Vector2I p2) => new Vector2I(p1.X % p2.X, p1.Y % p2.Y);

        public static Vector2I operator +(Vector2I p1, int p2) => new Vector2I(p1.X + p2, p1.Y + p2);
        public static Vector2I operator -(Vector2I p1, int p2) => new Vector2I(p1.X - p2, p1.Y - p2);
        public static Vector2I operator *(Vector2I p1, int p2) => new Vector2I(p1.X * p2, p1.Y * p2);
        public static Vector2I operator /(Vector2I p1, int p2) => new Vector2I(p1.X / p2, p1.Y / p2);
        public static Vector2I operator %(Vector2I p1, int p2) => new Vector2I(p1.X % p2, p1.Y % p2);

        public static implicit operator Vector2I(Vector2 v) => new Vector2I(v);
        public static implicit operator Vector2(Vector2I v) => new Vector2(v.X, v.Y);

    } // Vector2I
}
