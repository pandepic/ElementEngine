using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ElementEngine
{
    public class SparseSet
    {
        public ref struct Enumerator
        {
            private readonly Span<int> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(SparseSet sparseSet)
            {
                _span = new Span<int>(sparseSet.Dense, 0, sparseSet.Size);
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public ref int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_index];
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        protected int _arraySize;

        public int[] Sparse;
        public int[] Dense;

        public int Size { get; protected set; }
        public int MaxValue { get; protected set; }
        public int ArraySize { get => _arraySize; }
        public bool AllowResize { get; protected set; }

        public ref int this[int index] => ref Dense[index];

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public virtual void Remove(int value)
        {
            if (!Contains(value))
                throw new ArgumentException("Value is not contained in this SparseSet", "value");

            var index = GetIndex(value);

            Dense[index] = Dense[Size - 1];
            Sparse[Dense[Size - 1]] = index;
            Size -= 1;
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
    }

    public class SparseSet<T> : SparseSet
    {
        public new ref struct Enumerator
        {
            private readonly Span<T> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(SparseSet<T> sparseSet)
            {
                _span = new Span<T>(sparseSet.Data, 0, sparseSet.Size);
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_index];
            }
        }

        public new Enumerator GetEnumerator() => new Enumerator(this);

        public T[] Data;
        protected int _nextID = 0;

        public ReadOnlySpan<T> GetSpan()
        {
            return new ReadOnlySpan<T>(Data, 0, Size);
        }

        public ReadOnlySpan<T> GetSpan(int start)
        {
            return new ReadOnlySpan<T>(Data, start, Size - start);
        }

        public ReadOnlySpan<T> GetSpan(int start, int length)
        {
            return new ReadOnlySpan<T>(Data, start, length);
        }

        public SparseSet(int maxObjects) : base(maxObjects, true)
        {
            Data = new T[_arraySize];
        }

        public new ref T this[int index] => ref Data[Sparse[index]];

        public override bool TryResize(int newMaxValue)
        {
            if (base.TryResize(newMaxValue))
            {
                Array.Resize(ref Data, _arraySize);
                return true;
            }

            return false;
        }

        public bool TryAdd(T obj, out int id)
        {
            id = _nextID;
            return TryAdd(obj, _nextID);
        }

        public bool TryAdd(T obj, int id)
        {
            if (obj == null)
                return false;

            if (TryAdd(id, out var newIndex))
            {
                Data[newIndex] = obj;

                if (id == _nextID)
                    _nextID += 1;

                return true;
            }

            return false;
        }

        public bool TryGet(int id, out T obj)
        {
            if (!Contains(id))
            {
                obj = default;
                return false;
            }

            obj = Data[GetIndex(id)];
            return true;
        }

        public override void Remove(int id)
        {
            base.Remove(id);
            var index = GetIndex(id);
            Data[index] = Data[Size];
        }

        public override bool TryRemove(int id)
        {
            if (base.TryRemove(id))
            {
                var index = GetIndex(id);
                Data[index] = Data[Size];
                Data[Size] = default;
                return true;
            }

            return false;
        }
    }
}
