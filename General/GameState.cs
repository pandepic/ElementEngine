using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class GameState : IDisposable, IKeyboardHandler, IMouseHandler, IGameControllerHandler, IGameControlHandler
    {
        public int KeyboardPriority { get; set; } = 0;
        public int MousePriority { get; set; } = 0;
        public int GameControllerPriority { get; set; } = 0;

        private List<IDisposable> _disposeList { get; set; } = new List<IDisposable>();
        internal bool _registered = false;
        internal bool _initialized = false;

        #region IDisposable
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var d in _disposeList)
                        d?.Dispose();

                    _disposeList.Clear();
                    DisposingManaged();
                }

                DisposingUnmanaged();
                _disposed = true;
            }
        }
        #endregion

        public GameState()
        {
        }

        ~GameState()
        {
            Dispose(false);
        }

        internal void Register()
        {
            if (_registered)
                return;

            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
            InputManager.AddGameControllerHandler(this);
            InputManager.AddGameControlHandler(this);

            _registered = true;
        }

        internal void DeRegister()
        {
            if (!_registered)
                return;

            InputManager.RemoveKeyboardHandler(this);
            InputManager.RemoveMouseHandler(this);
            InputManager.RemoveGameControllerHandler(this);
            InputManager.RemoveGameControlHandler(this);

            _registered = false;
        }

        public void AddDisposable(IDisposable d)
        {
            _disposeList.Add(d);
        }

        internal void DoInitialize()
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }
        }

        public virtual void Initialize() { }
        public virtual void Load() { }
        public virtual void Unload() { }

        protected virtual void DisposingManaged() { }
        protected virtual void DisposingUnmanaged() { }

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw(GameTimer gameTimer) { }
        public virtual void EndOfFrame(GameTimer gameTimer) { }

        public virtual void OnWindowResized(Rectangle windowRect) { }

        public virtual void HandleGameControl(string controlName, GameControlState state, GameTimer gameTimer) { }
        public virtual void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public virtual void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public virtual void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public virtual void HandleTextInput(char key, GameTimer gameTimer) { }
        public virtual void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public virtual void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public virtual void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public virtual void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public virtual void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { }
        public virtual void HandleControllerGameControl(GameController controller, string controlName, GameControlState state, GameTimer gameTimer) { }
        public virtual void HandleControllerAxisMotion(GameController controller, string controlName, GameControllerAxisMotionType motionType, float value, GameTimer gameTimer) { }
    }
}
