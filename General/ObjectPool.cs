using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public interface IPoolable
    {
        bool IsAlive { get; set; }

        void Reset();
    }

    public class ObjectPool<T> where T : class, IPoolable, new()
    {
        public const int DefaultPoolSize = 100;
        public const int MaxGrowthLimit = 1000000;

        public T[] Buffer;
        protected bool _disposable = false;
        protected T _swapTemp;

        protected int _lastActiveIndex = -1;
        public int LastActiveIndex { get => _lastActiveIndex; }

        public int Size { get => Buffer.Length; }

        public bool AllowResize { get; set; }

        public T this[int index]
        {
            get => Buffer[index];
        }

        public ObjectPool(int size = DefaultPoolSize, bool allowResize = false)
        {
            AllowResize = allowResize;
            Buffer = new T[0];

            if (typeof(T) is IDisposable)
                _disposable = true;

            AddObjects(size);
        } // constructor

        ~ObjectPool()
        {
            if (_disposable)
            {
                for (var i = 0; i < Size; i++)
                {
                    ((IDisposable)Buffer[i])?.Dispose();
                }
            }
        }

        public void FastClear()
        {
            _lastActiveIndex = -1;
        }

        public void Clear()
        {
            for (var i = 0; i < Buffer.Length; i++)
            {
                Buffer[i].IsAlive = false;
            }

            _lastActiveIndex = -1;
        }

        public void Delete(int poolIndex)
        {
            if (!Buffer[poolIndex].IsAlive)
                return;

            Buffer[poolIndex].IsAlive = false;

            _swapTemp = Buffer[poolIndex];
            Buffer[poolIndex] = Buffer[_lastActiveIndex];
            Buffer[_lastActiveIndex] = _swapTemp;
            _swapTemp = null;
            _lastActiveIndex -= 1;
        }

        public void Delete(T obj)
        {
            for (var i = 0; i <= _lastActiveIndex; i++)
            {
                if (ReferenceEquals(Buffer[i], obj))
                {
                    Delete(i);
                    return;
                }
            }

            throw new Exception("Couldn't delete object, wasn't found alive in pool.");
        }

        public T New()
        {
            if ((_lastActiveIndex + 1) >= Size)
            {
                if (AllowResize)
                {
                    var currentSize = Size;
                    var newSize = currentSize * 2;
                    AddObjects(newSize - currentSize);
                    return New();
                }
                else
                {
                    return default;
                }
            }

            _lastActiveIndex += 1;

            var newObject = Buffer[_lastActiveIndex];
            newObject.IsAlive = true;
            newObject.Reset();
            return newObject;
        }

        protected void AddObjects(int count)
        {
            if (count > MaxGrowthLimit)
                count = MaxGrowthLimit;

            var newIndex = Size;

            Array.Resize(ref Buffer, Size + count);

            for (var i = 0; i < count; i++)
            {
                var obj = new T
                {
                    IsAlive = false
                };

                Buffer[newIndex] = obj;

                newIndex++;
            }
        }

        public IEnumerable<T> GetAlive()
        {
            for (var i = 0; i <= _lastActiveIndex; i++)
            {
                yield return Buffer[i];
            }
        }

        public IEnumerable<T> GetAll()
        {
            for (var i = 0; i < Size; i++)
            {
                yield return Buffer[i];
            }
        }

    } // ObjectPool
}
