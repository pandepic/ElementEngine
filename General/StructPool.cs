using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public interface IPoolableStruct
    {
        bool IsAlive { get; set; }

        void Reset();
    }

    public class StructPool<T> where T : struct, IPoolableStruct
    {
        public const int DefaultPoolSize = 100;
        public const int MaxGrowthLimit = 1000000;

        public T[] Buffer;

        protected int _lastActiveIndex = -1;
        public int LastActiveIndex { get => _lastActiveIndex; }

        public int Size { get => Buffer.Length; }

        public bool AllowResize { get; set; }

        public StructPool(int size = DefaultPoolSize, bool allowResize = false)
        {
            AllowResize = allowResize;
            Buffer = new T[size];
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

        /// <summary>
        /// Returns the array index of the newly active struct
        /// </summary>
        /// <returns></returns>
        public int New()
        {
            if ((_lastActiveIndex + 1) >= Size)
            {
                if (AllowResize)
                {
                    var currentSize = Size;
                    var newSize = currentSize * 2;
                    Array.Resize(ref Buffer, newSize);
                    return New();
                }
                else
                {
                    return -1;
                }
            }

            _lastActiveIndex += 1;
            Buffer[_lastActiveIndex].IsAlive = true;
            Buffer[_lastActiveIndex].Reset();

            return _lastActiveIndex;
        }

        public void Delete(int index)
        {
            Buffer[index] = Buffer[_lastActiveIndex];
            Buffer[_lastActiveIndex].IsAlive = false;

            _lastActiveIndex -= 1;
        }
    } // StructPool
}
