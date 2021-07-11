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

    public class UIWidgetList : IDisposable
    {
        public List<UIWidget> Widgets = new List<UIWidget>();

        public UIWidgetList()
        {

        }

        public UIWidget this[int index]
        {
            get
            {
                return Widgets[index];
            }

            set
            {
                Widgets[index] = value;
            }
        }

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
                    foreach (var widget in Widgets)
                    {
                        if (widget is IDisposable disposable)
                            disposable?.Dispose();
                    }
                }

                _disposed = true;
            }
        }
        #endregion

        ~UIWidgetList()
        {
            Dispose(false);
        }

        public UIWidget this[string name]
        {
            get
            {
                return Widgets.Where(w => w.Name == name).FirstOrDefault();
            }
        }

        public void Add(UIWidget widget)
        {
            foreach (var w in Widgets)
                if (w.Name == widget.Name)
                    throw new FrameListException("Widget with name " + widget.Name + " already in list.");

            Widgets.Add(widget);
        }

        public void Remove(int index)
        {
            Widgets.Remove(Widgets[index]);
        }

        public void Remove(UIWidget widget)
        {
            Widgets.Remove(widget);
        }

        public void Clear()
        {
            Widgets.Clear();
        }

        internal void UnFocusAll()
        {
            foreach (var widget in Widgets)
                widget.UnFocus();
        }

        internal void UnFocusAllExcept(string name)
        {
            foreach (var widget in Widgets)
                if (widget.Name != name)
                    widget.UnFocus();
        }

        public int Size()
        {
            return Widgets.Count;
        }

        public void OrderByDrawOrder()
        {
            Widgets = Widgets.OrderBy(w => w.DrawOrder).ToList();
        }

        public void LoadStandardXML()
        {
            for (var i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];
                widget.LoadStandardXML();
            }
        }

        public virtual void Update(GameTimer gameTimer)
        {
            for (var i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    widget.Update(gameTimer);
            }
        }

        public void Draw(SpriteBatch2D spriteBatch)
        {
            for (var i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Visible)
                    widget.Draw(spriteBatch);
            }
        }

        public void OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnMouseMoved(mousePosition - framePosition, prevMousePosition - framePosition, gameTimer);
            }
        }

        public void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnMouseDown(button, mousePosition - framePosition, gameTimer);
            }
        }

        public void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer, Vector2 framePosition)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnMouseClicked(button, mousePosition - framePosition, gameTimer);
            }
        }

        public bool OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                {
                    if (Widgets[i].OnMouseScroll(type, mouseWheelDelta, gameTimer))
                        return true;
                }
            }

            return false;
        }

        public void OnKeyPressed(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnKeyPressed(key, gameTimer);
            }
        }

        public void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnKeyReleased(key, gameTimer);
            }
        }

        public void OnKeyDown(Key key, GameTimer gameTimer)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnKeyDown(key, gameTimer);
            }
        }

        public void OnTextInput(char key, GameTimer gameTimer)
        {
            for (int i = 0; i < Widgets.Count; i++)
            {
                var widget = Widgets[i];

                if (widget.Active)
                    Widgets[i].OnTextInput(key, gameTimer);
            }
        }
    }
}
