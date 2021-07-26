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

        public UISpriteLayered(List<UISprite> sprites)
        {
            Sprites = sprites;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0)
        {
            foreach (var sprite in Sprites)
                sprite.Draw(parent, spriteBatch, GetDrawPosition(parent, position, size ?? Size), size, rotation);
        }

    } // UISpriteLayered
}
