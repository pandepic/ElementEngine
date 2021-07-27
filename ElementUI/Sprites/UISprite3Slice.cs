using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI.Sprites
{
    public enum UI3SliceType
    {
        Vertical,
        Horizontal,
    }

    public class UISprite3Slice : UISprite
    {
        public UI3SliceType Type;

        public UITexture Left;
        public UITexture Right;
        public UITexture Center;

        public UISprite3Slice(UI3SliceType type, string assetNameLeft, string assetNameRight, string assetNameCenter)
            : this(type,
                  new UITexture(AssetManager.LoadTexture2D(assetNameLeft)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameRight)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameCenter)))
        {
        }

        public UISprite3Slice(UI3SliceType type, UITexture left, UITexture right, UITexture center)
        {
            Left = left;
            Right = right;
            Center = center;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0)
        {
        }

    } // UISprite3Slice
}
