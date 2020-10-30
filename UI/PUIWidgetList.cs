//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace PandaEngine
//{
//    public class WidgetListException : Exception
//    {
//        public WidgetListException(string message) : base(message) { }
//    }

//    public class PUIWidgetList
//    {
//        protected List<PUIWidget> _widgets = new List<PUIWidget>();

//        public PUIWidgetList()
//        {

//        }

//        public PUIWidget this[int index]
//        {
//            get
//            {
//                return _widgets[index];
//            }

//            set
//            {
//                _widgets[index] = value;
//            }
//        }

//        public PUIWidget this[string name]
//        {
//            get
//            {
//                return _widgets.Where(w => w.Name == name).FirstOrDefault();
//            }
//        }

//        public void Add(PUIWidget widget)
//        {
//            foreach (var w in _widgets)
//                if (w.Name == widget.Name)
//                    throw new FrameListException("Widget with name " + widget.Name + " already in list.");

//            _widgets.Add(widget);
//        }

//        public void Remove(int index)
//        {
//            _widgets.Remove(_widgets[index]);
//        }

//        public void Remove(PUIWidget widget)
//        {
//            _widgets.Remove(widget);
//        }

//        public void Clear()
//        {
//            _widgets.Clear();
//        }

//        internal void UnFocusAll()
//        {
//            foreach (var widget in _widgets)
//                widget.UnFocus();
//        }

//        internal void UnFocusAllExcept(string name)
//        {
//            foreach (var widget in _widgets)
//                if (widget.Name != name)
//                    widget.UnFocus();
//        }

//        public int Size()
//        {
//            return _widgets.Count;
//        }

//        public void OrderByDrawOrder()
//        {
//            _widgets = _widgets.OrderBy(w => w.DrawOrder).ToList();
//        }

//        public void LoadStandardXML()
//        {
//            for (var i = 0; i < _widgets.Count; i++)
//            {
//                var widget = _widgets[i];
//                widget.LoadStandardXML();
//            }
//        }

//        public virtual void Update(GameTime gameTime)
//        {
//            for (var i = 0; i < _widgets.Count; i++)
//            {
//                var widget = _widgets[i];

//                if (widget.Active)
//                    widget.Update(gameTime);
//            }
//        }

//        public void Draw(SpriteBatch spriteBatch)
//        {
//            for (var i = 0; i < _widgets.Count; i++)
//            {
//                var widget = _widgets[i];

//                if (widget.Visible)
//                    widget.Draw(spriteBatch);
//            }
//        }

//        public void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTime gameTime, Vector2 framePosition)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnMouseMoved(originalPosition - framePosition, currentPosition - framePosition, gameTime);
//            }
//        }

//        public void OnMouseDown(MouseButtonID button, Vector2 mousePosition, GameTime gameTime, Vector2 framePosition)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnMouseDown(button, mousePosition - framePosition, gameTime);
//            }
//        }

//        public void OnMouseClicked(MouseButtonID button, Vector2 mousePosition, GameTime gameTime, Vector2 framePosition)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnMouseClicked(button, mousePosition - framePosition, gameTime);
//            }
//        }

//        public bool OnMouseScroll(MouseScrollDirection direction, int scrollValue, GameTime gameTime)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                if (_widgets[i].OnMouseScroll(direction, scrollValue, gameTime))
//                    return true;
//            }

//            return false;
//        }

//        public void OnKeyPressed(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnKeyPressed(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyReleased(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnKeyReleased(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyDown(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnKeyDown(key, gameTime, currentKeyState);
//            }
//        }

//        public virtual void OnTextInput(TextInputEventArgs e, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            for (int i = 0; i < _widgets.Count; i++)
//            {
//                _widgets[i].OnTextInput(e, gameTime, currentKeyState);
//            }
//        }

//        public Dictionary<string, object> GetWidgetScriptList()
//        {
//            var result = new Dictionary<string, object>();

//            for (var i = 0; i < _widgets.Count; i++)
//            {
//                var widget = _widgets[i];
//                result.Add(widget.Name, widget);
//            }

//            return result;
//        }
//    }
//}
