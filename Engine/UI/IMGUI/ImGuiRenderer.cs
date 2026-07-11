using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ElementEngine.UI;

internal enum ImGuiNativePlatform
{
    Windows,
    Linux,
    MacOS,
    Unsupported,
}

public sealed class ImGuiRenderer : IDisposable
{
    private const int FontBindingId = 1;
    private static readonly object NativeResolverLock = new();
    private static bool _nativeResolverConfigured;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly nint _context;
    private readonly Dictionary<Texture, TextureBinding> _textureBindings = new();
    private readonly Dictionary<nint, TextureBinding> _bindings = new();
    private readonly bool[] _mouseButtons = new bool[5];
    private readonly HashSet<Key> _pressedKeys = [];
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private readonly DeviceBuffer _projectionBuffer;
    private readonly ResourceLayout _projectionLayout;
    private readonly ResourceLayout _textureLayout;
    private readonly ResourceSet _projectionSet;
    private readonly Pipeline _pipeline;
    private Texture _fontTexture;
    private ResourceSet _fontTextureSet;
    private int _windowWidth;
    private int _windowHeight;
    private int _nextBindingId = 100;
    private bool _frameBegun;
    private bool _disposed;

    public ImGuiRenderer(
        GraphicsDevice graphicsDevice,
        OutputDescription outputDescription,
        int width,
        int height)
    {
        _graphicsDevice = graphicsDevice;
        _windowWidth = width;
        _windowHeight = height;
        EnsureSupportedNativePlatform();
        EnsureNativeLibraryResolver();
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

        var factory = graphicsDevice.ResourceFactory;
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10_000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2_000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _projectionLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ProjectionMatrixBuffer",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex)));
        _textureLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "MainTexture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment),
                new ResourceLayoutElementDescription(
                    "MainSampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment)));
        _projectionSet = factory.CreateResourceSet(new ResourceSetDescription(_projectionLayout, _projectionBuffer));

        var shaders = factory.CreateShaders(
            new ShaderDescription(ShaderStages.Vertex, System.Text.Encoding.UTF8.GetBytes(VertexShader), "main"),
            new ShaderDescription(ShaderStages.Fragment, System.Text.Encoding.UTF8.GetBytes(FragmentShader), "main"),
            new CrossCompileOptions(fixClipSpaceZ: true, invertVertexOutputY: false));

        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            RasterizerState = new RasterizerStateDescription
            {
                CullMode = FaceCullMode.None,
                DepthClipEnabled = true,
                ScissorTestEnabled = true,
            },
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ShaderSet = new ShaderSetDescription(
                [new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))],
                shaders),
            ResourceLayouts = [_projectionLayout, _textureLayout],
            Outputs = outputDescription,
        };
        _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

        foreach (var shader in shaders)
            shader.Dispose();

        RecreateFontTexture();
        SetPerFrameData(1f / 60f);
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void Update(float deltaSeconds, InputSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        ImGui.SetCurrentContext(_context);

        if (_frameBegun)
            ImGui.Render();

        SetPerFrameData(deltaSeconds);
        UpdateInput(snapshot);
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void Render(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_frameBegun)
            return;

        ImGui.SetCurrentContext(_context);
        ImGui.Render();
        _frameBegun = false;
        RenderDrawData(ImGui.GetDrawData(), commandList);
    }

    public nint GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (_textureBindings.TryGetValue(texture, out var existing))
            return existing.Id;

        var binding = new TextureBinding(
            _nextBindingId++,
            factory.CreateResourceSet(
                new ResourceSetDescription(_textureLayout, texture, _graphicsDevice.PointSampler)));
        _textureBindings.Add(texture, binding);
        _bindings.Add(binding.Id, binding);
        return binding.Id;
    }

    public void RemoveImGuiBinding(Texture texture)
    {
        if (texture is null || !_textureBindings.Remove(texture, out var binding))
            return;

        _bindings.Remove(binding.Id);
        binding.ResourceSet.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var binding in _textureBindings.Values)
            binding.ResourceSet.Dispose();

        _textureBindings.Clear();
        _bindings.Clear();
        _fontTextureSet.Dispose();
        _fontTexture.Dispose();
        _pipeline.Dispose();
        _projectionSet.Dispose();
        _textureLayout.Dispose();
        _projectionLayout.Dispose();
        _projectionBuffer.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();

        ImGui.SetCurrentContext(_context);
        ImGui.DestroyContext(_context);
        _disposed = true;
    }

    private unsafe void RecreateFontTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out var bytesPerPixel);

        _fontTextureSet?.Dispose();
        _fontTexture?.Dispose();
        _fontTexture = _graphicsDevice.ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)width,
                (uint)height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D));
        _graphicsDevice.UpdateTexture(
            _fontTexture,
            (nint)pixels,
            checked((uint)(width * height * bytesPerPixel)),
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            1,
            0,
            0);
        _fontTextureSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_textureLayout, _fontTexture, _graphicsDevice.PointSampler));

        io.Fonts.SetTexID((nint)FontBindingId);
        io.Fonts.ClearTexData();
    }

    private void SetPerFrameData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = new Vector2(
            ElementGlobals.Window?.ScaleFactor ?? 1f,
            ElementGlobals.Window?.ScaleFactor ?? 1f);
        io.DeltaTime = MathF.Max(deltaSeconds, 1f / 10_000f);
    }

    private void UpdateInput(InputSnapshot snapshot)
    {
        foreach (var mouseEvent in snapshot.MouseEvents)
        {
            var buttonIndex = mouseEvent.MouseButton switch
            {
                MouseButton.Left => 0,
                MouseButton.Right => 1,
                MouseButton.Middle => 2,
                MouseButton.Button1 => 3,
                MouseButton.Button2 => 4,
                _ => -1,
            };

            if (buttonIndex >= 0)
                _mouseButtons[buttonIndex] = mouseEvent.Down;
        }

        var io = ImGui.GetIO();
        io.AddMousePosEvent(snapshot.MousePosition.X, snapshot.MousePosition.Y);
        io.AddMouseButtonEvent(0, _mouseButtons[0]);
        io.AddMouseButtonEvent(1, _mouseButtons[1]);
        io.AddMouseButtonEvent(2, _mouseButtons[2]);
        io.AddMouseButtonEvent(3, _mouseButtons[3]);
        io.AddMouseButtonEvent(4, _mouseButtons[4]);
        io.AddMouseWheelEvent(0f, snapshot.WheelDelta);

        foreach (var character in snapshot.KeyCharPresses)
            io.AddInputCharacter(character);

        foreach (var keyEvent in snapshot.KeyEvents)
        {
            if (keyEvent.Down)
                _pressedKeys.Add(keyEvent.Key);
            else
                _pressedKeys.Remove(keyEvent.Key);

            if (TryMapKey(keyEvent.Key, out var imGuiKey))
                io.AddKeyEvent(imGuiKey, keyEvent.Down);

            if (TryGetModifierKey(keyEvent.Key, out var modifierKey))
                io.AddKeyEvent(modifierKey, IsModifierPressed(modifierKey));
        }
    }

    private unsafe void RenderDrawData(ImDrawDataPtr drawData, CommandList commandList)
    {
        if (drawData.CmdListsCount == 0)
            return;

        EnsureBufferCapacity(
            checked((uint)(drawData.TotalVtxCount * sizeof(ImDrawVert))),
            checked((uint)(drawData.TotalIdxCount * sizeof(ushort))));

        uint vertexOffset = 0;
        uint indexOffset = 0;
        for (var listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            var drawList = drawData.CmdListsRange[listIndex];
            commandList.UpdateBuffer(
                _vertexBuffer,
                vertexOffset * (uint)sizeof(ImDrawVert),
                (nint)drawList.VtxBuffer.Data,
                checked((uint)(drawList.VtxBuffer.Size * sizeof(ImDrawVert))));
            commandList.UpdateBuffer(
                _indexBuffer,
                indexOffset * sizeof(ushort),
                (nint)drawList.IdxBuffer.Data,
                checked((uint)(drawList.IdxBuffer.Size * sizeof(ushort))));
            vertexOffset += (uint)drawList.VtxBuffer.Size;
            indexOffset += (uint)drawList.IdxBuffer.Size;
        }

        var left = drawData.DisplayPos.X;
        var right = drawData.DisplayPos.X + drawData.DisplaySize.X;
        var top = drawData.DisplayPos.Y;
        var bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
        var projection = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, -1f, 1f);
        commandList.UpdateBuffer(_projectionBuffer, 0, projection);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _projectionSet);

        var clipOffset = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;
        var globalVertexOffset = 0;
        var globalIndexOffset = 0;

        for (var listIndex = 0; listIndex < drawData.CmdListsCount; listIndex++)
        {
            var drawList = drawData.CmdListsRange[listIndex];
            for (var commandIndex = 0; commandIndex < drawList.CmdBuffer.Size; commandIndex++)
            {
                var drawCommand = drawList.CmdBuffer[commandIndex];
                if (drawCommand.UserCallback != 0)
                    continue;

                var clipMin = new Vector2(
                    (drawCommand.ClipRect.X - clipOffset.X) * clipScale.X,
                    (drawCommand.ClipRect.Y - clipOffset.Y) * clipScale.Y);
                var clipMax = new Vector2(
                    (drawCommand.ClipRect.Z - clipOffset.X) * clipScale.X,
                    (drawCommand.ClipRect.W - clipOffset.Y) * clipScale.Y);
                clipMin = Vector2.Max(clipMin, Vector2.Zero);
                clipMax = Vector2.Min(
                    clipMax,
                    new Vector2(ElementGlobals.Window.FramebufferWidth, ElementGlobals.Window.FramebufferHeight));

                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                commandList.SetGraphicsResourceSet(1, GetTextureSet(drawCommand.TextureId));
                commandList.SetScissorRect(
                    0,
                    (uint)clipMin.X,
                    (uint)clipMin.Y,
                    (uint)(clipMax.X - clipMin.X),
                    (uint)(clipMax.Y - clipMin.Y));
                commandList.DrawIndexed(
                    drawCommand.ElemCount,
                    1,
                    drawCommand.IdxOffset + (uint)globalIndexOffset,
                    checked((int)drawCommand.VtxOffset + globalVertexOffset),
                    0);
            }

            globalIndexOffset += drawList.IdxBuffer.Size;
            globalVertexOffset += drawList.VtxBuffer.Size;
        }
    }

    private void EnsureBufferCapacity(uint vertexBytes, uint indexBytes)
    {
        if (vertexBytes > _vertexBuffer.SizeInBytes)
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(GrowCapacity(vertexBytes), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        if (indexBytes > _indexBuffer.SizeInBytes)
        {
            _indexBuffer.Dispose();
            _indexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(GrowCapacity(indexBytes), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }
    }

    private ResourceSet GetTextureSet(nint bindingId)
    {
        if (bindingId == FontBindingId)
            return _fontTextureSet;

        if (_bindings.TryGetValue(bindingId, out var binding))
            return binding.ResourceSet;

        throw new InvalidOperationException($"No ImGui texture is registered with binding {bindingId}.");
    }

    private static uint GrowCapacity(uint required) => checked(Math.Max(required + required / 2, required + 256));

    internal static bool IsNativePlatformSupported(ImGuiNativePlatform platform, Architecture architecture)
    {
        return platform switch
        {
            ImGuiNativePlatform.Windows => architecture is Architecture.X86 or Architecture.X64,
            ImGuiNativePlatform.Linux => architecture == Architecture.X64,
            ImGuiNativePlatform.MacOS => architecture is Architecture.X64 or Architecture.Arm64,
            _ => false,
        };
    }

    private static void EnsureSupportedNativePlatform()
    {
        var platform = OperatingSystem.IsWindows()
            ? ImGuiNativePlatform.Windows
            : OperatingSystem.IsLinux()
                ? ImGuiNativePlatform.Linux
                : OperatingSystem.IsMacOS()
                    ? ImGuiNativePlatform.MacOS
                    : ImGuiNativePlatform.Unsupported;
        if (IsNativePlatformSupported(platform, RuntimeInformation.ProcessArchitecture))
            return;

        throw new PlatformNotSupportedException(
            $"The bundled ImGui native runtime does not support {platform} {RuntimeInformation.ProcessArchitecture}. " +
            "Supported ImGui targets are Windows x86/x64, Linux x64, and macOS x64/ARM64.");
    }

    private static void EnsureNativeLibraryResolver()
    {
        if (!OperatingSystem.IsMacOS() || _nativeResolverConfigured)
            return;

        lock (NativeResolverLock)
        {
            if (_nativeResolverConfigured)
                return;

            try
            {
                NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, ResolveImGuiNativeLibrary);
            }
            catch (InvalidOperationException)
            {
                // The host may already provide a resolver for the ImGui assembly.
            }

            _nativeResolverConfigured = true;
        }
    }

    private static nint ResolveImGuiNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (libraryName is not "cimgui" and not "libcimgui")
            return 0;

        var universalLibrary = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            "osx-universal",
            "native",
            "libcimgui.dylib");
        return NativeLibrary.TryLoad(universalLibrary, out var handle) ? handle : 0;
    }

    private static bool TryMapKey(Key key, out ImGuiKey result)
    {
        if (key is >= Key.F1 and <= Key.F12)
        {
            result = (ImGuiKey)((int)ImGuiKey.F1 + (int)key - (int)Key.F1);
            return true;
        }

        if (key is >= Key.Keypad0 and <= Key.Keypad9)
        {
            result = (ImGuiKey)((int)ImGuiKey.Keypad0 + (int)key - (int)Key.Keypad0);
            return true;
        }

        if (key is >= Key.A and <= Key.Z)
        {
            result = (ImGuiKey)((int)ImGuiKey.A + (int)key - (int)Key.A);
            return true;
        }

        if (key is >= Key.Number0 and <= Key.Number9)
        {
            result = (ImGuiKey)((int)ImGuiKey._0 + (int)key - (int)Key.Number0);
            return true;
        }

        result = key switch
        {
            Key.ShiftLeft => ImGuiKey.LeftShift,
            Key.ShiftRight => ImGuiKey.RightShift,
            Key.ControlLeft => ImGuiKey.LeftCtrl,
            Key.ControlRight => ImGuiKey.RightCtrl,
            Key.AltLeft => ImGuiKey.LeftAlt,
            Key.AltRight => ImGuiKey.RightAlt,
            Key.WinLeft => ImGuiKey.LeftSuper,
            Key.WinRight => ImGuiKey.RightSuper,
            Key.Menu => ImGuiKey.Menu,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Enter => ImGuiKey.Enter,
            Key.Escape => ImGuiKey.Escape,
            Key.Space => ImGuiKey.Space,
            Key.Tab => ImGuiKey.Tab,
            Key.BackSpace => ImGuiKey.Backspace,
            Key.Insert => ImGuiKey.Insert,
            Key.Delete => ImGuiKey.Delete,
            Key.PageUp => ImGuiKey.PageUp,
            Key.PageDown => ImGuiKey.PageDown,
            Key.Home => ImGuiKey.Home,
            Key.End => ImGuiKey.End,
            Key.CapsLock => ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiKey.ScrollLock,
            Key.PrintScreen => ImGuiKey.PrintScreen,
            Key.Pause => ImGuiKey.Pause,
            Key.NumLock => ImGuiKey.NumLock,
            Key.KeypadDivide => ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiKey.KeypadAdd,
            Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
            Key.KeypadEnter => ImGuiKey.KeypadEnter,
            Key.Tilde => ImGuiKey.GraveAccent,
            Key.Minus => ImGuiKey.Minus,
            Key.Plus => ImGuiKey.Equal,
            Key.BracketLeft => ImGuiKey.LeftBracket,
            Key.BracketRight => ImGuiKey.RightBracket,
            Key.Semicolon => ImGuiKey.Semicolon,
            Key.Quote => ImGuiKey.Apostrophe,
            Key.Comma => ImGuiKey.Comma,
            Key.Period => ImGuiKey.Period,
            Key.Slash => ImGuiKey.Slash,
            Key.BackSlash or Key.NonUSBackSlash => ImGuiKey.Backslash,
            _ => ImGuiKey.None,
        };
        return result != ImGuiKey.None;
    }

    private static bool TryGetModifierKey(Key key, out ImGuiKey modifierKey)
    {
        modifierKey = key switch
        {
            Key.ShiftLeft or Key.ShiftRight => ImGuiKey.ModShift,
            Key.ControlLeft or Key.ControlRight => ImGuiKey.ModCtrl,
            Key.AltLeft or Key.AltRight => ImGuiKey.ModAlt,
            Key.WinLeft or Key.WinRight => ImGuiKey.ModSuper,
            _ => ImGuiKey.None,
        };
        return modifierKey != ImGuiKey.None;
    }

    private bool IsModifierPressed(ImGuiKey modifierKey) => modifierKey switch
    {
        ImGuiKey.ModShift => _pressedKeys.Contains(Key.ShiftLeft) || _pressedKeys.Contains(Key.ShiftRight),
        ImGuiKey.ModCtrl => _pressedKeys.Contains(Key.ControlLeft) || _pressedKeys.Contains(Key.ControlRight),
        ImGuiKey.ModAlt => _pressedKeys.Contains(Key.AltLeft) || _pressedKeys.Contains(Key.AltRight),
        ImGuiKey.ModSuper => _pressedKeys.Contains(Key.WinLeft) || _pressedKeys.Contains(Key.WinRight),
        _ => false,
    };

    private readonly record struct TextureBinding(nint Id, ResourceSet ResourceSet);

    private const string VertexShader = """
        #version 450
        layout(set = 0, binding = 0) uniform ProjectionMatrixBuffer { mat4 projection; };
        layout(location = 0) in vec2 in_position;
        layout(location = 1) in vec2 in_texCoord;
        layout(location = 2) in vec4 in_color;
        layout(location = 0) out vec2 fs_texCoord;
        layout(location = 1) out vec4 fs_color;
        void main()
        {
            gl_Position = projection * vec4(in_position, 0.0, 1.0);
            fs_texCoord = in_texCoord;
            fs_color = in_color;
        }
        """;

    private const string FragmentShader = """
        #version 450
        layout(set = 1, binding = 0) uniform texture2D MainTexture;
        layout(set = 1, binding = 1) uniform sampler MainSampler;
        layout(location = 0) in vec2 fs_texCoord;
        layout(location = 1) in vec4 fs_color;
        layout(location = 0) out vec4 out_color;
        void main()
        {
            out_color = fs_color * texture(sampler2D(MainTexture, MainSampler), fs_texCoord);
        }
        """;
}
