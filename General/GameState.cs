using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class GameState : IKeyboardHandler, IMouseHandler, IGameControlHandler
    {
        internal bool _registered = false;

        public GameState()
        {
        }

        internal void Register()
        {
            if (_registered)
                return;

            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
            InputManager.AddGameControlHandler(this);
            _registered = true;
        }

        internal void DeRegister()
        {
            if (!_registered)
                return;

            InputManager.RemoveKeyboardHandler(this);
            InputManager.RemoveMouseHandler(this);
            InputManager.RemoveGameControlHandler(this);
            _registered = false;
        }

        public virtual void Load() { }
        public virtual void Unload() { }

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw(GameTimer gameTimer) { }

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
    }
}
