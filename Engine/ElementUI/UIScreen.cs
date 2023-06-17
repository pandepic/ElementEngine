using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScreen : UIObject
    {
        public UIObject FocusedObject;

        public bool BlockInputWhenConsumed = false;

        internal UIContainer TooltipContainer;
        internal UIObject TooltipTarget;

        internal UIObject ExpandedDropdown;

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
            if (TooltipContainer != null && TooltipTarget == parent)
                return;

            if (TooltipContainer != null)
                RemoveChild(TooltipContainer);

            TooltipContainer = new("TooltipContainer", content.Style.ContainerStyle);
            TooltipContainer.DrawOrder = int.MaxValue;
            TooltipContainer._isTooltip = true;
            TooltipContainer.Disable();

            var label = new UILabel("TooltipLabel", content.Style.LabelStyle, content.Content);
            TooltipContainer.AddChild(label);

            TooltipTarget = parent;
            SetTooltipPosition(content.PositionType, content.Style);

            AddChild(TooltipContainer);
        }

        protected bool SetTooltipPosition(TooltipPositionType positionType, UITooltipStyle style)
        {
            var parentPos = TooltipTarget.DrawPosition;

            TooltipContainer.UpdateLayout();

            switch (positionType)
            {
                case TooltipPositionType.Auto:
                    {
                        if (SetTooltipPosition(TooltipPositionType.Top, style))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Right, style))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Bottom, style))
                            return true;
                        if (SetTooltipPosition(TooltipPositionType.Left, style))
                            return true;

                        return SetTooltipPosition(TooltipPositionType.Center, style);
                    }

                case TooltipPositionType.Top:
                    {
                        TooltipContainer.X = parentPos.X + style.Offset.X;
                        TooltipContainer.Y = parentPos.Y - TooltipContainer.Height - style.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Bottom:
                    {
                        TooltipContainer.X = parentPos.X + style.Offset.X;
                        TooltipContainer.Y = parentPos.Y + TooltipTarget.Height + style.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Left:
                    {
                        TooltipContainer.X = parentPos.X - TooltipContainer.Width - style.Offset.X;
                        TooltipContainer.Y = parentPos.Y + style.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Right:
                    {
                        TooltipContainer.X = parentPos.X + TooltipTarget.Width + style.Offset.X;
                        TooltipContainer.Y = parentPos.Y + style.Offset.Y;
                    }
                    break;

                case TooltipPositionType.Center:
                    {
                        TooltipContainer.X = parentPos.X + (TooltipTarget.Width / 2) + style.Offset.X;
                        TooltipContainer.Y = parentPos.Y + (TooltipTarget.Height / 2) + style.Offset.Y;
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
            var captured = false;

            if (ExpandedDropdown != null)
                captured = ExpandedDropdown.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            if (!captured)
                captured = base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseMotionBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            FocusedObject = null;

            var captured = false;

            if (ExpandedDropdown != null)
                captured = ExpandedDropdown.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (!captured)
                captured = base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonPressedBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var captured = false;

            if (ExpandedDropdown != null)
                captured = ExpandedDropdown.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);

            if (!captured)
                captured = base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonReleasedBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var captured = false;

            if (ExpandedDropdown != null)
                captured = ExpandedDropdown.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            if (!captured)
                captured = base.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            if (captured && BlockInputWhenConsumed)
                InputManager._mouseButtonDownBlocked = true;

            return captured;
        }

        internal override bool InternalHandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var captured = false;

            if (ExpandedDropdown != null)
                captured = ExpandedDropdown.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

            if (!captured)
                captured = base.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

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
    }
}
