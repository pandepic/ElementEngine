using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;

namespace ElementEngine
{
    public class PrimitiveBatch : IDisposable
    {
        public Sdl2Window Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;

        // Shared static resources
        protected static bool _staticResLoaded = false;
        protected static Shader[] _shaders;
        protected static Sampler _sampler = ElementGlobals.GraphicsDevice.PointSampler;

        protected static Vector2[] _rectVertexTemplate = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };

        // Rendering
        protected Matrix4x4 _projection;
        protected Matrix4x4 _view;

        // Graphics resources
        protected Pipeline _currentPipeline;
        protected Pipeline _pipelineTriangleList;
        protected Pipeline _pipelineTriangleStrip;
        protected Pipeline _pipelineLineLoop;
        protected DeviceBuffer _vertexBuffer;
        protected DeviceBuffer _transformBuffer;
        protected ResourceLayout _transformLayout;
        protected ResourceSet _transformSet;

        protected const int _maxBatchSize = 50000;
        protected bool _begin = false;
        protected Vertex2DPositionColor[] _vertexData;
        protected Vertex2DPositionColor[] _tempVertices;
        protected int _currentBatchCount = 0;

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
                    _pipelineTriangleList?.Dispose();
                    _pipelineTriangleStrip?.Dispose();
                    _pipelineLineLoop?.Dispose();
                    _vertexBuffer?.Dispose();
                    _transformBuffer?.Dispose();
                    _transformLayout?.Dispose();
                    _transformSet?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public PrimitiveBatch(bool invertY = false) : this(ElementGlobals.Window.Width, ElementGlobals.Window.Height, invertY) { }
        public PrimitiveBatch(int width, int height, bool invertY = false) : this(width, height, ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription, invertY) { }
        public PrimitiveBatch(Texture2D target, bool invertY = false) : this(target.Width, target.Height, target.GetFramebuffer().OutputDescription, invertY) { }

        public unsafe PrimitiveBatch(int width, int height, OutputDescription output, bool invertY = false)
        {
            var factory = GraphicsDevice.ResourceFactory;
            LoadStaticResources(factory);

            _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0f, 0f, 1f);

            if (invertY && !GraphicsDevice.IsUvOriginTopLeft)
                _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, 0f, height, 0f, 1f);

            _transformBuffer = factory.CreateBuffer(new BufferDescription((uint)(sizeof(Matrix4x4) * 2), BufferUsage.UniformBuffer));
            GraphicsDevice.UpdateBuffer(_transformBuffer, 0, Matrix4x4.Identity);
            GraphicsDevice.UpdateBuffer(_transformBuffer, (uint)sizeof(Matrix4x4), Matrix4x4.Identity);

            _transformLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription("ProjectionViewBuffer2", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(_transformLayout, _transformBuffer));

            _vertexData = new Vertex2DPositionColor[_maxBatchSize];
            _tempVertices = new Vertex2DPositionColor[_maxBatchSize];

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(_vertexData.Length * sizeof(Vertex2DPositionColor)), BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_vertexData.Length * sizeof(Vertex2DPositionColor)));

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
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { Vertex2DPositionColor.VertexLayout }, _shaders),
                ResourceLayouts = new ResourceLayout[]
                {
                    _transformLayout
                },
                Outputs = output
            };

            _pipelineTriangleList = factory.CreateGraphicsPipeline(pipelineDescription);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.LineStrip;
            _pipelineLineLoop = factory.CreateGraphicsPipeline(pipelineDescription);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            _pipelineTriangleStrip = factory.CreateGraphicsPipeline(pipelineDescription);
        }

        public static void LoadStaticResources(ResourceFactory factory)
        {
            if (_staticResLoaded)
                return;

            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultPrimitiveVS), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(DefaultShaders.DefaultPrimitiveFS), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));
            _staticResLoaded = true;
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

        public void DrawLine(Vector2I start, Vector2I end, RgbaFloat color, int lineSize)
        {
            var rotation = MathF.Atan2(end.Y - start.Y, end.X - start.X).ToDegrees();
            DrawFilledRect(new Rectangle(start.X, start.Y, (int)Vector2.Distance(start.ToVector2(), end.ToVector2()), lineSize), color, Vector2.Zero, rotation);
        }

        public void DrawOutlinedRect(Rectangle rect, RgbaFloat color, RgbaFloat lineColor, int lineSize, Vector2? origin = null, float rotation = 0f)
        {
            DrawFilledRect(rect, color, origin, rotation);
            DrawEmptyRect(rect, lineColor, lineSize, origin, rotation);
        }

        public void DrawEmptyRect(Rectangle rect, RgbaFloat color, int lineSize, Vector2? origin = null, float rotation = 0f)
        {
            if (!origin.HasValue)
                origin = new Vector2(0f, 0f);

            rotation = rotation.ToRadians();

            var topLeft = new Vector2(rect.X, rect.Y) - origin.Value;
            var topRight = new Vector2(rect.X + rect.Width, rect.Y) - origin.Value;
            var bottomLeft = new Vector2(rect.X, rect.Y + rect.Height) - origin.Value;
            var bottomRight = new Vector2(rect.X + rect.Width, rect.Y + rect.Height) - origin.Value;

            topLeft = MathHelper.RotatePointAroundOrigin(topLeft, rect.LocationF, rotation);
            topRight = MathHelper.RotatePointAroundOrigin(topRight, rect.LocationF, rotation);
            bottomLeft = MathHelper.RotatePointAroundOrigin(bottomLeft, rect.LocationF, rotation);
            bottomRight = MathHelper.RotatePointAroundOrigin(bottomRight, rect.LocationF, rotation);

            DrawLine(topLeft.ToVector2I(), topRight.ToVector2I(), color, lineSize);
            DrawLine(topRight.ToVector2I(), bottomRight.ToVector2I(), color, lineSize);
            DrawLine(bottomRight.ToVector2I(), bottomLeft.ToVector2I(), color, lineSize);
            DrawLine(bottomLeft.ToVector2I(), topLeft.ToVector2I(), color, lineSize);
        }

        public void DrawFilledRect(Rectangle rect, RgbaFloat color, Vector2? origin = null, float rotation = 0f)
        {
            if (!origin.HasValue)
                origin = new Vector2(0f, 0f);

            var sin = 0f;
            var cos = 0f;
            var nOriginX = -origin.Value.X;
            var nOriginY = -origin.Value.Y;

            if (rotation != 0f)
            {
                var radians = rotation.ToRadians();
                sin = MathF.Sin(radians);
                cos = MathF.Cos(radians);
            }

            var topLeft = new Vertex2DPositionColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                rect.X - origin.Value.X,
                                rect.Y - origin.Value.Y)
                            : new Vector2(
                                rect.X + nOriginX * cos - nOriginY * sin,
                                rect.Y + nOriginX * sin + nOriginY * cos),
                Color = color
            };

            var x = _rectVertexTemplate[(int)VertexTemplateType.TopRight].X;
            var w = rect.Width * x;

            var topRight = new Vertex2DPositionColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (rect.X - origin.Value.X) + w,
                                rect.Y - origin.Value.Y)
                            : new Vector2(
                                rect.X + (nOriginX + w) * cos - nOriginY * sin,
                                rect.Y + (nOriginX + w) * sin + nOriginY * cos),
                Color = color
            };

            var y = _rectVertexTemplate[(int)VertexTemplateType.BottomLeft].Y;
            var h = rect.Height * y;

            var bottomLeft = new Vertex2DPositionColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (rect.X - origin.Value.X),
                                (rect.Y - origin.Value.Y) + h)
                            : new Vector2(
                                rect.X + nOriginX * cos - (nOriginY + h) * sin,
                                rect.Y + nOriginX * sin + (nOriginY + h) * cos),
                Color = color
            };

            x = _rectVertexTemplate[(int)VertexTemplateType.BottomRight].X;
            y = _rectVertexTemplate[(int)VertexTemplateType.BottomRight].Y;
            w = rect.Width * x;
            h = rect.Height * y;

            var bottomRight = new Vertex2DPositionColor()
            {
                Position = rotation == 0.0f
                            ? new Vector2(
                                (rect.X - origin.Value.X) + w,
                                (rect.Y - origin.Value.Y) + h)
                            : new Vector2(
                                rect.X + (nOriginX + w) * cos - (nOriginY + h) * sin,
                                rect.Y + (nOriginX + w) * sin + (nOriginY + h) * cos),
                Color = color
            };

            _tempVertices[0] = bottomLeft;
            _tempVertices[1] = topRight;
            _tempVertices[2] = topLeft;
            _tempVertices[3] = bottomLeft;
            _tempVertices[4] = bottomRight;
            _tempVertices[5] = topRight;

            AddVertices(_pipelineTriangleList, 6);
        }

        public void DrawOutlinedCircle(Vector2 position, float radius, RgbaFloat color, RgbaFloat lineColor, int quality = 10)
        {
            DrawFilledCircle(position, radius, color, quality);
            DrawEmptyCircle(position, radius, lineColor, quality);
        }

        public void DrawEmptyCircle(Vector2 position, float radius, RgbaFloat color, int quality = 10)
        {
            var vertexCount = (int)(radius / quality) * 8;
            var loopIndex = 0;

            for (var i = 0f; i < 2 * MathF.PI; i += 2 * MathF.PI / vertexCount)
            {
                _tempVertices[loopIndex] = new Vertex2DPositionColor()
                {
                    Position = new Vector2(MathF.Cos(i) * radius + position.X, MathF.Sin(i) * radius + position.Y),
                    Color = color,
                };

                loopIndex += 1;
            }

            _tempVertices[loopIndex] = new Vertex2DPositionColor()
            {
                Position = _tempVertices[0].Position,
                Color = color,
            };

            loopIndex += 1;

            AddVertices(_pipelineLineLoop, loopIndex);
        }

        public void DrawFilledCircle(Vector2 position, float radius, RgbaFloat color, int quality = 10)
        {
            var vertexCount = (int)(radius / quality) * 8;
            var loopIndex = 0;

            for (var i = 0f; i < 2 * MathF.PI; i += 2 * MathF.PI / vertexCount)
            {
                _tempVertices[loopIndex] = new Vertex2DPositionColor()
                {
                    Position = new Vector2(MathF.Cos(i) * radius + position.X, MathF.Sin(i) * radius + position.Y),
                    Color = color,
                };

                loopIndex += 1;

                _tempVertices[loopIndex] = new Vertex2DPositionColor()
                {
                    Position = position,
                    Color = color,
                };

                loopIndex += 1;
            }

            _tempVertices[loopIndex] = new Vertex2DPositionColor()
            {
                Position = _tempVertices[0].Position,
                Color = color,
            };

            loopIndex += 1;

            AddVertices(_pipelineTriangleStrip, loopIndex);
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

        protected void AddVertices(Pipeline pipeline, int count)
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call Draw.");

            if (_currentPipeline == null || _currentPipeline != pipeline)
            {
                if (_currentPipeline != null)
                    Flush();

                _currentPipeline = pipeline;
            }

            if (_currentBatchCount + count >= _vertexData.Length)
                Flush();

            for (var i = 0; i < count; i++)
            {
                _vertexData[_currentBatchCount] = _tempVertices[i];
                _currentBatchCount += 1;
            }
        }

        public void End()
        {
            if (!_begin)
                throw new Exception("You must begin a batch before you can call End.");

            Flush();
            _begin = false;
        }

        public unsafe void Flush()
        {
            if (_currentBatchCount == 0 || _currentPipeline == null)
                return;

            CommandList.UpdateBuffer(_vertexBuffer, 0, ref _vertexData[0], (uint)(_currentBatchCount * sizeof(Vertex2DPositionColor)));
            CommandList.SetVertexBuffer(0, _vertexBuffer);
            CommandList.SetPipeline(_currentPipeline);
            CommandList.SetGraphicsResourceSet(0, _transformSet);
            CommandList.Draw((uint)_currentBatchCount);

            _currentBatchCount = 0;
            _currentPipeline = null;
        }

    } // PrimitiveBatch
}
