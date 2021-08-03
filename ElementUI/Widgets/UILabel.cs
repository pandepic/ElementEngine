using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UILabel : UIObject
    {
        public new UILabelStyle Style => (UILabelStyle)_style;

        public UIFontStyle FontStyle;
        public UIFontWeight FontWeight;

        internal string _text = "";
        public string Text
        {
            get => _text;
            set => SetText(value);
        }

        public UILabel(string name, UILabelStyle style, string text) : base(name)
        {
            ApplyStyle(style);

            FontWeight = Style.FontWeight ?? FontWeight;
            FontStyle = Style.FontStyle ?? FontStyle;

            SetText(text);
            CanFocus = false;
        }

        public void SetText(string text)
        {
            _text = text;
            Size = Style.FontFamily.GetFont(FontStyle, FontWeight).MeasureText(_text, Style.FontSize, Style.Outline).ToVector2I();
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(Style.FontFamily.GetFont(FontStyle, FontWeight), _text, DrawPosition.ToVector2(), Style.Color, Style.FontSize, Style.Outline);
            base.Draw(spriteBatch);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            return base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            return base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            return base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

    } // UILabel
}
