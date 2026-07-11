using System;
using System.Collections.Generic;
using System.Numerics;
using ElementEngine.UI;

namespace ElementEngine
{
    public enum MouseWheelChangeType
    {
        WheelUp,
        WheelDown
    }

    public enum GamepadInputType
    {
        Button,
        LeftAxis,
        RightAxis,
    }

    public enum GamepadAxisMotionType
    {
        X,
        Y,
    }

    public enum GamepadButtonType
    {
        Invalid,
        A,
        B,
        X,
        Y,
        Back,
        Guide,
        Start,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftTrigger,
        RightTrigger,
        Max,
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

        public static Dictionary<Key, bool> KeysDown { get; set; } = new();
        public static Dictionary<MouseButton, MouseButtonDownState> MouseButtonsDown { get; set; } = new();

        public static Dictionary<int, Gamepad> Gamepads = new(); // <controller index, controller>
        public static Gamepad DefaultGamepad { get; private set; }
        public static float DefaultGamepadDeadzone = 0.2f;
        public static event Action<Gamepad> OnGamepadAdded;
        public static event Action<Gamepad> OnGamepadRemoved;

        private readonly static List<IKeyboardHandler> _keyboardHandlers = new();
        private readonly static List<IMouseHandler> _mouseHandlers = new();
        private readonly static List<IGamepadHandler> _gamepadHandlers = new();

        private static GameControlsManager _gameControlsManager;
        private static bool _gamepadsEnabled;
        private static bool _applyGamepadMappings;
        private static float _gamepadRefreshTimer;

        internal static bool _keyPressedBlocked;
        internal static bool _keyReleasedBlocked;
        internal static bool _keyDownBlocked;
        internal static bool _textInputBlocked;

        internal static bool _mouseMotionBlocked;
        internal static bool _mouseButtonPressedBlocked;
        internal static bool _mouseButtonReleasedBlocked;
        internal static bool _mouseButtonDownBlocked;
        internal static bool _mouseWheelBlocked;

        internal static bool _gamepadButtonPressedBlocked;
        internal static bool _gamepadButtonReleasedBlocked;
        internal static bool _gamepadButtonDownBlocked;
        internal static bool _gamepadAxisMotionBlocked;

        internal static void Dispose()
        {
            UnregisterGameControlsManager();

            foreach (var (_, controller) in Gamepads)
                controller?.Dispose();

            Gamepads.Clear();
            DefaultGamepad = null;
            _gamepadsEnabled = false;
            _applyGamepadMappings = false;
            _gamepadRefreshTimer = 0f;
            KeysDown.Clear();
            MouseButtonsDown.Clear();
            PrevSnapshot = null;
            PrevMousePosition = Vector2.Zero;
            MousePosition = Vector2.Zero;
            MouseWheelDelta = 0f;
        }

        internal static void LoadGamepads(bool applyMappings = false)
        {
            foreach (var (_, controller) in Gamepads)
                controller?.Dispose();

            Gamepads.Clear();
            DefaultGamepad = null;
            _gamepadsEnabled = true;
            _applyGamepadMappings = applyMappings;
            _gamepadRefreshTimer = 0f;

            AddEnumeratedGamepads(NativeGamepadProvider.Enumerate(_applyGamepadMappings));
        }

        internal static bool TryAddGamepad(INativeGamepad device)
        {
            ArgumentNullException.ThrowIfNull(device);

            if (Gamepads.ContainsKey(device.DeviceId))
            {
                device.Dispose();
                return false;
            }

            var gamepad = new Gamepad(device, DefaultGamepadDeadzone);

            if (DefaultGamepad == null)
                DefaultGamepad = gamepad;

            Gamepads.Add(device.DeviceId, gamepad);
            try
            {
                OnGamepadAdded?.Invoke(gamepad);
                Logging.Information("Gamepad detected [Index:{index}] [Name:{name}].", gamepad.ControllerIndex, gamepad.ControllerName);
                return true;
            }
            catch
            {
                Gamepads.Remove(device.DeviceId);
                if (DefaultGamepad == gamepad)
                    SelectDefaultGamepad();

                gamepad.Dispose();
                throw;
            }
        }

        internal static bool TryRemoveGamepad(int index)
        {
            if (!Gamepads.TryGetValue(index, out var gamepad))
                return false;

            Gamepads.Remove(index);
            if (DefaultGamepad == gamepad)
                SelectDefaultGamepad();

            try
            {
                OnGamepadRemoved?.Invoke(gamepad);
                Logging.Information("Gamepad removed [Index:{index}] [Name:{name}].", gamepad.ControllerIndex, gamepad.ControllerName);
            }
            finally
            {
                gamepad.Dispose();
            }

            return true;
        }

        private static void SelectDefaultGamepad()
        {
            DefaultGamepad = null;

            foreach (var controller in Gamepads.Values)
            {
                DefaultGamepad = controller;
                break;
            }
        }

        private static void AddEnumeratedGamepads(IReadOnlyList<INativeGamepad> devices)
        {
            for (var index = 0; index < devices.Count; index++)
            {
                try
                {
                    TryAddGamepad(devices[index]);
                }
                catch
                {
                    for (var remainingIndex = index + 1; remainingIndex < devices.Count; remainingIndex++)
                        devices[remainingIndex].Dispose();

                    throw;
                }
            }
        }

        public static void SetGamepadDeadzones(float value)
        {
            foreach (var (_, controller) in Gamepads)
                controller.Deadzone = value;

            DefaultGamepadDeadzone = value;
        }

        public static void LoadGameControls(string settingsSection = "Controls")
        {
            var gameControlsManager = new GameControlsManager(settingsSection);
            UnregisterGameControlsManager();
            _gameControlsManager = gameControlsManager;

            AddKeyboardHandler(_gameControlsManager);
            AddMouseHandler(_gameControlsManager);
            AddGamepadHandler(_gameControlsManager);
        }

        private static void UnregisterGameControlsManager()
        {
            if (_gameControlsManager == null)
                return;

            RemoveKeyboardHandler(_gameControlsManager);
            RemoveMouseHandler(_gameControlsManager);
            RemoveGamepadHandler(_gameControlsManager);
            _gameControlsManager = null;
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
            _gamepadButtonPressedBlocked = false;
            _gamepadButtonReleasedBlocked = false;
            _gamepadButtonDownBlocked = false;
            _gamepadAxisMotionBlocked = false;

            PrevMousePosition = MousePosition;
            MousePosition = snapshot.MousePosition;
            var captureMouse = IMGUIManager.WantCaptureMouse();
            var captureKeyboard = IMGUIManager.WantCaptureKeyboard();
            MouseWheelDelta = captureMouse ? 0f : snapshot.WheelDelta;

            if (!captureMouse)
            {
                if (MousePosition != PrevMousePosition)
                    HandleMouseMotion(gameTimer);

                if (MouseWheelDelta != 0)
                    HandleMouseWheel(gameTimer);
            }

            if (!captureKeyboard)
            {
                for (var i = 0; i < snapshot.KeyCharPresses.Count; i++)
                {
                    _textInputBlocked = false;

                    for (var h = 0; h < _keyboardHandlers.Count; h++)
                    {
                        if (_textInputBlocked)
                            break;

                        _keyboardHandlers[h]?.HandleTextInput(snapshot.KeyCharPresses[i], gameTimer);
                    }
                }
            }

            for (var i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                var keyEvent = snapshot.KeyEvents[i];
                var hadPreviousState = KeysDown.TryGetValue(keyEvent.Key, out var wasDown);
                KeysDown[keyEvent.Key] = keyEvent.Down;

                if (captureKeyboard)
                    continue;

                if (!hadPreviousState || wasDown != keyEvent.Down)
                {
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

            for (var i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                var mouseEvent = snapshot.MouseEvents[i];
                var hadPreviousState = MouseButtonsDown.TryGetValue(mouseEvent.MouseButton, out var state);
                state ??= new MouseButtonDownState();
                var wasDown = state.Down;
                state.Down = mouseEvent.Down;

                if (!hadPreviousState)
                    MouseButtonsDown.Add(mouseEvent.MouseButton, state);

                if (wasDown != mouseEvent.Down)
                    state.RepeatTimer = 0f;

                if (captureMouse)
                    continue;

                if (!hadPreviousState || wasDown != mouseEvent.Down)
                {
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

            if (_gamepadsEnabled)
            {
                _gamepadRefreshTimer -= gameTimer.DeltaS;
                if (_gamepadRefreshTimer <= 0f)
                {
                    _gamepadRefreshTimer = 1f;
                    AddEnumeratedGamepads(NativeGamepadProvider.Enumerate(_applyGamepadMappings));
                }
            }

            List<int> disconnectedGamepads = null;
            foreach (var (deviceId, controller) in Gamepads)
            {
                if (controller.Poll())
                    continue;

                disconnectedGamepads ??= new List<int>();
                disconnectedGamepads.Add(deviceId);
            }

            if (disconnectedGamepads != null)
            {
                foreach (var deviceId in disconnectedGamepads)
                    TryRemoveGamepad(deviceId);
            }

            foreach (var (_, controller) in Gamepads)
            {
                foreach (var buttonEvent in controller.ButtonEvents)
                {
                    if (buttonEvent.PrevIsPressed)
                    {
                        if (buttonEvent.IsPressed)
                            HandleControllerButtonDown(controller, buttonEvent.ButtonType, gameTimer);
                        else
                            HandleControllerButtonReleased(controller, buttonEvent.ButtonType, gameTimer);
                    }
                    else
                    {
                        if (buttonEvent.IsPressed)
                            HandleControllerButtonPressed(controller, buttonEvent.ButtonType, gameTimer);
                    }
                }

                foreach (var axisMotionEvent in controller.AxisMotionEvents)
                    HandleControllerAxisMotion(controller, axisMotionEvent.InputType, axisMotionEvent.MotionType, axisMotionEvent.Value, gameTimer);

                controller.ButtonEvents.Clear();
                controller.AxisMotionEvents.Clear();
            }

            if (!captureMouse && MouseButtonDownAutoRepeat)
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

            if (_keyPressedBlocked)
                return;

            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyPressed(key, gameTimer);
        }

        internal static void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            if (_keyReleasedBlocked)
                return;

            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyReleased(key, gameTimer);
        }

        internal static void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            if (_keyDownBlocked)
                return;

            for (var i = 0; i < _keyboardHandlers.Count; i++)
                _keyboardHandlers[i]?.HandleKeyDown(key, gameTimer);
        }

        internal static void HandleMouseMotion(GameTimer gameTimer)
        {
            if (_mouseMotionBlocked)
                return;

            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseMotion(MousePosition, PrevMousePosition, gameTimer);
        }

        internal static void HandleMouseWheel(GameTimer gameTimer)
        {
            if (_mouseWheelBlocked)
                return;

            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseWheel(MousePosition, MouseWheelDelta > 0 ? MouseWheelChangeType.WheelUp : MouseWheelChangeType.WheelDown, MouseWheelDelta, gameTimer);
        }

        internal static void HandleMouseButtonPressed(MouseButton button, GameTimer gameTimer)
        {
            HandleMouseButtonDown(button, gameTimer);

            if (_mouseButtonPressedBlocked)
                return;

            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonPressed(MousePosition, button, gameTimer);
        }

        internal static void HandleMouseButtonReleased(MouseButton button, GameTimer gameTimer)
        {
            if (_mouseButtonReleasedBlocked)
                return;

            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonReleased(MousePosition, button, gameTimer);
        }

        internal static void HandleMouseButtonDown(MouseButton button, GameTimer gameTimer)
        {
            if (_mouseButtonDownBlocked)
                return;

            for (var i = 0; i < _mouseHandlers.Count; i++)
                _mouseHandlers[i]?.HandleMouseButtonDown(MousePosition, button, gameTimer);
        }

        internal static void HandleControllerButtonPressed(Gamepad controller, GamepadButtonType button, GameTimer gameTimer)
        {
            if (_gamepadButtonPressedBlocked)
                return;

            for (var i = 0; i < _gamepadHandlers.Count; i++)
                _gamepadHandlers[i]?.HandleGamepadButtonPressed(controller, button, gameTimer);
        }

        internal static void HandleControllerButtonReleased(Gamepad controller, GamepadButtonType button, GameTimer gameTimer)
        {
            if (_gamepadButtonReleasedBlocked)
                return;

            for (var i = 0; i < _gamepadHandlers.Count; i++)
                _gamepadHandlers[i]?.HandleGamepadButtonReleased(controller, button, gameTimer);
        }

        internal static void HandleControllerButtonDown(Gamepad controller, GamepadButtonType button, GameTimer gameTimer)
        {
            if (_gamepadButtonDownBlocked)
                return;

            for (var i = 0; i < _gamepadHandlers.Count; i++)
                _gamepadHandlers[i]?.HandleGamepadButtonDown(controller, button, gameTimer);
        }

        internal static void HandleControllerAxisMotion(Gamepad controller, GamepadInputType inputType, GamepadAxisMotionType motionType, float value, GameTimer gameTimer)
        {
            if (_gamepadAxisMotionBlocked)
                return;

            for (var i = 0; i < _gamepadHandlers.Count; i++)
                _gamepadHandlers[i]?.HandleGamepadAxisMotion(controller, inputType, motionType, value, gameTimer);
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

        public static void AddGamepadHandler(IGamepadHandler handler)
        {
            if (_gamepadHandlers.Contains(handler))
                return;

            _gamepadHandlers.Add(handler);
            _gamepadHandlers.Sort((c1, c2) => c1.GamepadPriority.CompareTo(c2.GamepadPriority));
        }

        public static void AddGameControlHandler(IGameControlHandler handler)
        {
            if (_gameControlsManager == null)
                return;
            if (_gameControlsManager.Handlers.Contains(handler))
                return;

            _gameControlsManager.Handlers.Add(handler);
        }

        public static void RemoveKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Remove(handler);
        public static void RemoveMouseHandler(IMouseHandler handler) => _mouseHandlers.Remove(handler);
        public static void RemoveGamepadHandler(IGamepadHandler handler) => _gamepadHandlers.Remove(handler);
        public static void RemoveGameControlHandler(IGameControlHandler handler) => _gameControlsManager?.Handlers.Remove(handler);
    }
}
