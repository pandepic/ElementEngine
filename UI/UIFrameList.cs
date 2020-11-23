using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class FrameListException : Exception
    {
        public FrameListException(string message) : base(message) { }
    }

    public class UIFrameList
    {
        protected List<UIFrame> _frames = new List<UIFrame>();
        protected List<UIFrame> _reverseFrames = new List<UIFrame>();
        protected int _position = -1;

        public int Count { get => _frames.Count; }

        public UIFrameList() { }

        public UIFrame this[int index]
        {
            get
            {
                return _frames[index];
            }

            set
            {
                _frames[index] = value;
            }
        }

        public UIFrame this[string name]
        {
            get
            {
                return _frames.Where(f => f.Name == name).FirstOrDefault();
            }
        }

        public void OrderByDrawOrder()
        {
            _frames = _frames.OrderBy(f => f.DrawOrder).ToList();
            _reverseFrames = _frames.OrderByDescending(f => f.DrawOrder).ToList();
        }

        public void Add(UIFrame frame)
        {
            foreach (var f in _frames)
                if (f.Name == frame.Name)
                    throw new FrameListException("Frame with name " + frame.Name + " already in list.");

            _frames.Add(frame);
        }

        public void Remove(int index)
        {
            _frames.Remove(_frames[index]);
        }

        public void Remove(UIFrame frame)
        {
            _frames.Remove(frame);
        }

        public void Clear()
        {
            _frames.Clear();
        }

        internal void UnFocusAll()
        {
            foreach (var frame in _frames)
                frame.UnFocus();
        }

        internal void UnFocusAllExcept(string name)
        {
            foreach (var frame in _frames)
                if (frame.Name != name)
                    frame.UnFocus();
        }

        public Dictionary<string, object> GetFrameScriptList()
        {
            var result = new Dictionary<string, object>();

            foreach (var f in _frames)
            {
                result.Add(f.Name, f);
            }

            return result;
        }

        public void Draw(SpriteBatch2D spriteBatch)
        {
            foreach (var frame in _frames)
            {
                if (frame.Visible)
                    frame.Draw(spriteBatch);
            }
        }

        public void Update(GameTimer gameTimer)
        {
            foreach (var frame in _reverseFrames)
            {
                if (frame.Active)
                    frame.Update(gameTimer);
            }
        }

        public void OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in _reverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseMoved(mousePosition, prevMousePosition, gameTimer);
            }
        }

        public void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in _reverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseDown(button, mousePosition, gameTimer);
            }
        }

        public void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in _reverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseClicked(button, mousePosition, gameTimer);
            }
        }

        public void OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in _reverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseScroll(type, mouseWheelDelta, gameTimer);
            }
        }

        public void OnKeyPressed(Key key, GameTimer gameTimer)
        {
            foreach (var frame in _reverseFrames)
            {
                frame.OnKeyPressed(key, gameTimer);
            }
        }

        public void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            foreach (var frame in _reverseFrames)
            {
                frame.OnKeyReleased(key, gameTimer);
            }
        }

        public void OnKeyDown(Key key, GameTimer gameTimer)
        {
            foreach (var frame in _reverseFrames)
            {
                frame.OnKeyDown(key, gameTimer);
            }
        }

        public void OnTextInput(char key, GameTimer gameTimer)
        {
            foreach (var frame in _reverseFrames)
            {
                frame.OnTextInput(key, gameTimer);
            }
        }
    } // UIFrameList
}
