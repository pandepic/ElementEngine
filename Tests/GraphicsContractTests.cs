using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GraphicsContractTests
{
    [Fact]
    public void CommandListCopiesUpdateDataWhenRecorded()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var buffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(4, BufferUsage.UniformBuffer));
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();
        byte[] data = [1, 2, 3, 4];

        commands.Begin();
        commands.UpdateBuffer(buffer, 0, data);
        data[0] = 99;
        commands.End();
        graphicsDevice.SubmitCommands(commands);

        var update = Assert.IsType<UpdateBufferCommand>(Assert.Single(backend.SubmittedCommands));
        Assert.Equal([1, 2, 3, 4], update.Data.ToArray());
    }

    [Fact]
    public void CommandListRequiresBeginBeforeCommands()
    {
        using var commands = new CommandList();

        Assert.Throws<InvalidOperationException>(() => commands.Draw(3));
        Assert.Throws<InvalidOperationException>(commands.End);
    }

    [Fact]
    public void CommandListRejectsNestedBegin()
    {
        using var commands = new CommandList();

        commands.Begin();

        Assert.Throws<InvalidOperationException>(commands.Begin);
    }

    [Fact]
    public void CommandListBeginClearsPreviouslyRecordedCommands()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.Draw(3);
        commands.End();
        commands.Begin();
        commands.End();
        graphicsDevice.SubmitCommands(commands);

        Assert.Empty(backend.SubmittedCommands);
    }

    [Fact]
    public void DisposedCommandListCannotBeReused()
    {
        var commands = new CommandList();
        commands.Dispose();

        Assert.Throws<ObjectDisposedException>(commands.Begin);
    }

    [Fact]
    public void SubmissionRejectsResourceFromAnotherGraphicsDevice()
    {
        using var firstBackend = new TestGraphicsBackend();
        using var secondBackend = new TestGraphicsBackend();
        using var firstDevice = new GraphicsDevice(firstBackend);
        using var secondDevice = new GraphicsDevice(secondBackend);
        using var buffer = secondDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.VertexBuffer));
        using var commands = firstDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.SetVertexBuffer(0, buffer);
        commands.End();

        Assert.Throws<ArgumentException>(() => firstDevice.SubmitCommands(commands));
        Assert.Empty(firstBackend.SubmittedCommands);
    }

    [Fact]
    public void SubmissionRevalidatesTextureViewTarget()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var layout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment)));
        using var texture = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Sampled));
        using var view = graphicsDevice.ResourceFactory.CreateTextureView(texture);
        using var resourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(layout, view));
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.SetGraphicsResourceSet(0, resourceSet);
        commands.End();
        texture.Dispose();

        Assert.Throws<ObjectDisposedException>(() => graphicsDevice.SubmitCommands(commands));
    }

    [Fact]
    public void SubmissionRevalidatesFramebufferAttachments()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var target = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.RenderTarget));
        using var framebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
        {
            ColorTargets = [new FramebufferAttachmentDescription(target, 0)],
        });
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.SetFramebuffer(framebuffer);
        commands.End();
        target.Dispose();

        Assert.Throws<ObjectDisposedException>(() => graphicsDevice.SubmitCommands(commands));
    }

    [Fact]
    public void GraphicsResourceDisposalDestroysNativeResourceOnce()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var buffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.VertexBuffer));

        buffer.Dispose();
        buffer.Dispose();

        Assert.Equal(buffer, Assert.Single(backend.DestroyedResources));
    }

    [Fact]
    public void LayoutResourceSetAndFramebufferCopyCallerArrays()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var elements = new[]
        {
            new ResourceLayoutElementDescription(
                "Texture",
                ResourceKind.TextureReadOnly,
                ShaderStages.Fragment),
        };
        using var layout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(elements));
        using var firstTexture = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Sampled | TextureUsage.RenderTarget));
        using var secondTexture = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Sampled | TextureUsage.RenderTarget));
        object[] resources = [firstTexture];
        using var resourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(layout, resources));
        var attachments = new[] { new FramebufferAttachmentDescription(firstTexture, 0) };
        using var framebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
        {
            ColorTargets = attachments,
        });

        elements[0] = new ResourceLayoutElementDescription(
            "Buffer",
            ResourceKind.UniformBuffer,
            ShaderStages.Vertex);
        resources[0] = secondTexture;
        attachments[0] = new FramebufferAttachmentDescription(secondTexture, 0);

        Assert.Equal(ResourceKind.TextureReadOnly, layout.Description.Elements[0].Kind);
        Assert.Same(firstTexture, resourceSet.Description.BoundResources[0]);
        Assert.Same(firstTexture, framebuffer.Description.ColorTargets[0].Target);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void TextureRejectsEmptyDimensions(int width, int height, int depth)
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var description = new TextureDescription(
            (uint)width,
            (uint)height,
            (uint)depth,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.ResourceFactory.CreateTexture(description));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void TextureRejectsMissingSubresources(int mipLevels, int arrayLayers)
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var description = new TextureDescription(
            4,
            4,
            1,
            (uint)mipLevels,
            (uint)arrayLayers,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.ResourceFactory.CreateTexture(description));
    }

    [Theory]
    [InlineData(TextureType.Texture1D, 4, 2, 1, 1)]
    [InlineData(TextureType.Texture2D, 4, 4, 2, 1)]
    [InlineData(TextureType.Texture3D, 4, 4, 4, 2)]
    public void TextureRejectsShapeIncompatibleWithType(
        TextureType type,
        int width,
        int height,
        int depth,
        int arrayLayers)
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var description = new TextureDescription(
            (uint)width,
            (uint)height,
            (uint)depth,
            1,
            (uint)arrayLayers,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            type);

        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.ResourceFactory.CreateTexture(description));
    }

    [Fact]
    public void TextureRejectsStagingCombinedWithOtherUsage()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);

        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.ResourceFactory.CreateTexture(
                Texture2DDescription(TextureUsage.Staging | TextureUsage.Sampled)));
    }

    [Fact]
    public void TextureRejectsMoreMipLevelsThanItsDimensionsSupport()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var description = new TextureDescription(
            4,
            4,
            1,
            4,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.ResourceFactory.CreateTexture(description));
    }

    [Fact]
    public void FramebufferDerivesItsOutputDescriptionFromAttachments()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var color = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            4,
            4,
            1,
            1,
            1,
            PixelFormat.R16_G16_B16_A16_Float,
            TextureUsage.RenderTarget,
            TextureType.Texture2D));
        using var depth = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            4,
            4,
            1,
            1,
            1,
            PixelFormat.D24_UNorm_S8_UInt,
            TextureUsage.DepthStencil,
            TextureType.Texture2D));
        using var framebuffer = graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
        {
            ColorTargets = [new FramebufferAttachmentDescription(color, 0)],
            DepthTarget = new FramebufferAttachmentDescription(depth, 0),
        });

        Assert.Equal([PixelFormat.R16_G16_B16_A16_Float], framebuffer.OutputDescription.ColorAttachments);
        Assert.Equal(PixelFormat.D24_UNorm_S8_UInt, framebuffer.OutputDescription.DepthAttachment);
    }

    [Fact]
    public void FramebufferRejectsInvalidAttachments()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var sampled = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Sampled));
        using var fourByFour = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.RenderTarget));
        using var twoByTwo = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            2,
            2,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.RenderTarget,
            TextureType.Texture2D));

        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
            {
                ColorTargets = [new FramebufferAttachmentDescription(sampled, 0)],
            }));
        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
            {
                ColorTargets =
                [
                    new FramebufferAttachmentDescription(fourByFour, 0),
                    new FramebufferAttachmentDescription(twoByTwo, 0),
                ],
            }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
            {
                ColorTargets = [new FramebufferAttachmentDescription(fourByFour, 1)],
            }));
    }

    [Fact]
    public void FramebufferRejectsAttachmentFromAnotherGraphicsDevice()
    {
        using var firstBackend = new TestGraphicsBackend();
        using var secondBackend = new TestGraphicsBackend();
        using var firstDevice = new GraphicsDevice(firstBackend);
        using var secondDevice = new GraphicsDevice(secondBackend);
        using var target = secondDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.RenderTarget));

        Assert.Throws<ArgumentException>(() =>
            firstDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription
            {
                ColorTargets = [new FramebufferAttachmentDescription(target, 0)],
            }));
    }

    [Fact]
    public void TextureUpdateForwardsExactSubresourceAndCopiesData()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            8,
            8,
            1,
            4,
            2,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D));
        byte[] data = [1, 2, 3, 4];

        graphicsDevice.UpdateTexture(texture, data, 1, 1, 0, 2, 2, 1, 1, 1);
        data[0] = 99;

        var update = Assert.IsType<TextureUpdate>(backend.LastTextureUpdate);
        Assert.Same(texture, update.Texture);
        Assert.Equal([1, 2, 3, 4], update.Data);
        Assert.Equal((1u, 1u, 0u), (update.X, update.Y, update.Z));
        Assert.Equal((2u, 2u, 1u), (update.Width, update.Height, update.Depth));
        Assert.Equal(1u, update.MipLevel);
        Assert.Equal(1u, update.ArrayLayer);
    }

    [Fact]
    public void TextureUpdateRejectsInvalidSubresources()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            8,
            8,
            1,
            4,
            2,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled,
            TextureType.Texture2D));
        byte[] data = [1];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.UpdateTexture(texture, data, 0, 0, 0, 0, 1, 1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.UpdateTexture(texture, data, 0, 0, 0, 1, 1, 1, 4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.UpdateTexture(texture, data, 0, 0, 0, 1, 1, 1, 0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            graphicsDevice.UpdateTexture(texture, data, 3, 0, 0, 2, 1, 1, 1, 0));
        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.UpdateTexture(texture, Array.Empty<byte>(), 0, 0, 0, 1, 1, 1, 0, 0));

        Assert.Null(backend.LastTextureUpdate);
    }

    [Fact]
    public void MappingRequiresAStagingTexture()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var sampled = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Sampled));
        using var staging = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.Staging));

        Assert.Throws<ArgumentException>(() => graphicsDevice.Map<byte>(sampled, MapMode.Read));

        graphicsDevice.Map<byte>(staging, MapMode.Read);
        graphicsDevice.Unmap(staging);

        Assert.Equal(1, backend.MapCalls);
        Assert.Equal(1, backend.UnmapCalls);
    }

    [Fact]
    public void ResizeSkipsZeroSizedSwapchains()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);

        graphicsDevice.ResizeMainWindow(0, 10);
        graphicsDevice.ResizeMainWindow(10, 0);
        graphicsDevice.ResizeMainWindow(640, 480);

        Assert.Equal([(640u, 480u)], backend.ResizeCalls);
    }

    [Fact]
    public void ShaderCreationValidatesDescriptionsBeforeCallingBackend()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var validVertex = new ShaderDescription(ShaderStages.Vertex, [1], "main");
        var validFragment = new ShaderDescription(ShaderStages.Fragment, [1], "main");

        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateShaders(
            validFragment,
            validFragment,
            new CrossCompileOptions()));
        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateShaders(
            validVertex with { ShaderBytes = [] },
            validFragment,
            new CrossCompileOptions()));
        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateShaders(
            validVertex,
            validFragment with { EntryPoint = " " },
            new CrossCompileOptions()));
    }

    [Fact]
    public void ShaderCreationRejectsInvalidBackendResult()
    {
        using var backend = new TestGraphicsBackend
        {
            CreateShadersOverride = _ => [new object()],
        };
        using var graphicsDevice = new GraphicsDevice(backend);

        Assert.Throws<InvalidOperationException>(() => graphicsDevice.ResourceFactory.CreateShaders(
            new ShaderDescription(ShaderStages.Vertex, [1], "main"),
            new ShaderDescription(ShaderStages.Fragment, [1], "main"),
            new CrossCompileOptions()));
    }

    [Fact]
    public void ResourceSetRejectsResourcesWithIncompatibleKindsOrUsage()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var uniformLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Buffer",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex)));
        using var textureLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Texture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment)));
        using var samplerLayout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "Sampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment)));
        using var vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.VertexBuffer));
        using var renderTarget = graphicsDevice.ResourceFactory.CreateTexture(
            Texture2DDescription(TextureUsage.RenderTarget));

        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(uniformLayout, vertexBuffer)));
        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(textureLayout, renderTarget)));
        Assert.Throws<ArgumentException>(() => graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(samplerLayout, renderTarget)));
    }

    [Fact]
    public void PipelineRejectsShadersFromAnotherGraphicsDevice()
    {
        using var firstBackend = new TestGraphicsBackend();
        using var secondBackend = new TestGraphicsBackend();
        using var firstDevice = new GraphicsDevice(firstBackend);
        using var secondDevice = new GraphicsDevice(secondBackend);
        var shaders = secondDevice.ResourceFactory.CreateShaders(
            new ShaderDescription(ShaderStages.Vertex, [1], "main"),
            new ShaderDescription(ShaderStages.Fragment, [1], "main"),
            new CrossCompileOptions());

        try
        {
            var description = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription { CullMode = FaceCullMode.None },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ShaderSet = new ShaderSetDescription([], shaders),
                ResourceLayouts = [],
                Outputs = OutputDescription.Default,
            };

            Assert.Throws<ArgumentException>(() =>
                firstDevice.ResourceFactory.CreateGraphicsPipeline(description));
        }
        finally
        {
            foreach (var shader in shaders)
                shader.Dispose();
        }
    }

    private static TextureDescription Texture2DDescription(TextureUsage usage)
    {
        return new TextureDescription(
            4,
            4,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            usage,
            TextureType.Texture2D);
    }
}
