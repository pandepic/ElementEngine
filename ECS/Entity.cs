using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public struct Entity
    {
        public readonly int ID;
        public bool IsAlive;
        public int GenerationID;
        public readonly Registry Registry;

        public Entity(int id, Registry registry)
        {
            ID = id;
            IsAlive = true;
            GenerationID = 0;
            Registry = registry;
        }

        public bool TryAddComponent<T>(T component) where T : struct
        {
            return Registry.TryAddComponent(this, component);
        }

        public bool TryRemoveComponent<T>() where T : struct
        {
            return Registry.TryRemoveComponent<T>(this);
        }

        public ref T GetComponent<T>() where T : struct
        {
            return ref Registry.GetComponent<T>(this);
        }

        public override bool Equals(object obj)
        {
            if (obj is Entity entity)
                return entity == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }

        public static bool operator ==(Entity e1, Entity e2) => e1.ID == e2.ID && e1.ID == e2.ID;
        public static bool operator !=(Entity e1, Entity e2) => !(e1 == e2);

    } // Entity
}
