using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    internal static class ComponentManager<T> where T : struct
    {
        internal static ComponentStore<T>[] Pool = new ComponentStore<T>[10];
    }

    public class Registry
    {
        internal const int DefaultMaxComponents = 100;
        internal static int _nextRegistryID = 0;
        
        protected int _nextEntityID = 0;

        public int RegistryID;

        public Dictionary<int, IComponentStore> ComponentData = new Dictionary<int, IComponentStore>();
        public List<Group> RegisteredGroups = new List<Group>();

        public Registry()
        {
            RegistryID = _nextRegistryID++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStore<T> GetComponentStore<T>() where T : struct
        {
            return ComponentManager<T>.Pool[RegistryID];
        }

        public Entity CreateEntity()
        {
            return new Entity(_nextEntityID++, this);
        }

        public void DestroyEntity(Entity entity)
        {
            foreach (var (_, componentStore) in ComponentData)
                componentStore.TryRemove(entity.ID);

            foreach (var group in RegisteredGroups)
                group.RemoveEntity(entity);
        }

        public bool TryAddComponent<T>(Entity entity, T component) where T : struct
        {
            var typeHash = typeof(T).GetHashCode();

            if (!ComponentData.ContainsKey(typeHash))
            {
                if (RegistryID >= ComponentManager<T>.Pool.Length)
                    Array.Resize(ref ComponentManager<T>.Pool, ComponentManager<T>.Pool.Length * 2);

                ComponentManager<T>.Pool[RegistryID] = new ComponentStore<T>(DefaultMaxComponents);
                ComponentData.Add(typeHash, ComponentManager<T>.Pool[RegistryID]);
            }

            if (GetComponentStore<T>().TryAdd(component, entity.ID))
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

                        if (ComponentData.ContainsKey(groupTypeHash))
                        {
                            var store = ComponentData[groupTypeHash];
                            if (!store.Contains(entity.ID))
                                matchesGroup = false;
                        }
                        else
                        {
                            matchesGroup = false;
                        }
                    }

                    if (matchesGroup)
                        group.AddEntity(entity);
                }

                return true;
            }

            return false;
        } // TryAddComponent

        public bool TryRemoveComponent<T>(Entity entity) where T : struct
        {
            if (GetComponentStore<T>().TryRemove(entity.ID))
            {
                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var type = typeof(T);
                    var group = RegisteredGroups[i];

                    if (group.Types.Contains(type))
                        group.RemoveEntity(entity);
                }

                return true;
            }

            return false;
        } // TryRemoveComponent

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Entity entity) where T : struct
        {
            return ref GetComponentStore<T>().GetRef(entity.ID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : struct
        {
            return GetComponentStore<T>().Contains(entity.ID);
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
