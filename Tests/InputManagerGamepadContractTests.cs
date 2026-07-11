using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class InputManagerGamepadContractTests : IDisposable
{
    private readonly GameTimer _timer = new();

    public InputManagerGamepadContractTests()
    {
        InputManager.Dispose();
        InputManager.DefaultGamepadDeadzone = 0.2f;
        _timer.SetFrameTime(TimeSpan.FromSeconds(1d / 60d));
    }

    [Fact]
    public void DuplicateNativeDeviceIsDisposedWithoutReplacingConnectedGamepad()
    {
        var original = new MutableNativeGamepad(1);
        var duplicate = new MutableNativeGamepad(1);

        Assert.True(InputManager.TryAddGamepad(original));
        Assert.False(InputManager.TryAddGamepad(duplicate));

        Assert.Single(InputManager.Gamepads);
        Assert.Equal(original.Name, InputManager.Gamepads[1].ControllerName);
        Assert.Equal(0, original.DisposeCalls);
        Assert.Equal(1, duplicate.DisposeCalls);
    }

    [Fact]
    public void DisconnectedGamepadIsRemovedDuringInputUpdate()
    {
        var disconnected = new MutableNativeGamepad(1);
        var connected = new MutableNativeGamepad(2);
        Assert.True(InputManager.TryAddGamepad(disconnected));
        Assert.True(InputManager.TryAddGamepad(connected));
        disconnected.Connected = false;

        InputManager.Update(new InputSnapshot(), _timer);

        Assert.False(InputManager.Gamepads.ContainsKey(1));
        Assert.Equal(1, disconnected.DisposeCalls);
        Assert.Equal(2, InputManager.DefaultGamepad.ControllerIndex);
    }

    [Fact]
    public void AddCallbackFailureRollsBackAndDisposesNativeDevice()
    {
        var device = new MutableNativeGamepad(3);
        var expected = new InvalidOperationException("callback failed");
        Action<Gamepad> callback = _ => throw expected;
        InputManager.OnGamepadAdded += callback;

        try
        {
            var actual = Assert.Throws<InvalidOperationException>(() => InputManager.TryAddGamepad(device));

            Assert.Same(expected, actual);
            Assert.Empty(InputManager.Gamepads);
            Assert.Null(InputManager.DefaultGamepad);
            Assert.Equal(1, device.DisposeCalls);
        }
        finally
        {
            InputManager.OnGamepadAdded -= callback;
        }
    }

    [Fact]
    public void DeadzoneChangeAppliesToConnectedAndFutureGamepads()
    {
        Assert.True(InputManager.TryAddGamepad(new MutableNativeGamepad(1)));

        InputManager.SetGamepadDeadzones(0.35f);
        Assert.True(InputManager.TryAddGamepad(new MutableNativeGamepad(2)));

        Assert.All(InputManager.Gamepads.Values, gamepad => Assert.Equal(0.35f, gamepad.Deadzone));
        Assert.Equal(0.35f, InputManager.DefaultGamepadDeadzone);
    }

    public void Dispose()
    {
        InputManager.Dispose();
        InputManager.DefaultGamepadDeadzone = 0.2f;
    }

    private sealed class MutableNativeGamepad(int deviceId) : INativeGamepad
    {
        private static readonly IReadOnlyDictionary<GamepadAxis, float> EmptyAxes =
            new Dictionary<GamepadAxis, float>();

        private static readonly IReadOnlySet<GamepadButtonType> EmptyButtons =
            new HashSet<GamepadButtonType>();

        public int DeviceId { get; } = deviceId;
        public string Name => $"Controller {DeviceId}";
        public bool Connected { get; set; } = true;
        public int DisposeCalls { get; private set; }

        public bool TryPoll(out NativeGamepadState state)
        {
            state = new NativeGamepadState(EmptyAxes, EmptyButtons);
            return Connected;
        }

        public void Dispose() => DisposeCalls++;
    }
}
