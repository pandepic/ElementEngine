using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UITextbox : UIObject
    {
        public new UITextboxStyle Style => (UITextboxStyle)_style;

        public event Action<UIOnValueChangedArgs<string>> OnValueChanged;

        public UITextbox(string name, UITextboxStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.BackgroundNormal);
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.BackgroundNormal?.Update(gameTimer);
            Style.BackgroundDisabled?.Update(gameTimer);

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = IsActive ? Style.BackgroundNormal : Style.BackgroundDisabled;
            sprite?.Draw(this, spriteBatch, DrawPosition, _size);

            base.Draw(spriteBatch);
        }
    } // UITextbox
}
