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

        public UICheckbox GetSelected()
        {
            foreach (var child in Children)
            {
                if (child.IsChecked)
                    return child;
            }

            return null;
        }
    } // UIRadioSelectionGroup

    public class UICheckbox : UIObject
    {
        public new UICheckboxStyle Style => (UICheckboxStyle)_style;

        public readonly UIRadioSelectionGroup RadioGroup;
        public bool IsPressed;
        public bool IsHovered;

        public UIFontStyle FontStyle;
        public UIFontWeight FontWeight;

        public bool IsRadioButton => RadioGroup != null;
        public UILabelStyle TextStyle => IsHovered ? Style.TextStyleHover ?? Style.TextStyleNormal : Style.TextStyleNormal;
        
        public event Action<UIOnValueChangedArgs<bool>> OnValueChanged;
        
        internal bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                var prev = _isChecked;
                _isChecked = value;

                if (prev != _isChecked)
                    OnValueChanged?.Invoke(new UIOnValueChangedArgs<bool>(this, prev, _isChecked));
            }
        }

        internal string _text = "";
        public string Text
        {
            get => _text;
            set => SetText(value);
        }

        internal Vector2I _textSize;

        public UICheckbox(string name, UICheckboxStyle style, string text, UIRadioSelectionGroup radioGroup = null) : base(name)
        {
            ApplyStyle(style);

            FontWeight = TextStyle.FontWeight ?? FontWeight;
            FontStyle = TextStyle.FontStyle ?? FontStyle;

            if (radioGroup != null)
            {
                RadioGroup = radioGroup;
                RadioGroup.Children.AddIfNotContains(this);

                if (RadioGroup.Children.Count == 1)
                    RadioGroup.Select(this);
            }

            Text = text;
        }

        public void SetText(string text)
        {
            _text = text;
            _textSize = TextStyle.FontFamily.GetFont(FontStyle, FontWeight).MeasureText(text, TextStyle.FontSize, TextStyle.Outline).ToVector2I();

            Size = new Vector2I(
                (int)(Style.SpriteUnchecked.Size.X + Style.TextPadding + _textSize.X),
                Math.Max(_textSize.Y, Style.SpriteUnchecked.Size.Y));
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
            var textColor = TextStyle.Color;

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
                else if (!IsChecked && IsHovered)
                    sprite = Style.SpriteHover ?? sprite;
            }

            var textSize = TextStyle.FontFamily.GetFont(FontStyle, FontWeight).MeasureText(_text, TextStyle.FontSize, TextStyle.Outline).ToVector2I();
            if (textSize != _textSize)
                SetText(_text);

            var textPosition = new Vector2I(
                DrawPosition.X + sprite.Size.X + Style.TextPadding,
                DrawPosition.Y + ((sprite.Size.Y / 2f) - (_textSize.Y / 2)));

            sprite?.Draw(this, spriteBatch, DrawPosition, null);
            TextStyle.FontFamily.GetFont(FontStyle, FontWeight).DrawText(spriteBatch, _text, textPosition.ToVector2(), textColor, TextStyle.FontSize, TextStyle.Outline);

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
