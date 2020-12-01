using SharpNeat.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElementEngine
{
    public static partial class Extensions
    {
        private static readonly FastRandom _rng = new FastRandom();

        public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
        {
            if (!d.ContainsKey(key))
                d.Add(key, value);
            else
                d[key] = value;
        } // AddSet

        public static void AddOrIncrement<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
        {
            if (!d.ContainsKey(key))
                d.Add(key, value);
            else
            {
                dynamic a = d[key];
                dynamic b = value;
                d[key] = a + b;
            }
        } // AddIncrement

        public static T GetRandomItem<T>(this List<T> list, FastRandom rng = null)
        {
            if (rng == null)
                rng = _rng;

            return list[rng.Next(0, list.Count)];
        } // GetRandomItem

        public static void Shuffle<T>(this List<T> list, FastRandom rng = null)
        {
            if (rng == null)
                rng = _rng;

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        } // Shuffle

        public static T GetLastItem<T>(this List<T> list)
        {
            return list[list.Count - 1];
        }

        public static bool ListCompare<T>(this List<T> list1, List<T> list2)
        {
            return list1.All(list2.Contains);
        }

        public static K GetRandomItem<T, K>(this Dictionary<T, K> dictionary, FastRandom rng = null)
        {
            if (rng == null)
                rng = _rng;

            return dictionary.ElementAt(rng.Next(0, dictionary.Count)).Value;
        } // GetRandomItem
    }
}
