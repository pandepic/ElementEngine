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

        public bool BlockInputWhenConsumed = false;

        public UIScreen(Vector2I? position = null, Vector2I? size = null, string name = "Screen", bool blockInputWhenConsumed = true) : base(name)
        {
            BlockInputWhenConsumed = blockInputWhenConsumed;

            if (BlockInputWhenConsumed)
            {
                KeyboardPriority = int.MinValue;
                MousePriority = int.MinValue;
            }

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

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseMotionBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            FocusedObject = null;

            var captured = base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonPressedBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonReleasedBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonDownBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseWheelBlocked = true;

            return captured;
        }

        public override bool InternalHandleKeyPressed(Key key, GameTimer gameTimer)
        {
            var captured = base.InternalHandleKeyPressed(key, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._keyPressedBlocked = true;

            return captured;
        }

        public override bool InternalHandleKeyReleased(Key key, GameTimer gameTimer)
        {
            var captured = base.InternalHandleKeyReleased(key, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._keyReleasedBlocked = true;

            return captured;
        }

        public override bool InternalHandleKeyDown(Key key, GameTimer gameTimer)
        {
            var captured = base.InternalHandleKeyDown(key, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._keyDownBlocked = true;

            return captured;
        }

        public override bool InternalHandleTextInput(char key, GameTimer gameTimer)
        {
            var captured = base.InternalHandleTextInput(key, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._textInputBlocked = true;

            return captured;
        }

    } // UIScreen
}
