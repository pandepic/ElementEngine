using System;
using System.IO;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Veldrid;

namespace PandaEngine
{
    public class FontTexture : ITexture
    {
        public Texture2D Texture { get; set; }

        public FontTexture(int width, int height)
        {
            Texture = new Texture2D((uint)width, (uint)height);
        }

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
        }

        public void SetData(System.Drawing.Rectangle bounds, FssColor[] data)
        {
            Texture.SetData(bounds, data);
        }
    } // FontTexture

    public class FontTextureCreator : ITextureCreator
    {
        public ITexture Create(int width, int height)
        {
            return new FontTexture(width, height);
        }
    } // FontTextureCreator

    public class SpriteFont : IDisposable
    {
        internal static readonly int FontStashSize = 8000;
        internal static StbTrueTypeSharpFontLoader FontLoader = new StbTrueTypeSharpFontLoader();
        internal static FontTextureCreator FontTextureCreator = new FontTextureCreator();

        public FontSystem FontSystem = new FontSystem(FontLoader, FontTextureCreator, FontStashSize, FontStashSize);

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
                    FontSystem?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public SpriteFont(FileStream fs)
        {
            using var ms = new MemoryStream();
            fs.CopyTo(ms);

            FontSystem.AddFontMem(ms.ToArray());
        }

        public void DrawText(SpriteBatch2D spriteBatch, string text, Vector2 position, RgbaByte color, int size)
        {
            FontSystem.FontSize = size;
            var fss = new FssColor(color.R, color.G, color.B, color.A);
            FontSystem.DrawText(spriteBatch, position.X, position.Y, text, fss, 0f);
        }
    } // SpriteFont
}
