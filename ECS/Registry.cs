using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public struct EntityStatus
    {
        public int ID;
        public bool IsAlive;
        public int GenerationID;
        public HashSet<int> Components;

        public EntityStatus(int id)
        {
            ID = id;
            IsAlive = true;
            GenerationID = 0;
            Components = new HashSet<int>();
        }
    } // EntityStatus

    public delegate void ECSEvent<T>(Entity entity, ref T component);

    internal static class ComponentManager<T> where T : struct
    {
        internal static ComponentStore<T>[] Pool = new ComponentStore<T>[10];
        internal static ECSEvent<T>[] OnFirstAddedPool = new ECSEvent<T>[10];
    }

    internal struct RemoveComponent
    {
        public Entity Entity;
        public IComponentStore ComponentStore;
        public Type Type;
    }

    public class Registry
    {
        internal static Registry[] _registries = new Registry[10];
        internal const int _defaultMaxComponents = 100;
        internal static short _nextRegistryID = 0;

        internal Queue<Entity> _removeEntities = new Queue<Entity>();
        internal Queue<RemoveComponent> _removeComponents = new Queue<RemoveComponent>();

        protected int _nextEntityID = 0;

        public readonly short RegistryID;

        public Dictionary<int, IComponentStore> ComponentStores = new Dictionary<int, IComponentStore>();
        public Dictionary<int, Type> ComponentHashCodeLookup = new Dictionary<int, Type>();
        public List<Group> RegisteredGroups = new List<Group>();
        public SparseSet<EntityStatus> Entities = new SparseSet<EntityStatus>(1000);
        public SparseSet DeadEntities = new SparseSet(1000);

        public Dictionary<int, List<Group>> GroupComponentMap = new Dictionary<int, List<Group>>();

        public Registry()
        {
            RegistryID = _nextRegistryID++;

            if (RegistryID >= _registries.Length)
                Array.Resize(ref _registries, _registries.Length * 2);

            _registries[RegistryID] = this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStore<T> GetComponentStore<T>() where T : struct
        {
            return ComponentManager<T>.Pool[RegistryID];
        }

        public Entity CreateEntity()
        {
            if (DeadEntities.Size > 0)
            {
                var id = DeadEntities[0];
                DeadEntities.Remove(id);
                return CreateEntity(id);
            }
            else
            {
                return CreateEntity(_nextEntityID++);
            }
        } // CreateEntity

        public Entity CreateEntity(int id)
        {
            if (id == 0)
                return CreateEntity();

            if (Entities.Contains(id))
            {
                var status = Entities[id];

                if (status.IsAlive)
                    return new Entity(id, this);

                status.GenerationID += 1;
                status.IsAlive = true;
                status.Components.Clear();
                Entities[id] = status;
            }
            else
            {
                Entities.TryAdd(new EntityStatus(id), id);
            }

            return new Entity(id, this);
        } // CreateEntity

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(int id)
        {
            return new Entity(id, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasEntity(int id)
        {
            return Entities.Contains(id);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityAlive(Entity entity)
        {
            if (!Entities.Contains(entity.ID))
                return false;

            ref var status = ref Entities[entity.ID];
            return status.IsAlive && status.GenerationID == entity.GenerationID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityGeneration(Entity entity)
        {
            return GetEntityGeneration(entity.ID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityGeneration(int id)
        {
            if (!Entities.Contains(id))
                return 0;

            ref var status = ref Entities[id];
            return status.GenerationID;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!Entities.Contains(entity.ID))
                return;

            ref var status = ref Entities[entity.ID];
            status.IsAlive = false;

            _removeEntities.Enqueue(entity);
        }

        public void DestroyEntityImmediate(Entity entity)
        {
            if (!Entities.Contains(entity.ID))
                return;

            DeadEntities.TryAdd(entity.ID, out var _);

            ref var status = ref Entities[entity.ID];
            status.IsAlive = false;

            foreach (var (_, componentStore) in ComponentStores)
                componentStore.TryRemove(entity.ID);

            foreach (var group in RegisteredGroups)
                group.RemoveEntity(entity);
        }

        public void SystemsFinished()
        {
            while (_removeEntities.TryDequeue(out var removeEntity))
                DestroyEntityImmediate(removeEntity);

            while (_removeComponents.TryDequeue(out var removeComponent))
                TryRemoveComponentImmediate(removeComponent.ComponentStore, removeComponent.Entity, removeComponent.Type);
        }

        public void Clear()
        {
            SystemsFinished();

            DeadEntities.Clear();
            Entities.Clear();
            _removeEntities.Clear();
            _removeComponents.Clear();

            foreach (var store in ComponentStores)
                store.Value.Clear();
        }

        public bool TryAddComponent<T>(Entity entity, T component) where T : struct
        {
            var typeHash = typeof(T).GetHashCode();

            if (!ComponentStores.ContainsKey(typeHash))
            {
                if (RegistryID >= ComponentManager<T>.Pool.Length)
                {
                    Array.Resize(ref ComponentManager<T>.Pool, ComponentManager<T>.Pool.Length * 2);
                    Array.Resize(ref ComponentManager<T>.OnFirstAddedPool, ComponentManager<T>.OnFirstAddedPool.Length * 2);
                }

                ComponentManager<T>.Pool[RegistryID] = new ComponentStore<T>(_defaultMaxComponents);
                ComponentStores.Add(typeHash, ComponentManager<T>.Pool[RegistryID]);
                ComponentHashCodeLookup.Add(typeHash, typeof(T));
            }

            if (GetComponentStore<T>().TryAdd(component, entity.ID))
            {
                // didn't have this component type yet, add new data
                ref var status = ref Entities[entity.ID];
                status.Components.Add(typeHash);

                var type = typeof(T);

                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var group = RegisteredGroups[i];
                    var matchesGroup = true;

                    if (group.EntityLookup.Contains(entity.ID))
                    {
                        if (group.ExcludeTypes == null || group.ExcludeTypes.Length == 0)
                            continue;

                        foreach (var excludeType in group.ExcludeTypes)
                        {
                            if (excludeType == type)
                            {
                                group.RemoveEntity(entity);
                                matchesGroup = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (group.ExcludeTypes != null && group.ExcludeTypes.Length > 0)
                        {
                            foreach (var excludeType in group.ExcludeTypes)
                            {
                                var excludeTypeHash = excludeType.GetHashCode();

                                if (status.Components.Contains(excludeTypeHash))
                                {
                                    group.RemoveEntity(entity);
                                    matchesGroup = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (!matchesGroup)
                        continue;

                    if (!group.Types.Contains(type))
                        continue;

                    foreach (var groupType in group.Types)
                    {
                        var groupTypeHash = groupType.GetHashCode();

                        if (ComponentStores.ContainsKey(groupTypeHash))
                        {
                            var store = ComponentStores[groupTypeHash];
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

                var onFirstAdded = ComponentManager<T>.OnFirstAddedPool[RegistryID];

                if (onFirstAdded != null)
                    onFirstAdded(entity, ref component);

                return true;
            }
            else
            {
                // already had this component type, override existing component data
                var store = GetComponentStore<T>();
                store[entity.ID] = component;
                return false;
            }

        } // TryAddComponent

        public void RemoveComponent<T>(Entity entity) where T : struct
        {
            RemoveComponent(GetComponentStore<T>(), entity, typeof(T));
        }

        internal void RemoveComponent(IComponentStore store, Entity entity, Type type)
        {
            _removeComponents.Enqueue(new RemoveComponent()
            {
                Entity = entity,
                ComponentStore = store,
                Type = type,
            });
        }

        public bool TryRemoveComponentImmediate<T>(Entity entity) where T : struct
        {
            return TryRemoveComponentImmediate(GetComponentStore<T>(), entity, typeof(T));
        }

        public bool TryRemoveComponentImmediate(IComponentStore store, Entity entity, Type type)
        {
            if (store.TryRemove(entity.ID))
            {
                var typeHash = type.GetHashCode();

                ref var status = ref Entities[entity.ID];
                status.Components.Remove(typeHash);

                if (GroupComponentMap.TryGetValue(typeHash, out var typeGroups))
                {
                    for (var i = 0; i < typeGroups.Count; i++)
                    {
                        var group = typeGroups[i];
                        group.RemoveEntity(entity);
                    }
                }

                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var group = RegisteredGroups[i];
                    var matchesGroup = true;

                    if (group.ExcludeTypes == null || group.ExcludeTypes.Length == 0)
                        continue;

                    foreach (var groupType in group.Types)
                    {
                        if (!status.Components.Contains(groupType.GetHashCode()))
                        {
                            matchesGroup = false;
                            break;
                        }
                    }

                    if (matchesGroup)
                    {
                        foreach (var excludeType in group.ExcludeTypes)
                        {
                            if (status.Components.Contains(excludeType.GetHashCode()))
                            {
                                matchesGroup = false;
                                break;
                            }
                        }
                    }

                    if (!matchesGroup)
                        group.RemoveEntity(entity);
                    else
                        group.AddEntity(entity);
                }

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Entity entity) where T : struct
        {
            Debug.Assert(HasComponent<T>(entity));
            return ref GetComponentStore<T>()[entity.ID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Entity entity) where T : struct
        {
            return GetComponentStore<T>().Contains(entity.ID);
        }

        public void RegisterOnFirstAdded<T>(ECSEvent<T> action) where T : struct
        {
            ComponentManager<T>.OnFirstAddedPool[RegistryID] = action;
        }

        #region Register group
        public GroupBuilder BuildGroup()
        {
            return new GroupBuilder(this);
        }

        public Group RegisterGroup(params Type[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                throw new Exception("Can't register a group without valid component types");

            if (_nextEntityID > 0)
                throw new Exception("Must register groups before creating entities");

            var group = new Group(this, componentTypes);
            RegisteredGroups.Add(group);

            foreach (var type in componentTypes)
            {
                var typeHash = type.GetHashCode();

                if (!GroupComponentMap.TryGetValue(typeHash, out var typeGroups))
                {
                    typeGroups = new List<Group>();
                    GroupComponentMap.Add(typeHash, typeGroups);
                }

                typeGroups.Add(group);
            }

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
        #endregion

        #region Create view

        public View<T> View<T>() where T : struct
            => new View<T>(this);

        public View<T, U> View<T, U>() where T : struct where U : struct
            => new View<T, U>(this);

        public View<T, U, V> View<T, U, V>() where T : struct where U : struct where V : struct
            => new View<T, U, V>(this);

        #endregion

    } // Registry
}
