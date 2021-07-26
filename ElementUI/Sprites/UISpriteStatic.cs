using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISpriteStatic : UISprite
    {
        public UITexture Texture;

        public UISpriteStatic(string assetName)
            : this(new UITexture(AssetManager.LoadTexture2D(assetName)))
        {
        }

        public UISpriteStatic(UITexture texture)
        {
            Texture = texture;
            Size = Texture.SourceRect.SizeF;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0f)
        {
            Texture.Draw(spriteBatch, GetDrawPosition(parent, position, size ?? Size), Texture.GetScale(size), rotation);
        }
    }
}
