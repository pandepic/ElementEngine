using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class InputManagerTests : IDisposable
{
    private readonly RecordingKeyboardHandler _handler = new();
    private readonly GameTimer _timer = new();

    public InputManagerTests()
    {
        InputManager.KeysDown.Clear();
        InputManager.AddKeyboardHandler(_handler);
        _timer.SetFrameTime(TimeSpan.FromSeconds(1d / 60d));
    }

    [Fact]
    public void KeyboardTransitionsUpdateStateAndDispatchHandlers()
    {
        var pressed = new InputSnapshot();
        pressed.KeyEvents.Add(new KeyEvent(Key.W, true));
        InputManager.Update(pressed, _timer);

        var repeated = new InputSnapshot();
        repeated.KeyEvents.Add(new KeyEvent(Key.W, true, true));
        InputManager.Update(repeated, _timer);

        var released = new InputSnapshot();
        released.KeyEvents.Add(new KeyEvent(Key.W, false));
        InputManager.Update(released, _timer);

        Assert.Equal(["down:W", "pressed:W", "down:W", "released:W"], _handler.Events);
        Assert.False(InputManager.IsKeyDown(Key.W));
    }

    public void Dispose()
    {
        InputManager.RemoveKeyboardHandler(_handler);
        InputManager.KeysDown.Clear();
    }

    private sealed class RecordingKeyboardHandler : IKeyboardHandler
    {
        public int KeyboardPriority { get; set; }
        public List<string> Events { get; } = [];

        public void HandleKeyPressed(Key key, GameTimer gameTimer) => Events.Add($"pressed:{key}");
        public void HandleKeyReleased(Key key, GameTimer gameTimer) => Events.Add($"released:{key}");
        public void HandleKeyDown(Key key, GameTimer gameTimer) => Events.Add($"down:{key}");
    }
}
