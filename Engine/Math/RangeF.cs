using System;
using System.Text.Json.Serialization;

namespace ElementEngine
{
    public struct RangeF
    {
        [JsonIgnore] private static Random _rng = new Random();

        public float Min;
        public float Max;

        [JsonIgnore] public float Size => Max - Min;

        public RangeF(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float GetRandomValue(Random rng = null)
        {
            if (rng == null)
                rng = _rng;

            return rng.NextFloat(Min, Max);
        }

        public RangeI ToRangeF() => new RangeI((int)Min, (int)Max);

        public override string ToString()
        {
            return string.Format("{0}, {1}", Min, Max);
        }

        public static RangeF FromString(string str)
        {
            var split = str.Trim().Replace(" ", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new RangeF(float.Parse(split[0]), float.Parse(split[1]));
        }

        public override bool Equals(object obj)
        {
            if (obj is RangeF range)
                return range == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Min, Max);
        }

        public static bool operator ==(RangeF r1, RangeF r2) => r1.Min == r2.Min && r1.Max == r2.Max;
        public static bool operator !=(RangeF r1, RangeF r2) => !(r1 == r2);
    }
}
