using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace ElementEngine
{
    public class SimplePipeline : IDisposable
    {
        public List<ISimpleUniformBuffer> UniformBuffers = new List<ISimpleUniformBuffer>();
        public List<SimplePipelineTexture2D> PipelineTextures = new List<SimplePipelineTexture2D>();

        public Pipeline Pipeline;
        public GraphicsPipelineDescription PipelineDescription;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly SimpleShader Shader;
        public readonly FaceCullMode CullMode;
        public readonly BlendStateDescription BlendState;
        public readonly OutputDescription Output;

        public bool CanDisposeShader = true;

        #region IDisposable
        protected bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Pipeline?.Dispose();

            if (CanDisposeShader)
                Shader?.Dispose();

            foreach (var buffer in UniformBuffers)
                buffer?.Dispose();

            foreach (var texture in PipelineTextures)
                texture?.Dispose();

            _isDisposed = true;
        }
        #endregion

        public SimplePipeline(GraphicsDevice graphicsDevice, SimpleShader shader, OutputDescription output, BlendStateDescription blendState, FaceCullMode cullMode)
        {
            GraphicsDevice = graphicsDevice;
            Shader = shader;
            CullMode = cullMode;
            BlendState = blendState;
            Output = output;
        }

        public void SetupForSpritebatch(SamplerType samplerType)
        {
            var viewProjectionBuffer = new SimpleUniformBuffer<Matrix4x4>(
                ElementGlobals.GraphicsDevice,
                "ProjectionViewBuffer",
                2,
                ShaderStages.Vertex);

            viewProjectionBuffer.SetValue(0, Matrix4x4.Identity);
            viewProjectionBuffer.SetValue(1, Matrix4x4.Identity);
            viewProjectionBuffer.UpdateBuffer();

            AddUniformBuffer(viewProjectionBuffer);
            AddPipelineTexture(new(GraphicsDevice, "fTexture", samplerType));
        }

        public void AddUniformBuffer(ISimpleUniformBuffer uniformBuffer)
        {
            UniformBuffers.Add(uniformBuffer);
        }

        public void AddPipelineTexture(SimplePipelineTexture2D pipelineTexture)
        {
            PipelineTextures.Add(pipelineTexture);
        }

        public Pipeline GeneratePipeline()
        {
            var resourceLayouts = new ResourceLayout[UniformBuffers.Count + PipelineTextures.Count];
            var layoutsIndex = 0;

            foreach (var uniformBuffer in UniformBuffers)
            {
                resourceLayouts[layoutsIndex] = uniformBuffer.ResourceLayout;
                layoutsIndex += 1;
            }

            foreach (var pipelineTexture in PipelineTextures)
            {
                resourceLayouts[layoutsIndex] = pipelineTexture.ResourceLayout;
                layoutsIndex += 1;
            }

            PipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendState,
                DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription
                {
                    DepthClipEnabled = true,
                    CullMode = CullMode,
                    ScissorTestEnabled = true,
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { Shader.VertexLayout }, shaders: Shader.Shaders),
                ResourceLayouts = resourceLayouts,
                Outputs = Output
            };

            Pipeline = GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(PipelineDescription);
            return Pipeline;
        }

    }
}
