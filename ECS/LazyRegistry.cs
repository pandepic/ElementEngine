using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class LazyRegistry
    {
        protected int _nextEntityID = 0;

        public Dictionary<Type, IComponentStore> ComponentData = new Dictionary<Type, IComponentStore>();

        public int MaxEntities { get; protected set; }

        public LazyRegistry(int maxEntities)
        {
            MaxEntities = maxEntities;
        }

        public ComponentStore<T> GetComponentStore<T>() where T : struct
        {
            var type = typeof(T);

            if (ComponentData.TryGetValue(type, out var componentStore))
                return (ComponentStore<T>)componentStore;

            var newStore = new ComponentStore<T>(MaxEntities);
            ComponentData.Add(type, newStore);
            return newStore;
        }

        public int CreateEntity()
        {
            return _nextEntityID++;
        }

        public void DestroyEntity(int entityID)
        {
            foreach (var (_, componentStore) in ComponentData)
                componentStore.TryRemove(entityID);
        }

        public bool TryAddComponent<T>(int entityID, T component) where T : struct
        {
            return GetComponentStore<T>().TryAdd(component, entityID);
        } // TryAddComponent

        public bool TryRemoveComponent<T>(int entityID) where T : struct
        {
            return GetComponentStore<T>().TryRemove(entityID);
        } // TryRemoveComponent

        public ref T GetComponent<T>(int entityID) where T : struct
        {
            var componentStore = GetComponentStore<T>();
            if (!componentStore.Contains(entityID))
                throw new ArgumentException("This entity doesn't have the component requested.", "T");

            return ref componentStore.GetRef(entityID);
        }

        public LazyView<T> LazyView<T>() where T : struct
            => new LazyView<T>(this);

        public LazyView<T, U> LazyView<T, U>() where T : struct where U : struct
            => new LazyView<T, U>(this);

        public LazyView<T, U, V> LazyView<T, U, V>() where T : struct where U : struct where V : struct
            => new LazyView<T, U, V>(this);

    } // LazyRegistry
}
