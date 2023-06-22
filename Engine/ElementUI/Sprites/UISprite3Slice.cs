using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISprite3SliceHorizontal : UISprite
    {
        public UITexture Left;
        public UITexture Right;
        public UITexture Center;

        public UISprite3SliceHorizontal(UISprite3SliceHorizontal copyFrom, bool baseCopy = false)
        {
            Left = copyFrom.Left;
            Right = copyFrom.Right;
            Center = copyFrom.Center;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UISprite3SliceHorizontal(string assetNameLeft, string assetNameRight, string assetNameCenter, Vector2I? size = null)
            : this(new UITexture(AssetManager.Instance.LoadTexture2D(assetNameLeft)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameRight)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameCenter)),
                  size)
        {
        }

        public UISprite3SliceHorizontal(UITexture left, UITexture right, UITexture center, Vector2I? size = null)
        {
            Left = left;
            Right = right;
            Center = center;

            Size = size ?? Vector2I.One;
            Size.Y = MathHelper.Max(Left.Height, Right.Height, Center.Height);
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            UIRendering.Render3SliceHorizontal(
                spriteBatch,
                Color ?? RgbaByte.White,
                GetDrawPosition(parent, position, size ?? Size),
                size ?? Size,
                Left,
                Right,
                Center);
        }
    }

    public class UISprite3SliceVertical : UISprite
    {
        public UITexture Top;
        public UITexture Bottom;
        public UITexture Center;

        public UISprite3SliceVertical(string assetNameTop, string assetNameBottom, string assetNameCenter, Vector2I? size = null)
            : this(new UITexture(AssetManager.Instance.LoadTexture2D(assetNameTop)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameBottom)),
                  new UITexture(AssetManager.Instance.LoadTexture2D(assetNameCenter)),
                  size)
        {
        }

        public UISprite3SliceVertical(UITexture top, UITexture bottom, UITexture center, Vector2I? size = null)
        {
            Top = top;
            Bottom = bottom;
            Center = center;

            Size = size ?? Vector2I.One;
            Size.X = MathHelper.Max(Top.Width, Bottom.Width, Center.Width);
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            UIRendering.Render3SliceVertical(
                spriteBatch,
                Color ?? RgbaByte.White,
                GetDrawPosition(parent, position, size ?? Size),
                size ?? Size,
                Top,
                Bottom,
                Center);
        }
    }
}
