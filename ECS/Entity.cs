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
        public readonly Registry Registry;

        public bool IsAlive;
        public int GenerationID;

        public Entity(int id, Registry registry)
        {
            ID = id;
            IsAlive = true;
            GenerationID = 0;
            Registry = registry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddComponent<T>(T component) where T : struct
        {
            return Registry.TryAddComponent(this, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemoveComponent<T>() where T : struct
        {
            return Registry.TryRemoveComponent<T>(this);
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
            return HashCode.Combine(ID);
        }

        public static bool operator ==(Entity e1, Entity e2) => e1.ID == e2.ID && e1.GenerationID == e2.GenerationID && e1.Registry.RegistryID == e2.Registry.RegistryID;
        public static bool operator !=(Entity e1, Entity e2) => !(e1 == e2);

    } // Entity
}
