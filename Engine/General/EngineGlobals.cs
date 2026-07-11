using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.ExceptionServices;

namespace ElementEngine
{
    public class ElementGlobals
    {
        protected static bool _loaded = false;

        // Graphics resources
        public static GameWindow Window;
        public static GraphicsDevice GraphicsDevice;
        public static CommandList CommandList;
        public static Viewport Viewport;

        internal static SpriteBatch2D ScreenSpaceSpriteBatch2D;
        internal static List<Action> ScreenSpaceDrawList = new List<Action>();

        // Target resolution
        public static Vector2I TargetResolutionSize => new Vector2I(TargetResolutionWidth, TargetResolutionHeight);
        public static Vector2 TargetResolutionSizeF => new Vector2(TargetResolutionWidth, TargetResolutionHeight);
        public static int TargetResolutionWidth => Window.Width;
        public static int TargetResolutionHeight => Window.Height;

        // Engine systems
        public static BaseGame Game;

        // UI
        public static Dictionary<string, Type> UIWidgetTypes { get; set; } = new Dictionary<string, Type>()
        {
            { "Button", typeof(UI.UIWBasicButton) },
            { "ImageButton", typeof(UI.UIWImageButton) },
            { "Label", typeof(UI.UIWLabel) },
            { "TextBox", typeof(UI.UIWTextBox) },
            { "ImageBox", typeof(UI.UIWImageBox) },
            { "HScrollBar", typeof(UI.UIWHScrollBar) },
            { "VScrollBar", typeof(UI.UIWVScrollBar) },
            { "HProgressBar", typeof(UI.UIWHProgressBar) },
            { "CheckBox", typeof(UI.UIWCheckbox) },
        };

        public static void RegisterWidgetType(Type type, string elementName)
        {
            if (UIWidgetTypes.ContainsValue(type))
                throw new Exception("Type already exists.");

            if (UIWidgetTypes.ContainsKey(elementName))
                throw new Exception("Element name already exists.");

            UIWidgetTypes.Add(elementName, type);
        }

        protected ElementGlobals() { }

        public static void Load(BaseGame game)
        {
            Game = game;
            _loaded = true;
        }

        public static void Unload()
        {
            if (!_loaded && CommandList == null && GraphicsDevice == null && Window == null)
                return;

            var exceptions = new List<Exception>();
            var graphicsDevice = GraphicsDevice;
            var commandList = CommandList;
            var screenSpaceSpriteBatch = ScreenSpaceSpriteBatch2D;
            var window = Window;

            CommandList = null;
            ScreenSpaceSpriteBatch2D = null;
            GraphicsDevice = null;
            Window = null;
            ScreenSpaceDrawList.Clear();
            Game = null;
            Viewport = default;
            _loaded = false;

            CaptureException(exceptions, () => graphicsDevice?.WaitForIdle());
            CaptureException(exceptions, UI.IMGUIManager.Dispose);
            CaptureException(exceptions, () => commandList?.Dispose());
            CaptureException(exceptions, () => screenSpaceSpriteBatch?.Dispose());
            CaptureException(exceptions, SpriteBatch2D.DisposeCache);
            CaptureException(exceptions, PrimitiveBatch.DisposeCache);
            CaptureException(exceptions, TileBatch2D.DisposeCache);
            CaptureException(exceptions, () => graphicsDevice?.Dispose());
            CaptureException(exceptions, () => window?.Dispose());

            if (exceptions.Count == 1)
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

            if (exceptions.Count > 1)
                throw new AggregateException("One or more errors occurred while unloading the engine.", exceptions);
        } // Unload

        private static void CaptureException(List<Exception> exceptions, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

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
    } // ElementGlobals
}
