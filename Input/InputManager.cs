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
        public int KeyboardPriority { get; set; }

        public void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public void HandleTextInput(char key, GameTimer gameTimer) { }
    }

    public interface IMouseHandler
    {
        public int MousePriority { get; set; }

        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { }
    }

    public static class InputManager
    {
        public class MouseButtonDownState
        {
            public bool Down;
            public float RepeatTimer;
        }

        /// <summary>
        /// How often should a mouse button down event auto repeat while the button is held in seconds
        /// </summary>
        public static float MouseButtonDownRepeatTime = 0.1f;
        public static bool MouseButtonDownAutoRepeat = true;

        public static InputSnapshot PrevSnapshot { get; set; }
        public static Vector2 PrevMousePosition { get; set; }
        public static Vector2 MousePosition { get; set; }
        public static float MouseWheelDelta { get; set; }

        public static Dictionary<Key, bool> KeysDown { get; set; } = new Dictionary<Key, bool>();
        public static Dictionary<MouseButton, MouseButtonDownState> MouseButtonsDown { get; set; } = new Dictionary<MouseButton, MouseButtonDownState>();

        private readonly static List<IKeyboardHandler> _keyboardHandlers = new();
        private readonly static List<IMouseHandler> _mouseHandlers = new();

        private static GameControlsManager _gameControlsManager;
        
        internal static bool _keyPressedBlocked;
        internal static bool _keyReleasedBlocked;
        internal static bool _keyDownBlocked;
        internal static bool _textInputBlocked;

        internal static bool _mouseMotionBlocked;
        internal static bool _mouseButtonPressedBlocked;
        internal static bool _mouseButtonReleasedBlocked;
        internal static bool _mouseButtonDownBlocked;
        internal static bool _mouseWheelBlocked;

        public static void LoadGameControls(string settingsSection = "Controls")
        {
            _gameControlsManager = new GameControlsManager(settingsSection);
            AddKeyboardHandler(_gameControlsManager);
            AddMouseHandler(_gameControlsManager);
        }

        public static Key GetGameControlKey(string name)
        {
            foreach (var control in _gameControlsManager.KeyboardControls)
            {
                if (control.Name == name)
                    return control.ControlKeys[0][0];
            }

            throw new Exception($"Game control not found: {name}");
        }

        public static bool IsKeyDown(Key key)
        {
            if (KeysDown.TryGetValue(key, out var down))
                return down;
            else
                return false;
        }

        public static bool IsMouseButtonDown(MouseButton button)
        {
            if (MouseButtonsDown.TryGetValue(button, out var state))
                return state.Down;
            else
                return false;
        }

        public static void Update(InputSnapshot snapshot, GameTimer gameTimer)
        {
            #region Remove null handlers
            for (var i = _keyboardHandlers.Count - 1; i >= 0; i--)
            {
                var keyboardHandler = _keyboardHandlers[i];
                if (keyboardHandler == null)
                    _keyboardHandlers.RemoveAt(i);
            }

            for (var i = _mouseHandlers.Count - 1; i >= 0; i--)
            {
                var mouseHandler = _mouseHandlers[i];
                if (mouseHandler == null)
                    _mouseHandlers.RemoveAt(i);
            }
            #endregion

            _keyPressedBlocked = false;
            _keyReleasedBlocked = false;
            _keyDownBlocked = false;
            _textInputBlocked = false;
            _mouseMotionBlocked = false;
            _mouseButtonPressedBlocked = false;
            _mouseButtonReleasedBlocked = false;
            _mouseButtonDownBlocked = false;
            _mouseWheelBlocked = false;

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
                    if (MouseButtonsDown[mouseEvent.MouseButton].Down != mouseEvent.Down)
                    {
                        MouseButtonsDown[mouseEvent.MouseButton].Down = mouseEvent.Down;

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
                    MouseButtonsDown.Add(mouseEvent.MouseButton, new MouseButtonDownState() { Down = mouseEvent.Down });

                    if (mouseEvent.Down)
                        HandleMouseButtonPressed(mouseEvent.MouseButton, gameTimer);
                    else
                        HandleMouseButtonReleased(mouseEvent.MouseButton, gameTimer);
                }
            } // MouseEvents

            if (MouseButtonDownAutoRepeat)
            {
                foreach (var (button, mouseDown) in MouseButtonsDown)
                {
                    if (!mouseDown.Down)
                        continue;

                    mouseDown.RepeatTimer += gameTimer.DeltaS;

                    if (mouseDown.RepeatTimer >= MouseButtonDownRepeatTime)
                    {
                        mouseDown.RepeatTimer = 0;
                        HandleMouseButtonDown(button, gameTimer);
                    }
                }
            }

            PrevSnapshot = snapshot;

        } // Update

        internal static void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            HandleKeyDown(key, gameTimer);

            for (var i = 0; i < _keyboardHandlers.Count; i++)
            {
                if (_keyPressedBlocked)
                    break;

                _keyboardHandlers[i]?.HandleKeyPressed(key, gameTimer);
            }
        }

        internal static void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
            {
                if (_keyReleasedBlocked)
                    break;

                _keyboardHandlers[i]?.HandleKeyReleased(key, gameTimer);
            }
        }

        internal static void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            for (var i = 0; i < _keyboardHandlers.Count; i++)
            {
                if (_keyDownBlocked)
                    break;

                _keyboardHandlers[i]?.HandleKeyDown(key, gameTimer);
            }
        }

        internal static void HandleMouseMotion(GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseMotionBlocked)
                    break;

                _mouseHandlers[i]?.HandleMouseMotion(MousePosition, PrevMousePosition, gameTimer);
            }
        }

        internal static void HandleMouseWheel(GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseWheelBlocked)
                    break;

                _mouseHandlers[i]?.HandleMouseWheel(MousePosition, MouseWheelDelta > 0 ? MouseWheelChangeType.WheelUp : MouseWheelChangeType.WheelDown, MouseWheelDelta, gameTimer);
            }
        }

        internal static void HandleMouseButtonPressed(MouseButton button, GameTimer gameTimer)
        {
            HandleMouseButtonDown(button, gameTimer);

            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseButtonPressedBlocked)
                    break;

                _mouseHandlers[i]?.HandleMouseButtonPressed(MousePosition, button, gameTimer);
            }
        }

        internal static void HandleMouseButtonReleased(MouseButton button, GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseButtonReleasedBlocked)
                    break;

                _mouseHandlers[i]?.HandleMouseButtonReleased(MousePosition, button, gameTimer);
            }
        }

        internal static void HandleMouseButtonDown(MouseButton button, GameTimer gameTimer)
        {
            for (var i = 0; i < _mouseHandlers.Count; i++)
            {
                if (_mouseButtonDownBlocked)
                    break;

                _mouseHandlers[i]?.HandleMouseButtonDown(MousePosition, button, gameTimer);
            }
        }

        public static void AddKeyboardHandler(IKeyboardHandler handler)
        {
            if (_keyboardHandlers.Contains(handler))
                return;

            _keyboardHandlers.Add(handler);
            _keyboardHandlers.Sort((k1, k2) => k1.KeyboardPriority.CompareTo(k2.KeyboardPriority));
        }

        public static void AddMouseHandler(IMouseHandler handler)
        {
            if (_mouseHandlers.Contains(handler))
                return;

            _mouseHandlers.Add(handler);
            _mouseHandlers.Sort((m1, m2) => m1.MousePriority.CompareTo(m2.MousePriority));
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
