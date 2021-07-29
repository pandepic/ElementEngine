using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIButton : UIObject
    {
        public new UIButtonStyle Style => (UIButtonStyle)_style;

        public bool IsPressed;
        public bool IsHovered;

        public UIButton(string name, UIButtonStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.SpriteNormal);
        }

        public UISprite CurrentSprite()
        {
            var sprite = Style.SpriteNormal;

            if (!IsActive)
            {
                sprite = Style.SpriteDisabled ?? Style.SpriteNormal;
            }
            else
            {
                if (IsPressed)
                    sprite = Style.SpritePressed ?? Style.SpriteNormal;
                else if (IsHovered)
                    sprite = Style.SpriteHover ?? Style.SpriteNormal;
            }

            return sprite;
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.SpriteNormal?.Update(gameTimer);
            Style.SpriteDisabled?.Update(gameTimer);
            Style.SpritePressed?.Update(gameTimer);
            Style.SpriteHover?.Update(gameTimer);

            base.Update(gameTimer);

            //if (IsHovered && !Bounds.Contains(InputManager.MousePosition))
            //    IsHovered = false;
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = CurrentSprite();

            sprite?.Draw(this, spriteBatch, _position, _size);
            base.Draw(spriteBatch);
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer))
                return true;

            IsHovered = true;
            return true;
        }

        internal override void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            IsHovered = false;
            IsPressed = false;

            base.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer))
                return true;

            IsPressed = true;
            return true;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer))
                return true;

            if (IsPressed)
            {
                IsPressed = false;
                TriggerEvent(UIEventType.OnClick);
                return true;
            }

            return false;
        }

    } // UIButton
}
