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
        public UIScreen(Vector2I? position = null, Vector2I? size = null, string name = "Screen") : base(name)
        {
            _uiPosition.Position = position ?? Vector2I.Zero;
            _uiSize.Size = size ?? new Vector2I(ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight);

            UpdateLayout();
            Enable();
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
        public override void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public override void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public override void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public override void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public override void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { }

        public override void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public override void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public override void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public override void HandleTextInput(char key, GameTimer gameTimer) { }
        #endregion

    } // UIScreen
}
