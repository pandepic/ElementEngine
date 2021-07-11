using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public enum UIEventType
    {
        OnMouseClicked,
        OnValueChanged,
    } // UIEventType

    public interface IUIEventHandler
    {
        public void HandleUIEvent(UIMenu source, UIEventType type, UIWidget widget);
    }

    public class UIMenu : IDisposable, IKeyboardHandler, IMouseHandler
    {
        public UIFrameList Frames { get; set; } = new UIFrameList();
        public Dictionary<string, XElement> Templates { get; set; } = new Dictionary<string, XElement>();
        protected List<IUIEventHandler> _eventHandlers { get; set; } = new List<IUIEventHandler>();

        public bool Focused { get; private set; } = false;

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    InputManager.RemoveKeyboardHandler(this);
                    InputManager.RemoveMouseHandler(this);
                    Frames?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIMenu()
        {
        }

        ~UIMenu()
        {
            Dispose(false);
        }

        public void EnableInput()
        {
            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
        }

        public void DisableInput()
        {
            InputManager.RemoveKeyboardHandler(this);
            InputManager.RemoveMouseHandler(this);
        }

        public void Load(string assetName, string templatesName = "")
        {
            using var fs = AssetManager.GetAssetStream(assetName);

            XDocument doc = XDocument.Load(fs);
            XElement menuRoot = doc.Element("Menu");

            XElement templatesRoot = null;

            if (!string.IsNullOrWhiteSpace(templatesName))
            {
                using var fsTemplates = AssetManager.GetAssetStream(templatesName);

                templatesRoot = XDocument.Load(fsTemplates).Element("Templates");

                foreach (var template in templatesRoot.Elements("Template"))
                {
                    var name = template.Attribute("TemplateName").Value;
                    Templates.Add(name, template);
                }
            }

            List<XElement> frames = menuRoot.Elements("Frame").ToList();

            foreach (var frame in frames)
            {
                UIFrame newFrame = new UIFrame(this, frame, Templates);
                Frames.Add(newFrame);
            } // foreach

            Frames.OrderByDrawOrder();

        } // Load

        public void AddUIEventHandler(IUIEventHandler handler)
        {
            if (_eventHandlers.Contains(handler))
                return;

            _eventHandlers.Add(handler);
        }

        public void RemoveUIEventHandler(IUIEventHandler handler) => _eventHandlers.Remove(handler);

        internal void TriggerUIEvent(UIEventType type, UIWidget widget)
        {
            for (var i = 0; i < _eventHandlers.Count; i++)
                _eventHandlers[i]?.HandleUIEvent(this, type, widget);
        }

        public UIFrame GetFrame(string frame)
        {
            return Frames[frame];
        }

        public dynamic GetWidget(string frame, string widget)
        {
            return Frames[frame].Widgets[widget];
        }

        public T GetWidget<T>(string frame, string widget) where T : UIWidget
        {
            return (T)GetWidget(frame, widget);
        }

        public dynamic GetWidget(string name)
        {
            for (var f = 0; f < Frames.Count; f++)
            {
                var frame = Frames[f];
                var widget = frame.GetWidget(name);
                if (widget != null)
                    return widget;
            }

            return default;
        }

        public T GetWidget<T>(string name) where T : UIWidget
        {
            return (T)GetWidget(name);
        }

        public void UnFocus()
        {
            Frames.UnFocusAll();
            Focused = false;
        }

        internal void GrabFocus(UIFrame frame)
        {
            Frames.UnFocusAllExcept(frame.Name);
            Focused = true;
        }

        internal void DropFocus(UIFrame frame)
        {
            Focused = false;
        }

        public void Update(GameTimer gameTimer)
        {
            Frames.Update(gameTimer);
        } // Update

        public void Draw(SpriteBatch2D spriteBatch)
        {
            Frames.Draw(spriteBatch);
        } // Draw

        public void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            Frames.OnKeyPressed(key, gameTimer);
        } // HandleKeyPressed

        public void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            Frames.OnKeyReleased(key, gameTimer);
        } // HandleKeyReleased

        public void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            Frames.OnKeyDown(key, gameTimer);
        } // HandleKeyDown

        public void HandleTextInput(char key, GameTimer gameTimer)
        {
            Frames.OnTextInput(key, gameTimer);
        }

        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            Frames.OnMouseMoved(mousePosition, prevMousePosition, gameTimer);
        } // HandleMouseMotion

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            Frames.OnMouseDown(button, mousePosition, gameTimer);
        } // HandleMouseButtonPressed

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            Frames.OnMouseClicked(button, mousePosition, gameTimer);
        } // HandleMouseButtonReleased

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            Frames.OnMouseDown(button, mousePosition, gameTimer);
        } // HandleMouseButtonDown

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            Frames.OnMouseScroll(type, mouseWheelDelta, gameTimer);
        } // HandleMouseWheel

    } // UIMenu
}
