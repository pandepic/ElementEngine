using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ElementEngine
{
    public static partial class Extensions
    {
        #region Get/Set array and list by x/y positions (useful in tile maps etc.)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetByXY<T>(this T[] array, int x, int y, int width)
        {
            return array[x + width * y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetByXY<T>(this T[] array, Vector2I xy, int width)
        {
            return array[xy.X + width * xy.Y];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetByXY<T>(this T[] array, int x, int y, int width, T value)
        {
            array[x + width * y] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetByXY<T>(this T[] array, Vector2I xy, int width, T value)
        {
            array[xy.X + width * xy.Y] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromXY<T>(this T[] array, int x, int y, int width)
        {
            return x + width * y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromXY<T>(this T[] array, Vector2I xy, int width)
        {
            return xy.X + width * xy.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetByXY<T>(this List<T> list, int x, int y, int width, T value)
        {
            list[x + width * y] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetByXY<T>(this List<T> list, Vector2I xy, int width, T value)
        {
            list[xy.X + width * xy.Y] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromXY<T>(this List<T> list, int x, int y, int width)
        {
            return x + width * y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromXY<T>(this List<T> list, Vector2I xy, int width)
        {
            return xy.X + width * xy.Y;
        }
        #endregion

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

        public static T GetRandomItem<T>(this List<T> list, Random rng = null)
        {
            if (rng == null)
                rng = _rng;

            return list[rng.Next(0, list.Count)];
        } // GetRandomItem

        public static void Shuffle<T>(this List<T> list, Random rng = null)
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

        public static void RemoveLastItem<T>(this List<T> list)
        {
            list.RemoveAt(list.Count - 1);
        }

        public static bool ListCompare<T>(this List<T> list1, List<T> list2)
        {
            return list1.All(list2.Contains);
        }

        public static K GetRandomItem<T, K>(this Dictionary<T, K> dictionary, Random rng = null)
        {
            if (rng == null)
                rng = _rng;

            return dictionary.ElementAt(rng.Next(0, dictionary.Count)).Value;
        } // GetRandomItem

        public static bool AddIfNotContains<T>(this List<T> list, T value)
        {
            if (list.Contains(value))
                return false;

            list.Add(value);
            return true;
        }

        public static bool AddIfNotContains<T>(this List<T> list, List<T> values)
        {
            foreach (var value in values)
            {
                if (!list.AddIfNotContains(value))
                    return false;
            }

            return true;
        }
    }
}
