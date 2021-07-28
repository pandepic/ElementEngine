using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISpriteLayered : UISprite
    {
        public List<UISprite> Sprites;

        public UISpriteLayered(params UISprite[] sprites)
            : this(sprites.ToList())
        {
        }

        public UISpriteLayered(List<UISprite> sprites)
        {
            Sprites = sprites;

            // set to a size that can contain all sprites
            foreach (var sprite in Sprites)
            {
                if (sprite.Size.X > Size.X)
                    Size.X = sprite.Size.X;
                if (sprite.Size.Y > Size.Y)
                    Size.Y = sprite.Size.Y;
            }
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            foreach (var sprite in Sprites)
                sprite.Draw(parent, spriteBatch, position, size, rotation);
        }

    } // UISpriteLayered
}
