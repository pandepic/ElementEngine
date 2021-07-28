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

        public UISpriteColor(Veldrid.RgbaByte color, Vector2I? size = null)
        {
            Size = size ?? Vector2I.One;
            Texture = new Texture2D(Size.X, Size.Y, color);
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            spriteBatch.DrawTexture2D(
                texture: Texture,
                position: GetDrawPosition(parent, position, size ?? Size).ToVector2(),
                scale: (size ?? Size).ToVector2(),
                rotation: rotation);
        }
    }
}
