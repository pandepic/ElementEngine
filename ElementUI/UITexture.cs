using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UITexture
    {
        public Texture2D Texture;
        public Rectangle SourceRect;

        public int Width => SourceRect.Width;
        public int Height => SourceRect.Height;

        public UITexture(string assetName, Rectangle? sourceRect = null)
            : this(AssetManager.LoadTexture2D(assetName), sourceRect)
        {
        }

        public UITexture(Texture2D texture, Rectangle? sourceRect = null)
        {
            Texture = texture;
            SourceRect = sourceRect ?? new Rectangle(Vector2I.Zero, texture.Size);
        }

        public void Draw(SpriteBatch2D spriteBatch, Vector2 position, Vector2? scale = null, float rotation = 0f)
        {
            spriteBatch.DrawTexture2D(
                texture: Texture,
                position: position,
                sourceRect: SourceRect,
                scale: scale ?? new Vector2(1f),
                rotation: rotation);
        }

        public Vector2 GetScale(Vector2I? targetSize)
        {
            if (!targetSize.HasValue)
                return new Vector2(1f);

            return targetSize.Value.ToVector2() / Texture.SizeF;
        }

    } // UITexture
}
