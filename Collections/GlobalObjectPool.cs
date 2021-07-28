using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public static class GlobalObjectPool<T> where T : new()
    {
        public static List<T> Buffer = new List<T>();

        public static T Rent()
        {
            if (Buffer.Count > 0)
            {
                var obj = Buffer[Buffer.Count - 1];
                Buffer.RemoveAt(Buffer.Count - 1);
                return obj;
            }

            return new();
        }

        public static void Return(T obj)
        {
            Buffer.Add(obj);
        }

    } // GlobalObjectPool
}
