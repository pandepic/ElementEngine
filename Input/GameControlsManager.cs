using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public enum GameControlState
    {
        Pressed,
        Released,
        Down,
    }

    public enum GameControlInputType
    {
        Keyboard,
        Mouse
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
    };

    public interface IHandleGameControls
    {
        public void HandleGameControl(string controlName, GameControlState state, GameTimer gameTimer);
    }

    public class GameControlsManager : IKeyboardHandler, IMouseHandler
    {
        public List<IHandleGameControls> Handlers = new List<IHandleGameControls>();

        public List<KeyboardGameControl> KeyboardControls = new List<KeyboardGameControl>();
        public List<MouseGameControl> MouseControls = new List<MouseGameControl>();

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
                        ControlKeys = new List<List<Key>>(),
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
                    Logging.Logger.Information("[{component}] loaded keyboard control {name}.", "GameControlsManager", newKeyboardControl.Name);
                }
                else if (controlType == GameControlInputType.Mouse)
                {
                    var newMouseControl = new MouseGameControl()
                    {
                        Name = setting.Name,
                        ControlButtons = new List<MouseButton>(),
                    };

                    foreach (var control in controlComboSplit)
                        newMouseControl.ControlButtons.Add(control.ToEnum<MouseButton>());
                    
                    MouseControls.Add(newMouseControl);
                    loadedCount += 1;
                    Logging.Logger.Information("[{component}] loaded mouse control {name}.", "GameControlsManager", newMouseControl.Name);
                }
            } // foreach setting

            stopWatch.Stop();
            Logging.Logger.Information("[{component}] loaded {count} controls from {section} in {time:0.00} ms.", "GameControlsManager", loadedCount, settingsSection, stopWatch.Elapsed.TotalMilliseconds);

        } // GameControlsManager

        public void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
        }

        public void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
        }

        public void HandleKeyDown(Key key, GameTimer gameTimer)
        {
        }

        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
        }

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
        }

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
        }

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
        }

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
        }

    } // GameControlManager
}
