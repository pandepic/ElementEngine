using System;
using System.Collections;
using System.Collections.Generic;
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

    internal static class ComponentManager<T> where T : struct
    {
        internal static ComponentStore<T>[] Pool = new ComponentStore<T>[10];
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

        public Dictionary<int, IComponentStore> ComponentData = new Dictionary<int, IComponentStore>();
        public List<Group> RegisteredGroups = new List<Group>();
        public SparseSet<EntityStatus> Entities = new SparseSet<EntityStatus>(1000);
        public SparseSet DeadEntities = new SparseSet(1000);

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
                ref var status = ref Entities.GetRef(id);

                if (status.IsAlive)
                    return new Entity(id, this);

                status.GenerationID += 1;
                status.IsAlive = true;
                status.Components.Clear();
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

            ref var status = ref Entities.GetRef(entity.ID);
            return status.IsAlive;
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

            ref var status = ref Entities.GetRef(id);
            return status.GenerationID;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!Entities.Contains(entity.ID))
                return;

            ref var status = ref Entities.GetRef(entity.ID);
            status.IsAlive = false;

            _removeEntities.Enqueue(entity);
        }

        public void DestroyEntityImmediate(Entity entity)
        {
            if (!Entities.Contains(entity.ID))
                return;

            DeadEntities.TryAdd(entity.ID, out var _);

            ref var status = ref Entities.GetRef(entity.ID);
            status.IsAlive = false;

            foreach (var (_, componentStore) in ComponentData)
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

        public bool TryAddComponent<T>(Entity entity, T component) where T : struct
        {
            var typeHash = typeof(T).GetHashCode();

            if (!ComponentData.ContainsKey(typeHash))
            {
                if (RegistryID >= ComponentManager<T>.Pool.Length)
                    Array.Resize(ref ComponentManager<T>.Pool, ComponentManager<T>.Pool.Length * 2);

                ComponentManager<T>.Pool[RegistryID] = new ComponentStore<T>(_defaultMaxComponents);
                ComponentData.Add(typeHash, ComponentManager<T>.Pool[RegistryID]);
            }

            if (GetComponentStore<T>().TryAdd(component, entity.ID))
            {
                ref var status = ref Entities.GetRef(entity.ID);
                status.Components.Add(typeHash);

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
            else
            {
                var store = GetComponentStore<T>();
                store[store.GetIndex(entity.ID)] = component;
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
                ref var status = ref Entities.GetRef(entity.ID);
                status.Components.Remove(type.GetHashCode());

                for (var i = 0; i < RegisteredGroups.Count; i++)
                {
                    var group = RegisteredGroups[i];

                    if (group.Types.Contains(type))
                        group.RemoveEntity(entity);
                }

                return true;
            }

            return false;
        }

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
