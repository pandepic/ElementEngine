using ElementEngine;
using System.Runtime.InteropServices;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GraphicsDeviceTests
{
    [Fact]
    public void DefaultBackendMatchesOperatingSystem()
    {
        var expected = OperatingSystem.IsWindows()
            ? GraphicsBackend.DirectX12
            : OperatingSystem.IsLinux()
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.Metal;

        Assert.Equal(expected, GraphicsBackendSelector.GetDefault());
    }

    [Fact]
    public void ImmediateBufferUpdateForwardsCopiedBytes()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var buffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.UniformBuffer));

        graphicsDevice.UpdateBuffer(buffer, 4, 0x12345678u);

        Assert.Equal(buffer, backend.LastUpdatedBuffer);
        Assert.Equal(4u, backend.LastUpdatedBufferOffset);
        Assert.Equal(BitConverter.GetBytes(0x12345678u), backend.LastUpdatedBufferData);
    }

    [Fact]
    public void ImmediateBufferUpdateRejectsOverflow()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var buffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(4, BufferUsage.UniformBuffer));

        Assert.Throws<ArgumentOutOfRangeException>(() => graphicsDevice.UpdateBuffer(buffer, 2, 0x12345678u));
    }

    [Fact]
    public void CommandListPreservesRenderingCommandOrder()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commands.SetViewport(0, new Viewport(0, 0, 640, 480, 0, 1));
        commands.ClearColorTarget(0, RgbaFloat.Black);
        commands.End();
        graphicsDevice.SubmitCommands(commands);

        Assert.Collection(
            backend.SubmittedCommands,
            command => Assert.IsType<SetFramebufferCommand>(command),
            command => Assert.IsType<SetViewportCommand>(command),
            command => Assert.IsType<ClearColorTargetCommand>(command));
    }

    [Fact]
    public void CommandListRecordsDepthStencilClear()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        commands.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commands.ClearDepthStencil(1f, 0xFF);
        commands.End();
        graphicsDevice.SubmitCommands(commands);

        Assert.Collection(
            backend.SubmittedCommands,
            command => Assert.IsType<SetFramebufferCommand>(command),
            command =>
            {
                var clear = Assert.IsType<ClearDepthStencilCommand>(command);
                Assert.Equal(1f, clear.Depth);
                Assert.Equal(0xFF, clear.Stencil);
            });
    }

    [Fact]
    public void CommandListRejectsOutOfRangeClearDepth()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.ClearDepthStencil(1.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.ClearDepthStencil(float.NaN));
    }

    [Fact]
    public void CommandListRejectsSecondaryViewportAndScissorIndices()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();
        Assert.Throws<NotSupportedException>(() => commands.SetViewport(1, new Viewport(0, 0, 640, 480, 0, 1)));
        Assert.Throws<NotSupportedException>(() => commands.SetScissorRect(1, 0, 0, 10, 10));
        Assert.Throws<NotSupportedException>(() => commands.SetFullScissorRect(1));
    }

    [Fact]
    public void CommandListMustEndBeforeSubmission()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();

        commands.Begin();

        Assert.Throws<InvalidOperationException>(() => graphicsDevice.SubmitCommands(commands));
    }

    [Fact]
    public void ResourceSetMustMatchItsLayout()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var layout = graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        using var texture = graphicsDevice.ResourceFactory.CreateTexture(
            new TextureDescription(1, 1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));

        Assert.Throws<ArgumentException>(() =>
            graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, texture)));
    }

    [Fact]
    public void ResourceSetRejectsResourceFromAnotherGraphicsDevice()
    {
        using var firstBackend = new RecordingBackend();
        using var secondBackend = new RecordingBackend();
        using var firstDevice = new GraphicsDevice(firstBackend);
        using var secondDevice = new GraphicsDevice(secondBackend);
        using var layout = firstDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));
        using var texture = secondDevice.ResourceFactory.CreateTexture(
            new TextureDescription(
                1,
                1,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D));

        Assert.Throws<ArgumentException>(() =>
            firstDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(layout, texture)));
    }

    [Fact]
    public void SubmissionRejectsResourceDisposedAfterRecording()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var commands = graphicsDevice.ResourceFactory.CreateCommandList();
        var buffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.VertexBuffer));

        commands.Begin();
        commands.SetVertexBuffer(0, buffer);
        commands.End();
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => graphicsDevice.SubmitCommands(commands));
    }

    [Fact]
    public void TextureWrapperUsesWrappedTextureDeviceAndFormat()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var texture = graphicsDevice.ResourceFactory.CreateTexture(
            new TextureDescription(
                2,
                2,
                1,
                1,
                1,
                PixelFormat.R16_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D));
        using var wrapper = new Texture2D(texture);

        Assert.Same(graphicsDevice, wrapper.GraphicsDevice);
        Assert.Equal(2, wrapper.BytesPerPixel);
    }

    [Fact]
    public void DisposeReleasesBackendWhenIdleWaitFails()
    {
        var expected = new InvalidOperationException("wait failed");
        var backend = new RecordingBackend();
        var graphicsDevice = new GraphicsDevice(backend);
        backend.WaitForIdleException = expected;

        var actual = Assert.Throws<InvalidOperationException>(graphicsDevice.Dispose);

        Assert.Same(expected, actual);
        Assert.True(backend.DisposeCalled);
        Assert.Throws<ObjectDisposedException>(() =>
            graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(4, BufferUsage.UniformBuffer)));
        graphicsDevice.Dispose();
    }

    [Fact]
    public void SpritePipelineUsesItsGraphicsDeviceWithoutRequiringDepth()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        using var shader = new SimpleShader(
            graphicsDevice,
            "vertex shader",
            "fragment shader",
            new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2)));
        using var pipeline = new SimplePipeline(
            graphicsDevice,
            shader,
            OutputDescription.Default,
            BlendStateDescription.SingleAlphaBlend,
            FaceCullMode.None);
        pipeline.CanDisposeShader = false;

        pipeline.SetupForSpritebatch(SamplerType.Point);
        pipeline.GeneratePipeline();

        Assert.False(backend.LastPipelineDescription.DepthStencilState.DepthTestEnabled);
        Assert.False(backend.LastPipelineDescription.DepthStencilState.DepthWriteEnabled);
        Assert.Equal(ComparisonKind.Always, backend.LastPipelineDescription.DepthStencilState.DepthComparison);
    }

    [Fact]
    public void PipelineRejectsDepthStateWithoutDepthOutput()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var shaders = CreateTestShaders(graphicsDevice);

        try
        {
            var description = CreateTestPipelineDescription(shaders);
            description.DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual);

            Assert.Throws<ArgumentException>(() =>
                graphicsDevice.ResourceFactory.CreateGraphicsPipeline(description));
        }
        finally
        {
            foreach (var shader in shaders)
                shader.Dispose();
        }
    }

    [Fact]
    public void PipelineRejectsMoreThanSixteenResourceLayouts()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var shaders = CreateTestShaders(graphicsDevice);
        var layouts = Enumerable.Range(0, 17)
            .Select(index => graphicsDevice.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        $"Buffer{index}",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Vertex))))
            .ToArray();

        try
        {
            var description = CreateTestPipelineDescription(shaders);
            description.ResourceLayouts = layouts;

            Assert.Throws<NotSupportedException>(() =>
                graphicsDevice.ResourceFactory.CreateGraphicsPipeline(description));
        }
        finally
        {
            foreach (var layout in layouts)
                layout.Dispose();
            foreach (var shader in shaders)
                shader.Dispose();
        }
    }

    [Fact]
    public void PipelineRejectsMoreThanEightVertexLayouts()
    {
        using var backend = new RecordingBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var shaders = CreateTestShaders(graphicsDevice);

        try
        {
            var description = CreateTestPipelineDescription(shaders);
            description.ShaderSet = new ShaderSetDescription(
                Enumerable.Range(0, 9).Select(_ => new VertexLayoutDescription()).ToArray(),
                shaders);

            Assert.Throws<NotSupportedException>(() =>
                graphicsDevice.ResourceFactory.CreateGraphicsPipeline(description));
        }
        finally
        {
            foreach (var shader in shaders)
                shader.Dispose();
        }
    }

    private static Shader[] CreateTestShaders(GraphicsDevice graphicsDevice)
    {
        return graphicsDevice.ResourceFactory.CreateShaders(
            new ShaderDescription(ShaderStages.Vertex, [1], "main"),
            new ShaderDescription(ShaderStages.Fragment, [1], "main"),
            new CrossCompileOptions());
    }

    private static GraphicsPipelineDescription CreateTestPipelineDescription(Shader[] shaders)
    {
        return new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            RasterizerState = new RasterizerStateDescription
            {
                CullMode = FaceCullMode.None,
                DepthClipEnabled = true,
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ShaderSet = new ShaderSetDescription([], shaders),
            ResourceLayouts = [],
            Outputs = OutputDescription.Default,
        };
    }

    private sealed class RecordingBackend : IGraphicsBackend
    {
        private readonly List<GCHandle> _maps = [];

        public GraphicsBackend BackendType => GraphicsBackend.Metal;
        public bool IsUvOriginTopLeft => true;
        public OutputDescription SwapchainOutputDescription => OutputDescription.Default;
        public DeviceBuffer? LastUpdatedBuffer { get; private set; }
        public uint LastUpdatedBufferOffset { get; private set; }
        public byte[] LastUpdatedBufferData { get; private set; } = [];
        public IReadOnlyList<GraphicsCommand> SubmittedCommands { get; private set; } = [];
        public GraphicsPipelineDescription LastPipelineDescription { get; private set; }
        public Exception? WaitForIdleException { get; set; }
        public bool DisposeCalled { get; private set; }

        public object CreateBuffer(BufferDescription description) => new object();
        public object CreateTexture(TextureDescription description) => new object();
        public object CreateTextureView(Texture texture) => new object();
        public object CreateSampler(SamplerDescription description) => new object();
        public object[] CreateShaders(ShaderDescription[] descriptions, CrossCompileOptions options)
            => descriptions.Select(_ => new object()).ToArray();
        public object CreateResourceLayout(ResourceLayoutDescription description) => new object();
        public object CreateResourceSet(ResourceSetDescription description) => new object();
        public object CreateGraphicsPipeline(GraphicsPipelineDescription description)
        {
            LastPipelineDescription = description;
            return new object();
        }
        public object CreateFramebuffer(FramebufferDescription description) => new object();
        public void DestroyResource(GraphicsResource resource) { }

        public void UpdateBuffer(DeviceBuffer buffer, uint offset, ReadOnlySpan<byte> data)
        {
            LastUpdatedBuffer = buffer;
            LastUpdatedBufferOffset = offset;
            LastUpdatedBufferData = data.ToArray();
        }

        public void UpdateTexture(
            Texture texture,
            ReadOnlySpan<byte> data,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
        }

        public MappedResource Map(Texture texture, MapMode mode)
        {
            var data = new byte[checked((int)(texture.Width * texture.Height * 4))];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _maps.Add(handle);
            return new MappedResource(handle.AddrOfPinnedObject(), (uint)data.Length, texture.Width * 4, (uint)data.Length);
        }

        public void Unmap(Texture texture)
        {
            if (_maps.Count == 0)
                return;

            _maps[^1].Free();
            _maps.RemoveAt(_maps.Count - 1);
        }

        public void Submit(IReadOnlyList<GraphicsCommand> commands) => SubmittedCommands = commands.ToArray();
        public void Present() { }
        public void ResizeSwapchain(uint width, uint height) { }
        public void WaitForIdle()
        {
            if (WaitForIdleException is not null)
                throw WaitForIdleException;
        }

        public void Dispose()
        {
            DisposeCalled = true;
            foreach (var map in _maps)
            {
                if (map.IsAllocated)
                    map.Free();
            }

            _maps.Clear();
        }
    }
}
