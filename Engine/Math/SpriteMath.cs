﻿namespace ElementEngine
{
    public static class SpriteMath
    {
        public static Rectangle GetFrameRect(Texture2D texture, Vector2I frameSize, int frame, bool zeroBased = false)
        {
            if (!zeroBased)
                frame -= 1;

            return new Rectangle()
            {
                X = frame % (texture.Width / frameSize.X) * frameSize.X,
                Y = frame / (texture.Width / frameSize.X) * frameSize.Y,
                Width = frameSize.X,
                Height = frameSize.Y,
            };
        }

        public static Vector2I PixelIndexToPosition(Texture2D texture, int index)
        {
            return new Vector2I(
                index % texture.Width,
                index / texture.Width);
        }
    }
}