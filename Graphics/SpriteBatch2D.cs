using FontStashSharp;
using FontStashSharp.Interfaces;
using SharpDX.Mathematics.Interop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;

namespace ElementEngine
{
    public enum SamplerType
    {
        Point,
        Linear,
        Aniso4x
    }

    public enum SpriteFlipType
    {
        None,
        Vertical,
        Horizontal,
        Both
    }

    public enum VertexTemplateType
    {
        TopLeft = 0,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class SpriteBatchItem : IPoolable
    {
        public Vertex2DPositionTexCoordsColor[] VertexData;
        public Texture2D Texture;

        public SpriteBatchItem()
        {
            VertexData = new Vertex2DPositionTexCoordsColor[SpriteBatch2D.VerticesPerQuad];
        }

        public bool IsAlive { get; set; }

        public void Reset()
        {
            Texture = null;
        }
    }

    public class SpriteBatch2D : IDisposable, IFontStashRenderer
    {
        public Sdl2Window Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;

        // Constants
        public const int IndicesPerQuad = 6;
        public const int VerticesPerQuad = 4;

        // Graphics resources
        protected Pipeline _pipeline;
        protected DeviceBuffer _vertexBuffer;
        protected DeviceBuffer _indexBuffer;
        protected DeviceBuffer _transformBuffer;
        protected ResourceLayout _transformLayout;
        protected ResourceSet _transformSet;
        protected ResourceLayout _textureLayout;

        protected Dictionary<string, ResourceSet> _textureSets;
        protected Sampler _sampler;

        // Shared static resources
        protected static bool _staticResLoaded = false;
        protected static Shader[] _shaders;
        protected static Vector2[] _vertexTemplate = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };

        // Rendering
        protected Matrix4x4 _projection;
        protected Matrix4x4 _view;

        // Batch state
        protected int _maxBatchSize = 10000;
        protected bool _begin = false;
        protected Vertex2DPositionTexCoordsColor[] _vertexData;
        protected int _currentBatchCount = 0;
        protected Texture2D _currentTexture = null;

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
                    _pipeline?.Dispose();
                    _vertexBuffer?.Dispose();
                    _indexBuffer?.Dispose();
                    _transformBuffer?.Dispose();
                    _transformLayout?.Dispose();
                    _transformSet?.Dispose();
                    _textureLayout?.Dispose();

                    foreach (var set in _textureSets)
                        set.Value?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public SpriteBatch2D() : this(ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight) { }
        public SpriteBatch2D(int width, int height, bool invertY = false) : this(width, height, ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription, invertY) { }
        public SpriteBatch2D(Texture2D target, bool invertY = false) : this(target.Width, target.Height, target.GetFramebuffer().OutputDescription, invertY) { }

        public unsafe SpriteBatch2D(int width, int height, OutputDescription output, bool invertY = false)
        {
            _textureSets = new Dictionary<string, ResourceSet>();

            var factory = GraphicsDevice.ResourceFactory;
            LoadStaticResources(factory);

            _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0f, 0f, 1f);

            if (invertY && !GraphicsDevice.IsUvOriginTopLeft)
                _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, 0f, height, 0f, 1f);

            _transformBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Matrix4x4) * 2), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, Matrix4x4.Identity);
            GraphicsDevice.UpdateBuffer(_transformBuffer, (uint)sizeof(Matrix4x4), Matrix4x4.Identity);

            _transformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(_transformLayout, _transformBuffer));

            _textureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("fTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("fTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _vertexData = new Vertex2DPositionTexCoordsColor[_maxBatchSize * VerticesPerQuad];

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(_vertexData.Length * sizeof(Vertex2DPositionTexCoordsColor)), BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_vertexData.Length * sizeof(Vertex2DPositionTexCoordsColor)));

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription
                {
                    DepthClipEnabled = true,
                    CullMode = FaceCullMode.None,
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { Vertex2DPositionTexCoordsColor.VertexLayout }, shaders: _shaders),
                ResourceLayouts = new ResourceLayout[]
                {
                    _transformLayout,
                    _textureLayout
                },
                Outputs = output
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            // if culling this is the right order for bottom left
            var indicesTemplate = new ushort[]
            {
                2, 1, 0, // tri 1
                2, 3, 1 // tri 2
            };

            /* if culling this is the right order for top left
            var indicesTemplate = new ushort[]
            {
                2, 1, 0, // tri 1
                2, 3, 1 // tri 2
            };
            */

            var indices = new ushort[_maxBatchSize * IndicesPerQuad];

            for (int i = 0; i < _maxBatchSize; i++)
            {
                var startIndex = i * IndicesPerQuad;
                var offset = i * VerticesPerQuad;

                indices[startIndex + 0] = (ushort)(indicesTemplate[0] + offset);
                indices[startIndex + 1] = (ushort)(indicesTemplate[1] + offset);
                indices[startIndex + 2] = (ushort)(indicesTemplate[2] + offset);

                indices[startIndex + 3] = (ushort)(indicesTemplate[3] + offset);
                indices[startIndex + 4] = (ushort)(indicesTemplate[4] + offset);
                indices[startIndex + 5] = (ushort)(indicesTemplate[5] + offset);
            }

            _indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, ref indices[0], (uint)(indices.Length * sizeof(ushort)));

        } // SpriteBatch2D

        ~SpriteBatch2D()
        {
            Dispose(false);
        }

        public static void LoadStaticResources(ResourceFactory factory)
        {
            if (_staticResLoaded)
                return;

            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultSpriteVS), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(DefaultShaders.DefaultSpriteFS), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));
            _staticResLoaded = true;
        }

        public static void CleanupStaticResources()
        {
            if (!_staticResLoaded)
                return;

            for (var i = 0; i < _shaders.Length; i++)
                _shaders[i]?.Dispose();
        }

        public void Begin(SamplerType samplerType, Matrix4x4? view = null)
        {
            switch (samplerType)
            {
                case SamplerType.Point:
                    Begin(GraphicsDevice.PointSampler, view);
                    break;

                case SamplerType.Linear:
                    Begin(GraphicsDevice.LinearSampler, view);
                    break;

                case SamplerType.Aniso4x:
                    Begin(GraphicsDevice.Aniso4xSampler, view);
                    break;

                default:
                    throw new ArgumentException("Unknown value", "samplerType");
            }
        }

        public unsafe void Begin(Sampler sampler, Matrix4x4? view = null)
        {
            if (_begin)
                throw new Exception("You must end the current batch before starting a new one.");

            if (view == null)
                _view = Matrix4x4.Identity;
            else
                _view = view.Value;

            _sampler = sampler;
            _begin = true;

            CommandList.UpdateBuffer(_transformBuffer, 0, _projection);
            CommandList.UpdateBuffer(_transformBuffer, (uint)sizeof(Matrix4x4), _view);
        }

        public void DrawText(SpriteFont font, string text, Vector2 position, RgbaByte color, int size, int outlineSize = 0)
        {
            font.DrawText(this, text, position, color, size, outlineSize);
        }

        void IFontStashRenderer.Draw(ITexture2D texture, System.Drawing.Rectangle dest, System.Drawing.Rectangle source, System.Drawing.Color color, float depth)
        {
            var colorF = new RgbaFloat(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            DrawTexture2D((texture as FontTexture).Texture, new Vector2(dest.X, dest.Y), new Rectangle(source.X, source.Y, source.Width, source.Height), null, null, 0f, colorF);
        }

        public void DrawSprite(Sprite sprite, Vector2 position)
        {
            sprite.Draw(this, position);
        }

        public void DrawTexture2D(Texture2D texture, Rectangle destination, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0f, RgbaFloat? color = null, SpriteFlipType flip = SpriteFlipType.None)
        {
            if (!scale.HasValue)
                scale = Vector2.One;
            if (!sourceRect.HasValue)
                sourceRect = new Rectangle(0, 0, (int)texture.Width, (int)texture.Height);

            if (destination.Width > 0 && destination.Width != sourceRect.Value.Width)
                scale = new Vector2(((float)destination.Width / (float)sourceRect.Value.Width) * scale.Value.X, scale.Value.Y);
            if (destination.Height > 0 && destination.Height != sourceRect.Value.Height)
                scale = new Vector2(scale.Value.X, ((float)destination.Height / (float)sourceRect.Value.Height) * scale.Value.Y);

            DrawTexture2D(texture, new Vector2(destination.X, destination.Y), sourceRect, scale, origin, rotation, color, flip);
        }

        public void DrawTexture2D(Texture2D texture, Vector2 position, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0f, RgbaFloat? color = null, SpriteFlipType flip = SpriteFlipType.None)
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call Draw.");

            if (!color.HasValue)
                color = RgbaFloat.White;
            if (!sourceRect.HasValue)
                sourceRect = new Rectangle(0, 0, (int)texture.Width, (int)texture.Height);
            if (!origin.HasValue)
                origin = new Vector2(0f, 0f);
            if (!scale.HasValue)
                scale = new Vector2(1f, 1f);

            var spriteScale = new Vector2(sourceRect.Value.Width, sourceRect.Value.Height) * scale.Value;
            var spriteOrigin = origin.Value * scale.Value;
            var texelWidth = texture.TexelWidth;
            var texelHeight = texture.TexelHeight;
            var flipX = (flip == SpriteFlipType.Horizontal || flip == SpriteFlipType.Both);
            var flipY = (flip == SpriteFlipType.Vertical || flip == SpriteFlipType.Both);
            var source = sourceRect.Value;

            var sin = 0.0f;
            var cos = 0.0f;
            var nOriginX = -spriteOrigin.X;
            var nOriginY = -spriteOrigin.Y;

            if (rotation != 0.0f)
            {
                var radians = rotation.ToRadians();
                sin = MathF.Sin(radians);
                cos = MathF.Cos(radians);
            }

            var topLeft = new Vertex2DPositionTexCoordsColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                position.X - spriteOrigin.X,
                                position.Y - spriteOrigin.Y)
                            : new Vector2(
                                position.X + nOriginX * cos - nOriginY * sin,
                                position.Y + nOriginX * sin + nOriginY * cos),
                TexCoords = new Vector2(
                            flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                            flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                Color = color.Value
            };

            var x = _vertexTemplate[(int)VertexTemplateType.TopRight].X;
            var w = spriteScale.X * x;

            var topRight = new Vertex2DPositionTexCoordsColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (position.X - spriteOrigin.X) + w,
                                position.Y - spriteOrigin.Y)
                            : new Vector2(
                                position.X + (nOriginX + w) * cos - nOriginY * sin,
                                position.Y + (nOriginX + w) * sin + nOriginY * cos),
                TexCoords = new Vector2(
                            flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                            flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                Color = color.Value
            };

            var y = _vertexTemplate[(int)VertexTemplateType.BottomLeft].Y;
            var h = spriteScale.Y * y;

            var bottomLeft = new Vertex2DPositionTexCoordsColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (position.X - spriteOrigin.X),
                                (position.Y - spriteOrigin.Y) + h)
                            : new Vector2(
                                position.X + nOriginX * cos - (nOriginY + h) * sin,
                                position.Y + nOriginX * sin + (nOriginY + h) * cos),
                TexCoords = new Vector2(
                            flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                            flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                Color = color.Value
            };

            x = _vertexTemplate[(int)VertexTemplateType.BottomRight].X;
            y = _vertexTemplate[(int)VertexTemplateType.BottomRight].Y;
            w = spriteScale.X * x;
            h = spriteScale.Y * y;

            var bottomRight = new Vertex2DPositionTexCoordsColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (position.X - spriteOrigin.X) + w,
                                (position.Y - spriteOrigin.Y) + h)
                            : new Vector2(
                                position.X + (nOriginX + w) * cos - (nOriginY + h) * sin,
                                position.Y + (nOriginX + w) * sin + (nOriginY + h) * cos),
                TexCoords = new Vector2(
                            flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                            flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                Color = color.Value
            };

            AddQuad(texture, topLeft, topRight, bottomLeft, bottomRight);

        } // DrawTexture2D

        public void DrawTexture2D(Texture2D texture, Matrix3x2 worldMatrix, Rectangle? sourceRect = null, RgbaFloat? color = null, SpriteFlipType flip = SpriteFlipType.None)
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call Draw.");

            if (!color.HasValue)
                color = RgbaFloat.White;
            if (!sourceRect.HasValue)
                sourceRect = new Rectangle(0, 0, (int)texture.Width, (int)texture.Height);

            var texelWidth = texture.TexelWidth;
            var texelHeight = texture.TexelHeight;
            var source = sourceRect.Value;
            var flipX = (flip == SpriteFlipType.Horizontal || flip == SpriteFlipType.Both);
            var flipY = (flip == SpriteFlipType.Vertical || flip == SpriteFlipType.Both);

            AddQuad(
                texture,
                new Vertex2DPositionTexCoordsColor() // top left
                {
                    Position = Vector2.Transform(new Vector2(0f, 0f), worldMatrix),
                    TexCoords = new Vector2(
                        flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                        flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                    Color = color.Value
                },
                new Vertex2DPositionTexCoordsColor() // top right
                {
                    Position = Vector2.Transform(new Vector2(1f, 0f), worldMatrix),
                    TexCoords = new Vector2(
                        flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                        flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                    Color = color.Value
                },
                new Vertex2DPositionTexCoordsColor() // bottom left
                {
                    Position = Vector2.Transform(new Vector2(0f, 1f), worldMatrix),
                    TexCoords = new Vector2(
                        flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                        flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                    Color = color.Value
                },
                new Vertex2DPositionTexCoordsColor() // bottom right
                {
                    Position = Vector2.Transform(new Vector2(1f, 1f), worldMatrix),
                    TexCoords = new Vector2(
                        flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                        flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                    Color = color.Value
                }
            );
        } // DrawTexture2D

        protected void AddQuad(Texture2D texture, Vertex2DPositionTexCoordsColor topLeft, Vertex2DPositionTexCoordsColor topRight, Vertex2DPositionTexCoordsColor bottomLeft, Vertex2DPositionTexCoordsColor bottomRight)
        {
            if (_currentTexture == null || _currentTexture != texture)
            {
                if (_currentBatchCount > 0 && _currentTexture != null)
                    Flush(_currentTexture);

                _currentTexture = texture;
            }

            if (_currentBatchCount >= (_maxBatchSize - 1))
                Flush(_currentTexture);

            var index = _currentBatchCount * VerticesPerQuad;

            _vertexData[index] = topLeft;
            _vertexData[index + 1] = topRight;
            _vertexData[index + 2] = bottomLeft;
            _vertexData[index + 3] = bottomRight;

            _currentBatchCount += 1;
        }

        public void End()
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call End.");

            Flush(_currentTexture);
            _begin = false;
        }

        public unsafe void Flush(Texture2D texture)
        {
            if (_currentBatchCount == 0)
                return;

            CommandList.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_currentBatchCount * VerticesPerQuad * sizeof(Vertex2DPositionTexCoordsColor)));
            CommandList.SetVertexBuffer(0, _vertexBuffer);
            CommandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            CommandList.SetPipeline(_pipeline);
            CommandList.SetGraphicsResourceSet(0, _transformSet);

            if (_textureSets.TryGetValue(texture.AssetName, out ResourceSet textureSet))
            {
                CommandList.SetGraphicsResourceSet(1, textureSet);
            }
            else
            {
                var textureSetDescription = new ResourceSetDescription(_textureLayout, texture.Texture, _sampler);
                var newTextureSet = GraphicsDevice.ResourceFactory.CreateResourceSet(textureSetDescription);
                _textureSets.Add(texture.AssetName, newTextureSet);
                CommandList.SetGraphicsResourceSet(1, newTextureSet);
            }

            CommandList.DrawIndexed((uint)(_currentBatchCount * IndicesPerQuad));
            _currentBatchCount = 0;
        }
    }
}
