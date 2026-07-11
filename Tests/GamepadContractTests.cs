using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GamepadContractTests
{
    [Fact]
    public void TriggerAxisProducesButtonPressHoldAndRelease()
    {
        var nativeGamepad = new MutableNativeGamepad();
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        nativeGamepad.SetAxes((GamepadAxis.LeftTrigger, 0.5f));
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.LeftTrigger, true, false),
            Assert.Single(gamepad.ButtonEvents));

        gamepad.ButtonEvents.Clear();
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.LeftTrigger, true, true),
            Assert.Single(gamepad.ButtonEvents));

        gamepad.ButtonEvents.Clear();
        nativeGamepad.SetAxes((GamepadAxis.LeftTrigger, 0.1f));
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new GamepadButtonEvent(GamepadButtonType.LeftTrigger, false, true),
            Assert.Single(gamepad.ButtonEvents));
    }

    [Fact]
    public void AxisCrossingIntoDeadzoneEmitsZero()
    {
        var nativeGamepad = new MutableNativeGamepad();
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        nativeGamepad.SetAxes((GamepadAxis.RightY, 0.75f));
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new AxisMotionEvent(GamepadInputType.RightAxis, GamepadAxisMotionType.Y, 0.75f),
            Assert.Single(gamepad.AxisMotionEvents));

        gamepad.AxisMotionEvents.Clear();
        nativeGamepad.SetAxes((GamepadAxis.RightY, 0.1f));
        Assert.True(gamepad.Poll());
        Assert.Equal(
            new AxisMotionEvent(GamepadInputType.RightAxis, GamepadAxisMotionType.Y, 0f),
            Assert.Single(gamepad.AxisMotionEvents));
        Assert.Equal(0.1f, gamepad.GetAxis(GamepadAxis.RightY));
    }

    [Fact]
    public void MissingAxisInNewStateReturnsItToZero()
    {
        var nativeGamepad = new MutableNativeGamepad();
        using var gamepad = new Gamepad(nativeGamepad, 0.2f);

        nativeGamepad.SetAxes((GamepadAxis.LeftX, -0.5f));
        Assert.True(gamepad.Poll());
        gamepad.AxisMotionEvents.Clear();
        nativeGamepad.SetAxes();

        Assert.True(gamepad.Poll());
        Assert.Equal(
            new AxisMotionEvent(GamepadInputType.LeftAxis, GamepadAxisMotionType.X, 0f),
            Assert.Single(gamepad.AxisMotionEvents));
        Assert.Equal(0f, gamepad.GetAxis(GamepadAxis.LeftX));
    }

    [Fact]
    public void GamepadDisposesNativeDeviceOnce()
    {
        var nativeGamepad = new MutableNativeGamepad();
        var gamepad = new Gamepad(nativeGamepad, 0.2f);

        gamepad.Dispose();
        gamepad.Dispose();

        Assert.Equal(1, nativeGamepad.DisposeCalls);
    }

    private sealed class MutableNativeGamepad : INativeGamepad
    {
        private IReadOnlyDictionary<GamepadAxis, float> _axes = new Dictionary<GamepadAxis, float>();
        private static readonly IReadOnlySet<GamepadButtonType> EmptyButtons = new HashSet<GamepadButtonType>();

        public int DeviceId => 9;
        public string Name => "Mutable Controller";
        public int DisposeCalls { get; private set; }

        public void SetAxes(params (GamepadAxis Axis, float Value)[] axes)
        {
            _axes = axes.ToDictionary(item => item.Axis, item => item.Value);
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            state = new NativeGamepadState(_axes, EmptyButtons);
            return true;
        }

        public void Dispose() => DisposeCalls++;
    }
}
