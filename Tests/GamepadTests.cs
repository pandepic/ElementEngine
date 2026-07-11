using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GamepadTests
{
    [Fact]
    public void PollTracksButtonPressHoldAndRelease()
    {
        using var nativeGamepad = new FakeNativeGamepad();
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        nativeGamepad.SetState(
            new Dictionary<GamepadAxis, float>(),
            new HashSet<GamepadButtonType> { GamepadButtonType.A });
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.A, true, false),
            Assert.Single(gamepad.ButtonEvents));

        gamepad.ButtonEvents.Clear();
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.A, true, true),
            Assert.Single(gamepad.ButtonEvents));

        gamepad.ButtonEvents.Clear();
        nativeGamepad.SetState(
            new Dictionary<GamepadAxis, float>(),
            new HashSet<GamepadButtonType>());
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.A, false, true),
            Assert.Single(gamepad.ButtonEvents));
        Assert.False(gamepad.IsButtonPressed(GamepadButtonType.A));
    }

    [Fact]
    public void PollAppliesDeadzoneOnlyToAxisEvents()
    {
        using var nativeGamepad = new FakeNativeGamepad();
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        nativeGamepad.SetState(new Dictionary<GamepadAxis, float>
        {
            [GamepadAxis.LeftX] = 0.1f,
        }, new HashSet<GamepadButtonType>());
        Assert.True(gamepad.Poll());
        Assert.Empty(gamepad.AxisMotionEvents);
        Assert.Equal(0.1f, gamepad.GetAxis(GamepadAxis.LeftX));

        nativeGamepad.SetState(new Dictionary<GamepadAxis, float>
        {
            [GamepadAxis.LeftX] = 0.5f,
        }, new HashSet<GamepadButtonType>());
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new AxisMotionEvent(GamepadInputType.LeftAxis, GamepadAxisMotionType.X, 0.5f),
            Assert.Single(gamepad.AxisMotionEvents));
    }

    [Fact]
    public void PollReportsDisconnectedNativeDevice()
    {
        using var nativeGamepad = new FakeNativeGamepad { Connected = false };
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        Assert.False(gamepad.Poll());
    }

    private sealed class FakeNativeGamepad : INativeGamepad
    {
        private NativeGamepadState _state = new(
            new Dictionary<GamepadAxis, float>(),
            new HashSet<GamepadButtonType>());

        public int DeviceId => 7;
        public string Name => "Test Controller";
        public bool Connected { get; set; } = true;

        public void SetState(
            IReadOnlyDictionary<GamepadAxis, float> axes,
            IReadOnlySet<GamepadButtonType> buttons)
        {
            _state = new NativeGamepadState(axes, buttons);
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            state = _state;
            return Connected;
        }

        public void Dispose()
        {
        }
    }
}
