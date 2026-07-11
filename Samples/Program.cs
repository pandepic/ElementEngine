using ElementEngine;
using Samples;

int? smokeFrameLimit = null;
GraphicsBackend? graphicsBackend = null;
var graphicsDebug = false;

for (var index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--smoke-frames" when index + 1 < args.Length && int.TryParse(args[++index], out var parsedFrameLimit):
            if (parsedFrameLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(args), "The smoke frame count must be greater than zero.");

            smokeFrameLimit = parsedFrameLimit;
            break;
        case "--backend" when index + 1 < args.Length &&
                              Enum.TryParse<GraphicsBackend>(args[++index], true, out var parsedBackend):
            graphicsBackend = parsedBackend;
            break;
        case "--debug":
            graphicsDebug = true;
            break;
        default:
            throw new ArgumentException($"Unknown or incomplete argument: {args[index]}", nameof(args));
    }
}

Game.RequestedGraphicsBackend = graphicsBackend;
Game.RendererSmokeChecksEnabled = smokeFrameLimit.HasValue;
Game.GraphicsDebugEnabled = graphicsDebug;
using var game = new Game(smokeFrameLimit);
game.Run();
