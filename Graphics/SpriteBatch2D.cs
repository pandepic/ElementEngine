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

namespace PandaEngine
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpriteBatch2DVertex
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaFloat Color;
    }

    public struct SpriteBatchItem
    {
        public SpriteBatch2DVertex[] VertexData;
        public Texture2D Texture;
    }

    public class SpriteBatch2D : IDisposable
    {
        public Sdl2Window Window => PandaGlobals.Window;
        public GraphicsDevice GraphicsDevice => PandaGlobals.GraphicsDevice;
        public CommandList CommandList => PandaGlobals.CommandList;

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
        protected ResourceSetDescription _textureSetDescription;

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
        protected int _maxBatchSize = 1000;
        protected bool _begin = false;
        protected List<SpriteBatchItem> _batchItems;
        protected SpriteBatch2DVertex[] _vertexData;

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

        public SpriteBatch2D() : this(PandaGlobals.Window.Width, PandaGlobals.Window.Height)
        {
        }

        public SpriteBatch2D(int width, int height) : this(width, height, PandaGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription)
        {
        }

        public unsafe SpriteBatch2D(int width, int height, OutputDescription output)
        {
            _batchItems = new List<SpriteBatchItem>();
            _textureSets = new Dictionary<string, ResourceSet>();

            var factory = GraphicsDevice.ResourceFactory;
            LoadStaticResources(factory);

            _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, 0f, height, 0f, 1f);

            _transformBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Matrix4x4) * 2), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, Matrix4x4.Identity);
            GraphicsDevice.UpdateBuffer(_transformBuffer, (uint)sizeof(Matrix4x4), Matrix4x4.Identity);

            _transformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("mProjectionViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(_transformLayout, _transformBuffer));
            
            _textureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("fTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("fTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _vertexData = new SpriteBatch2DVertex[_maxBatchSize * VerticesPerQuad];

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(_vertexData.Length * sizeof(SpriteBatch2DVertex)), BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_vertexData.Length * sizeof(SpriteBatch2DVertex)));

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription
                {
                    DepthClipEnabled = false,
                    CullMode = FaceCullMode.None,
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { vertexLayout }, shaders: _shaders),
                ResourceLayouts = new ResourceLayout[2]
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

            ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultSpriteVS), "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(DefaultShaders.DefaultSpriteFS), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: true));

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

        public void Begin(Sampler sampler, Matrix4x4? view = null)
        {
            if (_begin)
                throw new Exception("You must end the current batch before starting a new one.");

            if (view == null)
                _view = Matrix4x4.Identity;
            else
                _view = view.Value;

            _sampler = sampler;
            _begin = true;
        }

        public void Draw(Texture2D texture, Vector2 position, RgbaFloat? color = null, Rectangle? sourceRect = null, Vector2? scale = null, Vector2? origin = null, float rotation = 0f, SpriteFlipType flip = SpriteFlipType.None)
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

            var batchItem = new SpriteBatchItem
            {
                Texture = texture,
                VertexData = new SpriteBatch2DVertex[4],
            };

            for (var i = 0; i < _vertexTemplate.Length; i++)
            {
                var x = _vertexTemplate[i].X;
                var y = _vertexTemplate[i].Y;
                var w = spriteScale.X * x;
                var h = spriteScale.Y * y;

                batchItem.VertexData[i].Color = color.Value;

                switch ((VertexTemplateType)i)
                {
                    case VertexTemplateType.TopLeft:
                        {
                            if (flipX)
                                batchItem.VertexData[i].TexCoords.X = (source.X + source.Width) * texelWidth;
                            else
                                batchItem.VertexData[i].TexCoords.X = source.X * texelWidth;

                            if (flipY)
                                batchItem.VertexData[i].TexCoords.Y = (source.Y + source.Height) * texelHeight;
                            else
                                batchItem.VertexData[i].TexCoords.Y = source.Y * texelHeight;

                            if (rotation == 0.0f)
                            {
                                batchItem.VertexData[i].Position.X = (position.X - spriteOrigin.X);
                                batchItem.VertexData[i].Position.Y = (position.Y - spriteOrigin.Y);
                            }
                            else
                            {
                                batchItem.VertexData[i].Position.X = position.X + nOriginX * cos - nOriginY * sin;
                                batchItem.VertexData[i].Position.Y = position.Y + nOriginX * sin + nOriginY * cos;
                            }
                        }
                        break;

                    case VertexTemplateType.TopRight:
                        {
                            if (flipX)
                                batchItem.VertexData[i].TexCoords.X = source.X * texelWidth;
                            else
                                batchItem.VertexData[i].TexCoords.X = (source.X + source.Width) * texelWidth;

                            if (flipY)
                                batchItem.VertexData[i].TexCoords.Y = (source.Y + source.Height) * texelHeight;
                            else
                                batchItem.VertexData[i].TexCoords.Y = source.Y * texelHeight;

                            if (rotation == 0.0f)
                            {
                                batchItem.VertexData[i].Position.X = (position.X - spriteOrigin.X) + w;
                                batchItem.VertexData[i].Position.Y = (position.Y - spriteOrigin.Y);
                            }
                            else
                            {
                                batchItem.VertexData[i].Position.X = position.X + (nOriginX + w) * cos - nOriginY * sin;
                                batchItem.VertexData[i].Position.Y = position.Y + (nOriginX + w) * sin + nOriginY * cos;
                            }
                        }
                        break;

                    case VertexTemplateType.BottomLeft:
                        {
                            if (flipX)
                                batchItem.VertexData[i].TexCoords.X = (source.X + source.Width) * texelWidth;
                            else
                                batchItem.VertexData[i].TexCoords.X = source.X * texelWidth;

                            if (flipY)
                                batchItem.VertexData[i].TexCoords.Y = source.Y * texelHeight;
                            else
                                batchItem.VertexData[i].TexCoords.Y = (source.Y + source.Height) * texelHeight;

                            if (rotation == 0.0f)
                            {
                                batchItem.VertexData[i].Position.X = (position.X - spriteOrigin.X);
                                batchItem.VertexData[i].Position.Y = (position.Y - spriteOrigin.Y) + h;
                            }
                            else
                            {
                                batchItem.VertexData[i].Position.X = position.X + nOriginX * cos - (nOriginY + h) * sin;
                                batchItem.VertexData[i].Position.Y = position.Y + nOriginX * sin + (nOriginY + h) * cos;
                            }
                        }
                        break;

                    case VertexTemplateType.BottomRight:
                        {
                            if (flipX)
                                batchItem.VertexData[i].TexCoords.X = source.X * texelWidth;
                            else
                                batchItem.VertexData[i].TexCoords.X = (source.X + source.Width) * texelWidth;

                            if (flipY)
                                batchItem.VertexData[i].TexCoords.Y = source.Y * texelHeight;
                            else
                                batchItem.VertexData[i].TexCoords.Y = (source.Y + source.Height) * texelHeight;

                            if (rotation == 0.0f)
                            {
                                batchItem.VertexData[i].Position.X = (position.X - spriteOrigin.X) + w;
                                batchItem.VertexData[i].Position.Y = (position.Y - spriteOrigin.Y) + h;
                            }
                            else
                            {
                                batchItem.VertexData[i].Position.X = position.X + (nOriginX + w) * cos - (nOriginY + h) * sin;
                                batchItem.VertexData[i].Position.Y = position.Y + (nOriginX + w) * sin + (nOriginY + h) * cos;
                            }
                        }
                        break;
                }
            }

            _batchItems.Add(batchItem);
        } // Draw

        public void Draw(Texture2D texture, Matrix3x2 worldMatrix, RgbaFloat? color = null, Rectangle? sourceRect = null, SpriteFlipType flip = SpriteFlipType.None)
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call Draw.");

            if (!color.HasValue)
                color = RgbaFloat.White;
            if (!sourceRect.HasValue)
                sourceRect = new Rectangle(0, 0, (int)texture.Width, (int)texture.Height);
            
            var batchItem = new SpriteBatchItem
            {
                Texture = texture,
                VertexData = new SpriteBatch2DVertex[4]
            };

            var texelWidth = texture.TexelWidth;
            var texelHeight = texture.TexelHeight;
            var source = sourceRect.Value;
            var flipX = (flip == SpriteFlipType.Horizontal || flip == SpriteFlipType.Both);
            var flipY = (flip == SpriteFlipType.Vertical || flip == SpriteFlipType.Both);

            // top left
            batchItem.VertexData[0] = new SpriteBatch2DVertex()
            {
                Position = Vector2.Transform(new Vector2(0f, 0f), worldMatrix),
                TexCoords = new Vector2(
                    flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                    flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                Color = color.Value
            };

            // top right
            batchItem.VertexData[1] = new SpriteBatch2DVertex()
            {
                Position = Vector2.Transform(new Vector2(1f, 0f), worldMatrix),
                TexCoords = new Vector2(
                    flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                    flipY ? (source.Y + source.Height) * texelHeight : source.Y * texelHeight),
                Color = color.Value
            };

            // bottom left
            batchItem.VertexData[2] = new SpriteBatch2DVertex()
            {
                Position = Vector2.Transform(new Vector2(0f, 1f), worldMatrix),
                TexCoords = new Vector2(
                    flipX ? (source.X + source.Width) * texelWidth : source.X * texelWidth,
                    flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                Color = color.Value
            };

            // bottom right
            batchItem.VertexData[3] = new SpriteBatch2DVertex()
            {
                Position = Vector2.Transform(new Vector2(1f, 1f), worldMatrix),
                TexCoords = new Vector2(
                    flipX ? source.X * texelWidth : (source.X + source.Width) * texelWidth,
                    flipY ? source.Y * texelHeight : (source.Y + source.Height) * texelHeight),
                Color = color.Value
            };

            _batchItems.Add(batchItem);
        }

        public unsafe void End()
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call End.");

            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, _projection);
            GraphicsDevice.UpdateBuffer(_transformBuffer, (uint)sizeof(Matrix4x4), _view);

            Texture2D currentTexture = null;
            var currentBatchCount = 0;

            for (var i = 0; i < _batchItems.Count; i++)
            {
                var batchItem = _batchItems[i];

                if (currentTexture == null || currentTexture != batchItem.Texture)
                {
                    if (currentBatchCount > 0 && currentTexture != null)
                        Flush(currentTexture, ref currentBatchCount);

                    currentTexture = batchItem.Texture;
                }

                if (currentBatchCount >= (_maxBatchSize - 1))
                    Flush(currentTexture, ref currentBatchCount);

                for (var j = 0; j < VerticesPerQuad; j++)
                    _vertexData[(currentBatchCount * VerticesPerQuad) + j] = batchItem.VertexData[j];

                currentBatchCount += 1;
            }

            Flush(currentTexture, ref currentBatchCount);

            _begin = false;
            _batchItems.Clear();
        }

        public unsafe void Flush(Texture2D texture, ref int count)
        {
            if (count == 0)
                return;

            CommandList.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(count * VerticesPerQuad * sizeof(SpriteBatch2DVertex)));
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
                var textureSetDescription = new ResourceSetDescription(_textureLayout, texture.Data, _sampler);
                var newTextureSet = GraphicsDevice.ResourceFactory.CreateResourceSet(textureSetDescription);
                _textureSets.Add(texture.AssetName, newTextureSet);
                CommandList.SetGraphicsResourceSet(1, newTextureSet);
            }

            CommandList.DrawIndexed((uint)(count * IndicesPerQuad));
            count = 0;
        }
    }
}
