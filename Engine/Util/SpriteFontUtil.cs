using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine
{
    public static class SpriteFontUtil
    {
        public static void DrawCenteredText(SpriteBatch2D spriteBatch, SpriteFont font, string text, Rectangle centerOn, RgbaByte color, int size, int outlineSize = 0)
        {
            DrawCenteredText(spriteBatch, font, text, centerOn.Center, color, size, outlineSize);
        }

        public static void DrawCenteredText(SpriteBatch2D spriteBatch, SpriteFont font, string text, Vector2I centerOn, RgbaByte color, int size, int outlineSize = 0)
        {
            DrawCenteredText(spriteBatch, font, text, centerOn.ToVector2(), color, size, outlineSize);
        }

        public static void DrawCenteredText(SpriteBatch2D spriteBatch, SpriteFont font, string text, Vector2 centerOn, RgbaByte color, int size, int outlineSize = 0)
        {
            var textSize = font.MeasureText(text, size, outlineSize);
            var pos = (centerOn - (textSize / 2f)).ToVector2I();

            spriteBatch.DrawText(font, text, pos.ToVector2(), color, size, outlineSize);
        }
    }
}
