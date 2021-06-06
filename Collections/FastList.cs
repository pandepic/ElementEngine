using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public class FastList<T>
    {
        public T[] Data;
        public int Count { get; protected set; } = 0;

        public ref T this[int index] => ref Data[index];

        public FastList(int count = 10)
        {
            Data = new T[count];
        }

        public void Add(T item)
        {
            if (Count == Data.Length)
                Array.Resize(ref Data, Data.Length * 2);

            Data[Count] = item;
            Count += 1;
        }

        public void Remove(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (var i = 0; i < Count; i++)
            {
                if (comparer.Equals(Data[i], item))
                {
                    RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveAt(int index)
        {
            Debug.Assert(index < Count);

            Count -= 1;

            if (index < Count)
                Array.Copy(Data, index + 1, Data, index, Count - index);

            Data[Count] = default(T);
        }

        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (var i = 0; i < Count; i++)
            {
                if (comparer.Equals(Data[i], item))
                    return true;
            }

            return false;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public void Clear()
        {
            Array.Clear(Data, 0, Count);
            Count = 0;
        }

        public void Reset()
        {
            Count = 0;
        }

		public void Sort()
        {
            Array.Sort(Data, 0, Count);
        }

        public void Sort(IComparer comparer)
        {
            Array.Sort(Data, 0, Count, comparer);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(Data, 0, Count, comparer);
        }
    }
}
