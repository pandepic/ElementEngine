using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace ElementEngine
{
    public enum GameControlState
    {
        Pressed,
        Released,
        Down,
        WheelUp,
        WheelDown,
    }

    public enum GameControlInputType
    {
        Keyboard,
        Mouse,
        GameController,
    }

    public class KeyboardGameControl
    {
        public string Name { get; set; }
        public List<List<Key>> ControlKeys { get; set; }
    };

    public class MouseGameControl
    {
        public string Name { get; set; }
        public List<MouseButton> ControlButtons { get; set; }
        public List<MouseWheelChangeType> WheelInputs { get; set; }
    };

    public class GameControllerGameControl
    {
        public string Name { get; set; }
        public GameControllerInputType InputType { get; set; }
        public List<List<GameControllerButtonType>> ControlButtons { get; set; }
    }

    public interface IGameControlHandler
    {
        public void HandleGameControl(string controlName, GameControlState state, GameTimer gameTimer);
        public void HandleControllerGameControl(GameController controller, string controlName, GameControlState state, GameTimer gameTimer);
        public void HandleControllerAxisMotion(GameController controller, string controlName, GameControllerAxisMotionType motionType, float value, GameTimer gameTimer);
    }

    public class GameControlsManager : IKeyboardHandler, IMouseHandler, IGameControllerHandler
    {
        public int KeyboardPriority { get; set; } = 0;
        public int MousePriority { get; set; } = 0;
        public int GameControllerPriority { get; set; } = 0;

        public List<IGameControlHandler> Handlers = new List<IGameControlHandler>();

        public List<KeyboardGameControl> KeyboardControls = new List<KeyboardGameControl>();
        public List<MouseGameControl> MouseControls = new List<MouseGameControl>();
        public List<GameControllerGameControl> GameControllerControls = new List<GameControllerGameControl>();

        public GameControlsManager(string settingsSection)
        {
            if (!SettingsManager.Sections.ContainsKey(settingsSection))
                throw new ArgumentException("Settings section " + settingsSection + " not found.", "settingsSection");

            var stopWatch = Stopwatch.StartNew();
            var loadedCount = 0;

            var section = SettingsManager.Sections[settingsSection];

            foreach (var kvp in section.Settings)
            {
                var setting = kvp.Value;

                if (string.IsNullOrWhiteSpace(setting.Value))
                    continue;

                var controlType = setting.OtherAttributes["Type"].ToEnum<GameControlInputType>();
                var controlComboSplit = setting.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);

                if (controlType == GameControlInputType.Keyboard)
                {
                    var newKeyboardControl = new KeyboardGameControl()
                    {
                        Name = setting.Name,
                        ControlKeys = new(),
                    };

                    foreach (var controlComboInfo in controlComboSplit)
                    {
                        var controlKeyList = new List<Key>();
                        var controlSplit = controlComboInfo.Split("+", StringSplitOptions.RemoveEmptyEntries);

                        foreach (var controlInfo in controlSplit)
                            controlKeyList.Add(controlInfo.ToEnum<Key>());

                        newKeyboardControl.ControlKeys.Add(controlKeyList);
                    }

                    KeyboardControls.Add(newKeyboardControl);
                    loadedCount += 1;
                    Logging.Information("[{component}] loaded keyboard control {name}.", "GameControlsManager", newKeyboardControl.Name);
                }
                else if (controlType == GameControlInputType.Mouse)
                {
                    var newMouseControl = new MouseGameControl()
                    {
                        Name = setting.Name,
                        ControlButtons = new(),
                        WheelInputs = new(),
                    };

                    foreach (var control in controlComboSplit)
                    {
                        if (Enum.TryParse(control, out MouseButton buttonType))
                            newMouseControl.ControlButtons.Add(buttonType);
                        else if (Enum.TryParse(control, out MouseWheelChangeType wheelType))
                            newMouseControl.WheelInputs.Add(wheelType);
                    }

                    if (newMouseControl.ControlButtons.Count == 0 && newMouseControl.WheelInputs.Count == 0)
                        continue;

                    MouseControls.Add(newMouseControl);
                    loadedCount += 1;

                    Logging.Information("[{component}] loaded mouse control {name}.", "GameControlsManager", newMouseControl.Name);
                }
                else if (controlType == GameControlInputType.GameController)
                {
                    var newControl = new GameControllerGameControl()
                    {
                        Name = setting.Name,
                        ControlButtons = new(),
                    };

                    if (Enum.TryParse<GameControllerInputType>(setting.Value, out var inputType))
                    {
                        newControl.InputType = inputType;
                    }
                    else
                    {
                        newControl.InputType = GameControllerInputType.Button;

                        foreach (var controlComboInfo in controlComboSplit)
                        {
                            var controlButtonList = new List<GameControllerButtonType>();
                            var controlSplit = controlComboInfo.Split("+", StringSplitOptions.RemoveEmptyEntries);

                            foreach (var controlInfo in controlSplit)
                                controlButtonList.Add(controlInfo.ToEnum<GameControllerButtonType>());

                            newControl.ControlButtons.Add(controlButtonList);
                        }
                    }

                    GameControllerControls.Add(newControl);
                    loadedCount += 1;

                    Logging.Information("[{component}] loaded controller control {name}.", "GameControlsManager", newControl.Name);
                }
            } // foreach setting

            stopWatch.Stop();
            Logging.Information("[{component}] loaded {count} controls from {section} in {time:0.00} ms.", "GameControlsManager", loadedCount, settingsSection, stopWatch.Elapsed.TotalMilliseconds);

        } // GameControlsManager

        public void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            foreach (var control in KeyboardControls)
                CheckKeyboardControl(control, key, GameControlState.Pressed, gameTimer);

        } // HandleKeyPressed

        public void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            foreach (var control in KeyboardControls)
                CheckKeyboardControl(control, key, GameControlState.Released, gameTimer);

        } // HandleKeyReleased

        public void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            foreach (var control in KeyboardControls)
                CheckKeyboardControl(control, key, GameControlState.Down, gameTimer);

        } // HandleKeyDown

        public void CheckKeyboardControl(KeyboardGameControl control, Key key, GameControlState state, GameTimer gameTimer)
        {
            foreach (var controlKeyList in control.ControlKeys)
            {
                bool mainKey = false;
                bool otherKeys = false;
                bool firstOtherKey = true;

                if (controlKeyList.Count == 1)
                    otherKeys = true;

                foreach (var controlKey in controlKeyList)
                {
                    if (controlKey == key)
                    {
                        mainKey = true;
                    }
                    else
                    {
                        var keyFound = false;

                        if (InputManager.IsKeyDown(controlKey))
                            keyFound = true;

                        if (firstOtherKey && keyFound)
                        {
                            otherKeys = true;
                            firstOtherKey = false;
                        }
                        else
                        {
                            otherKeys = otherKeys && keyFound;
                        }
                    }
                }

                if (mainKey && otherKeys)
                    TriggerGameControl(control.Name, state, gameTimer);
            }
        } // CheckKeyboardControl

        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
        }

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            foreach (var control in MouseControls)
            {
                foreach (var controlButton in control.ControlButtons)
                {
                    if (button == controlButton)
                        TriggerGameControl(control.Name, GameControlState.Pressed, gameTimer);
                }
            }

        } // HandleMouseButtonPressed

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            foreach (var control in MouseControls)
            {
                foreach (var controlButton in control.ControlButtons)
                {
                    if (button == controlButton)
                        TriggerGameControl(control.Name, GameControlState.Released, gameTimer);
                }
            }
        } // HandleMouseButtonReleased

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            foreach (var control in MouseControls)
            {
                foreach (var controlButton in control.ControlButtons)
                {
                    if (button == controlButton)
                        TriggerGameControl(control.Name, GameControlState.Down, gameTimer);
                }
            }
        } // HandleMouseButtonDown

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            foreach (var control in MouseControls)
            {
                foreach (var wheelInput in control.WheelInputs)
                {
                    if (wheelInput == type)
                        TriggerGameControl(control.Name, type == MouseWheelChangeType.WheelUp ? GameControlState.WheelUp : GameControlState.WheelDown, gameTimer);
                }
            }
        } // HandleMouseWheel
        
        public void HandleControllerButtonPressed(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            foreach (var control in GameControllerControls)
                CheckGameControllerControl(controller, control, button, GameControlState.Pressed, gameTimer);
        }

        public void HandleControllerButtonReleased(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            foreach (var control in GameControllerControls)
                CheckGameControllerControl(controller, control, button, GameControlState.Released, gameTimer);
        }

        public void HandleControllerButtonDown(GameController controller, GameControllerButtonType button, GameTimer gameTimer)
        {
            foreach (var control in GameControllerControls)
                CheckGameControllerControl(controller, control, button, GameControlState.Down, gameTimer);
        }

        public void CheckGameControllerControl(GameController controller, GameControllerGameControl control, GameControllerButtonType button, GameControlState state, GameTimer gameTimer)
        {
            foreach (var controlButtonList in control.ControlButtons)
            {
                bool mainButton = false;
                bool otherButtons = false;
                bool firstOtherButton = true;

                if (controlButtonList.Count == 1)
                    otherButtons = true;

                foreach (var controlButton in controlButtonList)
                {
                    if (controlButton == button)
                    {
                        mainButton = true;
                    }
                    else
                    {
                        var buttonFound = false;

                        if (controller.IsButtonPressed(controlButton))
                            buttonFound = true;

                        if (firstOtherButton && buttonFound)
                        {
                            otherButtons = true;
                            firstOtherButton = false;
                        }
                        else
                        {
                            otherButtons = otherButtons && buttonFound;
                        }
                    }
                }

                if (mainButton && otherButtons)
                    TriggerControllerGameControl(controller, control.Name, state, gameTimer);
            }
        }

        public void HandleControllerAxisMotion(GameController controller, GameControllerInputType inputType, GameControllerAxisMotionType motionType, float value, GameTimer gameTimer)
        {
            foreach (var control in GameControllerControls)
            {
                if (control.InputType == inputType)
                {
                    foreach (var handler in Handlers)
                        handler.HandleControllerAxisMotion(controller, control.Name, motionType, value, gameTimer);
                }
            }
        }

        public void TriggerGameControl(string name, GameControlState state, GameTimer gameTimer)
        {
            foreach (var handler in Handlers)
                handler.HandleGameControl(name, state, gameTimer);
        }

        public void TriggerControllerGameControl(GameController controller, string name, GameControlState state, GameTimer gameTimer)
        {
            foreach (var handler in Handlers)
                handler.HandleControllerGameControl(controller, name, state, gameTimer);
        }

    } // GameControlManager
}
