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
        public Entity[] Entities;
        public SparseSet EntitySet;

        public Group(Registry registry, Type[] types)
        {
            Registry = registry;
            Types = new Type[types.Length];
            EntitySet = new SparseSet(1000);
            types.CopyTo(Types, 0);
            Entities = new Entity[100];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<Entity> GetEntities(int start, int length) => new ReadOnlySpan<Entity>(Entities, start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<Entity> GetEntities() => GetEntities(0, EntityCount);

        public void AddEntity(Entity entity)
        {
            if (EntitySet.Contains(entity.ID))
                return;

            EntitySet.TryAdd(entity.ID, out var _);

            if (EntityCount >= Entities.Length)
                Array.Resize(ref Entities, Entities.Length * 2);

            Entities[EntityCount++] = entity;
        }

        public void RemoveEntity(Entity entity)
        {
            if (!EntitySet.Contains(entity.ID))
                return;

            EntitySet.Remove(entity.ID);

            var entityIndex = 0;

            for (var i = 0; i < EntityCount; i++)
            {
                if (Entities[i] == entity)
                    entityIndex = i;
            }

            Entities[entityIndex] = Entities[EntityCount - 1];
            Entities[EntityCount - 1] = default;
        }
    }
}
