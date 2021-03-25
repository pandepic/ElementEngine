using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public struct Vector2L
    {
        public long X;
        public long Y;

        public static Vector2L Zero = new Vector2L(0, 0);

        public Vector2L(long val)
        {
            X = val;
            Y = val;
        }

        public Vector2L(long x, long y)
        {
            X = x;
            Y = y;
        }

        public Vector2L(int val)
        {
            X = val;
            Y = val;
        }

        public Vector2L(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2L(float x, float y)
        {
            X = (long)x;
            Y = (long)y;
        }

        public Vector2L(double x, double y)
        {
            X = (long)x;
            Y = (long)y;
        }

        public Vector2L(Vector2 v)
        {
            X = (long)v.X;
            Y = (long)v.Y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public float GetDistance(Vector2L vec)
        {
            return ToVector2().GetDistance(vec.ToVector2());
        }

        public static float GetDistance(Vector2L vec1, Vector2L vec2)
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
            if (obj is Vector2L point)
                return point == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Vector2L p1, Vector2L p2) => p1.X == p2.X && p1.Y == p2.Y;
        public static bool operator !=(Vector2L p1, Vector2L p2) => !(p1 == p2);

        public static Vector2L operator +(Vector2L p1, Vector2L p2) => new Vector2L(p1.X + p2.X, p1.Y + p2.Y);
        public static Vector2L operator -(Vector2L p1, Vector2L p2) => new Vector2L(p1.X - p2.X, p1.Y - p2.Y);
        public static Vector2L operator *(Vector2L p1, Vector2L p2) => new Vector2L(p1.X * p2.X, p1.Y * p2.Y);
        public static Vector2L operator /(Vector2L p1, Vector2L p2) => new Vector2L(p1.X / p2.X, p1.Y / p2.Y);
        public static Vector2L operator %(Vector2L p1, Vector2L p2) => new Vector2L(p1.X % p2.X, p1.Y % p2.Y);

        public static Vector2L operator +(Vector2L p1, long p2) => new Vector2L(p1.X + p2, p1.Y + p2);
        public static Vector2L operator -(Vector2L p1, long p2) => new Vector2L(p1.X - p2, p1.Y - p2);
        public static Vector2L operator *(Vector2L p1, long p2) => new Vector2L(p1.X * p2, p1.Y * p2);
        public static Vector2L operator /(Vector2L p1, long p2) => new Vector2L(p1.X / p2, p1.Y / p2);
        public static Vector2L operator %(Vector2L p1, long p2) => new Vector2L(p1.X % p2, p1.Y % p2);

        public static Vector2L operator +(Vector2L p1, int p2) => new Vector2L(p1.X + p2, p1.Y + p2);
        public static Vector2L operator -(Vector2L p1, int p2) => new Vector2L(p1.X - p2, p1.Y - p2);
        public static Vector2L operator *(Vector2L p1, int p2) => new Vector2L(p1.X * p2, p1.Y * p2);
        public static Vector2L operator /(Vector2L p1, int p2) => new Vector2L(p1.X / p2, p1.Y / p2);
        public static Vector2L operator %(Vector2L p1, int p2) => new Vector2L(p1.X % p2, p1.Y % p2);

        public static implicit operator Vector2L(Vector2 v) => new Vector2L(v);
        public static implicit operator Vector2(Vector2L v) => new Vector2(v.X, v.Y);

    } // Vector2L
}
