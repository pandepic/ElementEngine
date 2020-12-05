using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Vulkan;

namespace ElementEngine
{
    public enum TileBatch2DWrapMode
    {
        None,
        Horizontal,
        Vertical,
        Both,
    }

    public class TileBatch2DAnimation
    {
        public int Index { get; set; }
        public int CurrentTileIndex { get; set; }
        public int CurrentFrame => Animation.Frames[CurrentTileIndex];
        public int FirstFrame => Animation.TileID;
        public TileAnimation Animation { get; set; }
        public float Timer { get; set; }
    }

    public class TileBatch2DLayer
    {
        public Texture2D DataTexture { get; set; }
        public ResourceSet TextureSetData { get; set; }
    }

    public class TileBatch2D : IDisposable
    {
        public Sdl2Window Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;

        // Graphics resources
        protected Pipeline _pipeline;
        protected DeviceBuffer _vertexBuffer;

        protected DeviceBuffer _transformBuffer;
        protected ResourceLayout _transformLayout;
        protected ResourceSet _transformSet;
        protected DeviceBuffer _animationBuffer;
        protected ResourceLayout _animationLayout;
        protected ResourceSet _animationSet;
        protected ResourceLayout _textureLayoutData;
        protected ResourceLayout _textureLayoutAtlas;
        protected ResourceSet _textureSetAtlas;
        protected Shader[] _shaders;

        // Shared static resources
        protected static Sampler _sampler = ElementGlobals.GraphicsDevice.PointSampler;

        protected static Vertex2DTileBatch[] _vertexData = new Vertex2DTileBatch[6]
        {
            new Vertex2DTileBatch(-1f, -1f, 0f, 1f), // tri 1
            new Vertex2DTileBatch(1f, -1f, 1f, 1f),
            new Vertex2DTileBatch(1f, 1f, 1f, 0f),
            new Vertex2DTileBatch(-1f, -1f, 0f, 1f), // tri 2
            new Vertex2DTileBatch(1f, 1f, 1f, 0f),
            new Vertex2DTileBatch(-1f, 1f, 0f, 0f),
        };

        protected static Vector2[] _transformBufferData = new Vector2[6];

        // Map data
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int TileSheetTilesWidth { get; set; }
        public int TileSheetTilesHeight { get; set; }
        public Vector2 TileSize { get; set; }
        public Vector2 InverseTileSize { get; set; }
        public Vector2 ViewportSize { get; set; }
        public Vector2 ScaledViewportSize { get; set; }
        public float TileScale { get; set; } = 1f;
        public Texture2D AtlasTexture { get; set; }
        public List<TileBatch2DLayer> Layers { get; set; } = new List<TileBatch2DLayer>();

        protected Dictionary<int, TileBatch2DAnimation> _animationLookup = new Dictionary<int, TileBatch2DAnimation>();
        protected TileBatch2DAnimation[] _animations;
        protected Vector4[] _animationOffsets;
        protected RgbaByte[] _dataArray;
        protected bool _currentLayerEnded = false;

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
                    _transformBuffer?.Dispose();
                    _transformLayout?.Dispose();
                    _transformSet?.Dispose();
                    _animationBuffer?.Dispose();
                    _animationLayout?.Dispose();
                    _animationSet?.Dispose();
                    _textureLayoutData?.Dispose();
                    _textureLayoutAtlas?.Dispose();

                    if (_shaders != null)
                    {
                        for (var i = 0; i < _shaders.Length; i++)
                            _shaders[i]?.Dispose();
                    }

                    ClearLayers();
                }

                _disposed = true;
            }
        }
        #endregion

        public unsafe TileBatch2D(int mapWidth, int mapHeight, int tileWidth, int tileHeight, Texture2D atlasTexture, TileBatch2DWrapMode wrapMode = TileBatch2DWrapMode.None, Dictionary<int, TileAnimation> tileAnimations = null)
            : this(mapWidth, mapHeight, tileWidth, tileHeight, atlasTexture, ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription, wrapMode, tileAnimations) { }

        public unsafe TileBatch2D(int mapWidth, int mapHeight, int tileWidth, int tileHeight, Texture2D atlasTexture, OutputDescription output, TileBatch2DWrapMode wrapMode = TileBatch2DWrapMode.None, Dictionary<int, TileAnimation> tileAnimations = null)
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;

            ViewportSize = new Vector2(Window.Width, Window.Height);
            ScaledViewportSize = ViewportSize;

            TileSize = new Vector2(tileWidth, tileHeight);
            InverseTileSize = new Vector2(1f / tileWidth, 1f / tileHeight);
            
            AtlasTexture = atlasTexture;

            TileSheetTilesWidth = AtlasTexture.Width / tileWidth;
            TileSheetTilesHeight = AtlasTexture.Height / tileHeight;

            var factory = GraphicsDevice.ResourceFactory;

            if (tileAnimations != null && tileAnimations.Count > 0)
            {
                _animations = new TileBatch2DAnimation[tileAnimations.Count];

                var index = 0;

                foreach (var kvp in tileAnimations)
                {
                    var newAnim = new TileBatch2DAnimation()
                    {
                        Index = index + 1,
                        Animation = kvp.Value,
                        Timer = kvp.Value.DurationPerFrame,
                        CurrentTileIndex = 0,
                    };

                    _animations[index] = newAnim;
                    _animationLookup.Add(kvp.Value.TileID, newAnim);
                    index += 1;
                }

                var animOffsetCount = tileAnimations.Count + 1;

                while ((sizeof(Vector4) * animOffsetCount) % 16 != 0)
                    animOffsetCount += 1;

                _animationOffsets = new Vector4[animOffsetCount];
            }
            else
            {
                var animOffsetCount = 1;

                while ((sizeof(Vector4) * animOffsetCount) % 16 != 0)
                    animOffsetCount += 1;

                _animationOffsets = new Vector4[animOffsetCount];
            }

            for (var i = 0; i < _animationOffsets.Length; i++)
                _animationOffsets[i] = Vector4.Zero;

            var wrapX = false;
            var wrapY = false;

            if (wrapMode == TileBatch2DWrapMode.Horizontal || wrapMode == TileBatch2DWrapMode.Both)
                wrapX = true;
            if (wrapMode == TileBatch2DWrapMode.Vertical || wrapMode == TileBatch2DWrapMode.Both)
                wrapY = true;

            // shaders
            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultTileVS), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(DefaultShaders.DefaultTileFS
                .Replace("{ANIM_COUNT}", _animationOffsets.Length.ToString())
                .Replace("{WRAP_X}", wrapX ? "true" : "false")
                .Replace("{WRAP_Y}", wrapY ? "true" : "false")
                ), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));

            // transform uniforms
            _transformBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Vector2) * _transformBufferData.Length), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, _transformBufferData);

            _transformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("TransformBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(_transformLayout, _transformBuffer));

            // animation uniforms
            _animationBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Vector4) * _animationOffsets.Length), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_animationBuffer, 0, _animationOffsets);

            _animationLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("AnimationBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            _animationSet = factory.CreateResourceSet(new ResourceSetDescription(_animationLayout, _animationBuffer));

            // texture layouts
            _textureLayoutData = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("fDataImage", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("fDataImageSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _textureLayoutAtlas = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("fAtlasImage", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("fAtlasImageSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _textureSetAtlas = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayoutData, AtlasTexture.Texture, _sampler));

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(_vertexData.Length * sizeof(Vertex2DTileBatch)), BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_vertexData.Length * sizeof(Vertex2DTileBatch)));

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
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { Vertex2DTileBatch.VertexLayout }, shaders: _shaders),
                ResourceLayouts = new ResourceLayout[]
                {
                    _transformLayout,
                    _animationLayout,
                    _textureLayoutAtlas,
                    _textureLayoutData
                },
                Outputs = output
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        }

        ~TileBatch2D()
        {
            Dispose(false);
        }

        public void SetTransformBuffer(Vector2 inverseTileTextureSize, Vector2 inverseSpriteTextureSize, Vector2 tileSize, Vector2 viewOffset, Vector2 viewportSize, Vector2 inverseTileSize)
        {
            _transformBufferData[0] = inverseTileTextureSize;
            _transformBufferData[1] = inverseSpriteTextureSize;
            _transformBufferData[2] = tileSize;
            _transformBufferData[3] = viewOffset;
            _transformBufferData[4] = viewportSize;
            _transformBufferData[5] = inverseTileSize;
        }

        public void ClearLayers()
        {
            foreach (var layer in Layers)
                layer?.DataTexture?.Dispose();

            Layers.Clear();
        }

        public void ClearDataArray()
        {
            for (var y = 0; y < MapHeight; y++)
            {
                for (var x = 0; x < MapWidth; x++)
                {
                    var index = x + MapWidth * y;
                    _dataArray[index] = new RgbaByte(255, 255, 0, 255);
                }
            }
        } // ClearDataArray

        public void SetTileAtPosition(int posx, int posy, byte x, byte y, byte animIndex = 0)
        {
            var index = posx + MapWidth * posy;
            SetTileAtIndex(index, x, y, animIndex);
        }

        public void SetTileAtPosition(int posx, int posy, int tileIndex)
        {
            byte x = (byte)(tileIndex % TileSheetTilesWidth);
            byte y = (byte)(tileIndex / TileSheetTilesWidth);

            byte animIndex = 0;
            if (_animationLookup.TryGetValue(tileIndex, out var animation))
                animIndex = (byte)animation.Index;

            SetTileAtPosition(posx, posy, x, y, animIndex);
        }

        public void SetTileAtIndex(int index, byte x, byte y, byte animIndex = 0)
        {
            _dataArray[index] = new RgbaByte(x, y, animIndex, 255);
            _currentLayerEnded = false;
        }

        public void SetTileAtIndex(int index, int tileIndex)
        {
            byte x = (byte)(tileIndex % TileSheetTilesWidth);
            byte y = (byte)(tileIndex / TileSheetTilesWidth);

            SetTileAtIndex(index, x, y);
        }

        public void BeginBuild()
        {
            ClearLayers();

            _dataArray = new RgbaByte[MapWidth * MapHeight];
            ClearDataArray();

            _currentLayerEnded = false;
        }

        public void EndLayer()
        {
            var newLayer = new TileBatch2DLayer()
            {
                DataTexture = new Texture2D(MapWidth, MapHeight),
            };

            newLayer.DataTexture.SetData(_dataArray);
            newLayer.TextureSetData = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayoutData, newLayer.DataTexture.Texture, _sampler));

            Layers.Add(newLayer);
            ClearDataArray();

            _currentLayerEnded = true;
        }

        public void EndBuild()
        {
            if (!_currentLayerEnded)
                EndLayer();

            _dataArray = null;
        }

        public unsafe void Update(GameTimer gameTimer)
        {
            if (_animations == null)
                return;

            for (var i = 0; i < _animations.Length; i++)
            {
                var animation = _animations[i];
                animation.Timer -= gameTimer.DeltaMS;

                if (animation.Timer <= 0)
                {
                    animation.Timer = animation.Animation.DurationPerFrame;
                    animation.CurrentTileIndex += 1;
                    if (animation.CurrentTileIndex >= animation.Animation.Frames.Count)
                        animation.CurrentTileIndex = 0;

                    var firstFrame = animation.FirstFrame;
                    var currentFrame = animation.CurrentFrame;

                    var firstFramePos = new Vector4()
                    {
                        X = (firstFrame % TileSheetTilesWidth) * TileSize.X,
                        Y = (firstFrame / TileSheetTilesWidth) * TileSize.Y,
                    };

                    var currentFramePos = new Vector4()
                    {
                        X = (currentFrame % TileSheetTilesWidth) * TileSize.X,
                        Y = (currentFrame / TileSheetTilesWidth) * TileSize.Y,
                    };

                    _animationOffsets[animation.Index] = currentFramePos - firstFramePos;
                }
            }
        } // Update

        public void DrawAll(Camera2D camera, float scale = 1f) => DrawAll(camera, Vector2.Zero, scale);
        public void DrawAll(Camera2D camera, Vector2 position, float scale = 1f)
        {
            DrawLayers(0, Layers.Count - 1, camera, position, scale);
        }

        public void DrawLayersFrom(int from, Camera2D camera, float scale = 1f) => DrawLayersFrom(from, camera, Vector2.Zero, scale);
        public void DrawLayersFrom(int from, Camera2D camera, Vector2 position, float scale = 1f)
        {
            DrawLayers(from, Layers.Count - 1, camera, position, scale);
        }

        public void DrawLayersTo(int to, Camera2D camera, float scale = 1f) => DrawLayersTo(to, camera, Vector2.Zero, scale);
        public void DrawLayersTo(int to, Camera2D camera, Vector2 position, float scale = 1f)
        {
            DrawLayers(0, to, camera, position, scale);
        }

        public void DrawLayers(int start, int end, Camera2D camera, float scale = 1f) => DrawLayers(start, end, camera, Vector2.Zero, scale);
        public void DrawLayers(int start, int end, Camera2D camera, Vector2 position, float scale = 1f)
        {
            var totalScale = scale * camera.Zoom;
            var scaledOrigin = camera.Origin / totalScale;
            var offsetOrigin = camera.Origin - scaledOrigin;

            DrawLayers(start, end, camera.Position + offsetOrigin + position, totalScale);
        }

        public void DrawAll(Vector2 position, float scale = 1f)
        {
            DrawLayers(0, Layers.Count - 1, position, scale);
        }

        public void DrawLayersFrom(int from, Vector2 position, float scale = 1f)
        {
            DrawLayers(from, Layers.Count - 1, position, scale);
        }

        public void DrawLayersTo(int to, Vector2 position, float scale = 1f)
        {
            DrawLayers(0, to, position, scale);
        }

        public void DrawLayers(int start, int end, Vector2 position, float scale = 1f)
        {
            if (Layers.Count <= 0)
                return;

            ScaledViewportSize = ViewportSize / scale;

            SetTransformBuffer(
                Layers[0].DataTexture.TexelSize,
                AtlasTexture.TexelSize,
                TileSize,
                new Vector2(MathF.Floor(position.X * TileScale), MathF.Floor(position.Y * TileScale)),
                ScaledViewportSize,
                InverseTileSize);

            CommandList.UpdateBuffer(_transformBuffer, 0, _transformBufferData);
            CommandList.UpdateBuffer(_animationBuffer, 0, _animationOffsets);
            CommandList.SetVertexBuffer(0, _vertexBuffer);
            CommandList.SetPipeline(_pipeline);
            CommandList.SetGraphicsResourceSet(0, _transformSet);
            CommandList.SetGraphicsResourceSet(1, _animationSet);
            CommandList.SetGraphicsResourceSet(2, _textureSetAtlas);

            for (var i = start; i <= end; i++)
            {
                var layer = Layers[i];
                CommandList.SetGraphicsResourceSet(3, layer.TextureSetData);
                CommandList.Draw((uint)_vertexData.Length);
            }
        }
    } // TileBatch2D
}
