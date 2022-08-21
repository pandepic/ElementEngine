using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS
{
    public class GroupBuilder
    {
        private readonly Registry _registry;

        private HashSet<Type> _includedTypes = new();
        private HashSet<Type> _excludedTypes = new();

        public GroupBuilder(Registry registry)
        {
            _registry = registry;
        }

        public GroupBuilder Include<T>() where T : struct
        {
            return Include(typeof(T));
        }

        public GroupBuilder Include(Type t)
        {
            _includedTypes.Add(t);
            return this;
        }

        public GroupBuilder Exclude<T>() where T : struct
        {
            return Exclude(typeof(T));
        }

        public GroupBuilder Exclude(Type t)
        {
            _excludedTypes.Add(t);
            return this;
        }

        public Group Build()
        {
            var includedTypes = _includedTypes.ToArray();
            var group = _registry.RegisterGroup(includedTypes);

            group.ExcludeTypes = _excludedTypes.ToArray();

            return group;
        }
    }
}
