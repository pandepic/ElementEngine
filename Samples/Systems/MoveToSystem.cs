using System.Numerics;
using ElementEngine;
using ElementEngine.ECS;

namespace Samples
{
    internal class MoveToSystem : BaseSystem
    {
        public MoveToSystem(Registry registry) : base(registry)
        {
            Group = registry.BuildGroup()
                .Include<MoveTo>()
                .Include<Transform>()
                .Build();
        }

        public override void Run(GameTimer gameTimer)
        {
            foreach (var entity in Group.Entities)
            {
                ref var transform = ref entity.GetComponent<Transform>();
                ref var moveTo = ref entity.GetComponent<MoveTo>();

                var direction = Vector2.Normalize(moveTo.Target - transform.Position);
                transform.Position += direction * moveTo.Velocity * gameTimer.DeltaS;
            }
        }
    }
}
