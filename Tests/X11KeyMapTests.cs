using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class X11KeyMapTests
{
    [Theory]
    [InlineData(0x0077, Key.W)]
    [InlineData(0x0057, Key.W)]
    [InlineData(0xFFBE, Key.F1)]
    [InlineData(0xFFE0, Key.F35)]
    [InlineData(0xFFE4, Key.ControlRight)]
    [InlineData(0xFF8D, Key.KeypadEnter)]
    [InlineData(0xFFFF, Key.Delete)]
    public void TranslatesCommonKeySymbols(ulong keySymbol, Key expected)
    {
        Assert.Equal(expected, X11KeyMap.Translate((nuint)keySymbol));
    }

    [Fact]
    public void UnknownKeySymbolStaysUnknown()
    {
        Assert.Equal(Key.Unknown, X11KeyMap.Translate(0));
    }
}
