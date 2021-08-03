using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScreen : UIObject
    {
        public UIObject FocusedObject;

        public UIScreen(Vector2I? position = null, Vector2I? size = null, string name = "Screen") : base(name)
        {
            _uiPosition.Position = position ?? Vector2I.Zero;
            _uiSize.Size = size ?? new Vector2I(ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight);

            UpdateLayout();
            Enable();

            CanFocus = false;
        }

        public override void Enable()
        {
            base.Enable();
            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
        }

        public override void Disable()
        {
            base.Disable();
            InputManager.RemoveKeyboardHandler(this);
            InputManager.RemoveMouseHandler(this);
        }

        public override void Update(GameTimer gameTimer)
        {
            CheckLayout();

            if (!IsActive)
                return;

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (!IsVisible)
                return;

            base.Draw(spriteBatch);
        }

        #region Input Handling
        public bool CapturedMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedKeyPressed(Key key, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedKeyReleased(Key key, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedKeyDown(Key key, GameTimer gameTimer)
        {
            return false;
        }

        public bool CapturedTextInput(char key, GameTimer gameTimer)
        {
            return false;
        }
        #endregion

    } // UIScreen
}
