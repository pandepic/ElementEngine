using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public static class GameControlManager
    {
        private class GameControlInputManager : IKeyboardHandler, IMouseHandler
        {
            public GameControlInputManager()
            {
                InputManager.AddKeyboardHandler(this);
                InputManager.AddMouseHandler(this);
            }

            public void HandleKeyPressed(Key key) { GameControlManager.HandleKeyPressed(key); }
            public void HandleKeyReleased(Key key) { GameControlManager.HandleKeyReleased(key); }
            public void HandleKeyDown(Key key) { GameControlManager.HandleKeyDown(key); }

            public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition) { GameControlManager.HandleMouseMotion(mousePosition, prevMousePosition); }
            public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button) { GameControlManager.HandleMouseButtonPressed(mousePosition, button); }
            public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button) { GameControlManager.HandleMouseButtonReleased(mousePosition, button); }
            public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button) { GameControlManager.HandleMouseButtonDown(mousePosition, button); }
            public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta) { GameControlManager.HandleMouseWheel(mousePosition, type, mouseWheelDelta); }
        } // GameControlInputManager

        private static GameControlInputManager _gameControlInputManager = new GameControlInputManager();

        public static void Load()
        {

        }
                
        internal static void HandleKeyPressed(Key key)
        {
        }

        internal static void HandleKeyReleased(Key key)
        {
        }

        internal static void HandleKeyDown(Key key)
        {
        }

        internal static void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition)
        {
        }

        internal static void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button)
        {
        }
        
        internal static void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button)
        {
        }

        internal static void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button)
        {
        }
        
        internal static void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta)
        {
        }

    } // GameControlManager
}
