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

        private List<Type> _includedTypes = new();
        private List<Type> _excludedTypes = new();

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
            _includedTypes.AddIfNotContains(t);
            return this;
        }

        public GroupBuilder Exclude<T>() where T : struct
        {
            return Exclude(typeof(T));
        }

        public GroupBuilder Exclude(Type t)
        {
            _excludedTypes.AddIfNotContains(t);
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
