using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public class SparseSet : IEnumerable<int>
    {
        protected int _arraySize;

        public int[] Sparse;
        public int[] Dense;

        public int Size { get; protected set; }
        public int MaxValue { get; protected set; }
        public int ArraySize { get => _arraySize; }
        public bool AllowResize { get; protected set; }

        public SparseSet(int maxValue, bool allowResize = true)
        {
            Size = 0;
            AllowResize = allowResize;
            MaxValue = maxValue;

            _arraySize = MaxValue + 1;
            Sparse = new int[_arraySize];
            Dense = new int[_arraySize];
        }

        public virtual bool TryResize(int newMaxValue)
        {
            if (newMaxValue <= MaxValue)
                return false;

            // don't resize by less than double the current max
            if (newMaxValue < (MaxValue * 2))
                newMaxValue = MaxValue * 2;

            MaxValue = newMaxValue;
            _arraySize = MaxValue + 1;

            Array.Resize(ref Sparse, _arraySize);
            Array.Resize(ref Dense, _arraySize);

            return true;
        }

        public bool TryAdd(int value, out int index)
        {
            index = 0;

            if (Contains(value))
                return false;

            if (value > MaxValue && AllowResize)
            {
                if (AllowResize)
                {
                    if (!TryResize(value))
                        return false;
                }
                else
                {
                    return false;
                }
            }

            Dense[Size] = value;
            Sparse[value] = Size;
            Size += 1;

            index = GetIndex(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int value)
        {
            return Sparse[value];
        }

        public bool Contains(int value)
        {
            if (value < 0 || value > MaxValue)
                return false;

            return Sparse[value] < Size && Dense[Sparse[value]] == value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Size = 0;
        }

        public virtual bool TryRemove(int value)
        {
            if (!Contains(value))
                return false;

            var index = GetIndex(value);

            Dense[index] = Dense[Size - 1];
            Sparse[Dense[Size - 1]] = index;
            Size -= 1;

            return true;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = 0; i < Size; i++)
                yield return Dense[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    } // SparseSet

    public interface ISparseSetObject
    {
        public int SparseSetID { get; set; }
    }

    public class SparseSet<T> : SparseSet, IEnumerable<T> where T : ISparseSetObject
    {
        public T[] Data;
        protected int _nextID = 0;

        public SparseSet(int maxObjects) : base(maxObjects, true)
        {
            Data = new T[_arraySize];
        }

        public override bool TryResize(int newMaxValue)
        {
            if (base.TryResize(newMaxValue))
            {
                Array.Resize(ref Data, _arraySize);
                return true;
            }

            return false;
        }

        public bool TryAdd(T obj)
        {
            if (TryAdd(_nextID, out var newIndex))
            {
                Data[newIndex] = obj;
                obj.SparseSetID = _nextID;
                _nextID += 1;

                return true;
            }

            return false;
        }

        public T Get(int id) => Data[GetIndex(id)];

        public bool TryGet(int id, out T obj)
        {
            if (!Contains(id))
            {
                obj = default(T);
                return false;
            }

            obj = Data[GetIndex(id)];
            return true;
        }

        public bool TryRemove(T obj) => TryRemove(obj.SparseSetID);

        public new bool TryRemove(int id)
        {
            if (base.TryRemove(id))
            {
                var index = GetIndex(id);
                Data[index] = Data[Size];
                return true;
            }

            return false;
        }

        public bool Contains(T obj) => Contains(obj.SparseSetID);

        public new IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Size; i++)
                yield return Data[i];
        }

    } // SparseSet<T>
}
