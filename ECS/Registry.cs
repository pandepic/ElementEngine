using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class Registry
    {
        protected int _nextEntityID = 0;

        public Dictionary<Type, IComponentStore> ComponentData = new Dictionary<Type, IComponentStore>();
        public List<View> RegisteredViews = new List<View>();

        public int MaxEntities { get; protected set; }

        public Registry(int maxEntities)
        {
            MaxEntities = maxEntities;
        }

        public ComponentStore<T> GetComponentStore<T>() where T : struct
        {
            var type = typeof(T);

            if (ComponentData.TryGetValue(type, out var componentStore))
                return (ComponentStore<T>)componentStore;

            var newStore = new ComponentStore<T>(MaxEntities);
            ComponentData.Add(type, newStore);
            return newStore;
        }

        public int CreateEntity()
        {
            return _nextEntityID++;
        }

        public void DestroyEntity(int entityID)
        {
            foreach (var (_, componentStore) in ComponentData)
                componentStore.TryRemove(entityID);

            foreach (var view in RegisteredViews)
                view.Entities.TryRemove(entityID);
        }

        public bool TryAddComponent<T>(int entityID, T component) where T : struct
        {
            if (GetComponentStore<T>().TryAdd(component, entityID))
            {
                for (var i = 0; i < RegisteredViews.Count; i++)
                {
                    var type = typeof(T);
                    var view = RegisteredViews[i];

                    if (view.Types.Contains(type))
                    {
                        view.Entities.TryAdd(entityID, out var _);
                    }
                }

                return true;
            }

            return false;
        } // TryAddComponent

        public bool TryRemoveComponent<T>(int entityID) where T : struct
        {
            if (GetComponentStore<T>().TryRemove(entityID))
            {
                for (var i = 0; i < RegisteredViews.Count; i++)
                {
                    var type = typeof(T);
                    var view = RegisteredViews[i];

                    if (view.Types.Contains(type))
                        view.Entities.TryRemove(entityID);
                }

                return true;
            }

            return false;
        } // TryRemoveComponent

        public ref T GetComponent<T>(int entityID) where T : struct
        {
            var componentStore = GetComponentStore<T>();
            if (!componentStore.Contains(entityID))
                throw new ArgumentException("This entity doesn't have the component requested.", "T");

            return ref componentStore.GetRef(entityID);
        }

        public View RegisterView(Type[] componentTypes)
        {
            if (_nextEntityID > 0)
                throw new Exception("Must register views before creating entities");

            var view = new View(componentTypes);
            RegisteredViews.Add(view);

            return view;
        }

        public View RegisterView<T>() where T : struct
        {
            return RegisterView(new Type[] {
                typeof(T)
            });
        }

        public View RegisterView<T, U>() where T : struct where U : struct
        {
            return RegisterView(new Type[] {
                typeof(T),
                typeof(U)
            });
        }

        public View RegisterView<T, U, V>() where T : struct where U : struct where V : struct
        {
            return RegisterView(new Type[] {
                typeof(T),
                typeof(U),
                typeof(V)
            });
        }

    } // Registry
}
