using System;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace ElementEngine
{
    public class SimpleShader : IDisposable
    {
        public Shader[] Shaders;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly VertexLayoutDescription VertexLayout;

        protected bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (Shaders != null)
            {
                foreach (var shader in Shaders)
                    shader?.Dispose();
            }

            _isDisposed = true;
        }

        public SimpleShader(GraphicsDevice graphicsDevice, string vertexShader, string fragmentShader, VertexLayoutDescription vertexLayout)
        {
            GraphicsDevice = graphicsDevice;
            VertexLayout = vertexLayout;

            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexShader), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentShader), "main");

            Shaders = GraphicsDevice.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));
        }
    }
}
