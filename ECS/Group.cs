using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class Group
    {
        public Registry Registry;
        public Type[] Types;
        public int EntityCount = 0;
        public Entity[] EntityBuffer;
        public SparseSet EntityLookup;

        public Group(Registry registry, Type[] types)
        {
            Registry = registry;
            Types = new Type[types.Length];
            EntityLookup = new SparseSet(1000);
            types.CopyTo(Types, 0);
            EntityBuffer = new Entity[100];
        }

        public ReadOnlySpan<Entity> Entities => new ReadOnlySpan<Entity>(EntityBuffer, 0, EntityCount);

        public void AddEntity(Entity entity)
        {
            if (EntityLookup.Contains(entity.ID))
                return;

            EntityLookup.TryAdd(entity.ID, out var _);

            if (EntityCount >= EntityBuffer.Length)
                Array.Resize(ref EntityBuffer, EntityBuffer.Length * 2);

            EntityBuffer[EntityCount++] = entity;
        }

        public void RemoveEntity(Entity entity)
        {
            if (!EntityLookup.Contains(entity.ID))
                return;

            EntityLookup.Remove(entity.ID);

            var entityIndex = 0;

            for (var i = 0; i < EntityCount; i++)
            {
                if (EntityBuffer[i] == entity)
                    entityIndex = i;
            }

            EntityBuffer[entityIndex] = EntityBuffer[EntityCount - 1];
            EntityBuffer[EntityCount - 1] = default;
        }
    }
}
