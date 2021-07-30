using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILabel : UIObject
    {
        public new UILabelStyle Style => (UILabelStyle)_style;

        internal string _text = "";
        public string Text
        {
            get => _text;
            set => SetText(value);
        }

        public UILabel(string name, UILabelStyle style, string text) : base(name)
        {
            ApplyStyle(style);
            SetText(text);

            CanFocus = false;
        }

        public void SetText(string text)
        {
            _text = text;
            Size = Style.Font.MeasureText(_text, Style.Size, Style.Outline).ToVector2I();
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(Style.Font, _text, _position.ToVector2(), Style.Color, Style.Size, Style.Outline);
            base.Draw(spriteBatch);
        }
    }
}
