using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class Sprite : IDisposable
    {
        public Texture2D Texture { get; set; }
        public RgbaByte Color { get; set; } = RgbaByte.White;
        public SpriteFlipType Flip { get; set; } = SpriteFlipType.None;
        public Rectangle SourceRect;
        public Vector2 Scale = Vector2.One;
        public Vector2 Origin = Vector2.Zero;
        public float Rotation;

        public virtual int Width { get => Texture.Width; }
        public virtual int Height { get => Texture.Height; }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Texture?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public Sprite() { }

        public Sprite(Texture2D texture, bool centerOrigin = false)
        {
            InitSprite(texture, centerOrigin);
        }

        ~Sprite()
        {
            Dispose(false);
        }

        protected void InitSprite(Texture2D texture, bool centerOrigin = false)
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
            spriteBatch.DrawTexture2D(Texture, position, SourceRect, Scale, Origin, Rotation, Color, Flip);
        }
    }
}
