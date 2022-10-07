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
        public Vector2I TextSize { get; protected set; }

        internal string _internalText = "";
        internal string _text = "";
        public string Text
        {
            get => _internalText;
            set => SetText(value);
        }
        
        public string DisplayText
        {
            get => _text;
        }

        public SpriteFont CurrentFont => Style.FontFamily.GetFont(FontStyle, FontWeight);

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
            _internalText = text;

            if (Style.LabelDisplayMode == LabelDisplayMode.Normal)
            {
                _text = text;
            }
            else if (Style.LabelDisplayMode == LabelDisplayMode.Password)
            {
                _text = "";

                for (var i = 0; i < _internalText.Length; i++)
                    _text += "*";
            }

            if (Style.WordWrapWidth.HasValue)
            {
                var width = 0;
                var words = _internalText.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                _text = "";

                foreach (var word in words)
                {
                    var textSize = CurrentFont.MeasureText(word, Style.FontSize, Style.Outline);

                    if (width + textSize.X > Style.WordWrapWidth.Value)
                    {
                        width = 0;
                        _text += "\n";
                    }

                    _text += word + " ";
                    width += (int)textSize.X;
                }
            }

            TextSize = CurrentFont.MeasureText(_text, Style.FontSize, Style.Outline).ToVector2I();
            Size = TextSize;
        }

        protected override void InnerDraw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(CurrentFont, DisplayText, DrawPosition.ToVector2(), Style.Color, Style.FontSize, Style.Outline);
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
