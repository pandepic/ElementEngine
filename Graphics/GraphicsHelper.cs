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

        public static Texture2D Create3SliceTexture(int width, Texture2D atlas, Rectangle left, Rectangle center, Rectangle right, string name = null)
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
                spriteBatch.DrawTexture2D(atlas, new Vector2(0, 0), left);
            }

            if (right != null)
            {
                endX = width - right.Width;
                spriteBatch.DrawTexture2D(atlas, new Vector2(endX, 0), right);
            }

            while (currentX < endX)
            {
                var drawWidth = center.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(atlas, new Rectangle(currentX, 0, drawWidth, center.Height), center);
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

        public static Texture2D Create9SliceTexture(int width, int height, Texture2D atlas,
            Rectangle topLeft, Rectangle topCenter, Rectangle topRight,
            Rectangle middleLeft, Rectangle middleCenter, Rectangle middleRight,
            Rectangle bottomLeft, Rectangle bottomCenter, Rectangle bottomRight,
            string name = null)
        {
            var newTexture = new Texture2D(width, height, name);

            var topTexture = Create3SliceTexture(width, atlas, topLeft, topCenter, topRight);
            var middleTexture = Create3SliceTexture(width, atlas, middleLeft, middleCenter, middleRight);
            var bottomTexture = Create3SliceTexture(width, atlas, bottomLeft, bottomCenter, bottomRight);

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

        public static int GetPixelFormatBytesPerPixel(PixelFormat format)
        {
            return format switch
            {
                PixelFormat.R8_G8_B8_A8_UNorm => 4,
                PixelFormat.B8_G8_R8_A8_UNorm => 4,
                PixelFormat.R8_UNorm => 1,
                PixelFormat.R16_UNorm => 2,
                PixelFormat.R32_G32_B32_A32_Float => 16,
                PixelFormat.R32_Float => 4,
                PixelFormat.D24_UNorm_S8_UInt => 4,
                PixelFormat.D32_Float_S8_UInt => 5,
                PixelFormat.R32_G32_B32_A32_UInt => 16,
                PixelFormat.R8_G8_SNorm => 2,
                PixelFormat.R8_SNorm => 1,
                PixelFormat.R8_UInt => 1,
                PixelFormat.R8_SInt => 1,
                PixelFormat.R16_SNorm => 2,
                PixelFormat.R16_UInt => 2,
                PixelFormat.R16_SInt => 2,
                PixelFormat.R16_Float => 2,
                PixelFormat.R32_UInt => 4,
                PixelFormat.R32_SInt => 4,
                PixelFormat.R8_G8_UNorm => 2,
                PixelFormat.R8_G8_UInt => 2,
                PixelFormat.R8_G8_SInt => 2,
                PixelFormat.R16_G16_UNorm => 4,
                PixelFormat.R16_G16_SNorm => 4,
                PixelFormat.R16_G16_UInt => 4,
                PixelFormat.R16_G16_SInt => 4,
                PixelFormat.R16_G16_Float => 4,
                PixelFormat.R32_G32_UInt => 8,
                PixelFormat.R32_G32_SInt => 8,
                PixelFormat.R32_G32_Float => 8,
                PixelFormat.R8_G8_B8_A8_SNorm => 4,
                PixelFormat.R8_G8_B8_A8_UInt => 4,
                PixelFormat.R8_G8_B8_A8_SInt => 4,
                PixelFormat.R16_G16_B16_A16_UNorm => 8,
                PixelFormat.R16_G16_B16_A16_SNorm => 8,
                PixelFormat.R16_G16_B16_A16_UInt => 8,
                PixelFormat.R16_G16_B16_A16_SInt => 8,
                PixelFormat.R16_G16_B16_A16_Float => 8,
                PixelFormat.R32_G32_B32_A32_SInt => 16,
                PixelFormat.ETC2_R8_G8_B8_UNorm => 8,
                PixelFormat.ETC2_R8_G8_B8_A8_UNorm => 24,
                PixelFormat.R8_G8_B8_A8_UNorm_SRgb => 4,
                PixelFormat.B8_G8_R8_A8_UNorm_SRgb => 4,
                _ => throw new Exception("Bytes per pixel not covered for format " + format.ToString()),
            };
        } // GetPixelFormatBytesPerPixel

    } // GraphicsHelper
}
