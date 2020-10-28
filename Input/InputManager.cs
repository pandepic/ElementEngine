using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public enum MouseWheelChangeType
    {
        Up,
        Down
    }

    public interface IKeyboardHandler
    {
        void HandleKeyPressed(Key key);
        void HandleKeyReleased(Key key);
        void HandleKeyDown(Key key);
    }

    public interface IMouseHandler
    {
        void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition);
        void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button);
        void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button);
        void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button);
        void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta);
    }

    public static class InputManager
    {
        public static InputSnapshot PrevSnapshot { get; set; }
        public static Vector2 PrevMousePosition { get; set; }
        public static Vector2 MousePosition { get; set; }
        public static float MouseWheelDelta { get; set; }

        public static Dictionary<Key, bool> KeysDown { get; set; } = new Dictionary<Key, bool>();
        public static Dictionary<MouseButton, bool> MouseButtonsDown { get; set; } = new Dictionary<MouseButton, bool>();

        private readonly static List<IKeyboardHandler> _keyboardHandlers = new List<IKeyboardHandler>();
        private readonly static List<IMouseHandler> _mouseHandlers = new List<IMouseHandler>();

        public static bool IsKeyDown(Key key)
        {
            if (KeysDown.TryGetValue(key, out bool down))
                return down;
            else
                return false;
        }

        public static void Update(InputSnapshot snapshot, GameTimer gameTimer)
        {
            PrevMousePosition = MousePosition;
            MousePosition = snapshot.MousePosition;
            
            if (MousePosition != PrevMousePosition)
                HandleMouseMotion();

            MouseWheelDelta = snapshot.WheelDelta;

            if (MouseWheelDelta != 0)
                HandleMouseWheel();

            for (var i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                var keyEvent = snapshot.KeyEvents[i];
                
                if (KeysDown.ContainsKey(keyEvent.Key))
                {
                    if (KeysDown[keyEvent.Key] != keyEvent.Down)
                    {
                        KeysDown[keyEvent.Key] = keyEvent.Down;

                        if (keyEvent.Down)
                            HandleKeyPressed(keyEvent.Key);
                        else
                            HandleKeyReleased(keyEvent.Key);
                    }
                    else if (keyEvent.Down)
                    {
                        HandleKeyDown(keyEvent.Key);
                    }
                }
                else
                {
                    KeysDown.Add(keyEvent.Key, keyEvent.Down);

                    if (keyEvent.Down)
                        HandleKeyPressed(keyEvent.Key);
                    else
                        HandleKeyReleased(keyEvent.Key);
                }
            } // KeyEvents
            
            for (var i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                var mouseEvent = snapshot.MouseEvents[i];
                
                if (MouseButtonsDown.ContainsKey(mouseEvent.MouseButton))
                {
                    if (MouseButtonsDown[mouseEvent.MouseButton] != mouseEvent.Down)
                    {
                        MouseButtonsDown[mouseEvent.MouseButton] = mouseEvent.Down;

                        if (mouseEvent.Down)
                            HandleMouseButtonPressed(mouseEvent.MouseButton);
                        else
                            HandleMouseButtonReleased(mouseEvent.MouseButton);
                    }
                    else if (mouseEvent.Down)
                    {
                        HandleMouseButtonDown(mouseEvent.MouseButton);
                    }
                }
                else
                {
                    MouseButtonsDown.Add(mouseEvent.MouseButton, mouseEvent.Down);

                    if (mouseEvent.Down)
                        HandleMouseButtonPressed(mouseEvent.MouseButton);
                    else
                        HandleMouseButtonReleased(mouseEvent.MouseButton);
                }
            } // MouseEvents

            PrevSnapshot = snapshot;
        }

        public static void HandleKeyPressed(Key key)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i].HandleKeyPressed(key);
        }

        public static void HandleKeyReleased(Key key)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i].HandleKeyReleased(key);
        }

        public static void HandleKeyDown(Key key)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i].HandleKeyDown(key);
        }

        public static void HandleMouseMotion()
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i].HandleMouseMotion(MousePosition, PrevMousePosition);
        }

        public static void HandleMouseWheel()
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i].HandleMouseWheel(MousePosition, MouseWheelDelta > 0 ? MouseWheelChangeType.Up : MouseWheelChangeType.Down, MouseWheelDelta);
        }

        public static void HandleMouseButtonPressed(MouseButton button)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i].HandleMouseButtonPressed(MousePosition, button);
        }

        public static void HandleMouseButtonReleased(MouseButton button)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i].HandleMouseButtonReleased(MousePosition, button);
        }

        public static void HandleMouseButtonDown(MouseButton button)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i].HandleMouseButtonDown(MousePosition, button);
        }

        public static void AddKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Add(handler);
        public static void RemoveKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Remove(handler);
        public static void AddMouseHandler(IMouseHandler handler) => _mouseHandlers.Add(handler);
        public static void RemoveMouseHandler(IMouseHandler handler) => _mouseHandlers.Remove(handler);
    }
}
