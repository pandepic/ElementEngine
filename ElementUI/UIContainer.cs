using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIContainer : UIObject
    {
        public new UIContainerStyle Style => (UIContainerStyle)_style;

        public bool IsDraggable => (Style.DraggableRect.HasValue || Style.IsFullDraggableRect) && !_uiPosition.IsAutoPosition;
        public bool IsDragging { get; protected set; }

        public bool IgnoreMouseEvents = false;

        public readonly UIScrollbarV ScrollbarV;
        public readonly UIScrollbarH ScrollbarH;

        internal bool _dirtyNextUpdate = false;
        internal Vector2 _prevDragMousePosition;

        public UIContainer(string name, UIContainerStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(style.Background);

            if (_isScrollable)
            {
                if (Style.ScrollbarV != null)
                {
                    ScrollbarV = new UIScrollbarV(name + "_ScrollbarV", Style.ScrollbarV, 0, 0, ScrollSpeed, 0);
                    ScrollbarV._uiPosition.StopAutoPositionX();
                    ScrollbarV._uiPosition.StopAutoPositionY();
                    ScrollbarV.AnchorTop = true;
                    ScrollbarV.AnchorRight = true;
                    ScrollbarV.IgnoreOverflow = true;
                    ScrollbarV.DrawOrder = int.MaxValue;
                    AddChild(ScrollbarV);

                    ScrollbarV.OnValueChanged += (args) =>
                    {
                        var minY = (_uiSize._fullChildBounds.Bottom - PaddingBounds.Height) * -1;
                        var maxY = _uiSize._fullChildBounds.Top * -1;
                        var absY = Math.Abs(maxY - minY);

                        _childOffset.Y = minY + (absY - args.CurrentValue);
                        ClampScroll();
                    };

                    ScrollbarV.IsVisible = false;
                    ScrollbarV.IsActive = false;
                }

                if (Style.ScrollbarH != null)
                {
                    ScrollbarH = new UIScrollbarH(name + "_ScrollbarH", Style.ScrollbarH, 0, 0, ScrollSpeed, 0);
                    ScrollbarH._uiPosition.StopAutoPositionX();
                    ScrollbarH._uiPosition.StopAutoPositionY();
                    ScrollbarH.AnchorLeft = true;
                    ScrollbarH.AnchorBottom = true;
                    ScrollbarH.IgnoreOverflow = true;
                    ScrollbarH.DrawOrder = int.MaxValue;
                    AddChild(ScrollbarH);

                    ScrollbarH.OnValueChanged += (args) =>
                    {
                        var minX = (_uiSize._fullChildBounds.Right - PaddingBounds.Width) * -1;
                        var maxX = _uiSize._fullChildBounds.Left * -1;
                        var absX = Math.Abs(maxX - minX);

                        _childOffset.X = minX + (absX - args.CurrentValue);
                        ClampScroll();
                    };

                    ScrollbarH.IsVisible = false;
                    ScrollbarH.IsActive = false;
                }
            }

            UpdateScrollbars(true);
        } // constructor

        protected void UpdateScrollbars(bool force)
        {
            if (!_isScrollable)
                return;

            var minX = (_uiSize._fullChildBounds.Right - PaddingBounds.Width) * -1;
            var maxX = _uiSize._fullChildBounds.Left * -1;
            var minY = (_uiSize._fullChildBounds.Bottom - PaddingBounds.Height) * -1;
            var maxY = _uiSize._fullChildBounds.Top * -1;

            var absX = Math.Abs(maxX - minX);
            var absY = Math.Abs(maxY - minY);

            if (ScrollbarV != null)
            {
                if (absY == 0 || _uiSize._fullChildBounds.Height <= PaddingBounds.Height)
                {
                    ScrollbarV.IsVisible = false;
                    ScrollbarV.IsActive = false;
                }
                else
                {
                    var height = PaddingBounds.Height;

                    if (ScrollbarV.MaxValue != absY || ScrollbarV.Height != height || force)
                    {
                        ScrollbarV.MinValue = 0;
                        ScrollbarV.MaxValue = absY;
                        ScrollbarV.Height = height;

                        var diff = Math.Abs(_childOffset.Y - minY);
                        ScrollbarV.CurrentValue = absY - diff;

                        ScrollbarV.IsVisible = true;
                        ScrollbarV.IsActive = true;
                    }
                }
            }

            if (ScrollbarH != null)
            {
                if (absX == 0 || _uiSize._fullChildBounds.Width <= PaddingBounds.Width)
                {
                    ScrollbarH.IsVisible = false;
                    ScrollbarH.IsActive = false;
                }
                else
                {
                    var width = PaddingBounds.Width;
                    if (ScrollbarV != null && ScrollbarV.IsVisible)
                        width = PaddingBounds.Width - ScrollbarV._uiSize.Size.Value.X;

                    if (ScrollbarH.MaxValue != absX || ScrollbarH.Width != width || force)
                    {
                        ScrollbarH.MinValue = 0;
                        ScrollbarH.MaxValue = absX;
                        ScrollbarH.Width = width;

                        var diff = Math.Abs(_childOffset.X - minX);
                        ScrollbarH.CurrentValue = absX - diff;

                        ScrollbarH.IsVisible = true;
                        ScrollbarH.IsActive = true;
                    }
                }
            }
        } // UpdateScrollbars

        internal override void InternalOnScrollY()
        {
            UpdateScrollbars(true);
        }

        public override void UpdateLayout(bool secondCheck = true)
        {
            base.UpdateLayout(secondCheck);
            UpdateScrollbars(false);
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.Background?.Update(gameTimer);
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            Style.Background?.Draw(this, spriteBatch, DrawPosition, _size);
            base.Draw(spriteBatch);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer))
                return true;

            var disableDrag = false;

            if (ScrollbarV?.IsSliding == true)
                disableDrag = true;
            if (ScrollbarH?.IsSliding == true)
                disableDrag = true;

            if (IsDraggable && !disableDrag)
            {
                var draggableRect = (Style.IsFullDraggableRect ? new Rectangle(0, 0, Width, Height) : Style.DraggableRect.Value) + Position;

                if (draggableRect.Contains(mousePosition))
                {
                    IsDragging = true;
                    _prevDragMousePosition = mousePosition;
                }

                UIObject parent = Parent;
                UIObject child = this;

                while (parent != null)
                {
                    if (child is UIContainer container && container.IsDraggable)
                        parent.BringToFront(child);

                    child = parent;
                    parent = parent.Parent;
                }

                return true;
            }

            if (!IgnoreMouseEvents)
                return true;
            else
                return false;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer))
                return true;

            if (IsDragging)
            {
                IsDragging = false;
                //return false;
            }

            if (!IgnoreMouseEvents)
                return true;
            else
                return false;
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer))
                return true;

            if (IsDragging && _prevDragMousePosition.ToVector2I() != mousePosition.ToVector2I())
            {
                if (!_uiPosition.Position.HasValue)
                    _uiPosition.Position = new Vector2I();

                var offset = mousePosition.ToVector2I() - _prevDragMousePosition.ToVector2I();
                _uiPosition.Position += offset;
                _prevDragMousePosition = mousePosition;
                SetLayoutDirty();
                return true;
            }

            if (!IgnoreMouseEvents)
                return true;
            else
                return false;
        }

        internal override void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (IsDragging)
                IsDragging = false;

            base.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal override bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            if (!captured && !IgnoreMouseEvents)
                return true;
            else
                return captured;
        }

        internal override bool InternalHandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var captured = base.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

            if (!captured && !IgnoreMouseEvents)
                return true;
            else
                return captured;
        }
    } // UIContainer
}
