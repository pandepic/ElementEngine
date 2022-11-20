using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class ViewBuilder
    {
        private readonly Registry _registry;

        private HashSet<Type> _includedTypes = new();
        private HashSet<Type> _excludedTypes = new();

        public ViewBuilder(Registry registry)
        {
            _registry = registry;
        }

        public void Clear()
        {
            _includedTypes.Clear();
            _excludedTypes.Clear();
        }

        public ViewBuilder Include<T>() where T : struct
        {
            return Include(typeof(T));
        }

        public ViewBuilder Include(Type t)
        {
            _includedTypes.Add(t);
            return this;
        }

        public ViewBuilder Exclude<T>() where T : struct
        {
            return Exclude(typeof(T));
        }

        public ViewBuilder Exclude(Type t)
        {
            _excludedTypes.Add(t);
            return this;
        }

        public List<Entity> Build()
        {
            return _registry.GetView(_includedTypes, _excludedTypes);
        }
    }
}
