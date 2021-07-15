using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Veldrid;

namespace ElementEngine
{
    internal class FontTexture : ITexture2D
    {
        public Texture2D Texture { get; set; }

        public FontTexture(int width, int height)
        {
            Texture = new Texture2D(width, height);
        }

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
        }

        public void SetData(System.Drawing.Rectangle bounds, byte[] data)
        {
            if (data.Length % 4 != 0)
                throw new Exception("Byte buffer has invalid format, must be 4 bytes per pixel in RGBA order.");

            Texture.SetData(data, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }
    } // FontTexture

    internal class FontTextureCreator : ITexture2DCreator
    {
        public ITexture2D Create(int width, int height)
        {
            return new FontTexture(width, height);
        }
    } // FontTextureCreator

    public class SpriteFont : IDisposable
    {
        internal static readonly int FontStashSize = 1024;
        internal static FontTextureCreator FontTextureCreator = new FontTextureCreator();

        public Dictionary<int, FontSystem> FontSystemsByOutlineSize { get; set; } = new Dictionary<int, FontSystem>();
        public byte[] FontData;

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
                    foreach (var system in FontSystemsByOutlineSize)
                        system.Value?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public SpriteFont(FileStream fs)
        {
            using var ms = new MemoryStream();

            fs.CopyTo(ms);
            FontData = ms.ToArray();
        }

        ~SpriteFont()
        {
            Dispose(false);
        }

        protected FontSystem GetFontSystem(int outlineSize = 0)
        {
            if (!FontSystemsByOutlineSize.ContainsKey(outlineSize))
            {
                var newSystem = new FontSystem(StbTrueTypeSharpFontLoader.Instance, FontTextureCreator, FontStashSize, FontStashSize, 0, outlineSize, false);
                newSystem.AddFont(FontData);
                FontSystemsByOutlineSize.Add(outlineSize, newSystem);
            }

            return FontSystemsByOutlineSize[outlineSize];

        } // GetFontSystem

        public void DrawText(SpriteBatch2D spriteBatch, string text, Vector2 position, RgbaByte color, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            var font = fontSystem.GetFont(size);
            font.DrawText(spriteBatch, position.X, position.Y, text, color.ToDrawingColor());
        } // DrawText

        public Vector2 MeasureText(string text, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            var font = fontSystem.GetFont(size);

            Bounds bounds = new Bounds();
            font.TextBounds(0, 0, text, ref bounds);
            
            return new Vector2(bounds.X2, bounds.Y2);
        } // MeasureText

        public Vector2 MeasureTextTrimmed(string text, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            var font = fontSystem.GetFont(size);

            Bounds bounds = new Bounds();
            font.TextBounds(0, 0, text, ref bounds);
            
            return new Vector2(bounds.X2 - bounds.X, bounds.Y2 - bounds.Y);
        } // MeasureText

        public Rectangle MeasureTextRect(string text, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            var font = fontSystem.GetFont(size);
            
            Bounds bounds = new Bounds();
            font.TextBounds(0, 0, text, ref bounds);
            
            return new Rectangle(bounds.X, bounds.Y, bounds.X2, bounds.Y2);
        } // MeasureText

    } // SpriteFont
}
