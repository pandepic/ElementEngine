using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public struct LazyView<T> : IEnumerable<int> where T : struct
    {
        public LazyRegistry Registry;
        
        public LazyView(LazyRegistry registry)
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

    } // LazyView<T>

    public struct LazyView<T, U> : IEnumerable<int> where T : struct where U : struct
    {
        public LazyRegistry Registry;

        public LazyView(LazyRegistry registry)
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

    } // LazyView<T, U>

    public struct LazyView<T, U, V> : IEnumerable<int> where T : struct where U : struct where V : struct
    {
        public LazyRegistry Registry;

        public LazyView(LazyRegistry registry)
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

    } // LazyView<T, U, V>
}
