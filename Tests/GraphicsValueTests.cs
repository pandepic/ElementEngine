using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GraphicsValueTests
{
    [Fact]
    public void RgbaFloatConversionPreservesBlueChannel()
    {
        var color = new RgbaFloat(0.1f, 0.2f, 0.3f, 0.4f);

        var converted = color.ToRgbaVector();

        Assert.Equal(color.R, converted.R);
        Assert.Equal(color.G, converted.G);
        Assert.Equal(color.B, converted.B);
        Assert.Equal(color.A, converted.A);
    }
}
