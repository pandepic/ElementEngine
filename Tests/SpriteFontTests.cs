using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class SpriteFontTests
{
    [Fact]
    public void FontTextureUploadIgnoresUnusedScratchBufferTail()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var previousGraphicsDevice = ElementGlobals.GraphicsDevice;
        ElementGlobals.GraphicsDevice = graphicsDevice;

        try
        {
            using var fontTexture = new FontTexture(64, 64);
            var data = Enumerable.Range(0, 2392).Select(index => (byte)index).ToArray();

            fontTexture.SetData(new System.Drawing.Rectangle(3, 5, 18, 18), data);

            var update = Assert.IsType<TextureUpdate>(backend.LastTextureUpdate);
            Assert.Equal(data.AsSpan(0, 1296).ToArray(), update.Data);
            Assert.Equal((3u, 5u, 0u), (update.X, update.Y, update.Z));
            Assert.Equal((18u, 18u, 1u), (update.Width, update.Height, update.Depth));
            Assert.Equal(0u, update.MipLevel);
            Assert.Equal(0u, update.ArrayLayer);
        }
        finally
        {
            ElementGlobals.GraphicsDevice = previousGraphicsDevice;
        }
    }

    [Fact]
    public void FontTextureUploadRejectsInsufficientScratchBuffer()
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var previousGraphicsDevice = ElementGlobals.GraphicsDevice;
        ElementGlobals.GraphicsDevice = graphicsDevice;

        try
        {
            using var fontTexture = new FontTexture(64, 64);

            var exception = Assert.Throws<ArgumentException>(() =>
                fontTexture.SetData(new System.Drawing.Rectangle(3, 5, 18, 18), new byte[1295]));

            Assert.Equal("data", exception.ParamName);
            Assert.Null(backend.LastTextureUpdate);
        }
        finally
        {
            ElementGlobals.GraphicsDevice = previousGraphicsDevice;
        }
    }
}
