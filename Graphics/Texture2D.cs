using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace ElementEngine
{
    public class Texture2D : IDisposable
    {
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;

        protected Texture _texture;
        public Texture Texture { get => _texture; }

        public TextureDescription Description { get; protected set; }

        public string TextureName { get; set; }
        public string AssetName { get; set; }

        public float TexelWidth => 1.0f / _texture.Width;
        public float TexelHeight => 1.0f / _texture.Height;
        public Vector2 TexelSize => new Vector2(TexelWidth, TexelHeight);

        public int Width { get => (int)_texture.Width; }
        public int Height { get => (int)_texture.Height; }
        public Vector2I Size { get => new Vector2I(Width, Height); }
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

        public Texture2D(Texture texture, string name = null)
        {
            _texture = texture;
            Description = _texture.GetDescription();
            SetName(name);
        }

        public Texture2D(int width, int height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this((uint)width, (uint)height, name, format, usage) { }

        public Texture2D(uint width, uint height, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
        {
            _texture = GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(width, height, 1, 1, 1, format, usage, TextureType.Texture2D));
            Description = _texture.GetDescription();
            SetName(name);
        }

        public unsafe Texture2D(uint width, uint height, RgbaByte color, string name = null, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage usage = TextureUsage.Sampled | TextureUsage.RenderTarget)
            : this(width, height, name, format, usage)
        {
            var data = color.ToBuffer((int)(width * height));
            GraphicsDevice.UpdateTexture(_texture, data, 0, 0, 0, width, height, 1, 0, 0);
        }

        ~Texture2D()
        {
            Dispose(false);
        }

        private void SetName(string name = null)
        {
            if (name == null)
                name = Guid.NewGuid().ToString();

            _texture.Name = name;
            TextureName = name;
            AssetName = name;
        } // SetName

        public void SetData<T>(ReadOnlySpan<T> data, Rectangle? area = null, TexturePremultiplyType premultiplyType = TexturePremultiplyType.None) where T : unmanaged
        {
            SetData(data.ToArray(), area, premultiplyType);
        }

        /// <summary>
        /// Specifically used by FontStashSharp for font atlas rendering.
        /// </summary>
        public void SetData(System.Drawing.Rectangle bounds, byte[] byteData)
        {
            SetData(byteData.ToRgbaByte(), new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }

        public void SetData<T>(T[] data, Rectangle? area = null, TexturePremultiplyType premultiplyType = TexturePremultiplyType.None) where T : unmanaged
        {
            Rectangle rect = area ?? new Rectangle(0, 0, Width, Height);
            GraphicsDevice.UpdateTexture(Texture, data, (uint)rect.X, (uint)rect.Y, 0, (uint)rect.Width, (uint)rect.Height, 1, 0, 0);
        }

        public unsafe byte[] GetData()
        {
            var view = GraphicsDevice.Map<byte>(Texture, MapMode.Read);

            var data = new byte[view.SizeInBytes];
            Marshal.Copy(view.MappedResource.Data, data, 0, (int)view.SizeInBytes);

            GraphicsDevice.Unmap(Texture);

            return data;

        } // GetData

        public void ApplyPremultiply(TexturePremultiplyType type)
        {
            if (type == TexturePremultiplyType.None)
                return;

            var data = GetData().ToRgbaByte();

            for (var i = 0; i < data.Length; i++)
            {
                var color = data[i];
                float ratio = color.A / 255f;

                if (type == TexturePremultiplyType.Premultiply)
                    data[i] = new RgbaByte((byte)(color.R * ratio), (byte)(color.G * ratio), (byte)(color.B * ratio), color.A);
                else if (type == TexturePremultiplyType.UnPremultiply)
                    data[i] = new RgbaByte((byte)(color.R / ratio), (byte)(color.G / ratio), (byte)(color.B / ratio), color.A);
            }

            SetData(data);
        } // ApplyPremultiply

        public void SaveAsPng(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.OpenOrCreate);
            SaveAsPng(fs);
        } // SaveAsPng

        public void SaveAsPng(FileStream fs)
        {
            var temp = new Texture2D(Width, Height, null, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging);

            var commandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            commandList.Begin();
            commandList.CopyTexture(Texture, temp.Texture);
            commandList.End();
            GraphicsDevice.SubmitCommands(commandList);
            GraphicsDevice.WaitForIdle();

            var tempData = temp.GetData();
            var data = tempData.ToRgba32();

            var image = Image.LoadPixelData(data, Width, Height);
            image.SaveAsPng(fs);

            temp.Dispose();

        } // SaveAsPng

        #region Render target methods
        public Framebuffer GetFramebuffer()
        {
            if (_framebuffer == null)
            {
                _framebuffer = ElementGlobals.GraphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription()
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
                commandList = ElementGlobals.CommandList;

            commandList.SetFramebuffer(GetFramebuffer());
            commandList.SetViewport(0, new Viewport(0, 0, Width, Height, 0f, 1f));

        } // BeginRenderTarget

        public void EndRenderTarget(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            ElementGlobals.ResetFramebuffer(commandList);
            ElementGlobals.ResetViewport(commandList);
        }

        public void RenderTargetClear(RgbaFloat color, CommandList commandList = null)
        {
            if (commandList == null)
                commandList = ElementGlobals.CommandList;

            commandList.ClearColorTarget(0, color);

        } // RenderTargetClear

        public SpriteBatch2D GetRenderTargetSpriteBatch2D()
        {
            if (_renderTargetSpriteBatch2D == null)
                _renderTargetSpriteBatch2D = new SpriteBatch2D(this, true);

            return _renderTargetSpriteBatch2D;
        } // GetRenderTargetSpriteBatch2D
        #endregion

    } // Texture2D
}
