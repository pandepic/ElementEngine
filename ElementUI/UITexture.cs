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

        public UITexture(string assetName, Rectangle? sourceRect = null)
            : this(AssetManager.LoadTexture2D(assetName), sourceRect)
        {
        }

        public UITexture(Texture2D texture, Rectangle? sourceRect = null)
        {
            Texture = texture;

            if (sourceRect.HasValue)
                SourceRect = sourceRect.Value;
            else
                SourceRect = new Rectangle(Vector2I.Zero, texture.Size);
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

        public Vector2 GetScale(Vector2? targetSize)
        {
            if (!targetSize.HasValue)
                return new Vector2(1f);

            return targetSize.Value / Texture.SizeF;
        }

    } // UITexture
}
