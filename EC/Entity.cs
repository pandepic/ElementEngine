using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.EC
{
    public class Entity
    {
        public List<IComponent> Components = new List<IComponent>();

        public void Update(GameTimer gameTimer)
        {
            for (var i = 0; i < Components.Count; i++)
                Components[i].Update(gameTimer);
        }

        public void AddComponent<T>(T component) where T : class, IComponent
        {
            Components.Add(component);
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            for (var i = 0; i < Components.Count; i++)
            {
                var component = Components[i];

                if (component is T converted)
                    return converted;
            }

            return null;
        } // GetComponent

        public bool TryGetComponent<T>(out T component) where T : class, IComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

    } // Entity
}
