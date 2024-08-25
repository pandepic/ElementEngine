using System;
using System.Text.Json.Serialization;

namespace ElementEngine
{
    public struct RangeI
    {
        [JsonIgnore] private static Random _rng = new Random();

        public int Min;
        public int Max;

        [JsonIgnore] public int Size => Max - Min;

        public RangeI(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int GetRandomValue(Random rng = null)
        {
            if (rng == null)
                rng = _rng;

            return rng.Next(Min, Max + 1);
        }

        public RangeF ToRangeF() => new RangeF(Min, Max);

        public override string ToString()
        {
            return string.Format("{0}, {1}", Min, Max);
        }

        public static RangeF FromString(string str)
        {
            var split = str.Trim().Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new RangeF(int.Parse(split[0]), int.Parse(split[1]));
        }

        public override bool Equals(object obj)
        {
            if (obj is RangeI range)
                return range == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Min, Max);
        }

        public static bool operator ==(RangeI r1, RangeI r2) => r1.Min == r2.Min && r1.Max == r2.Max;
        public static bool operator !=(RangeI r1, RangeI r2) => !(r1 == r2);
    }
}
