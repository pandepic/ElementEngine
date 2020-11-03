using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class WidgetListException : Exception
    {
        public WidgetListException(string message) : base(message) { }
    }

    public class PUIWidgetList
    {
        protected List<PUIWidget> _widgets = new List<PUIWidget>();

        public PUIWidgetList()
        {

        }

        public PUIWidget this[int index]
        {
            get
            {
                return _widgets[index];
            }

            set
            {
                _widgets[index] = value;
            }
        }

        public PUIWidget this[string name]
        {
            get
            {
                return _widgets.Where(w => w.Name == name).FirstOrDefault();
            }
        }

        public void Add(PUIWidget widget)
        {
            foreach (var w in _widgets)
                if (w.Name == widget.Name)
                    throw new FrameListException("Widget with name " + widget.Name + " already in list.");

            _widgets.Add(widget);
        }

        public void Remove(int index)
        {
            _widgets.Remove(_widgets[index]);
        }

        public void Remove(PUIWidget widget)
        {
            _widgets.Remove(widget);
        }

        public void Clear()
        {
            _widgets.Clear();
        }

        internal void UnFocusAll()
        {
            foreach (var widget in _widgets)
                widget.UnFocus();
        }

        internal void UnFocusAllExcept(string name)
        {
            foreach (var widget in _widgets)
                if (widget.Name != name)
                    widget.UnFocus();
        }

        public int Size()
        {
            return _widgets.Count;
        }

        public void OrderByDrawOrder()
        {
            _widgets = _widgets.OrderBy(w => w.DrawOrder).ToList();
        }

        public void LoadStandardXML()
        {
            for (var i = 0; i < _widgets.Count; i++)
            {
                var widget = _widgets[i];
                widget.LoadStandardXML();
            }
        }

        public virtual void Update(GameTimer gameTimer)
        {
            for (var i = 0; i < _widgets.Count; i++)
            {
                var widget = _widgets[i];

                if (widget.Active)
                    widget.Update(gameTimer);
            }
        }

        public void Draw(SpriteBatch2D spriteBatch)
        {
            for (var i = 0; i < _widgets.Count; i++)
            {
                var widget = _widgets[i];

                if (widget.Visible)
                    widget.Draw(spriteBatch);
            }
        }

        public void OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnMouseMoved(mousePosition - framePosition, prevMousePosition - framePosition, gameTimer);
            }
        }

        public void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnMouseDown(button, mousePosition - framePosition, gameTimer);
            }
        }

        public void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnMouseClicked(button, mousePosition - framePosition, gameTimer);
            }
        }

        public bool OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                if (_widgets[i].OnMouseScroll(type, mouseWheelDelta, gameTimer))
                    return true;
            }

            return false;
        }

        public void OnKeyPressed(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnKeyPressed(key, gameTimer);
            }
        }

        public void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnKeyReleased(key, gameTimer);
            }
        }

        public void OnKeyDown(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].OnKeyDown(key, gameTimer);
            }
        }

        public Dictionary<string, object> GetWidgetScriptList()
        {
            var result = new Dictionary<string, object>();

            for (var i = 0; i < _widgets.Count; i++)
            {
                var widget = _widgets[i];
                result.Add(widget.Name, widget);
            }

            return result;
        }
    }
}
