using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public struct Entity
    {
        public readonly int ID;
        public readonly short RegistryID;
        public int GenerationID;

        public Registry Registry => Registry._registries[RegistryID];
        public bool IsAlive => Registry.IsEntityAlive(this);

        public Entity(int id, Registry registry)
        {
            ID = id;
            RegistryID = registry.RegistryID;
            GenerationID = registry.GetEntityGeneration(id);
        }

        public Entity(int id, int generationID, Registry registry)
        {
            ID = id;
            RegistryID = registry.RegistryID;
            GenerationID = generationID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddComponent<T>(T component) where T : struct
        {
            return Registry.TryAddComponent(this, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>() where T : struct
        {
            Registry.RemoveComponent<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemoveComponentImmediate<T>() where T : struct
        {
            return Registry.TryRemoveComponentImmediate<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T TryGetComponent<T>() where T : struct
        {
            return ref Registry.TryGetComponent<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct
        {
            return ref Registry.GetComponent<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct
        {
            return Registry.GetComponentStore<T>().Contains(ID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is Entity entity)
                return entity == this;
            else
                return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(ID, GenerationID, Registry);
        }

        public static bool operator ==(Entity e1, Entity e2) => e1.ID == e2.ID && e1.GenerationID == e2.GenerationID && e1.Registry.RegistryID == e2.Registry.RegistryID;
        public static bool operator !=(Entity e1, Entity e2) => !(e1 == e2);

    } // Entity
}
