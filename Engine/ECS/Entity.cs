﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ElementEngine.ECS
{
    public struct Entity
    {
        [JsonIgnore] public static Entity Empty => new Entity();

        public readonly int ID;
        public readonly short RegistryID;
        public int GenerationID;

        [JsonIgnore] public Registry Registry => Registry._registries[RegistryID];
        [JsonIgnore] public bool IsAlive => Registry.IsEntityAlive(this);
        [JsonIgnore] public EntityStatus Status => Registry.Entities[ID];

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
            Registry.RemoveComponent(Registry.GetComponentStore<T>(), this, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemoveComponentImmediate<T>() where T : struct
        {
            return Registry.TryRemoveComponentImmediate<T>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct
        {
            Debug.Assert(HasComponent<T>());
            return ref ComponentManager<T>.Pool[RegistryID][ID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct
        {
            var componentStore = ComponentManager<T>.Pool[Registry.RegistryID];

            if (componentStore == null)
                return false;

            return componentStore.Contains(ID);
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
            return HashCode.Combine(ID, GenerationID, RegistryID);
        }

        public static bool operator ==(Entity e1, Entity e2) => e1.ID == e2.ID && e1.GenerationID == e2.GenerationID && e1.Registry.RegistryID == e2.Registry.RegistryID;
        public static bool operator !=(Entity e1, Entity e2) => !(e1 == e2);

    } // Entity
}
