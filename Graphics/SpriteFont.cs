using System;
using System.Collections.Generic;
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

    public static class FontExtensions
    {
        public static FssColor ToFssColor(this RgbaByte color, bool premultiply = false)
        {
            if (!premultiply)
                return new FssColor(color.R, color.G, color.B, color.A);
            else
                return FssColor.FromNonPremultiplied(color.R, color.G, color.B, color.A);
        }
    } // FontExtensions

    public class SpriteFont : IDisposable
    {
        internal static readonly int FontStashSize = 8000;
        internal static StbTrueTypeSharpFontLoader FontLoader = new StbTrueTypeSharpFontLoader();
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

        public FontSystem GetFontSystem(int outlineSize = 0)
        {
            if (!FontSystemsByOutlineSize.ContainsKey(outlineSize))
            {
                var newSystem = new FontSystem(FontLoader, FontTextureCreator, FontStashSize, FontStashSize, 0, outlineSize);
                newSystem.AddFontMem(FontData);
                FontSystemsByOutlineSize.Add(outlineSize, newSystem);
            }

            return FontSystemsByOutlineSize[outlineSize];

        } // GetFontSystem

        public void DrawText(SpriteBatch2D spriteBatch, string text, Vector2 position, RgbaByte color, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            fontSystem.FontSize = size;
            fontSystem.DrawText(spriteBatch, position.X, position.Y, text, color.ToFssColor(), 0f);
        } // DrawText

        public Vector2 MeasureText(string text, int size, int outlineSize = 0)
        {
            var fontSystem = GetFontSystem(outlineSize);
            fontSystem.FontSize = size;
            Bounds bounds = new Bounds();
            fontSystem.TextBounds(0, 0, text, ref bounds);
            return new Vector2(bounds.X2 - bounds.X, bounds.Y2 - bounds.Y);
        } // MeasureText

    } // SpriteFont
}
