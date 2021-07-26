using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISpriteColor : UISprite
    {
        public Texture2D Texture;

        public UISpriteColor(Veldrid.RgbaByte color, Vector2? size = null)
        {
            Size = size ?? new Vector2(1);
            Texture = new Texture2D(1, 1, color);
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0)
        {
            spriteBatch.DrawTexture2D(
                texture: Texture,
                position: GetDrawPosition(parent, position, size ?? Size),
                scale: size ?? Size,
                rotation: rotation);
        }
    }
}
