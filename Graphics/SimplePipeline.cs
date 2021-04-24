using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace ElementEngine
{
    public interface ISimpleUniformBuffer
    {
        public string Name { get; set; }

        public DeviceBuffer Buffer { get; set; }
        public ResourceLayout ResourceLayout { get; set; }
        public ResourceSet ResourceSet { get; set; }

        public void Dispose();
    }

    public class SimpleUniformBuffer<T> : ISimpleUniformBuffer, IDisposable where T : unmanaged
    {
        public string Name { get; set; }
        public T[] Data;

        public DeviceBuffer Buffer { get; set; }
        public ResourceLayout ResourceLayout { get; set; }
        public ResourceSet ResourceSet { get; set; }

        public readonly ShaderStages Stages;
        public readonly GraphicsDevice GraphicsDevice;

        public void Dispose()
        {
            Buffer?.Dispose();
            ResourceLayout?.Dispose();
            ResourceSet?.Dispose();
        }

        public unsafe SimpleUniformBuffer(GraphicsDevice graphicsDevice, string name, int size, ShaderStages stages)
        {
            Name = name;
            Data = new T[size];
            Stages = stages;
            GraphicsDevice = graphicsDevice;

            var bufferSize = size * sizeof(T);
            Buffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)bufferSize, BufferUsage.UniformBuffer));
            ResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription(Name, ResourceKind.UniformBuffer, Stages)));
            ResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayout, Buffer));
        }

        public unsafe void SetValue(int index, T value, bool updateBuffer = false)
        {
            Data[index] = value;

            if (updateBuffer)
                GraphicsDevice.UpdateBuffer(Buffer, (uint)(index * sizeof(T)), Data[index]);
        }

        public void UpdateBuffer()
        {
            GraphicsDevice.UpdateBuffer(Buffer, 0, Data);
        }
    } // SimpleUniformBuffer

    public class SimplePipelineTexture2D : IDisposable
    {
        public ResourceLayout ResourceLayout;
        public ResourceSet ResourceSet;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly Texture2D Texture;
        public readonly Sampler Sampler;

        public ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

        public void Dispose()
        {
            ResourceLayout?.Dispose();
            ResourceSet?.Dispose();
        }

        public SimplePipelineTexture2D(GraphicsDevice graphicsDevice, string name, SamplerType samplerType)
        {
            GraphicsDevice = graphicsDevice;
            Sampler = GetSampler(samplerType);

            ResourceLayout = ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(name, ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription(name + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        }

        public SimplePipelineTexture2D(string name, Texture2D texture, SamplerType samplerType) : this(texture.GraphicsDevice, name, samplerType)
        {
            ResourceSet = GetResourceSet(texture);
        }

        public ResourceSet GetResourceSet(Texture2D texture)
        {
            var textureSetDescription = new ResourceSetDescription(ResourceLayout, texture.Texture, Sampler);
            var resourceSet = ResourceFactory.CreateResourceSet(textureSetDescription);

            return resourceSet;
        }

        internal Sampler GetSampler(SamplerType samplerType)
        {
            switch (samplerType)
            {
                case SamplerType.Point:
                    return GraphicsDevice.PointSampler;

                case SamplerType.Linear:
                    return GraphicsDevice.LinearSampler;

                case SamplerType.Aniso4x:
                    return GraphicsDevice.Aniso4xSampler;

                default:
                    throw new ArgumentException("Unknown value", "samplerType");
            }
        }
    }

    public class SimpleShader : IDisposable
    {
        public Shader[] Shaders;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly VertexLayoutDescription VertexLayout;

        public void Dispose()
        {
            if (Shaders != null)
            {
                foreach (var shader in Shaders)
                    shader?.Dispose();
            }
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

        public void Dispose()
        {
            Pipeline?.Dispose();
            Shader?.Dispose();

            foreach (var buffer in UniformBuffers)
                buffer?.Dispose();

            foreach (var texture in PipelineTextures)
                texture?.Dispose();
        }

        public SimplePipeline(GraphicsDevice graphicsDevice, SimpleShader shader, OutputDescription output, BlendStateDescription blendState, FaceCullMode cullMode)
        {
            GraphicsDevice = graphicsDevice;
            Shader = shader;
            CullMode = cullMode;
            BlendState = blendState;
            Output = output;
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
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { Shader.VertexLayout }, shaders: Shader.Shaders),
                ResourceLayouts = resourceLayouts,
                Outputs = Output
            };

            Pipeline = GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(PipelineDescription);
            return Pipeline;
        }

    } // SimplePipeline
}
