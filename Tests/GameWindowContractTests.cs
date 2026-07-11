using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GameWindowContractTests
{
    [Fact]
    public void DefaultNativeHandlesUseTheWindowHandle()
    {
        using var window = new TestGameWindow
        {
            Handle = 42,
        };

        Assert.Equal(0, window.NativeDisplayHandle);
        Assert.Equal(42, window.NativeViewHandle);
    }

    [Fact]
    public void ScaleFactorUsesFramebufferAndLogicalWidths()
    {
        using var window = new TestGameWindow
        {
            LogicalWidth = 800,
            LogicalHeight = 600,
            PixelWidth = 1600,
            PixelHeight = 1200,
        };

        Assert.Equal(2f, window.ScaleFactor);

        window.LogicalWidth = 0;
        Assert.Equal(1f, window.ScaleFactor);
    }

    [Fact]
    public void WindowLifecycleEventsAreForwarded()
    {
        using var window = new TestGameWindow();
        var events = new List<string>();
        window.Resized += () => events.Add("resized");
        window.Moved += () => events.Add("moved");
        window.FocusChanged += () => events.Add("focus");
        window.ScaleFactorChanged += () => events.Add("scale");
        window.Closing += () => events.Add("closing");

        window.RaiseAllEvents();

        Assert.Equal(["resized", "moved", "focus", "scale", "closing"], events);
    }

    [Fact]
    public void DisplayModeCompatibilityAliasesMatchNamedProperties()
    {
        var mode = new DisplayMode(2560, 1440, 165);

        Assert.Equal(mode.Width, mode.w);
        Assert.Equal(mode.Height, mode.h);
        Assert.Equal(mode.RefreshRate, mode.refresh_rate);
    }

    private sealed class TestGameWindow : GameWindow
    {
        public nint Handle { get; set; }
        public int LogicalWidth { get; set; } = 800;
        public int LogicalHeight { get; set; } = 600;
        public int PixelWidth { get; set; } = 800;
        public int PixelHeight { get; set; } = 600;

        public override NativeWindowKind NativeKind => NativeWindowKind.Cocoa;
        public override nint NativeHandle => Handle;
        public override bool Exists => true;
        public override bool Focused => true;
        public override int X => 0;
        public override int Y => 0;
        public override int Width => LogicalWidth;
        public override int Height => LogicalHeight;
        public override int FramebufferWidth => PixelWidth;
        public override int FramebufferHeight => PixelHeight;
        public override string Title { get; set; } = "Test";
        public override bool Resizable { get; set; } = true;
        public override bool CursorVisible { get; set; } = true;
        public override WindowState State { get; set; } = WindowState.Normal;

        public override InputSnapshot PumpEvents() => new();
        public override DisplayMode GetCurrentDisplayMode(int displayIndex = 0) => new(1920, 1080, 60);
        public override void Dispose() { }

        public void RaiseAllEvents()
        {
            OnResized();
            OnMoved();
            OnFocusChanged();
            OnScaleFactorChanged();
            OnClosing();
        }
    }
}
