using ElementEngine;
using System.Numerics;

namespace Samples;

internal static class RendererSmokeChecks
{
    public static void Run()
    {
        VerifyTextureUploadAndReadback();
        VerifyFramebufferClear();
        VerifySpriteRendering();
        VerifyPrimitiveRendering();
        VerifyTileRendering();
        Console.WriteLine($"Renderer smoke checks passed [{ElementGlobals.GraphicsDevice.BackendType}].");
    }

    private static void VerifyTextureUploadAndReadback()
    {
        var expected = new RgbaByte(17, 34, 51, 255);
        using var texture = new Texture2D(4, 4, expected);

        AssertAllPixels(texture, expected, "texture upload and readback");
    }

    private static void VerifyFramebufferClear()
    {
        var expected = new RgbaByte(11, 97, 203, 255);
        using var texture = new Texture2D(4, 4);
        var commandList = ElementGlobals.CommandList;

        commandList.Begin();
        texture.BeginRenderTarget(commandList);
        texture.RenderTargetClear(expected, commandList);
        commandList.End();
        ElementGlobals.GraphicsDevice.SubmitCommands(commandList);
        ElementGlobals.GraphicsDevice.WaitForIdle();

        AssertAllPixels(texture, expected, "framebuffer clear");
    }

    private static void VerifySpriteRendering()
    {
        RgbaByte[] expected =
        [
            new(255, 0, 0, 255),
            new(0, 255, 0, 255),
            new(0, 0, 255, 255),
            new(255, 255, 255, 255),
        ];
        using var source = new Texture2D(2, 2);
        using var target = new Texture2D(2, 2);
        using var spriteBatch = new SpriteBatch2D(target);
        source.SetData(expected);

        var commandList = ElementGlobals.CommandList;
        commandList.Begin();
        target.BeginRenderTarget(commandList);
        target.RenderTargetClear(RgbaByte.Clear, commandList);
        spriteBatch.Begin(SamplerType.Point);
        spriteBatch.DrawTexture2D(source, new Rectangle(0, 0, 2, 2));
        spriteBatch.End();
        commandList.End();
        ElementGlobals.GraphicsDevice.SubmitCommands(commandList);
        ElementGlobals.GraphicsDevice.WaitForIdle();

        var actual = target.GetData<RgbaByte>();
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("Renderer smoke check failed for sprite rendering or texture orientation.");
    }

    private static void VerifyPrimitiveRendering()
    {
        var expected = new RgbaByte(151, 43, 211, 255);
        using var target = new Texture2D(4, 4);
        using var primitiveBatch = new PrimitiveBatch(target);
        var commandList = ElementGlobals.CommandList;

        commandList.Begin();
        target.BeginRenderTarget(commandList);
        target.RenderTargetClear(RgbaByte.Clear, commandList);
        primitiveBatch.Begin(SamplerType.Point);
        primitiveBatch.DrawFilledRect(new Rectangle(0, 0, 4, 4), expected.ToRgbaFloat());
        primitiveBatch.End();
        commandList.End();
        ElementGlobals.GraphicsDevice.SubmitCommands(commandList);
        ElementGlobals.GraphicsDevice.WaitForIdle();

        AssertAllPixels(target, expected, "primitive rendering");
    }

    private static void VerifyTileRendering()
    {
        var expected = new RgbaByte(239, 127, 31, 255);
        using var atlas = new Texture2D(1, 1, expected);
        using var target = new Texture2D(4, 4);
        using var tileBatch = new TileBatch2D(4, 4, 1, 1, atlas, target);
        tileBatch.BeginBuild();
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 4; x++)
                tileBatch.SetTileAtPosition(x, y, 0);
        }
        tileBatch.EndBuild();

        var commandList = ElementGlobals.CommandList;
        commandList.Begin();
        target.BeginRenderTarget(commandList);
        target.RenderTargetClear(RgbaByte.Clear, commandList);
        tileBatch.DrawAll(Vector2.Zero);
        commandList.End();
        ElementGlobals.GraphicsDevice.SubmitCommands(commandList);
        ElementGlobals.GraphicsDevice.WaitForIdle();

        AssertAllPixels(target, expected, "tile rendering");
    }

    // Called once per frame while the engine's main swapchain command list is still
    // recording, so the frame ends up with two swapchain submissions - the case that
    // previously threw on Metal.
    public static void VerifySwapchainMultiPass()
    {
        var graphicsDevice = ElementGlobals.GraphicsDevice;
        using var commandList = graphicsDevice.ResourceFactory.CreateCommandList();

        commandList.Begin();
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, new RgbaFloat(1f, 0f, 1f, 1f));
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
    }

    private static void AssertAllPixels(Texture2D texture, RgbaByte expected, string operation)
    {
        var pixels = texture.GetData<RgbaByte>();
        for (var index = 0; index < pixels.Length; index++)
        {
            if (pixels[index] != expected)
            {
                throw new InvalidOperationException(
                    $"Renderer smoke check failed for {operation} at pixel {index}: " +
                    $"expected ({expected.R}, {expected.G}, {expected.B}, {expected.A}), " +
                    $"received ({pixels[index].R}, {pixels[index].G}, {pixels[index].B}, {pixels[index].A}).");
            }
        }
    }
}
