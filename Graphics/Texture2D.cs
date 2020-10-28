using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;

namespace PandaEngine
{
    public class Texture2D : IDisposable
    {
        protected Texture _texture;
        public Texture Data { get => _texture; }

        public TextureDescription Description { get; protected set; }

        public string AssetName { get; set; }

        public float TexelWidth { get => 1.0f / _texture.Width; }
        public float TexelHeight { get => 1.0f / _texture.Height; }

        public int Width { get => (int)_texture.Width; }
        public int Height { get => (int)_texture.Height; }
        public Point Size { get => new Point(Width, Height); }
        public Vector2 SizeF { get => Size.ToVector2(); }

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
                    _texture.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public Texture2D(Texture texture)
        {
            _texture = texture;
            Description = _texture.GetDescription();
        }

        public Texture2D(uint width, uint height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled)
        {
            _texture = PandaGlobals.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));

            if (name != null)
                _texture.Name = name;
        }

        public Texture2D(uint width, uint height, RgbaFloat color, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled)
        {
            unsafe
            {
                _texture = PandaGlobals.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));
                PandaGlobals.GraphicsDevice.UpdateTexture(_texture, (IntPtr)(&color), (uint)(sizeof(RgbaFloat) * (width * height)), 0, 0, 0, width, height, 1, 0, 0);
                
                Description = new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D);

                if (name != null)
                    _texture.Name = name;
            }
        }

        ~Texture2D()
        {
            Dispose(false);
        }

        public void Update<T>(ReadOnlySpan<T> data, uint? x = null, uint? y = null, uint? width = null, uint? height = null) where T : unmanaged
        {
            PandaGlobals.GraphicsDevice.UpdateTexture(_texture, data.ToArray(), x.GetValueOrDefault(), y.GetValueOrDefault(), 0u, width ?? _texture.Width, height ?? _texture.Height, 1u, 0u, 0u);
        }

        public void Update<T>(ReadOnlySpan<T> data, Rectangle destination) where T : unmanaged
        {
            PandaGlobals.GraphicsDevice.UpdateTexture(_texture, data.ToArray(), (uint)destination.X, (uint)destination.Y, 0u, (uint)destination.Width, (uint)destination.Height, 1u, 0u, 0u);
        }

    } // Texture2D
}
