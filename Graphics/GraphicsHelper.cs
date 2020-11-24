using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public static class GraphicsHelper
    {
        public static Texture2D Create3SliceTexture(int width, Texture2D left, Texture2D center, Texture2D right, string name = null)
        {
            var height = left.Height;
            if (center.Height > height)
                height = center.Height;
            if (right.Height > height)
                height = right.Height;

            var newTexture = new Texture2D(width, height, name);

            newTexture.BeginRenderTarget();
            newTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = newTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            var currentX = 0;
            var endX = width;

            if (left != null)
            {
                currentX += left.Width;
                spriteBatch.DrawTexture2D(left, new Vector2(0, 0));
            }

            if (right != null)
            {
                endX = width - right.Width;
                spriteBatch.DrawTexture2D(right, new Vector2(endX, 0));
            }

            while (currentX < endX)
            {
                var drawWidth = center.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(center, new Rectangle(currentX, 0, drawWidth, center.Height));
                currentX += center.Width;
            }

            spriteBatch.End();
            newTexture.EndRenderTarget();

            return newTexture;

        } // Create3SliceTexture

        public static Texture2D Create9SliceTexture(int width, int height,
            Texture2D topLeft, Texture2D topCenter, Texture2D topRight,
            Texture2D middleLeft, Texture2D middleCenter, Texture2D middleRight,
            Texture2D bottomLeft, Texture2D bottomCenter, Texture2D bottomRight,
            string name = null)
        {
            var newTexture = new Texture2D(width, height, name);

            var topTexture = Create3SliceTexture(width, topLeft, topCenter, topRight);
            var middleTexture = Create3SliceTexture(width, middleLeft, middleCenter, middleRight);
            var bottomTexture = Create3SliceTexture(width, bottomLeft, bottomCenter, bottomRight);

            var middleHeight = height - topTexture.Height - bottomTexture.Height;
            var middleDrawPos = new Vector2(0, topTexture.Height);

            newTexture.BeginRenderTarget();
            newTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = newTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            spriteBatch.DrawTexture2D(topTexture, Vector2.Zero);

            while (middleHeight > 0)
            {
                spriteBatch.DrawTexture2D(middleTexture, middleDrawPos, new Rectangle(0, 0, middleTexture.Width, middleHeight >= middleTexture.Height ? middleTexture.Height : middleHeight));
                middleDrawPos.Y += (middleHeight >= middleTexture.Height ? middleTexture.Height : middleHeight);
                middleHeight -= middleTexture.Height;
            }

            if (middleDrawPos.Y < height)
                spriteBatch.DrawTexture2D(bottomTexture, middleDrawPos);

            spriteBatch.End();
            newTexture.EndRenderTarget();

            topTexture.Dispose();
            middleTexture.Dispose();
            bottomTexture.Dispose();

            return newTexture;

        } // Create9SliceTexture

    } // GraphicsHelper
}
