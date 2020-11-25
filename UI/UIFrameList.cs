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
        public List<UIFrame> Frames = new List<UIFrame>();
        public List<UIFrame> ReverseFrames = new List<UIFrame>();
        protected int _position = -1;

        public int Count { get => Frames.Count; }

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
                    foreach (var frame in Frames)
                        frame?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIFrameList() { }

        ~UIFrameList()
        {
            Dispose(false);
        }

        public UIFrame this[int index]
        {
            get
            {
                return Frames[index];
            }

            set
            {
                Frames[index] = value;
            }
        }

        public UIFrame this[string name]
        {
            get
            {
                return Frames.Where(f => f.Name == name).FirstOrDefault();
            }
        }

        public void OrderByDrawOrder()
        {
            Frames = Frames.OrderBy(f => f.DrawOrder).ToList();
            ReverseFrames = Frames.OrderByDescending(f => f.DrawOrder).ToList();
        }

        public void Add(UIFrame frame)
        {
            foreach (var f in Frames)
                if (f.Name == frame.Name)
                    throw new FrameListException("Frame with name " + frame.Name + " already in list.");

            Frames.Add(frame);
        }

        public void Remove(int index)
        {
            Frames.Remove(Frames[index]);
        }

        public void Remove(UIFrame frame)
        {
            Frames.Remove(frame);
        }

        public void Clear()
        {
            Frames.Clear();
        }

        internal void UnFocusAll()
        {
            foreach (var frame in Frames)
                frame.UnFocus();
        }

        internal void UnFocusAllExcept(string name)
        {
            foreach (var frame in Frames)
                if (frame.Name != name)
                    frame.UnFocus();
        }

        public Dictionary<string, object> GetFrameScriptList()
        {
            var result = new Dictionary<string, object>();

            foreach (var f in Frames)
            {
                result.Add(f.Name, f);
            }

            return result;
        }

        public void Draw(SpriteBatch2D spriteBatch)
        {
            foreach (var frame in Frames)
            {
                if (frame.Visible)
                    frame.Draw(spriteBatch);
            }
        }

        public void Update(GameTimer gameTimer)
        {
            foreach (var frame in ReverseFrames)
            {
                if (frame.Active)
                    frame.Update(gameTimer);
            }
        }

        public void OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in ReverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseMoved(mousePosition, prevMousePosition, gameTimer);
            }
        }

        public void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in ReverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseDown(button, mousePosition, gameTimer);
            }
        }

        public void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in ReverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseClicked(button, mousePosition, gameTimer);
            }
        }

        public void OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var eventCaught = false;

            foreach (var frame in ReverseFrames)
            {
                if (!eventCaught && frame.Active)
                    eventCaught = frame.OnMouseScroll(type, mouseWheelDelta, gameTimer);
            }
        }

        public void OnKeyPressed(Key key, GameTimer gameTimer)
        {
            foreach (var frame in ReverseFrames)
            {
                frame.OnKeyPressed(key, gameTimer);
            }
        }

        public void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            foreach (var frame in ReverseFrames)
            {
                frame.OnKeyReleased(key, gameTimer);
            }
        }

        public void OnKeyDown(Key key, GameTimer gameTimer)
        {
            foreach (var frame in ReverseFrames)
            {
                frame.OnKeyDown(key, gameTimer);
            }
        }

        public void OnTextInput(char key, GameTimer gameTimer)
        {
            foreach (var frame in ReverseFrames)
            {
                frame.OnTextInput(key, gameTimer);
            }
        }
    } // UIFrameList
}
