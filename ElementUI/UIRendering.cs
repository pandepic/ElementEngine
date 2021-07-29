using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public static class UIRendering
    {
        public static void Render3SliceHorizontal(SpriteBatch2D spriteBatch, Vector2I position, Vector2I size, UITexture left, UITexture right, UITexture center)
        {
            var width = (int)size.X;
            var height = MathHelper.Max(left.Height, right.Height, center.Height);

            var currentX = 0;
            var endX = width;

            if (left != null)
            {
                currentX += left.Width;
                spriteBatch.DrawTexture2D(left.Texture, (new Vector2I(0, 0) + position).ToVector2(), left.SourceRect);
            }

            if (right != null)
            {
                endX = width - right.Width;
                spriteBatch.DrawTexture2D(right.Texture, (new Vector2I(endX, 0) + position).ToVector2(), right.SourceRect);
            }

            while (currentX < endX)
            {
                var drawWidth = center.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(center.Texture, new Rectangle(currentX + position.X, 0 + position.Y, drawWidth, center.Height), center.SourceRect);
                currentX += center.Width;
            }
        } // Render3SliceHorizontal

        public static void Render3SliceVertical(SpriteBatch2D spriteBatch, Vector2I position, Vector2I size, UITexture top, UITexture bottom, UITexture center)
        {
            var width = MathHelper.Max(top.Width, bottom.Width, center.Width);
            var height = (int)size.Y;

            var currentY = 0;
            var endY = height;

            if (top != null)
            {
                currentY += top.Height;
                spriteBatch.DrawTexture2D(top.Texture, (new Vector2I(0, 0) + position).ToVector2(), top.SourceRect);
            }

            if (bottom != null)
            {
                endY = height - bottom.Height;
                spriteBatch.DrawTexture2D(bottom.Texture, (new Vector2I(0, endY) + position).ToVector2(), bottom.SourceRect);
            }

            while (currentY < endY)
            {
                var drawHeight = center.Height;

                if ((currentY + drawHeight) > endY)
                    drawHeight = endY - currentY;

                spriteBatch.DrawTexture2D(center.Texture, new Rectangle(0 + position.X, currentY + position.Y, center.Width, drawHeight), center.SourceRect);
                currentY += center.Height;
            }
        } // Render3SliceVertical

        public static void Render9Slice(SpriteBatch2D spriteBatch, Vector2I position, Vector2I size,
            UITexture topLeft, UITexture topRight, UITexture topCenter,
            UITexture middleLeft, UITexture middleRight, UITexture middleCenter,
            UITexture bottomLeft, UITexture bottomRight, UITexture bottomCenter)
        {
            var topTextureHeight = MathHelper.Max(topLeft.Height, topRight.Height, topCenter.Height);
            var middleTextureHeight = MathHelper.Max(middleLeft.Height, middleRight.Height, middleCenter.Height);
            var bottomTextureHeight = MathHelper.Max(bottomLeft.Height, bottomRight.Height, bottomCenter.Height);

            var middleHeight = (int)(size.Y - topTextureHeight - bottomTextureHeight);
            var middleDrawPos = new Vector2I(0, topTextureHeight);

            Render3SliceHorizontal(spriteBatch, position, size, topLeft, topRight, topCenter);

            while (middleHeight > 0)
            {
                //spriteBatch.DrawTexture2D(middleTexture, middleDrawPos, new Rectangle(0, 0, middleTexture.Width, middleHeight >= middleTexture.Height ? middleTexture.Height : middleHeight));
                spriteBatch.PushScissorRect(0, new Rectangle(position + middleDrawPos, new Vector2I((int)size.X, middleHeight >= middleTextureHeight ? middleTextureHeight : middleHeight)), true);
                Render3SliceHorizontal(spriteBatch, position + middleDrawPos, size, middleLeft, middleRight, middleCenter);
                spriteBatch.PopScissorRect(0);
                middleDrawPos.Y += (middleHeight >= middleTextureHeight ? middleTextureHeight : middleHeight);
                middleHeight -= middleTextureHeight;
            }

            if (middleDrawPos.Y < size.Y)
                Render3SliceHorizontal(spriteBatch, position + middleDrawPos, size, bottomLeft, bottomRight, bottomCenter);

        } // Render9Slice

    } // UIRendering
}
