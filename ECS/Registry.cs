using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class Registry
    {
        protected const int _defaultMaxComponents = 100;
        protected int _nextEntityID = 0;

        //public Dictionary<int, IComponentStore> ComponentData = new Dictionary<int, IComponentStore>();
        public SparseSet<IComponentStore> ComponentData = new SparseSet<IComponentStore>(100);
        public List<Group> RegisteredGroups = new List<Group>();

        public Registry()
        {
        }

        /// <summary>
        /// Gets the component store for type T and creates a new one if there isn't one already.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The component store for the type T.</returns>
        public ComponentStore<T> GetComponentStore<T>() where T : struct
        {
            var type = typeof(T).GetHashCode();

            if (ComponentData.Contains(type))
                return (ComponentStore<T>)ComponentData.Get(type);

            var newStore = new ComponentStore<T>(_defaultMaxComponents);
            ComponentData.TryAdd(newStore, type);
            return newStore;
        }

        public ComponentStore<T> FastGetComponentStore<T>() where T : struct
        {
            var type = typeof(T);
            return (ComponentStore<T>)ComponentData.Get(type.GetHashCode());
        }

        public Entity CreateEntity()
        {
            return new Entity(_nextEntityID++, this);
        }

        public void DestroyEntity(Entity entity)
        {
            DestroyEntity(entity.ID);
        }

        public void DestroyEntity(int entityID)
        {
            foreach (var store in ComponentData)
                store.TryRemove(entityID);

            foreach (var group in RegisteredGroups)
                group.Entities.TryRemove(entityID);
        }

        public bool TryAddComponent<T>(Entity entity, T component) where T : struct
        {
            return TryAddComponent(entity.ID, component);
        }

        public bool TryAddComponent<T>(int entityID, T component) where T : struct
        {
            if (GetComponentStore<T>().TryAdd(component, entityID))
            {
                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var type = typeof(T);
                    var group = RegisteredGroups[i];
                    var matchesGroup = true;

                    if (!group.Types.Contains(type))
                        continue;
                    
                    foreach (var groupType in group.Types)
                    {
                        var groupTypeHash = groupType.GetHashCode();

                        if (ComponentData.Contains(groupTypeHash))
                        {
                            var store = ComponentData.Get(groupTypeHash);
                            if (!store.Contains(entityID))
                                matchesGroup = false;
                        }
                        else
                        {
                            matchesGroup = false;
                        }
                    }

                    if (matchesGroup)
                        group.Entities.TryAdd(entityID, out var _);
                }

                return true;
            }

            return false;
        } // TryAddComponent

        public bool TryRemoveComponent<T>(Entity entity) where T : struct
        {
            return TryRemoveComponent<T>(entity.ID);
        }

        public bool TryRemoveComponent<T>(int entityID) where T : struct
        {
            if (GetComponentStore<T>().TryRemove(entityID))
            {
                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var type = typeof(T);
                    var group = RegisteredGroups[i];

                    if (group.Types.Contains(type))
                        group.Entities.TryRemove(entityID);
                }

                return true;
            }

            return false;
        } // TryRemoveComponent

        public ref T GetComponent<T>(Entity entity) where T : struct
        {
            return ref GetComponent<T>(entity.ID);
        }

        public ref T GetComponent<T>(int entityID) where T : struct
        {
            var componentStore = FastGetComponentStore<T>();
            return ref componentStore.GetRef(entityID);
        }

        public bool HasComponent<T>(Entity entity) where T : struct
        {
            return HasComponent<T>(entity.ID);
        }

        public bool HasComponent<T>(int entityID) where T : struct
        {
            var componentStore = FastGetComponentStore<T>();
            return componentStore.Contains(entityID);
        }

        public Group RegisterGroup(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                throw new Exception("Can't register a group without valid component types");

            if (_nextEntityID > 0)
                throw new Exception("Must register groups before creating entities");

            var group = new Group(this, componentTypes);
            RegisteredGroups.Add(group);

            return group;
        }

        public Group RegisterGroup<T>() where T : struct
        {
            return RegisterGroup(typeof(T));
        }

        public Group RegisterGroup<T, U>() where T : struct where U : struct
        {
            return RegisterGroup(typeof(T), typeof(U));
        }

        public Group RegisterGroup<T, U, V>() where T : struct where U : struct where V : struct
        {
            return RegisterGroup(typeof(T), typeof(U), typeof(V));
        }

        public View<T> View<T>() where T : struct
            => new View<T>(this);

        public View<T, U> View<T, U>() where T : struct where U : struct
            => new View<T, U>(this);

        public View<T, U, V> View<T, U, V>() where T : struct where U : struct where V : struct
            => new View<T, U, V>(this);

    } // Registry
}
