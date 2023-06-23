using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ElementEngine.UI;
using Veldrid;
using Veldrid.Sdl2;

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

        internal static GamepadButtonType FromSDLControllerButton(SDL_GameControllerButton button)
        {
            return button switch
            {
                SDL_GameControllerButton.Invalid => GamepadButtonType.Invalid,
                SDL_GameControllerButton.A => GamepadButtonType.A,
                SDL_GameControllerButton.B => GamepadButtonType.B,
                SDL_GameControllerButton.X => GamepadButtonType.X,
                SDL_GameControllerButton.Y => GamepadButtonType.Y,
                SDL_GameControllerButton.Back => GamepadButtonType.Back,
                SDL_GameControllerButton.Guide => GamepadButtonType.Guide,
                SDL_GameControllerButton.Start => GamepadButtonType.Start,
                SDL_GameControllerButton.LeftStick => GamepadButtonType.LeftStick,
                SDL_GameControllerButton.RightStick => GamepadButtonType.RightStick,
                SDL_GameControllerButton.LeftShoulder => GamepadButtonType.LeftShoulder,
                SDL_GameControllerButton.RightShoulder => GamepadButtonType.RightShoulder,
                SDL_GameControllerButton.DPadUp => GamepadButtonType.DPadUp,
                SDL_GameControllerButton.DPadDown => GamepadButtonType.DPadDown,
                SDL_GameControllerButton.DPadLeft => GamepadButtonType.DPadLeft,
                SDL_GameControllerButton.DPadRight => GamepadButtonType.DPadRight,
                SDL_GameControllerButton.Max => GamepadButtonType.Max,
                _ => throw new NotImplementedException(),
            };
        }
        internal static SDL_GameControllerButton ToSDLControllerButton(GamepadButtonType button)
        {
            return button switch
            {
                GamepadButtonType.Invalid => SDL_GameControllerButton.Invalid,
                GamepadButtonType.A => SDL_GameControllerButton.A,
                GamepadButtonType.B => SDL_GameControllerButton.B,
                GamepadButtonType.X => SDL_GameControllerButton.X,
                GamepadButtonType.Y => SDL_GameControllerButton.Y,
                GamepadButtonType.Back => SDL_GameControllerButton.Back,
                GamepadButtonType.Guide => SDL_GameControllerButton.Guide,
                GamepadButtonType.Start => SDL_GameControllerButton.Start,
                GamepadButtonType.LeftStick => SDL_GameControllerButton.LeftStick,
                GamepadButtonType.RightStick => SDL_GameControllerButton.RightStick,
                GamepadButtonType.LeftShoulder => SDL_GameControllerButton.LeftShoulder,
                GamepadButtonType.RightShoulder => SDL_GameControllerButton.RightShoulder,
                GamepadButtonType.DPadUp => SDL_GameControllerButton.DPadUp,
                GamepadButtonType.DPadDown => SDL_GameControllerButton.DPadDown,
                GamepadButtonType.DPadLeft => SDL_GameControllerButton.DPadLeft,
                GamepadButtonType.DPadRight => SDL_GameControllerButton.DPadRight,
                GamepadButtonType.Max => SDL_GameControllerButton.Max,
                _ => throw new NotImplementedException(),
            };
        }
        
        private static bool TryConvertSDLControllerAxis(SDL_GameControllerAxis axis, out GamepadInputType inputType, out GamepadAxisMotionType motionType)
        {
            inputType = GamepadInputType.Button;
            motionType = GamepadAxisMotionType.X;

            if (axis == SDL_GameControllerAxis.Invalid || axis == SDL_GameControllerAxis.Max)
                return false;

            inputType = axis switch
            {
                SDL_GameControllerAxis.LeftX => GamepadInputType.LeftAxis,
                SDL_GameControllerAxis.LeftY => GamepadInputType.LeftAxis,
                SDL_GameControllerAxis.RightX => GamepadInputType.RightAxis,
                SDL_GameControllerAxis.RightY => GamepadInputType.RightAxis,
                SDL_GameControllerAxis.TriggerLeft => GamepadInputType.Button,
                SDL_GameControllerAxis.TriggerRight => GamepadInputType.Button,
                SDL_GameControllerAxis.Invalid => throw new NotImplementedException(),
                SDL_GameControllerAxis.Max => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            motionType = axis switch
            {
                SDL_GameControllerAxis.LeftX => GamepadAxisMotionType.X,
                SDL_GameControllerAxis.LeftY => GamepadAxisMotionType.Y,
                SDL_GameControllerAxis.RightX => GamepadAxisMotionType.X,
                SDL_GameControllerAxis.RightY => GamepadAxisMotionType.Y,
                SDL_GameControllerAxis.TriggerLeft => GamepadAxisMotionType.X,
                SDL_GameControllerAxis.TriggerRight => GamepadAxisMotionType.X,
                SDL_GameControllerAxis.Invalid => throw new NotImplementedException(),
                SDL_GameControllerAxis.Max => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            return true;
        }

        internal static void Dispose()
        {
            foreach (var (_, controller) in Gamepads)
                controller?.Dispose();
        }

        internal static void LoadGamepads()
        {
            foreach (var (_, controller) in Gamepads)
                controller?.Dispose();

            Gamepads.Clear();

            var joystickCount = Sdl2Native.SDL_NumJoysticks();

            for (var i = 0; i < joystickCount; i++)
                TryAddGamepad(i);
        }

        internal static bool TryAddGamepad(int index)
        {
            if (!Sdl2Native.SDL_IsGameController(index))
                return false;
            if (Gamepads.ContainsKey(index))
                return false;

            var gamepad = new Gamepad(index, DefaultGamepadDeadzone);

            if (DefaultGamepad == null)
                DefaultGamepad = gamepad;

            Gamepads.Add(index, gamepad);
            OnGamepadAdded?.Invoke(gamepad);

            Logging.Information("Gamepad detected [Index:{index}] [Name:{name}].", gamepad.ControllerIndex, gamepad.ControllerName);

            return true;
        }

        internal static bool TryRemoveGamepad(int index)
        {
            if (!Gamepads.TryGetValue(index, out var gamepad))
                return false;

            gamepad.Dispose();

            if (DefaultGamepad == gamepad)
                DefaultGamepad = null;

            Gamepads.Remove(index);
            OnGamepadRemoved?.Invoke(gamepad);

            Logging.Information("Gamepad removed [Index:{index}] [Name:{name}].", gamepad.ControllerIndex, gamepad.ControllerName);

            return true;
        }

        public static void SetGamepadDeadzones(float value)
        {
            foreach (var (_, controller) in Gamepads)
                controller.Deadzone = value;

            DefaultGamepadDeadzone = value;
        }

        private static void HandleTriggerEvent(Gamepad gamepad, GamepadButtonType buttonType, short value)
        {
            var pressed = value != 0;
            var prevPressed = gamepad.IsButtonPressed(buttonType);

            gamepad.ButtonsPressed[buttonType] = pressed;
            gamepad.ButtonEvents.Add(new(buttonType, gamepad.IsButtonPressed(buttonType), prevPressed));
        }

        internal static void ProcessGamepadEvents(ref SDL_Event ev)
        {
            switch (ev.type)
            {
                case SDL_EventType.ControllerAxisMotion:
                    {
                        var axisEvent = Unsafe.As<SDL_Event, SDL_ControllerAxisEvent>(ref ev);

                        if (Gamepads.TryGetValue(axisEvent.which, out var gamepad))
                        {
                            if (axisEvent.axis == SDL_GameControllerAxis.TriggerLeft)
                            {
                                HandleTriggerEvent(gamepad, GamepadButtonType.LeftTrigger, axisEvent.value);
                                return;
                            }
                            else if (axisEvent.axis == SDL_GameControllerAxis.TriggerRight)
                            {
                                HandleTriggerEvent(gamepad, GamepadButtonType.RightTrigger, axisEvent.value);
                                return;
                            }

                            var normalizedValue = Gamepad.NormalizeAxis(axisEvent.value);

                            gamepad.AxisValues[axisEvent.axis] = normalizedValue;

                            var eventValue = normalizedValue;

                            if (MathF.Abs(eventValue) < gamepad.Deadzone)
                                eventValue = 0f;

                            if (TryConvertSDLControllerAxis(axisEvent.axis, out var inputType, out var motionType))
                                gamepad.AxisMotionEvents.Add(new(inputType, motionType, eventValue));
                        }
                    }
                    break;

                case SDL_EventType.ControllerButtonDown:
                case SDL_EventType.ControllerButtonUp:
                    {
                        var buttonEvent = Unsafe.As<SDL_Event, SDL_ControllerButtonEvent>(ref ev);

                        if (Gamepads.TryGetValue(buttonEvent.which, out var gamepad))
                        {
                            var buttonType = FromSDLControllerButton(buttonEvent.button);
                            var prevPressed = gamepad.IsButtonPressed(buttonType);

                            gamepad.ButtonsPressed[buttonType] = buttonEvent.state == 1;
                            gamepad.ButtonEvents.Add(new(buttonType, gamepad.IsButtonPressed(buttonType), prevPressed));
                        }
                    }
                    break;

                case SDL_EventType.ControllerDeviceRemoved:
                    {
                        var deviceEvent = Unsafe.As<SDL_Event, SDL_ControllerDeviceEvent>(ref ev);
                        TryRemoveGamepad(deviceEvent.which);
                    }
                    break;

                case SDL_EventType.ControllerDeviceAdded:
                    {
                        var deviceEvent = Unsafe.As<SDL_Event, SDL_ControllerDeviceEvent>(ref ev);
                        TryAddGamepad(deviceEvent.which);
                    }
                    break;
            }
        }

        public static void LoadGameControls(string settingsSection = "Controls")
        {
            _gameControlsManager = new GameControlsManager(settingsSection);

            AddKeyboardHandler(_gameControlsManager);
            AddMouseHandler(_gameControlsManager);
            AddGamepadHandler(_gameControlsManager);
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

            if (!IMGUIManager.WantCaptureMouse())
            {
                if (MousePosition != PrevMousePosition)
                    HandleMouseMotion(gameTimer);

                MouseWheelDelta = snapshot.WheelDelta;

                if (MouseWheelDelta != 0)
                    HandleMouseWheel(gameTimer);
            }

            if (!IMGUIManager.WantCaptureKeyboard())
            {
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
                }
            }

            if (!IMGUIManager.WantCaptureMouse())
            {
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
                }
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

            if (!IMGUIManager.WantCaptureMouse() && MouseButtonDownAutoRepeat)
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
            if (_gameControlsManager.Handlers.Contains(handler))
                return;

            _gameControlsManager.Handlers.Add(handler);
        }

        public static void RemoveKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Remove(handler);
        public static void RemoveMouseHandler(IMouseHandler handler) => _mouseHandlers.Remove(handler);
        public static void RemoveGamepadHandler(IGamepadHandler handler) => _gamepadHandlers.Remove(handler);
        public static void RemoveGameControlHandler(IGameControlHandler handler) => _gameControlsManager.Handlers.Remove(handler);
    }
}
