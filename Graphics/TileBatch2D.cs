using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;

namespace ElementEngine.Graphics
{
    public class TileBatch2DLayer
    {
        public bool IsBelow { get; set; }
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
        protected ResourceLayout _textureLayoutData;
        protected ResourceLayout _textureLayoutAtlas;
        protected ResourceSet _textureSetAtlas;

        // Shared static resources
        protected static bool _staticResLoaded = false;
        protected static Shader[] _shaders;
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
                    _textureLayoutData?.Dispose();
                    _textureLayoutAtlas?.Dispose();

                    ClearLayers();
                }

                _disposed = true;
            }
        }
        #endregion

        public unsafe TileBatch2D(int mapWidth, int mapHeight, int tileWidth, int tileHeight, Texture2D atlasTexture)
            : this(mapWidth, mapHeight, tileWidth, tileHeight, atlasTexture, ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription) { }

        public unsafe TileBatch2D(int mapWidth, int mapHeight, int tileWidth, int tileHeight, Texture2D atlasTexture, OutputDescription output)
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
            LoadStaticResources(factory);

            _transformBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Vector2) * _transformBufferData.Length), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, _transformBufferData);

            _transformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("TransformBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(_transformLayout, _transformBuffer));

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

        public static void LoadStaticResources(ResourceFactory factory)
        {
            if (_staticResLoaded)
                return;

            ShaderDescription vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultTileVS), "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(DefaultShaders.DefaultTileFS), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));
            _staticResLoaded = true;
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
                    _dataArray[index] = new RgbaByte(255, 255, 255, 255);
                }
            }
        } // ClearDataArray

        public void SetTileAtPosition(int posx, int posy, byte x, byte y)
        {
            var index = posx + MapWidth * posy;
            SetTileAtIndex(index, x, y);
        }

        public void SetTileAtPosition(int posx, int posy, int tileIndex)
        {
            byte x = (byte)(tileIndex % TileSheetTilesWidth);
            byte y = (byte)(tileIndex / TileSheetTilesWidth);

            SetTileAtPosition(posx, posy, x, y);
        }

        public void SetTileAtIndex(int index, byte x, byte y)
        {
            _dataArray[index] = new RgbaByte(x, y, 255, 255);
            _currentLayerEnded = false;
        }

        public void SetTileAtIndex(int index, int tileIndex)
        {
            byte x = (byte)(tileIndex % MapWidth);
            byte y = (byte)(tileIndex / MapWidth);

            SetTileAtIndex(index, x, y);
        }

        public void BeginBuild()
        {
            ClearLayers();

            _dataArray = new RgbaByte[MapWidth * MapHeight];
            ClearDataArray();

            _currentLayerEnded = false;
        }

        public void EndLayer(bool below)
        {
            var newLayer = new TileBatch2DLayer()
            {
                IsBelow = below,
                DataTexture = new Texture2D(MapWidth, MapHeight),
            };

            newLayer.DataTexture.SetData(_dataArray);
            newLayer.TextureSetData = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayoutData, newLayer.DataTexture.Texture, _sampler));

            Layers.Add(newLayer);
            ClearDataArray();

            _currentLayerEnded = true;
        }

        public void EndBuild(bool below)
        {
            if (!_currentLayerEnded)
                EndLayer(below);

            _dataArray = null;
        }

        public void Draw(Vector2 position, bool below, float scale = 1f)
        {
            if (Layers.Count <= 0)
                return;

            ScaledViewportSize = ViewportSize / scale;

            SetTransformBuffer(
                Layers[0].DataTexture.TexelSize,
                AtlasTexture.TexelSize,
                TileSize,
                new Vector2(MathF.Floor(position.X * TileScale / scale), MathF.Floor(position.Y * TileScale / scale)),
                ScaledViewportSize,
                InverseTileSize);

            CommandList.UpdateBuffer(_transformBuffer, 0, _transformBufferData);
            CommandList.SetVertexBuffer(0, _vertexBuffer);
            CommandList.SetPipeline(_pipeline);
            CommandList.SetGraphicsResourceSet(0, _transformSet);
            CommandList.SetGraphicsResourceSet(1, _textureSetAtlas);

            for (var i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];

                if (layer.IsBelow != below)
                    continue;

                CommandList.SetGraphicsResourceSet(2, layer.TextureSetData);
                CommandList.Draw((uint)_vertexData.Length);
            }
        }
    } // TileBatch2D
}
