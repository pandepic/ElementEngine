using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class InputManagerGamepadTests : IDisposable
{
    public InputManagerGamepadTests()
    {
        InputManager.Dispose();
    }

    [Fact]
    public void RemovingDefaultGamepadSelectsAnotherConnectedGamepad()
    {
        var firstDevice = new FakeNativeGamepad(1);
        var secondDevice = new FakeNativeGamepad(2);

        Assert.True(InputManager.TryAddGamepad(firstDevice));
        Assert.True(InputManager.TryAddGamepad(secondDevice));
        Assert.Equal(1, InputManager.DefaultGamepad.ControllerIndex);

        Assert.True(InputManager.TryRemoveGamepad(1));

        Assert.True(firstDevice.Disposed);
        Assert.Equal(2, InputManager.DefaultGamepad.ControllerIndex);
    }

    public void Dispose()
    {
        InputManager.Dispose();
    }

    private sealed class FakeNativeGamepad(int deviceId) : INativeGamepad
    {
        private static readonly IReadOnlyDictionary<GamepadAxis, float> EmptyAxes =
            new Dictionary<GamepadAxis, float>();

        private static readonly IReadOnlySet<GamepadButtonType> EmptyButtons =
            new HashSet<GamepadButtonType>();

        public int DeviceId { get; } = deviceId;
        public string Name => $"Controller {DeviceId}";
        public bool Disposed { get; private set; }

        public bool TryPoll(out NativeGamepadState state)
        {
            state = new NativeGamepadState(EmptyAxes, EmptyButtons);
            return !Disposed;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
