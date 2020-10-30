//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace PandaEngine
//{
//    public class FrameListException : Exception
//    {
//        public FrameListException(string message) : base(message) { }
//    }

//    public class PUIFrameList
//    {
//        protected List<PUIFrame> _frames = new List<PUIFrame>();
//        protected List<PUIFrame> _reverseFrames = new List<PUIFrame>();
//        protected int _position = -1;

//        public int Count { get => _frames.Count; }

//        public PUIFrameList() { }

//        public PUIFrame this[int index]
//        {
//            get
//            {
//                return _frames[index];
//            }

//            set
//            {
//                _frames[index] = value;
//            }
//        }

//        public PUIFrame this[string name]
//        {
//            get
//            {
//                return _frames.Where(f => f.Name == name).FirstOrDefault();
//            }
//        }

//        public void OrderByDrawOrder()
//        {
//            _frames = _frames.OrderBy(f => f.DrawOrder).ToList();
//            _reverseFrames = _frames.OrderByDescending(f => f.DrawOrder).ToList();
//        }

//        public void Add(PUIFrame frame)
//        {
//            foreach (var f in _frames)
//                if (f.Name == frame.Name)
//                    throw new FrameListException("Frame with name " + frame.Name + " already in list.");

//            _frames.Add(frame);
//        }

//        public void Remove(int index)
//        {
//            _frames.Remove(_frames[index]);
//        }

//        public void Remove(PUIFrame frame)
//        {
//            _frames.Remove(frame);
//        }

//        public void Clear()
//        {
//            _frames.Clear();
//        }

//        internal void UnFocusAll()
//        {
//            foreach (var frame in _frames)
//                frame.UnFocus();
//        }

//        internal void UnFocusAllExcept(string name)
//        {
//            foreach (var frame in _frames)
//                if (frame.Name != name)
//                    frame.UnFocus();
//        }

//        public Dictionary<string, object> GetFrameScriptList()
//        {
//            var result = new Dictionary<string, object>();

//            foreach (var f in _frames)
//            {
//                result.Add(f.Name, f);
//            }

//            return result;
//        }

//        public Dictionary<string, object> GetWidgetScriptList()
//        {
//            var result = new Dictionary<string, object>();

//            foreach (var f in _frames)
//            {
//                foreach (var w in f.Widgets.GetWidgetScriptList())
//                    result.Add(f.Name + "_" + w.Key, w.Value);
//            }

//            return result;
//        }

//        public void Draw(SpriteBatch spriteBatch)
//        {
//            foreach (var frame in _frames)
//            {
//                if (frame.Visible)
//                    frame.Draw(spriteBatch);
//            }
//        }

//        public void Update(GameTime gameTime)
//        {
//            foreach (var frame in _reverseFrames)
//            {
//                if (frame.Active)
//                    frame.Update(gameTime);
//            }
//        }

//        public void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTime gameTime)
//        {
//            var eventCaught = false;

//            foreach (var frame in _reverseFrames)
//            {
//                if (!eventCaught && frame.Active)
//                    eventCaught = frame.OnMouseMoved(originalPosition, currentPosition, gameTime);
//            }
//        }

//        public void OnMouseDown(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            var eventCaught = false;

//            foreach (var frame in _reverseFrames)
//            {
//                if (!eventCaught && frame.Active)
//                    eventCaught = frame.OnMouseDown(button, mousePosition, gameTime);
//            }
//        }

//        public void OnMouseClicked(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            var eventCaught = false;

//            foreach (var frame in _reverseFrames)
//            {
//                if (!eventCaught && frame.Active)
//                    eventCaught = frame.OnMouseClicked(button, mousePosition, gameTime);
//            }
//        }

//        public void OnMouseScroll(MouseScrollDirection direction, int scrollValue, GameTime gameTime)
//        {
//            var eventCaught = false;

//            foreach (var frame in _reverseFrames)
//            {
//                if (!eventCaught && frame.Active)
//                    eventCaught = frame.OnMouseScroll(direction, scrollValue, gameTime);
//            }
//        }

//        public void OnKeyPressed(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var frame in _reverseFrames)
//            {
//                frame.OnKeyPressed(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyReleased(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var frame in _reverseFrames)
//            {
//                frame.OnKeyReleased(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnKeyDown(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var frame in _reverseFrames)
//            {
//                frame.OnKeyDown(key, gameTime, currentKeyState);
//            }
//        }

//        public void OnTextInput(TextInputEventArgs e, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            foreach (var frame in _reverseFrames)
//            {
//                frame.OnTextInput(e, gameTime, currentKeyState);
//            }
//        }
//    }
//}
