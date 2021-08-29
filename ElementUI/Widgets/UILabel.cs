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
            get => _text;
            set => SetText(value);
        }

        public string InternalText
        {
            get => _internalText;
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
            _text = text;

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
                // todo
            }

            TextSize = CurrentFont.MeasureText(Text, Style.FontSize, Style.Outline).ToVector2I();
            Size = TextSize;
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(CurrentFont, Text, DrawPosition.ToVector2(), Style.Color, Style.FontSize, Style.Outline);
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
