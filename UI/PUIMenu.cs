using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Veldrid;

namespace PandaEngine
{
    public enum PUIEventType
    {
        ButtonClick,
    } // PUIEventType

    public interface IPUIEventHandler
    {
        public void HandlePUIEvent(PUIMenu source, PUIEventType type, PUIWidget widget);
    }

    public class PUIMenu : IDisposable, IKeyboardHandler, IMouseHandler
    {
        public PUIFrameList Frames { get; set; } = new PUIFrameList();
        public Dictionary<string, XElement> Templates { get; set; } = new Dictionary<string, XElement>();
        protected List<IPUIEventHandler> _eventHandlers { get; set; } = new List<IPUIEventHandler>();

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
                }

                _disposed = true;
            }
        }
        #endregion

        public PUIMenu()
        {
            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
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
                PUIFrame newFrame = new PUIFrame(this, frame, Templates);
                Frames.Add(newFrame);
            } // foreach

            Frames.OrderByDrawOrder();

        } // Load

        public void AddPUIEventHandler(IPUIEventHandler handler)
        {
            if (_eventHandlers.Contains(handler))
                return;

            _eventHandlers.Add(handler);
        }

        public void RemovePUIEventHandler(IPUIEventHandler handler) => _eventHandlers.Remove(handler);

        internal void TriggerPUIEvent(PUIEventType type, PUIWidget widget)
        {
            for (var i = 0; i < _eventHandlers.Count; i++)
                _eventHandlers[i]?.HandlePUIEvent(this, type, widget);
        }

        public PUIFrame GetFrame(string frame)
        {
            return Frames[frame];
        }

        public dynamic GetWidget(string frame, string widget)
        {
            return Frames[frame].Widgets[widget];
        }

        public T GetWidget<T>(string frame, string widget) where T : PUIWidget
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

        public T GetWidget<T>(string name) where T : PUIWidget
        {
            return (T)GetWidget(name);
        }

        public void UnFocus()
        {
            Frames.UnFocusAll();
            Focused = false;
        }

        internal void GrabFocus(PUIFrame frame)
        {
            Frames.UnFocusAllExcept(frame.Name);
            Focused = true;
        }

        internal void DropFocus(PUIFrame frame)
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

    } // PUIMenu
}
