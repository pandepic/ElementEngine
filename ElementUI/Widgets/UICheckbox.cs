using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIRadioSelectionGroup
    {
        public string Name;
        public List<UICheckbox> Children = new List<UICheckbox>();

        public UIRadioSelectionGroup(string name)
        {
            Name = name;
        }

        public void Select(UICheckbox child)
        {
            foreach (var checkChild in Children)
            {
                if (checkChild == child)
                    checkChild.IsChecked = true;
                else
                    checkChild.IsChecked = false;
            }
        }
    } // UIRadioSelectionGroup

    public class UICheckbox : UIObject
    {
        public new UICheckboxStyle Style => (UICheckboxStyle)_style;

        public readonly UIRadioSelectionGroup RadioGroup;
        public bool IsChecked;
        public bool IsPressed;
        public bool IsHovered;

        internal string _text = "";
        public string Text
        {
            get => _text;
            set => SetText(value);
        }

        public bool IsRadioButton => RadioGroup != null;

        public UICheckbox(string name, UICheckboxStyle style, string text, UIRadioSelectionGroup radioGroup = null) : base(name)
        {
            ApplyStyle(style);

            if (radioGroup != null)
            {
                RadioGroup = radioGroup;
                RadioGroup.Children.AddIfNotContains(this);
            }

            Text = text;
        }

        public void SetText(string text)
        {
            _text = text;
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.SpriteUnchecked?.Update(gameTimer);
            Style.SpriteChecked?.Update(gameTimer);
            Style.SpritePressed?.Update(gameTimer);
            Style.SpriteHover?.Update(gameTimer);
            Style.SpriteDisabledUnchecked?.Update(gameTimer);
            Style.SpriteDisabledChecked?.Update(gameTimer);

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = IsChecked ? Style.SpriteChecked : Style.SpriteUnchecked;

            if (!IsActive)
            {
                if (IsChecked)
                    sprite = Style.SpriteDisabledChecked ?? sprite;
                else
                    sprite = Style.SpriteDisabledUnchecked ?? sprite;
            }
            else
            {
                if (!IsChecked && IsPressed)
                    sprite = Style.SpritePressed ?? sprite;
                else if (IsHovered)
                    sprite = Style.SpriteHover ?? sprite;
            }

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
                TriggerEvent(UIEventType.OnValueChanged);

                if (!IsRadioButton)
                {
                    IsChecked = !IsChecked;
                }
                else
                {
                    if (!IsChecked)
                        RadioGroup.Select(this);
                }

                return true;
            }

            return false;
        }
    } // UICheckbox
}
