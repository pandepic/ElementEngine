using System.Runtime.InteropServices;
using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class TextureFormatTests
{
    [Theory]
    [InlineData(PixelFormat.D24_UNorm_S8_UInt, 4)]
    [InlineData(PixelFormat.D32_Float_S8_UInt, 8)]
    [InlineData(PixelFormat.ETC2_R8_G8_B8_UNorm, 8)]
    [InlineData(PixelFormat.ETC2_R8_G8_B8_A8_UNorm, 16)]
    public void SpecialFormatsUseTheirFullStorageSize(PixelFormat format, int expectedBytes)
    {
        Assert.Equal(expectedBytes, GraphicsHelper.GetPixelFormatBytesPerPixel(format));
    }

    [Fact]
    public void EveryPixelFormatHasADefinedStorageSize()
    {
        foreach (var format in Enum.GetValues<PixelFormat>())
            Assert.True(GraphicsHelper.GetPixelFormatBytesPerPixel(format) > 0, $"No storage size for {format}.");
    }

    [Fact]
    public void PixelColorStructuresHaveStableNativeLayouts()
    {
        Assert.Equal(4, Marshal.SizeOf<RgbaByte>());
        Assert.Equal(16, Marshal.SizeOf<RgbaFloat>());
    }

    [Theory]
    [InlineData(PixelFormat.ETC2_R8_G8_B8_UNorm)]
    [InlineData(PixelFormat.ETC2_R8_G8_B8_A8_UNorm)]
    public void TextureReadbackRejectsBlockCompressedFormatsBeforeMapping(PixelFormat format)
    {
        using var backend = new TestGraphicsBackend();
        using var graphicsDevice = new GraphicsDevice(backend);
        var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            4,
            4,
            1,
            1,
            1,
            format,
            TextureUsage.Sampled,
            TextureType.Texture2D));
        using var wrapper = new Texture2D(texture);

        Assert.Throws<NotSupportedException>(wrapper.GetData);
        Assert.Equal(0, backend.MapCalls);
    }

    [Fact]
    public void TextureReadbackRemovesBackendRowPadding()
    {
        using var backend = new TestGraphicsBackend
        {
            MappedTextureData =
            [
                1, 2, 3, 4, 5, 6, 7, 8, 90, 91, 92, 93,
                9, 10, 11, 12, 13, 14, 15, 16, 94, 95, 96, 97,
            ],
            MappedTextureRowPitch = 12,
            MappedTextureDepthPitch = 24,
        };
        using var graphicsDevice = new GraphicsDevice(backend);
        var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            2,
            2,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Staging,
            TextureType.Texture2D));
        using var wrapper = new Texture2D(texture);

        var data = wrapper.GetData();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16], data);
        Assert.Equal(1, backend.MapCalls);
        Assert.Equal(1, backend.UnmapCalls);
    }

    [Fact]
    public void UnPremultiplyClearsTransparentPixelsAndClampsChannels()
    {
        using var backend = new TestGraphicsBackend
        {
            MappedTextureData =
            [
                255, 200, 100, 0,
                200, 100, 50, 128,
            ],
            MappedTextureRowPitch = 8,
            MappedTextureDepthPitch = 8,
        };
        using var graphicsDevice = new GraphicsDevice(backend);
        var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            2,
            1,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Staging,
            TextureType.Texture2D));
        using var wrapper = new Texture2D(texture);

        wrapper.ApplyPremultiply(TexturePremultiplyType.UnPremultiply);

        var update = Assert.IsType<TextureUpdate>(backend.LastTextureUpdate);
        var pixels = MemoryMarshal.Cast<byte, RgbaByte>(update.Data).ToArray();
        Assert.Equal(RgbaByte.Clear, pixels[0]);
        Assert.Equal(new RgbaByte(255, 199, 99, 128), pixels[1]);
    }

    [Fact]
    public void SaveAsPngTruncatesAnExistingLongerFile()
    {
        using var backend = new TestGraphicsBackend
        {
            MappedTextureData = [17, 34, 51, 255],
            MappedTextureRowPitch = 4,
            MappedTextureDepthPitch = 4,
        };
        using var graphicsDevice = new GraphicsDevice(backend);
        var texture = graphicsDevice.ResourceFactory.CreateTexture(new TextureDescription(
            1,
            1,
            1,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Staging,
            TextureType.Texture2D));
        using var wrapper = new Texture2D(texture);
        var path = Path.Combine(Path.GetTempPath(), $"ElementEngine-{Guid.NewGuid():N}.png");

        try
        {
            File.WriteAllBytes(path, new byte[4096]);

            wrapper.SaveAsPng(path);

            var data = File.ReadAllBytes(path);
            Assert.True(data.Length < 4096);
            Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], data[..8]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
