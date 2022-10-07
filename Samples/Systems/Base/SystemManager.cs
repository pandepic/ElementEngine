using ElementEngine;

namespace Samples
{
    internal class SystemManager
    {
        public List<BaseSystem> Systems = new();

        public void AddSystem<T>(T system) where T : BaseSystem
        {
            Systems.Add(system);
        }

        public T GetSystem<T>()
        {
            foreach (var system in Systems)
            {
                if (system is T t)
                    return t;
            }

            return default;
        }

        public void Run(GameTimer gameTimer)
        {
            foreach (var system in Systems)
                system.Run(gameTimer);
        }

        public void Cleanup()
        {
            foreach (var system in Systems)
                system.Cleanup();
        }
    }
}
