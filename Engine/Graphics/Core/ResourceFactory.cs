using System;
using System.Linq;

namespace ElementEngine;

public sealed class ResourceFactory
{
    private const int MaximumResourceLayouts = 16;
    private const int MaximumVertexLayouts = 8;

    private readonly GraphicsDevice _graphicsDevice;

    internal ResourceFactory(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public CommandList CreateCommandList()
    {
        _graphicsDevice.ThrowIfDisposed();
        return new CommandList();
    }

    public DeviceBuffer CreateBuffer(BufferDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();

        if (description.SizeInBytes == 0)
            throw new ArgumentOutOfRangeException(nameof(description), "Buffers must contain at least one byte.");

        var buffer = new DeviceBuffer(_graphicsDevice, description);
        buffer.NativeResource = _graphicsDevice.Backend.CreateBuffer(description);
        return buffer;
    }

    public Texture CreateTexture(TextureDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();

        if (description.Width == 0 || description.Height == 0 || description.Depth == 0)
            throw new ArgumentOutOfRangeException(nameof(description), "Texture dimensions must be greater than zero.");

        if (description.MipLevels == 0 || description.ArrayLayers == 0)
            throw new ArgumentOutOfRangeException(nameof(description), "Textures must contain at least one mip level and array layer.");

        if (description.Type == TextureType.Texture1D && (description.Height != 1 || description.Depth != 1))
            throw new ArgumentException("A 1D texture must have a height and depth of one.", nameof(description));

        if (description.Type == TextureType.Texture2D && description.Depth != 1)
            throw new ArgumentException("A 2D texture must have a depth of one.", nameof(description));

        if (description.Type == TextureType.Texture3D && description.ArrayLayers != 1)
            throw new ArgumentException("A 3D texture cannot have array layers.", nameof(description));

        if (description.Usage.HasFlag(TextureUsage.Staging) && description.Usage != TextureUsage.Staging)
            throw new ArgumentException("A staging texture cannot have additional usage flags.", nameof(description));

        var maximumDimension = Math.Max(description.Width, Math.Max(description.Height, description.Depth));
        var maximumMipLevels = 1u + (uint)Math.Floor(Math.Log2(maximumDimension));
        if (description.MipLevels > maximumMipLevels)
            throw new ArgumentOutOfRangeException(nameof(description), "The texture has too many mip levels for its dimensions.");

        var texture = new Texture(_graphicsDevice, description);
        texture.NativeResource = _graphicsDevice.Backend.CreateTexture(description);
        return texture;
    }

    public TextureView CreateTextureView(Texture target)
    {
        _graphicsDevice.ValidateResource(target, nameof(target));
        var view = new TextureView(_graphicsDevice, target);
        view.NativeResource = _graphicsDevice.Backend.CreateTextureView(target);
        return view;
    }

    internal Sampler CreateSampler(SamplerDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();
        var sampler = new Sampler(_graphicsDevice, description);
        sampler.NativeResource = _graphicsDevice.Backend.CreateSampler(description);
        return sampler;
    }

    public Shader[] CreateShaders(
        ShaderDescription vertexShader,
        ShaderDescription fragmentShader,
        CrossCompileOptions options)
    {
        _graphicsDevice.ThrowIfDisposed();
        ValidateShaderDescription(vertexShader, ShaderStages.Vertex, nameof(vertexShader));
        ValidateShaderDescription(fragmentShader, ShaderStages.Fragment, nameof(fragmentShader));

        var descriptions = new[] { vertexShader, fragmentShader };
        var nativeShaders = _graphicsDevice.Backend.CreateShaders(descriptions, options);

        if (nativeShaders is null ||
            nativeShaders.Length != descriptions.Length ||
            nativeShaders.Any(shader => shader is null))
            throw new InvalidOperationException("The graphics backend returned an invalid shader set.");

        var shaders = new Shader[descriptions.Length];
        for (var i = 0; i < descriptions.Length; i++)
        {
            shaders[i] = new Shader(_graphicsDevice, descriptions[i])
            {
                NativeResource = nativeShaders[i],
            };
        }

        return shaders;
    }

    public ResourceLayout CreateResourceLayout(ResourceLayoutDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();

        if (description.Elements is null || description.Elements.Length == 0)
            throw new ArgumentException("A resource layout must contain at least one element.", nameof(description));

        description = new ResourceLayoutDescription(description.Elements.ToArray());
        var layout = new ResourceLayout(_graphicsDevice, description);
        layout.NativeResource = _graphicsDevice.Backend.CreateResourceLayout(description);
        return layout;
    }

    public ResourceSet CreateResourceSet(ResourceSetDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();
        _graphicsDevice.ValidateResource(description.Layout, nameof(description.Layout));

        if (description.BoundResources is null)
            throw new ArgumentException("A resource set must provide bound resources.", nameof(description));

        if (description.Layout.Description.Elements.Length != description.BoundResources.Length)
            throw new ArgumentException("The resource count does not match the resource layout.", nameof(description));

        for (var index = 0; index < description.BoundResources.Length; index++)
            ValidateBoundResource(description.Layout.Description.Elements[index], description.BoundResources[index], index);

        description = new ResourceSetDescription(description.Layout, description.BoundResources.ToArray());
        var resourceSet = new ResourceSet(_graphicsDevice, description);
        resourceSet.NativeResource = _graphicsDevice.Backend.CreateResourceSet(description);
        return resourceSet;
    }

    public Pipeline CreateGraphicsPipeline(GraphicsPipelineDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();

        if (description.ShaderSet.Shaders is null || description.ShaderSet.Shaders.Length == 0)
            throw new ArgumentException("A graphics pipeline requires shaders.", nameof(description));

        if (description.ShaderSet.VertexLayouts is null)
            throw new ArgumentException("A graphics pipeline must provide vertex layouts.", nameof(description));

        if (description.ResourceLayouts is null)
            throw new ArgumentException("A graphics pipeline must provide resource layouts.", nameof(description));

        if (description.ResourceLayouts.Length > MaximumResourceLayouts)
            throw new NotSupportedException($"Graphics pipelines support at most {MaximumResourceLayouts} resource layouts.");

        if (description.ShaderSet.VertexLayouts.Length > MaximumVertexLayouts)
            throw new NotSupportedException($"Graphics pipelines support at most {MaximumVertexLayouts} vertex layouts.");

        if (description.Outputs.ColorAttachments is null || description.Outputs.ColorAttachments.Length == 0)
            throw new ArgumentException("A graphics pipeline requires at least one color output.", nameof(description));

        if ((description.DepthStencilState.DepthTestEnabled || description.DepthStencilState.DepthWriteEnabled) &&
            !description.Outputs.DepthAttachment.HasValue)
        {
            throw new ArgumentException(
                "Depth testing or writing requires a depth-stencil output.",
                nameof(description));
        }

        foreach (var shader in description.ShaderSet.Shaders)
            _graphicsDevice.ValidateResource(shader, nameof(description));

        foreach (var layout in description.ResourceLayouts)
            _graphicsDevice.ValidateResource(layout, nameof(description));

        var pipeline = new Pipeline(_graphicsDevice, description);
        pipeline.NativeResource = _graphicsDevice.Backend.CreateGraphicsPipeline(description);
        return pipeline;
    }

    public Framebuffer CreateFramebuffer(FramebufferDescription description)
    {
        _graphicsDevice.ThrowIfDisposed();

        if (description.ColorTargets is null || description.ColorTargets.Length == 0)
            throw new ArgumentException("A framebuffer requires at least one color target.", nameof(description));

        var width = description.ColorTargets[0].Target?.Width ?? 0;
        var height = description.ColorTargets[0].Target?.Height ?? 0;

        foreach (var target in description.ColorTargets)
        {
            ValidateFramebufferAttachment(target, TextureUsage.RenderTarget, width, height, nameof(description));
        }

        if (description.DepthTarget is { } depthTarget)
            ValidateFramebufferAttachment(depthTarget, TextureUsage.DepthStencil, width, height, nameof(description));

        description.ColorTargets = description.ColorTargets.ToArray();

        var output = new OutputDescription(
            description.ColorTargets.Select(target => target.Target.Format).ToArray(),
            description.DepthTarget?.Target.Format);
        var framebuffer = new Framebuffer(_graphicsDevice, description, output);
        framebuffer.NativeResource = _graphicsDevice.Backend.CreateFramebuffer(description);
        return framebuffer;
    }

    private static void ValidateShaderDescription(
        ShaderDescription description,
        ShaderStages expectedStage,
        string parameterName)
    {
        if (description.Stage != expectedStage)
            throw new ArgumentException($"The shader stage must be {expectedStage}.", parameterName);

        if (description.ShaderBytes is null || description.ShaderBytes.Length == 0)
            throw new ArgumentException("Shader source must not be empty.", parameterName);

        if (string.IsNullOrWhiteSpace(description.EntryPoint))
            throw new ArgumentException("A shader entry point is required.", parameterName);
    }

    private void ValidateBoundResource(
        ResourceLayoutElementDescription element,
        object boundResource,
        int index)
    {
        if (boundResource is not GraphicsResource resource)
            throw new ArgumentException($"Resource {index} is not a graphics resource.", nameof(boundResource));

        _graphicsDevice.ValidateResource(resource, nameof(boundResource));
        if (resource is TextureView textureView)
            _graphicsDevice.ValidateResource(textureView.Target, nameof(boundResource));

        switch (element.Kind, resource)
        {
            case (ResourceKind.UniformBuffer, DeviceBuffer buffer)
                when buffer.Usage.HasFlag(BufferUsage.UniformBuffer):
            case (ResourceKind.TextureReadOnly, Texture texture)
                when texture.Usage.HasFlag(TextureUsage.Sampled):
            case (ResourceKind.TextureReadOnly, TextureView view)
                when view.Target.Usage.HasFlag(TextureUsage.Sampled):
            case (ResourceKind.Sampler, Sampler):
                return;
            default:
                throw new ArgumentException(
                    $"Resource {index} is incompatible with layout element '{element.Name}'.",
                    nameof(boundResource));
        }
    }

    private void ValidateFramebufferAttachment(
        FramebufferAttachmentDescription attachment,
        TextureUsage requiredUsage,
        uint width,
        uint height,
        string parameterName)
    {
        _graphicsDevice.ValidateResource(attachment.Target, parameterName);

        if (!attachment.Target.Usage.HasFlag(requiredUsage))
            throw new ArgumentException($"Framebuffer attachments must have {requiredUsage} usage.", parameterName);

        var attachmentLayers = attachment.Target.Type == TextureType.Texture3D
            ? attachment.Target.Depth
            : attachment.Target.ArrayLayers;
        if (attachment.ArrayLayer >= attachmentLayers)
            throw new ArgumentOutOfRangeException(parameterName, "The framebuffer array layer is out of range.");

        if (attachment.Target.Width != width || attachment.Target.Height != height)
            throw new ArgumentException("Framebuffer attachments must have matching dimensions.", parameterName);
    }
}
