using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class InputTypeTests
{
    [Theory]
    [InlineData("W", Key.W)]
    [InlineData("Up", Key.Up)]
    [InlineData("Right", Key.Right)]
    [InlineData("ControlLeft", Key.ControlLeft)]
    public void ExistingKeyboardControlNamesRemainCompatible(string setting, Key expected)
    {
        Assert.True(Enum.TryParse<Key>(setting, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ExistingMouseControlNamesRemainCompatible()
    {
        Assert.True(Enum.TryParse<MouseButton>("Left", out var parsed));
        Assert.Equal(MouseButton.Left, parsed);
    }

    [Fact]
    public void InputSnapshotStartsEmpty()
    {
        var snapshot = new InputSnapshot();

        Assert.Empty(snapshot.KeyEvents);
        Assert.Empty(snapshot.MouseEvents);
        Assert.Empty(snapshot.KeyCharPresses);
        Assert.Equal(0f, snapshot.WheelDelta);
    }
}
