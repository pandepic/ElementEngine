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

        protected static Vector2[] _quadVertexTemplate = new Vector2[]
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
        protected Pipeline _pipeline;
        protected DeviceBuffer _vertexBuffer;
        protected DeviceBuffer _transformBuffer;
        protected ResourceLayout _transformLayout;
        protected ResourceSet _transformSet;

        protected const int _maxBatchSize = 10000;

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
                }

                _disposed = true;
            }
        }
        #endregion

        public PrimitiveBatch() : this(ElementGlobals.Window.Width, ElementGlobals.Window.Height) { }
        public PrimitiveBatch(int width, int height) : this(width, height, ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription) { }
        public PrimitiveBatch(Texture2D target) : this(target.Width, target.Height, target.GetFramebuffer().OutputDescription) { }

        public unsafe PrimitiveBatch(int width, int height, OutputDescription output)
        {
            var factory = GraphicsDevice.ResourceFactory;
            LoadStaticResources(factory);

            _projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, 0f, height, 0f, 1f);
        }

        public static void LoadStaticResources(ResourceFactory factory)
        {
            if (_staticResLoaded)
                return;

            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(DefaultShaders.DefaultPrimitiveVS), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(DefaultShaders.DefaultPrimitiveFS), "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: true));
            _staticResLoaded = true;
        }
    } // PrimitiveBatch
}
