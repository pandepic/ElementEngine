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

    public enum GameControllerInputType
    {
        Button,
        LeftAxis,
        RightAxis,
    }

    public enum GameControllerAxisMotionType
    {
        X,
        Y,
    }

    public enum GameControllerButtonType
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

        public static Dictionary<int, GameController> GameControllers = new(); // <controller index, controller>
        public static GameController DefaultGameController { get; private set; }
        public static float DefaultControllerDeadzone = 0.2f;
        public static event Action<GameController> OnGameControllerAdded;
        public static event Action<GameController> OnGameControllerRemoved;

        private readonly static List<IKeyboardHandler> _keyboardHandlers = new();
        private readonly static List<IMouseHandler> _mouseHandlers = new();
        private readonly static List<IGameControllerHandler> _gameControllerHandlers = new();

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

        internal static bool _gameControllerButtonPressedBlocked;
        internal static bool _gameControllerButtonReleasedBlocked;
        internal static bool _gameControllerButtonDownBlocked;
        internal static bool _gameControllerAxisMotionBlocked;

        internal static GameControllerButtonType FromSDLControllerButton(SDL_GameControllerButton button)
        {
            return button switch
            {
                SDL_GameControllerButton.Invalid => GameControllerButtonType.Invalid,
                SDL_GameControllerButton.A => GameControllerButtonType.A,
                SDL_GameControllerButton.B => GameControllerButtonType.B,
                SDL_GameControllerButton.X => GameControllerButtonType.X,
                SDL_GameControllerButton.Y => GameControllerButtonType.Y,
                SDL_GameControllerButton.Back => GameControllerButtonType.Back,
                SDL_GameControllerButton.Guide => GameControllerButtonType.Guide,
                SDL_GameControllerButton.Start => GameControllerButtonType.Start,
                SDL_GameControllerButton.LeftStick => GameControllerButtonType.LeftStick,
                SDL_GameControllerButton.RightStick => GameControllerButtonType.RightStick,
                SDL_GameControllerButton.LeftShoulder => GameControllerButtonType.LeftShoulder,
                SDL_GameControllerButton.RightShoulder => GameControllerButtonType.RightShoulder,
                SDL_GameControllerButton.DPadUp => GameControllerButtonType.DPadUp,
                SDL_GameControllerButton.DPadDown => GameControllerButtonType.DPadDown,
                SDL_GameControllerButton.DPadLeft => GameControllerButtonType.DPadLeft,
                SDL_GameControllerButton.DPadRight => GameControllerButtonType.DPadRight,
                SDL_GameControllerButton.Max => GameControllerButtonType.Max,
                _ => throw new NotImplementedException(),
            };
        }
        internal static SDL_GameControllerButton ToSDLControllerButton(GameControllerButtonType button)
        {
            return button switch
            {
                GameControllerButtonType.Invalid => SDL_GameControllerButton.Invalid,
                GameControllerButtonType.A => SDL_GameControllerButton.A,
                GameControllerButtonType.B => SDL_GameControllerButton.B,
                GameControllerButtonType.X => SDL_GameControllerButton.X,
                GameControllerButtonType.Y => SDL_GameControllerButton.Y,
                GameControllerButtonType.Back => SDL_GameControllerButton.Back,
                GameControllerButtonType.Guide => SDL_GameControllerButton.Guide,
                GameControllerButtonType.Start => SDL_GameControllerButton.Start,
                GameControllerButtonType.LeftStick => SDL_GameControllerButton.LeftStick,
                GameControllerButtonType.RightStick => SDL_GameControllerButton.RightStick,
                GameControllerButtonType.LeftShoulder => SDL_GameControllerButton.LeftShoulder,
                GameControllerButtonType.RightShoulder => SDL_GameControllerButton.RightShoulder,
                GameControllerButtonType.DPadUp => SDL_GameControllerButton.DPadUp,
                GameControllerButtonType.DPadDown => SDL_GameControllerButton.DPadDown,
                GameControllerButtonType.DPadLeft => SDL_GameControllerButton.DPadLeft,
                GameControllerButtonType.DPadRight => SDL_GameControllerButton.DPadRight,
                GameControllerButtonType.Max => SDL_GameControllerButton.Max,
                _ => throw new NotImplementedException(),
            };
        }
        
        private static bool TryConvertSDLControllerAxis(SDL_GameControllerAxis axis, out GameControllerInputType inputType, out GameControllerAxisMotionType motionType)
        {
            inputType = GameControllerInputType.Button;
            motionType = GameControllerAxisMotionType.X;

            if (axis == SDL_GameControllerAxis.Invalid || axis == SDL_GameControllerAxis.Max)
                return false;

            inputType = axis switch
            {
                SDL_GameControllerAxis.LeftX => GameControllerInputType.LeftAxis,
                SDL_GameControllerAxis.LeftY => GameControllerInputType.LeftAxis,
                SDL_GameControllerAxis.RightX => GameControllerInputType.RightAxis,
                SDL_GameControllerAxis.RightY => GameControllerInputType.RightAxis,
                SDL_GameControllerAxis.TriggerLeft => GameControllerInputType.Button,
                SDL_GameControllerAxis.TriggerRight => GameControllerInputType.Button,
                SDL_GameControllerAxis.Invalid => throw new NotImplementedException(),
                SDL_GameControllerAxis.Max => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            motionType = axis switch
            {
                SDL_GameControllerAxis.LeftX => GameControllerAxisMotionType.X,
                SDL_GameControllerAxis.LeftY => GameControllerAxisMotionType.Y,
                SDL_GameControllerAxis.RightX => GameControllerAxisMotionType.X,
                SDL_GameControllerAxis.RightY => GameControllerAxisMotionType.Y,
                SDL_GameControllerAxis.TriggerLeft => GameControllerAxisMotionType.X,
                SDL_GameControllerAxis.TriggerRight => GameControllerAxisMotionType.X,
                SDL_GameControllerAxis.Invalid => throw new NotImplementedException(),
                SDL_GameControllerAxis.Max => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            return true;
        }

        internal static void Dispose()
        {
            foreach (var (_, controller) in GameControllers)
                controller?.Dispose();
        }

        internal static void LoadGameControllers()
        {
            foreach (var (_, controller) in GameControllers)
                controller?.Dispose();

            GameControllers.Clear();

            var joystickCount = Sdl2Native.SDL_NumJoysticks();

            for (var i = 0; i < joystickCount; i++)
                TryAddGameController(i);
        }

        internal static bool TryAddGameController(int index)
        {
            if (!Sdl2Native.SDL_IsGameController(index))
                return false;
            if (GameControllers.ContainsKey(index))
                return false;

            var controller = new GameController(index, DefaultControllerDeadzone);

            if (DefaultGameController == null)
                DefaultGameController = controller;

            GameControllers.Add(index, controller);
            OnGameControllerAdded?.Invoke(controller);

            Logging.Information("Game controller detected [Index:{index}] [Name:{name}].", controller.ControllerIndex, controller.ControllerName);

            return true;
        }

        internal static bool TryRemoveGameController(int index)
        {
            if (!GameControllers.TryGetValue(index, out var controller))
                return false;

            controller.Dispose();

            if (DefaultGameController == controller)
                DefaultGameController = null;

            GameControllers.Remove(index);
            OnGameControllerRemoved?.Invoke(controller);

            Logging.Information("Game controller removed [Index:{index}] [Name:{name}].", controller.ControllerIndex, controller.ControllerName);

            return true;
        }

        public static void SetGameControllerDeadzones(float value)
        {
            foreach (var (_, controller) in GameControllers)
                controller.Deadzone = value;

            DefaultControllerDeadzone = value;
        }

        private static void HandleTriggerEvent(GameController controller, GameControllerButtonType buttonType, short value)
        {
            var pressed = value != 0;
            var prevPressed = controller.IsButtonPressed(buttonType);

            controller._buttonsPressed[buttonType] = pressed;
            controller.ButtonEvents.Add(new(buttonType, controller.IsButtonPressed(buttonType), prevPressed));
        }

        internal static void ProcessGameControllerEvents(ref SDL_Event ev)
        {
            switch (ev.type)
            {
                case SDL_EventType.ControllerAxisMotion:
                    {
                        var axisEvent = Unsafe.As<SDL_Event, SDL_ControllerAxisEvent>(ref ev);

                        if (GameControllers.TryGetValue(axisEvent.which, out var controller))
                        {
                            if (axisEvent.axis == SDL_GameControllerAxis.TriggerLeft)
                            {
                                HandleTriggerEvent(controller, GameControllerButtonType.LeftTrigger, axisEvent.value);
                                return;
                            }
                            else if (axisEvent.axis == SDL_GameControllerAxis.TriggerRight)
                            {
                                HandleTriggerEvent(controller, GameControllerButtonType.RightTrigger, axisEvent.value);
                                return;
                            }

                            var normalizedValue = GameController.NormalizeAxis(axisEvent.value);

                            controller._axisValues[axisEvent.axis] = normalizedValue;

                            var eventValue = normalizedValue;

                            if (MathF.Abs(eventValue) < controller.Deadzone)
                                eventValue = 0f;

                            if (TryConvertSDLControllerAxis(axisEvent.axis, out var inputType, out var motionType))
                                controller.AxisMotionEvents.Add(new(inputType, motionType, eventValue));
                        }
                    }
                    break;

                case SDL_EventType.ControllerButtonDown:
                case SDL_EventType.ControllerButtonUp:
                    {
                        var buttonEvent = Unsafe.As<SDL_Event, SDL_ControllerButtonEvent>(ref ev);

                        if (GameControllers.TryGetValue(buttonEvent.which, out var controller))
                        {
                            var buttonType = FromSDLControllerButton(buttonEvent.button);
                            var prevPressed = controller.IsButtonPressed(buttonType);

                            controller._buttonsPressed[buttonType] = buttonEvent.state == 1;
                            controller.ButtonEvents.Add(new(buttonType, controller.IsButtonPressed(buttonType), prevPressed));
                        }
                    }
                    break;

                case SDL_EventType.ControllerDeviceRemoved:
                    {
                        var deviceEvent = Unsafe.As<SDL_Event, SDL_ControllerDeviceEvent>(ref ev);
                        TryRemoveGameController(deviceEvent.which);
                    }
                    break;

                case SDL_EventType.ControllerDeviceAdded:
                    {
                        var deviceEvent = Unsafe.As<SDL_Event, SDL_ControllerDeviceEvent>(ref ev);
                        TryAddGameController(deviceEvent.which);
                    }
                    break;
            }
        }

        public static void LoadGameControls(string settingsSection = "Controls")
        {
            _gameControlsManager = new GameControlsManager(settingsSection);

            AddKeyboardHandler(_gameControlsManager);
            AddMouseHandler(_gameControlsManager);
            AddGameControllerHandler(_gameControlsManager);
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

            if (!IMGUIManager.WantCaptureMouse())
            {
                PrevMousePosition = MousePosition;
                MousePosition = snapshot.MousePosition;

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

            foreach (var (_, controller) in GameControllers)
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

        internal static void HandleControllerButtonPressed(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            if (_gameControllerButtonPressedBlocked)
                return;

            for (var i = 0; i < _gameControllerHandlers.Count; i++)
                _gameControllerHandlers[i]?.HandleControllerButtonPressed(controller, button, gameTimer);
        }

        internal static void HandleControllerButtonReleased(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            if (_gameControllerButtonReleasedBlocked)
                return;

            for (var i = 0; i < _gameControllerHandlers.Count; i++)
                _gameControllerHandlers[i]?.HandleControllerButtonReleased(controller, button, gameTimer);
        }

        internal static void HandleControllerButtonDown(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            if (_gameControllerButtonDownBlocked)
                return;

            for (var i = 0; i < _gameControllerHandlers.Count; i++)
                _gameControllerHandlers[i]?.HandleControllerButtonDown(controller, button, gameTimer);
        }

        internal static void HandleControllerAxisMotion(GameController controller, GameControllerInputType inputType, GameControllerAxisMotionType motionType, float value, GameTimer gameTimer)
        {
            if (_gameControllerAxisMotionBlocked)
                return;

            for (var i = 0; i < _gameControllerHandlers.Count; i++)
                _gameControllerHandlers[i]?.HandleControllerAxisMotion(controller, inputType, motionType, value, gameTimer);
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

        public static void AddGameControllerHandler(IGameControllerHandler handler)
        {
            if (_gameControllerHandlers.Contains(handler))
                return;

            _gameControllerHandlers.Add(handler);
            _gameControllerHandlers.Sort((c1, c2) => c1.GameControllerPriority.CompareTo(c2.GameControllerPriority));
        }

        public static void AddGameControlHandler(IGameControlHandler handler)
        {
            if (_gameControlsManager.Handlers.Contains(handler))
                return;

            _gameControlsManager.Handlers.Add(handler);
        }

        public static void RemoveKeyboardHandler(IKeyboardHandler handler) => _keyboardHandlers.Remove(handler);
        public static void RemoveMouseHandler(IMouseHandler handler) => _mouseHandlers.Remove(handler);
        public static void RemoveGameControllerHandler(IGameControllerHandler handler) => _gameControllerHandlers.Remove(handler);
        public static void RemoveGameControlHandler(IGameControlHandler handler) => _gameControlsManager.Handlers.Remove(handler);
    }
}
