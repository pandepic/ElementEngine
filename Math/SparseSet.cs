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
        public struct Enumerator : IEnumerator, IEnumerator<int>
        {
            private int _current;
            private SparseSet _set;
            private int _index;

            public int Current
            {
                get
                {
                    if (_index > -1)
                        return _current;
                    else if (_index == -1)
                        throw new InvalidOperationException("Enumerator not yet started");
                    else
                        throw new InvalidOperationException("Enumerator reached end");
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            internal Enumerator(SparseSet set)
            {
                _set = set;
                _index = -1;
                _current = default;
            }

            void IDisposable.Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index > -2 && _index < _set.Size - 1)
                {
                    _current = _set[++_index];
                    return true;
                }
                else
                {
                    _index = -2; // -2 indicates "past the end"
                    return false;
                }
            }

            public void Reset()
            {
                _index = -1;
            }
        } // Enumerator

        protected int _arraySize;

        public int[] Sparse;
        public int[] Dense;

        public int Size { get; protected set; }
        public int MaxValue { get; protected set; }
        public int ArraySize { get => _arraySize; }
        public bool AllowResize { get; protected set; }

        public int this[int index] => Dense[index];

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

        public IEnumerator<int> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    } // SparseSet

    public class SparseSet<T> : SparseSet, IEnumerable<T>
    {
        public new struct Enumerator : IEnumerator, IEnumerator<T>
        {
            private T _current;
            private SparseSet<T> _set;
            private int _index;

            public T Current
            {
                get
                {
                    if (_index > -1)
                        return _current;
                    else if (_index == -1)
                        throw new InvalidOperationException("Enumerator not yet started");
                    else
                        throw new InvalidOperationException("Enumerator reached end");
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            internal Enumerator(SparseSet<T> set)
            {
                _set = set;
                _index = -1;
                _current = default;
            }

            void IDisposable.Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index > -2 && _index < _set.Size - 1)
                {
                    _current = _set[++_index];
                    return true;
                }
                else
                {
                    _index = -2; // -2 indicates "past the end"
                    return false;
                }
            }

            public void Reset()
            {
                _index = -1;
            }
        } // Enumerator

        public T[] Data;
        protected int _nextID = 0;

        public SparseSet(int maxObjects) : base(maxObjects, true)
        {
            Data = new T[_arraySize];
        }

        public new ref T this[int index] => ref Data[GetIndex(index)];

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

        public new IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

    } // SparseSet<T>
}
