using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISprite9Slice : UISprite
    {
        public UITexture TopLeft, TopRight, TopCenter;
        public UITexture MiddleLeft, MiddleRight, MiddleCenter;
        public UITexture BottomLeft, BottomRight, BottomCenter;

        public UISprite9Slice(UISprite9Slice copyFrom, bool baseCopy = false)
        {
            TopLeft = copyFrom.TopLeft;
            TopRight = copyFrom.TopRight;
            TopCenter = copyFrom.TopCenter;

            MiddleLeft = copyFrom.MiddleLeft;
            MiddleRight = copyFrom.MiddleRight;
            MiddleCenter = copyFrom.MiddleCenter;

            BottomLeft = copyFrom.BottomLeft;
            BottomRight = copyFrom.BottomRight;
            BottomCenter = copyFrom.BottomCenter;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UISprite9Slice(string assetNameAll)
            : this(assetNameAll, assetNameAll, assetNameAll,
                  assetNameAll, assetNameAll, assetNameAll,
                  assetNameAll, assetNameAll, assetNameAll)
        {
        }

        public UISprite9Slice(
            string assetNameTopLeft, string assetNameTopRight, string assetNameTopCenter,
            string assetNameMiddleLeft, string assetNameMiddleRight, string assetNameMiddleCenter,
            string assetNameBottomLeft, string assetNameBottomRight, string assetNameBottomCenter)
            : this(
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameTopLeft)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameTopRight)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameTopCenter)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameMiddleLeft)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameMiddleRight)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameMiddleCenter)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameBottomLeft)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameBottomRight)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameBottomCenter)))
        {
        }

        public UISprite9Slice(UITexture all)
            : this(all, all, all,
                  all, all, all,
                  all, all, all)
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

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            UIRendering.Render9Slice(spriteBatch,
                Color ?? RgbaByte.White,
                GetDrawPosition(parent, position, size ?? Size),
                size ?? Size,
                TopLeft, TopRight, TopCenter,
                MiddleLeft, MiddleRight, MiddleCenter,
                BottomLeft, BottomRight, BottomCenter);
        }

    } // UISprite9Slice
}
