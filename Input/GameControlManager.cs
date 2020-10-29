using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public enum GameControlInputType
    {
        Keyboard,
        Mouse
    }

    public class GameControlManager<T> : IKeyboardHandler, IMouseHandler where T : IConvertible
    {
        public void HandleKeyPressed(Key key)
        {
        }

        public void HandleKeyReleased(Key key)
        {
        }

        public void HandleKeyDown(Key key)
        {
        }

        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition)
        {
        }

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button)
        {
        }

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button)
        {
        }

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button)
        {
        }

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta)
        {
        }

    } // GameControlManager
}
