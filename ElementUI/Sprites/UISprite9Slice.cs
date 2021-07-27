using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI.Sprites
{
    public class UISprite9Slice : UISprite
    {
        public UITexture TopLeft, TopRight, TopCenter;
        public UITexture MiddleLeft, MiddleRight, MiddleCenter;
        public UITexture BottomLeft, BottomRight, BottomCenter;

        public UISprite9Slice(
            string assetNameTopLeft, string assetNameTopRight, string assetNameTopCenter,
            string assetNameMiddleLeft, string assetNameMiddleRight, string assetNameMiddleCenter,
            string assetNameBottomLeft, string assetNameBottomRight, string assetNameBottomCenter)
            : this(
                  new UITexture(AssetManager.LoadTexture2D(assetNameTopLeft)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameTopRight)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameTopCenter)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameMiddleLeft)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameMiddleRight)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameMiddleCenter)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameBottomLeft)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameBottomRight)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameBottomCenter)))
        {
        }

        public UISprite9Slice(
            UITexture topLeft, UITexture topRight, UITexture topCenter,
            UITexture middleLeft, UITexture middleRight, UITexture middleCenter,
            UITexture bottomLeft, UITexture bottomRight, UITexture bottomCenter)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            TopCenter = topCenter;
            MiddleLeft = middleLeft;
            MiddleRight = middleRight;
            MiddleCenter = middleCenter;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            BottomCenter = bottomCenter;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0)
        {
        }

    } // UISprite9Slice
}
