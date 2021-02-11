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

        public T GetComponent<T>() where T : IComponent
        {
            for (var i = 0; i < Components.Count; i++)
            {
                var component = Components[i];

                if (component is T converted)
                    return converted;
            }

            return default;
        } // GetComponent

        public bool TryGetComponent<T>(out T component) where T : IComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

    } // Entity
}
