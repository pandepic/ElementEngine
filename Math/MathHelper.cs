using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ElementEngine
{
    public static class MathHelper
    {
        public const float ToRadians = (MathF.PI * 2f) / 360f;
        public const float ToDegrees = 360f / (MathF.PI * 2f);

        public static float GetAngleDegreesBetweenPositions(Vector2 origin, Vector2 target)
        {
            var angleRadians = MathF.Atan2((target.X - origin.X), (origin.Y - target.Y));
            var angleDegrees = angleRadians.ToDegrees();

            if (angleDegrees < 0.0f)
                angleDegrees += 360.0f;
            else if (angleDegrees > 360.0f)
                angleDegrees -= 360.0f;

            return angleDegrees;
        }

        public static double GetAngleDegreesBetweenPositions(Vector2D origin, Vector2D target)
        {
            var angleRadians = Math.Atan2((target.X - origin.X), (origin.Y - target.Y));
            var angleDegrees = angleRadians.ToDegrees();

            if (angleDegrees < 0.0f)
                angleDegrees += 360.0f;
            else if (angleDegrees > 360.0f)
                angleDegrees -= 360.0f;

            return angleDegrees;
        }

        public static int GetSeedFromString(string str)
        {
            using (var mySHA256 = SHA256Managed.Create())
            {
                byte[] toHashBytes = Encoding.UTF8.GetBytes(str);
                var hashValue = mySHA256.ComputeHash(toHashBytes);
                var hashValueStr = "";

                foreach (byte b in hashValue)
                {
                    hashValueStr += b.ToString("x2");
                }

                int hashInt = Convert.ToInt32(hashValueStr.Substring(0, 8), 16);

                return hashInt;
            }
        } // GetSeedFromString

        public static Vector2 GetPointOnBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float t2 = t * t;
            float u2 = u * u;
            float u3 = u2 * u;
            float t3 = t2 * t;

            Vector2 result =
                (u3) * p0 +
                (3f * u2 * t) * p1 +
                (3f * u * t2) * p2 +
                (t3) * p3;

            return result;
        } // GetPointOnBezierCurve

        public static Vector2 GetPointOnBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            float t2 = t * t;
            float u2 = u * u;

            Vector2 result =
                (u2) * p0 +
                (2f * u * t) * p1 +
                (t2) * p2;

            return result;
        } // GetPointOnBezierCurve

        public static float Clamp(float value, float min, float max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        public static double Clamp(double value, double min, double max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        public static Vector2 GetPointOnCircle(Vector2 position, float radius, int index, int numPoints)
        {
            float i = (2 * MathF.PI / numPoints) * index;
            return new Vector2(
                    MathF.Cos(i) * radius + position.X,
                    MathF.Sin(i) * radius + position.Y);
        }

        public static int Max(int v1, int v2, int v3)
        {
            return Math.Max(v1, Math.Max(v2, v3));
        }

        public static int Max(int v1, int v2, int v3, int v4)
        {
            return Math.Max(v1, Math.Max(v2, Math.Max(v3, v4)));
        }

        public static void Swap(ref int v1, ref int v2)
        {
            var t = v1;
            v1 = v2;
            v2 = t;
        }

        public static void Swap(ref float v1, ref float v2)
        {
            var t = v1;
            v1 = v2;
            v2 = t;
        }

        public static void Swap(ref double v1, ref double v2)
        {
            var t = v1;
            v1 = v2;
            v2 = t;
        }

    } // MathHelper
}
