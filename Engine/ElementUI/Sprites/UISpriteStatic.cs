using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISpriteStatic : UISprite
    {
        public UITexture Texture;

        public UISpriteStatic(UISpriteStatic copyFrom, bool baseCopy = false)
        {
            Texture = copyFrom.Texture;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UISpriteStatic(string assetName)
            : this(new UITexture(AssetManager.Instance.LoadTexture2D(assetName)))
        {
        }

        public UISpriteStatic(UITexture texture)
        {
            Texture = texture;
            Size = Texture.SourceRect.Size;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0f)
        {
            Texture.Draw(
                spriteBatch,
                GetDrawPosition(parent, position,
                IgnoreSize ? Size : (size ?? Size)).ToVector2(),
                IgnoreSize ? Vector2.One : Texture.GetScale(size),
                rotation,
                null,
                Color ?? RgbaByte.White);
        }
    }
}
