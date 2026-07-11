using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class WaylandKeyMapTests
{
    [Theory]
    [InlineData(1u, Key.Escape)]
    [InlineData(16u, Key.Q)]
    [InlineData(30u, Key.A)]
    [InlineData(42u, Key.ShiftLeft)]
    [InlineData(54u, Key.ShiftRight)]
    [InlineData(59u, Key.F1)]
    [InlineData(86u, Key.NonUSBackSlash)]
    [InlineData(88u, Key.F12)]
    [InlineData(103u, Key.Up)]
    [InlineData(125u, Key.WinLeft)]
    [InlineData(194u, Key.F24)]
    [InlineData(999u, Key.Unknown)]
    public void EvdevCodesMapToStableEngineKeys(uint keyCode, Key expected)
    {
        Assert.Equal(expected, WaylandKeyMap.Translate(keyCode));
    }

    [Fact]
    public void NumberRowUsesLogicalEngineOrdering()
    {
        for (uint keyCode = 2; keyCode <= 10; keyCode++)
            Assert.Equal((Key)((int)Key.Number1 + keyCode - 2), WaylandKeyMap.Translate(keyCode));

        Assert.Equal(Key.Number0, WaylandKeyMap.Translate(11));
    }
}
