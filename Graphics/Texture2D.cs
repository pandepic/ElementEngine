using FontStashSharp;
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
        public GraphicsDevice GraphicsDevice => PandaGlobals.GraphicsDevice;

        protected Texture _texture;
        public Texture Texture { get => _texture; }

        public TextureDescription Description { get; protected set; }

        public string TextureName { get; set; }
        public string AssetName { get; set; }

        public float TexelWidth { get => 1.0f / _texture.Width; }
        public float TexelHeight { get => 1.0f / _texture.Height; }

        public int Width { get => (int)_texture.Width; }
        public int Height { get => (int)_texture.Height; }
        public Vector2i Size { get => new Vector2i(Width, Height); }
        public Vector2 SizeF { get => Size.ToVector2(); }

        protected Framebuffer _framebuffer = null;
        protected SpriteBatch2D _renderTargetSpriteBatch2D = null;

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
                    _texture?.Dispose();
                    _framebuffer?.Dispose();
                    _renderTargetSpriteBatch2D?.Dispose();
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

        public Texture2D(uint width, uint height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
        {
            _texture = GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));

            if (name != null)
            {
                _texture.Name = name;
                TextureName = name;
            }

            AssetName = Guid.NewGuid().ToString();
        }

        public unsafe Texture2D(uint width, uint height, RgbaByte color, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
        {
            var data = new RgbaByte[width * height];
            for (var i = 0; i < data.Length; i++)
                data[i] = color;

            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);

            _texture = PandaGlobals.GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));
            GraphicsDevice.UpdateTexture(_texture, pinnedArray.AddrOfPinnedObject(), (uint)(sizeof(RgbaByte) * (width * height)), 0, 0, 0, width, height, 1, 0, 0);

            pinnedArray.Free();

            Description = new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D);

            if (name != null)
            {
                _texture.Name = name;
                TextureName = name;
            }

            AssetName = Guid.NewGuid().ToString();
        }

        ~Texture2D()
        {
            Dispose(false);
        }

        /// <summary>
        /// Specifically used by FontStashSharp for font atlas rendering.
        /// </summary>
        public void SetData(System.Drawing.Rectangle bounds, byte[] byteData)
        {
            var data = new RgbaByte[bounds.Width * bounds.Height];
            
            // convert flat byte array to RGBA with a pixel every 4 bytes   
            for (var i = 0; i < data.Length; i++)
                data[i] = new RgbaByte(byteData[(i * 4)], byteData[(i * 4) + 1], byteData[(i * 4) + 2], byteData[(i * 4) + 3]);
            
            GraphicsDevice.UpdateTexture(Texture, data, (uint)bounds.X, (uint)bounds.Y, 0, (uint)bounds.Width, (uint)bounds.Height, 1, 0, 0);

        } // SetData

        public void SetData<T>(Rectangle? area, T[] data) where T : unmanaged
        {
            Rectangle rect;

            if (area.HasValue)
                rect = area.Value;
            else
                rect = new Rectangle(0, 0, Width, Height);

            GraphicsDevice.UpdateTexture(Texture, data, (uint)rect.X, (uint)rect.Y, 0, (uint)rect.Width, (uint)rect.Height, 1, 0, 0);
        } // SetData

        public Framebuffer GetFramebuffer()
        {
            if (_framebuffer == null)
            {
                _framebuffer = PandaGlobals.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
                {
                    ColorTargets = new FramebufferAttachmentDescription[]
                    {
                        new FramebufferAttachmentDescription(Texture, 0),
                    },
                });
            }

            return _framebuffer;

        } // GetFramebuffer

        public void BeginRenderTarget(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = PandaGlobals.CommandList;

            commandList.SetFramebuffer(GetFramebuffer());
            commandList.SetViewport(0, new Viewport(0, 0, Width, Height, 0f, 1f));

        } // BeginRenderTarget

        public void EndRenderTarget(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = PandaGlobals.CommandList;

            PandaGlobals.ResetFramebuffer(commandList);
            PandaGlobals.ResetViewport(commandList);
        }

        public void RenderTargetClear(RgbaFloat color, CommandList commandList = null)
        {
            if (commandList == null)
                commandList = PandaGlobals.CommandList;

            commandList.ClearColorTarget(0, color);

        } // RenderTargetClear

        public SpriteBatch2D GetRenderTargetSpriteBatch2D()
        {
            if (_renderTargetSpriteBatch2D == null)
                _renderTargetSpriteBatch2D = new SpriteBatch2D(this);

            return _renderTargetSpriteBatch2D;
        } // GetRenderTargetSpriteBatch2D

    } // Texture2D
}
