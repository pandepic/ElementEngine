using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISprite3SliceHorizontal : UISprite
    {
        public UITexture Left;
        public UITexture Right;
        public UITexture Center;

        public UISprite3SliceHorizontal(string assetNameLeft, string assetNameRight, string assetNameCenter, Vector2I? size = null)
            : this(new UITexture(AssetManager.LoadTexture2D(assetNameLeft)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameRight)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameCenter)),
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
            UIRendering.Render3SliceHorizontal(spriteBatch, GetDrawPosition(parent, position, size ?? Size), size ?? Size, Left, Right, Center);
        }

    } // UISprite3SliceHorizontal

    public class UISprite3SliceVertical : UISprite
    {
        public UITexture Top;
        public UITexture Bottom;
        public UITexture Center;

        public UISprite3SliceVertical(string assetNameTop, string assetNameBottom, string assetNameCenter, Vector2I? size = null)
            : this(new UITexture(AssetManager.LoadTexture2D(assetNameTop)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameBottom)),
                  new UITexture(AssetManager.LoadTexture2D(assetNameCenter)),
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
            UIRendering.Render3SliceVertical(spriteBatch, GetDrawPosition(parent, position, size ?? Size), size ?? Size, Top, Bottom, Center);
        }

    } // UISprite3SliceVertical
}
