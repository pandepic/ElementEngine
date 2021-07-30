using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public enum MouseWheelChangeType
    {
        WheelUp,
        WheelDown
    }

    public interface IKeyboardHandler
    {
        public void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public void HandleTextInput(char key, GameTimer gameTimer) { }
    }

    public interface IMouseHandler
    {
        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { }
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

        private static GameControlsManager _gameControlsManager;
        private static readonly List<int> _removeList = new List<int>();

        public static void LoadGameControls(string settingsSection = "Controls")
        {
            _gameControlsManager = new GameControlsManager(settingsSection);
            AddKeyboardHandler(_gameControlsManager);
            AddMouseHandler(_gameControlsManager);
        }

        public static bool IsKeyDown(Key key)
        {
            if (KeysDown.TryGetValue(key, out bool down))
                return down;
            else
                return false;
        }

        public static void Update(InputSnapshot snapshot, GameTimer gameTimer)
        {
            #region Remove null handlers
            _removeList.Clear();

            for (var i = 0; i < _keyboardHandlers.Count; i++)
            {
                if (_keyboardHandlers[i] == null)
                    _removeList.Add(i);
            }

            for (var i = 0; i < _removeList.Count; i++)
                _keyboardHandlers.RemoveAt(_removeList[i]);

            _removeList.Clear();

            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseHandlers[i] == null)
                    _removeList.Add(i);
            }

            for (var i = 0; i < _removeList.Count; i++)
                _mouseHandlers.RemoveAt(_removeList[i]);

            #endregion

            PrevMousePosition = MousePosition;
            MousePosition = snapshot.MousePosition;

            if (MousePosition != PrevMousePosition)
                HandleMouseMotion(gameTimer);

            MouseWheelDelta = snapshot.WheelDelta;

            if (MouseWheelDelta != 0)
                HandleMouseWheel(gameTimer);

            for (var i = 0; i < snapshot.KeyCharPresses.Count; i++)
            {
                for (var h = 0; h < _keyboardHandlers.Count; h++)
                    _keyboardHandlers[h]?.HandleTextInput(snapshot.KeyCharPresses[i], gameTimer);
            }

            for (var i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                var keyEvent = snapshot.KeyEvents[i];

                if (KeysDown.ContainsKey(keyEvent.Key))
                {
                    if (KeysDown[keyEvent.Key] != keyEvent.Down)
                    {
                        KeysDown[keyEvent.Key] = keyEvent.Down;

                        if (keyEvent.Down)
                            HandleKeyPressed(keyEvent.Key, gameTimer);
                        else
                            HandleKeyReleased(keyEvent.Key, gameTimer);
                    }
                    else if (keyEvent.Down)
                    {
                        HandleKeyDown(keyEvent.Key, gameTimer);
                    }
                }
                else
                {
                    KeysDown.Add(keyEvent.Key, keyEvent.Down);

                    if (keyEvent.Down)
                        HandleKeyPressed(keyEvent.Key, gameTimer);
                    else
                        HandleKeyReleased(keyEvent.Key, gameTimer);
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
                            HandleMouseButtonPressed(mouseEvent.MouseButton, gameTimer);
                        else
                            HandleMouseButtonReleased(mouseEvent.MouseButton, gameTimer);
                    }
                    else if (mouseEvent.Down)
                    {
                        HandleMouseButtonDown(mouseEvent.MouseButton, gameTimer);
                    }
                }
                else
                {
                    MouseButtonsDown.Add(mouseEvent.MouseButton, mouseEvent.Down);

                    if (mouseEvent.Down)
                        HandleMouseButtonPressed(mouseEvent.MouseButton, gameTimer);
                    else
                        HandleMouseButtonReleased(mouseEvent.MouseButton, gameTimer);
                }
            } // MouseEvents

            PrevSnapshot = snapshot;

        } // Update

        internal static void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyPressed(key, gameTimer);
        }

        internal static void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyReleased(key, gameTimer);
        }

        internal static void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyDown(key, gameTimer);
        }

        internal static void HandleMouseMotion(GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseMotion(MousePosition, PrevMousePosition, gameTimer);
        }

        internal static void HandleMouseWheel(GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseWheel(MousePosition, MouseWheelDelta > 0 ? MouseWheelChangeType.WheelUp : MouseWheelChangeType.WheelDown, MouseWheelDelta, gameTimer);
        }

        internal static void HandleMouseButtonPressed(MouseButton button, GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonPressed(MousePosition, button, gameTimer);
        }

        internal static void HandleMouseButtonReleased(MouseButton button, GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonReleased(MousePosition, button, gameTimer);
        }

        internal static void HandleMouseButtonDown(MouseButton button, GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonDown(MousePosition, button, gameTimer);
        }

        public static void AddKeyboardHandler(IKeyboardHandler handler)
        {
            if (_keyboardHandlers.Contains(handler))
                return;

            _keyboardHandlers.Add(handler);
        }

        public static void AddMouseHandler(IMouseHandler handler)
        {
            if (_mouseHandlers.Contains(handler))
                return;

            _mouseHandlers.Add(handler);
        }

        public static void AddGameControlHandler(IGameControlHandler handler)
        {
            if (_gameControlsManager.Handlers.Contains(handler))
                return;

            _gameControlsManager.Handlers.Add(handler);
        }

        public static void RemoveKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Remove(handler);
        public static void RemoveMouseHandler(IMouseHandler handler) => _mouseHandlers.Remove(handler);
        public static void RemoveGameControlHandler(IGameControlHandler handler) => _gameControlsManager.Handlers.Remove(handler);
    }
}
