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
        public static Viewport Viewport;

        internal static SpriteBatch2D ScreenSpaceSpriteBatch2D;
        internal static List<Action> ScreenSpaceDrawList = new List<Action>();

        // Engine systems
        public static BaseGame Game;

        protected static bool _loaded = false;

        protected PandaGlobals() { }

        public static void Load(BaseGame game)
        {
            Game = game;

            _loaded = true;
        }

        public static void Unload()
        {
            if (!_loaded)
                return;

            SpriteBatch2D.CleanupStaticResources();

            CommandList?.Dispose();
            CommandList = null;

            GraphicsDevice?.Dispose();
            GraphicsDevice = null;

            ScreenSpaceSpriteBatch2D?.Dispose();
            ScreenSpaceSpriteBatch2D = null;

            _loaded = false;
        } // Unload

        internal static void TryRegisterScreenSpaceDraw(Action action)
        {
            if (!ScreenSpaceDrawList.Contains(action))
                ScreenSpaceDrawList.Add(action);

            if (ScreenSpaceSpriteBatch2D == null)
                ScreenSpaceSpriteBatch2D = new SpriteBatch2D();
        } // TryRegisterScreenSpaceDraw

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
