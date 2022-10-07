using ElementEngine;
using ElementEngine.ECS;

namespace Samples
{
    internal class SpriteSystem : BaseSystem
    {
        private readonly SpriteBatch2D _spriteBatch;

        public SpriteSystem(Registry registry, SpriteBatch2D spriteBatch) : base(registry)
        {
            _spriteBatch = spriteBatch;

            Group = registry.BuildGroup()
                .Include<Sprite>()
                .Include<Transform>()
                .Build();
        }

        public override void Run(GameTimer gameTimer)
        {
            _spriteBatch.Begin(SamplerType.Point);

            foreach (var entity in Group.Entities)
            {
                ref var sprite = ref entity.GetComponent<Sprite>();
                ref var transform = ref entity.GetComponent<Transform>();

                _spriteBatch.DrawTexture2D(sprite.Texture, transform.Position, origin: sprite.Origin);
            }

            _spriteBatch.End();
        }
    }
}
