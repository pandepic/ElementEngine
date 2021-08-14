using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIButtonTabGroup
    {
        public string Name;
        public List<UIButton> Children = new List<UIButton>();
        public event Action<UIOnSelectedTabChangedArgs> OnValueChanged;

        public UIButtonTabGroup(string name)
        {
            Name = name;
        }

        public void Select(string name)
        {
            foreach (var child in Children)
            {
                if (child.Name == name)
                {
                    Select(child);
                    return;
                }
            }
        }

        public void Select(UIButton child)
        {
            var prevSelected = GetSelected();

            foreach (var checkChild in Children)
            {
                if (checkChild == child)
                    checkChild.IsSelected = true;
                else
                    checkChild.IsSelected = false;
            }

            if (prevSelected != child)
                OnValueChanged?.Invoke(new UIOnSelectedTabChangedArgs(prevSelected, child));
        }

        public UIButton GetSelected()
        {
            foreach (var child in Children)
            {
                if (child.IsSelected)
                    return child;
            }

            return null;
        }
    } // UIButtonTabGroup

    public class UIButton : UIObject
    {
        public new UIButtonStyle Style => (UIButtonStyle)_style;

        public bool IsTab => TabGroup != null;
        public UIButtonTabGroup TabGroup;

        public event Action<UIOnClickArgs> OnClick;
        public event Action<UIOnClickArgs> OnMouseDown;

        public bool IsPressed { get; protected set; }
        public bool IsHovered { get; protected set; }

        internal bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
            }
        }

        public readonly UIImage ImageNormal;
        public readonly UIImage ImagePressed;
        public readonly UIImage ImageHover;
        public readonly UIImage ImageDisabled;
        public readonly UIImage ImageSelected;

        internal UIImage _prevImage;

        public UIButton(string name, UIButtonStyle style, UIButtonTabGroup tabGroup = null) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.ImageNormal.Sprite);

            ImageNormal = new UIImage(name + "_Normal", Style.ImageNormal);
            AddChild(ImageNormal);

            if (Style.ImagePressed != null)
            {
                ImagePressed = new UIImage(name + "_Pressed", Style.ImagePressed);
                AddChild(ImagePressed);
            }

            if (Style.ImageHover != null)
            {
                ImageHover = new UIImage(name + "_Hover", Style.ImageHover);
                AddChild(ImageHover);
            }

            if (Style.ImageDisabled != null)
            {
                ImageDisabled = new UIImage(name + "_Disabled", Style.ImageDisabled);
                AddChild(ImageDisabled);
            }

            if (Style.ImageSelected != null)
            {
                ImageSelected = new UIImage(name + "_Selected", Style.ImageSelected);
                AddChild(ImageSelected);
            }

            if (tabGroup != null)
            {
                TabGroup = tabGroup;
                TabGroup.Children.AddIfNotContains(this);

                if (TabGroup.Children.Count == 1)
                    TabGroup.Select(this);
            }

            UpdateCurrentImage();
        }

        public void UpdateCurrentImage()
        {
            var currentImage = ImageNormal;

            if (IsSelected)
                currentImage = ImageSelected ?? currentImage;

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

            if (_prevImage != currentImage)
            {
                ImageNormal?.HideDisable();
                ImagePressed?.HideDisable();
                ImageHover?.HideDisable();
                ImageDisabled?.HideDisable();
                ImageSelected?.HideDisable();

                currentImage.ShowEnable();
                _prevImage = currentImage;
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

                if (IsTab)
                    TabGroup.Select(this);

                return true;
            }

            return false;
        }

    } // UIButton
}
