using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public class Sprite
    {
        public Texture2D Texture { get; set; }
        public RgbaFloat Color { get; set; } = RgbaFloat.White;
        public SpriteFlipType Flip { get; set; } = SpriteFlipType.None;
        public Rectangle SourceRect;
        public Vector2 Scale = Vector2.One;
        public Vector2 Origin = Vector2.Zero;
        public float Rotation;

        public int Width { get => Texture.Width; }
        public int Height { get => Texture.Height; }

        public Sprite(Texture2D texture, bool centerOrigin = false)
        {
            Texture = texture;
            SourceRect = new Rectangle(0, 0, Width, Height);

            if (centerOrigin)
                Origin = new Vector2(Width / 2, Height / 2);
        }

        public virtual void Update(GameTimer gameTimer)
        {
        }

        public virtual void Draw(SpriteBatch2D spriteBatch, Vector2 position)
        {
            spriteBatch.DrawTexture2D(Texture, position, Color, SourceRect, Scale, Origin, Rotation, Flip);
        }
    }
}
