using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public struct Vector3I
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public static Vector3I Zero = new Vector3I(0, 0, 0);

        public Vector3I(int val)
        {
            X = val;
            Y = val;
            Z = val;
        }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I(float x, float y, float z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }

        public Vector3I(double x, double y, double z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }

        public Vector3I(Vector3 v)
        {
            X = (int)v.X;
            Y = (int)v.Y;
            Z = (int)v.Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", X, Y, Z);
        }

        public static Vector3I FromString(string str)
        {
            var split = str.Trim().Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new Vector3I(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3I point)
                return point == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3I p1, Vector3I p2) => p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        public static bool operator !=(Vector3I p1, Vector3I p2) => !(p1 == p2);

        public static Vector3I operator +(Vector3I p1, Vector3I p2) => new Vector3I(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        public static Vector3I operator -(Vector3I p1, Vector3I p2) => new Vector3I(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        public static Vector3I operator *(Vector3I p1, Vector3I p2) => new Vector3I(p1.X * p2.X, p1.Y * p2.Y, p1.Z * p2.Z);
        public static Vector3I operator /(Vector3I p1, Vector3I p2) => new Vector3I(p1.X / p2.X, p1.Y / p2.Y, p1.Z / p2.Z);
        public static Vector3I operator %(Vector3I p1, Vector3I p2) => new Vector3I(p1.X % p2.X, p1.Y % p2.Y, p1.Z % p2.Z);

        public static Vector3I operator +(Vector3I p1, int p2) => new Vector3I(p1.X + p2, p1.Y + p2, p1.Z + p2);
        public static Vector3I operator -(Vector3I p1, int p2) => new Vector3I(p1.X - p2, p1.Y - p2, p1.Z - p2);
        public static Vector3I operator *(Vector3I p1, int p2) => new Vector3I(p1.X * p2, p1.Y * p2, p1.Z * p2);
        public static Vector3I operator /(Vector3I p1, int p2) => new Vector3I(p1.X / p2, p1.Y / p2, p1.Z / p2);
        public static Vector3I operator %(Vector3I p1, int p2) => new Vector3I(p1.X % p2, p1.Y % p2, p1.Z % p2);

        public static implicit operator Vector3I(Vector3 v) => new Vector3I(v);
        public static implicit operator Vector3(Vector3I v) => new Vector3(v.X, v.Y, v.Z);

    } // Vector3I
}
