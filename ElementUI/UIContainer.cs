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

        internal Vector2 _prevDragMousePosition;

        public UIContainer(string name, UIContainerStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(style.Background);
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.Background?.Update(gameTimer);
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            Style.Background?.Draw(this, spriteBatch, _position, _size);
            base.Draw(spriteBatch);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer))
                return true;

            if (IsDraggable)
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

            return false;
        }

        internal override void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (IsDragging)
                IsDragging = false;

            base.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }
    } // UIContainer
}
