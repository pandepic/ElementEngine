using System;
using Veldrid;

namespace ElementEngine
{
    public class SimplePipelineTexture2D : IDisposable
    {
        public ResourceLayout ResourceLayout;
        public ResourceSet ResourceSet;

        public readonly GraphicsDevice GraphicsDevice;
        public readonly Texture2D Texture;
        public readonly Sampler Sampler;

        public ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

        protected bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            ResourceLayout?.Dispose();
            ResourceSet?.Dispose();

            _isDisposed = true;
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
            return samplerType switch
            {
                SamplerType.Point => GraphicsDevice.PointSampler,
                SamplerType.Linear => GraphicsDevice.LinearSampler,
                SamplerType.Aniso4x => GraphicsDevice.Aniso4xSampler,
                _ => throw new ArgumentException("Unknown value", "samplerType"),
            };
        }
    }
}
