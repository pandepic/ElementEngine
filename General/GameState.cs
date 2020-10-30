using System;
using System.Collections.Generic;
using System.Text;

namespace PandaEngine
{
    public class GameState : IKeyboardHandler, IMouseHandler, IHandleGameControls
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

        public virtual void Load()
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void Update(GameTimer gameTimer)
        {

        }

        public virtual void Draw(GameTimer gameTimer)
        {

        }

        public virtual void HandleGameControl(string controlName, GameControlState state, GameTimer gameTimer)
        {
        }
    }
}
