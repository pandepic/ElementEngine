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
        public readonly Registry Registry;

        public Entity(int id, Registry registry)
        {
            ID = id;
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

    } // Entity
}
