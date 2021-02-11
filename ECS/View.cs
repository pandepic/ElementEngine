using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public struct View<T> : IEnumerable<int> where T : struct
    {
        public Registry Registry;
        
        public View(Registry registry)
        {
            Registry = registry;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var store1 = Registry.GetComponentStore<T>();

            for (var i = 0; i < store1.Size; i++)
            {
                var entity = store1.Dense[i];
                yield return entity;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    } // View<T>

    public struct View<T, U> : IEnumerable<int> where T : struct where U : struct
    {
        public Registry Registry;

        public View(Registry registry)
        {
            Registry = registry;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var store1 = Registry.GetComponentStore<T>();
            var store2 = Registry.GetComponentStore<U>();

            for (var i = 0; i < store1.Size; i++)
            {
                var entity = store1.Dense[i];

                if (!store2.Contains(entity)) continue;
                yield return entity;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    } // View<T, U>

    public struct View<T, U, V> : IEnumerable<int> where T : struct where U : struct where V : struct
    {
        public Registry Registry;

        public View(Registry registry)
        {
            Registry = registry;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var store1 = Registry.GetComponentStore<T>();
            var store2 = Registry.GetComponentStore<U>();
            var store3 = Registry.GetComponentStore<V>();

            for (var i = 0; i < store1.Size; i++)
            {
                var entity = store1.Dense[i];

                if (!store2.Contains(entity) || !store3.Contains(entity)) continue;
                yield return entity;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    } // View<T, U, V>
}
