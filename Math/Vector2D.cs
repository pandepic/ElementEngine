using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public struct Vector2D
    {
        public double X;
        public double Y;

        public static Vector2D Zero = new Vector2D(0, 0);

        public Vector2D(long val)
        {
            X = val;
            Y = val;
        }

        public Vector2D(long x, long y)
        {
            X = x;
            Y = y;
        }

        public Vector2D(int val)
        {
            X = val;
            Y = val;
        }

        public Vector2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2D(float x, float y)
        {
            X = (int)x;
            Y = (int)y;
        }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector2D(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2((float)X, (float)Y);
        }

        public float GetDistance(Vector2D vec)
        {
            return ToVector2().GetDistance(vec.ToVector2());
        }

        public static float GetDistance(Vector2D vec1, Vector2D vec2)
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
            if (obj is Vector2D point)
                return point == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Vector2D p1, Vector2D p2) => p1.X == p2.X && p1.Y == p2.Y;
        public static bool operator !=(Vector2D p1, Vector2D p2) => !(p1 == p2);

        public static Vector2D operator +(Vector2D p1, Vector2D p2) => new Vector2D(p1.X + p2.X, p1.Y + p2.Y);
        public static Vector2D operator -(Vector2D p1, Vector2D p2) => new Vector2D(p1.X - p2.X, p1.Y - p2.Y);
        public static Vector2D operator *(Vector2D p1, Vector2D p2) => new Vector2D(p1.X * p2.X, p1.Y * p2.Y);
        public static Vector2D operator /(Vector2D p1, Vector2D p2) => new Vector2D(p1.X / p2.X, p1.Y / p2.Y);
        public static Vector2D operator %(Vector2D p1, Vector2D p2) => new Vector2D(p1.X % p2.X, p1.Y % p2.Y);

        public static Vector2D operator +(Vector2D p1, double p2) => new Vector2D(p1.X + p2, p1.Y + p2);
        public static Vector2D operator -(Vector2D p1, double p2) => new Vector2D(p1.X - p2, p1.Y - p2);
        public static Vector2D operator *(Vector2D p1, double p2) => new Vector2D(p1.X * p2, p1.Y * p2);
        public static Vector2D operator /(Vector2D p1, double p2) => new Vector2D(p1.X / p2, p1.Y / p2);
        public static Vector2D operator %(Vector2D p1, double p2) => new Vector2D(p1.X % p2, p1.Y % p2);

        public static Vector2D operator +(Vector2D p1, long p2) => new Vector2D(p1.X + p2, p1.Y + p2);
        public static Vector2D operator -(Vector2D p1, long p2) => new Vector2D(p1.X - p2, p1.Y - p2);
        public static Vector2D operator *(Vector2D p1, long p2) => new Vector2D(p1.X * p2, p1.Y * p2);
        public static Vector2D operator /(Vector2D p1, long p2) => new Vector2D(p1.X / p2, p1.Y / p2);
        public static Vector2D operator %(Vector2D p1, long p2) => new Vector2D(p1.X % p2, p1.Y % p2);

        public static implicit operator Vector2D(Vector2 v) => new Vector2D(v);
        public static implicit operator Vector2(Vector2D v) => new Vector2((float)v.X, (float)v.Y);

    } // Vector2I
}
