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

        public event Action<UIOnClickArgs> OnClick;
        public event Action<UIOnClickArgs> OnMouseDown;
        
        public bool IsPressed { get; protected set; }
        public bool IsHovered { get; protected set; }

        public readonly UIImage ImageNormal;
        public readonly UIImage ImagePressed;
        public readonly UIImage ImageHover;
        public readonly UIImage ImageDisabled;

        internal UIImage _prevCurrentImage = null;

        public UIButton(string name, UIButtonStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.ImageNormal.Sprite);

            ImageNormal = new UIImage(name + "_Normal", Style.ImageNormal);
            AddChild(ImageNormal);

            if (Style.ImagePressed != null)
            {
                ImagePressed = new UIImage(name + "_Normal", Style.ImagePressed);
                AddChild(ImagePressed);
            }

            if (Style.ImageHover != null)
            {
                ImageHover = new UIImage(name + "_Normal", Style.ImageHover);
                AddChild(ImageHover);
            }

            if (Style.ImageDisabled != null)
            {
                ImageDisabled = new UIImage(name + "_Normal", Style.ImageDisabled);
                AddChild(ImageDisabled);
            }

            UpdateCurrentImage();
        }

        public void UpdateCurrentImage()
        {
            var currentImage = ImageNormal;

            if (!IsActive)
            {
                currentImage = ImageDisabled ?? currentImage;
            }
            else
            {
                if (IsPressed)
                    currentImage = ImagePressed ?? currentImage;
                else if (IsHovered)
                    currentImage = ImageHover ?? currentImage;
            }

            if (_prevCurrentImage != currentImage)
            {
                ImageNormal?.Hide();
                ImageNormal?.Disable();
                ImagePressed?.Hide();
                ImagePressed?.Disable();
                ImageHover?.Hide();
                ImageHover?.Disable();
                ImageDisabled?.Hide();
                ImageDisabled?.Disable();

                currentImage.Show();
                currentImage.Enable();
            }

        } // UpdateCurrentImage

        public override void Update(GameTimer gameTimer)
        {
            if (!IsVisible)
                UpdateCurrentImage();

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            UpdateCurrentImage();
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

        internal override bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonDown(mousePosition, button, gameTimer))
                return true;

            if (IsPressed)
            {
                OnMouseDown?.Invoke(new UIOnClickArgs(this, button));
                return true;
            }

            return false;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer))
                return true;

            if (IsPressed)
            {
                IsPressed = false;
                OnClick?.Invoke(new UIOnClickArgs(this, button));
                return true;
            }

            return false;
        }

    } // UIButton
}
