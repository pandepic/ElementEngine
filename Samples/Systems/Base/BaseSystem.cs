using ElementEngine;
using ElementEngine.ECS;

namespace Samples
{
    internal abstract class BaseSystem
    {
        protected readonly Registry _registry;

        public Group Group;

        public BaseSystem(Registry registry)
        {
            _registry = registry;
        }

        public abstract void Run(GameTimer gameTimer);
        public virtual void Cleanup() { }
    }
}
