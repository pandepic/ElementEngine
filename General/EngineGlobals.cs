using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace PandaEngine
{
    public class PandaGlobals
    {
        // Graphics resources
        public static Sdl2Window Window;
        public static GraphicsDevice GraphicsDevice;
        public static CommandList CommandList;
        public static SpriteBatch2D SpriteBatch2D;
        public static Viewport Viewport;

        // Engine systems
        public static BaseGame Game;

        protected static bool _loaded = false;

        protected PandaGlobals() { }

        public static void Load(BaseGame game)
        {
            Game = game;
            SpriteBatch2D = new SpriteBatch2D();

            _loaded = true;
        }

        public static void Unload()
        {
            if (!_loaded)
                return;

            SpriteBatch2D.CleanupStaticResources();
            CommandList.Dispose();
            GraphicsDevice.Dispose();

            _loaded = false;
        }

        public static void ResetFramebuffer(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = CommandList;

            commandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        }

        public static void ResetViewport(CommandList commandList = null)
        {
            if (commandList == null)
                commandList = CommandList;

            commandList.SetViewport(0, Viewport);
        }
    } // PandaGlobals
}
