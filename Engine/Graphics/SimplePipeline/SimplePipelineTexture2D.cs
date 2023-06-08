using System;
using Veldrid;

namespace ElementEngine
{
    public class SimplePipelineTexture2D : IDisposable
    {
        public ResourceLayout ResourceLayout;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly Texture2D Texture;

        public ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

        protected bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            ResourceLayout?.Dispose();

            _isDisposed = true;
        }

        public SimplePipelineTexture2D(GraphicsDevice graphicsDevice, string name, SamplerType samplerType)
            : this(graphicsDevice, name)
        {
        }

        public SimplePipelineTexture2D(GraphicsDevice graphicsDevice, string name)
        {
            GraphicsDevice = graphicsDevice;

            ResourceLayout = ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(name, ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription(name + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        }

        public SimplePipelineTexture2D(string name, Texture2D texture) : this(texture.GraphicsDevice, name)
        {
        }

        public ResourceSet GetResourceSet(Sampler sampler)
        {
            return Texture.GetResourceSet(sampler, ResourceLayout);
        }
    }
}
