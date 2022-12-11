using System;
using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScreen : UIObject
    {
        public UIObject FocusedObject;
        public UITooltipStyle TooltipStyle;

        public bool BlockInputWhenConsumed = false;

        internal UIContainer TooltipContainer;
        internal UIObject TooltipTarget;

        protected bool _removeTooltip;

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

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            if (_removeTooltip)
            {
                RemoveChild(TooltipContainer);
                TooltipContainer = null;
                TooltipTarget = null;
                _removeTooltip = false;
            }

            CheckLayout();

            if (!IsActive)
                return;
        }

        protected override void InnerDraw(SpriteBatch2D spriteBatch)
        {
            if (!IsVisible)
                return;
        }

        internal void ShowTooltip(UIObject parent, UITooltipContent content)
        {
            if (TooltipStyle == null)
                return;
            if (TooltipContainer != null && TooltipTarget == parent)
                return;

            if (TooltipContainer != null)
                RemoveChild(TooltipContainer);

            TooltipContainer = new("TooltipContainer", TooltipStyle.ContainerStyle);
            TooltipContainer.DrawOrder = int.MaxValue;
            TooltipContainer._isTooltip = true;
            TooltipContainer.Disable();

            var label = new UILabel("TooltipLabel", TooltipStyle.LabelStyle, content.Content);
            TooltipContainer.AddChild(label);

            TooltipTarget = parent;
            SetTooltipPosition(content.PositionType);

            AddChild(TooltipContainer);
        }

        protected bool SetTooltipPosition(TooltipPositionType positionType)
        {
            var parentPos = TooltipTarget.DrawPosition;

            TooltipContainer.UpdateLayout();

            switch (positionType)
            {
                case TooltipPositionType.Auto:
                    {
                        if (SetTooltipPosition(TooltipPositionType.Top))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Right))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Bottom))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Left))
                            return true;

                        return SetTooltipPosition(TooltipPositionType.Center);
                    }

                case TooltipPositionType.Top:
                    {
                        TooltipContainer.X = parentPos.X + TooltipStyle.Offset.X;
                        TooltipContainer.Y = parentPos.Y - TooltipContainer.Height - TooltipStyle.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Bottom:
                    {
                        TooltipContainer.X = parentPos.X + TooltipStyle.Offset.X;
                        TooltipContainer.Y = parentPos.Y + TooltipTarget.Height + TooltipStyle.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Left:
                    {
                        TooltipContainer.X = parentPos.X - TooltipContainer.Width - TooltipStyle.Offset.X;
                        TooltipContainer.Y = parentPos.Y + TooltipStyle.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Right:
                    {
                        TooltipContainer.X = parentPos.X + TooltipTarget.Width + TooltipStyle.Offset.X;
                        TooltipContainer.Y = parentPos.Y + TooltipStyle.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Center:
                    {
                        TooltipContainer.X = parentPos.X + (TooltipTarget.Width / 2) + TooltipStyle.Offset.X;
                        TooltipContainer.Y = parentPos.Y + (TooltipTarget.Height / 2) + TooltipStyle.Offset.Y;
                    }
                    break;
            }

            TooltipContainer.UpdateLayout();

            if (!Bounds.Contains(TooltipContainer.Bounds))
                return false;

            return true;
        }

        internal void HideTooltip(UIObject parent)
        {
            if (TooltipContainer == null)
                return;
            if (TooltipTarget != parent)
                return;

            _removeTooltip = true;
        }

        #region Input Handling
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
        #endregion

    } // UIScreen
}
