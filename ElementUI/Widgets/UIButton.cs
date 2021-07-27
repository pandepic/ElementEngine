using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = Style.SpriteNormal;

            if (!IsActive)
                sprite = Style.SpriteDisabled ?? Style.SpriteNormal;

            sprite?.Draw(this, spriteBatch, _position, _size);
            base.Draw(spriteBatch);
        }
    }
}
