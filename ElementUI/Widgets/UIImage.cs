using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIImage : UIObject
    {
        public new UIImageStyle Style => (UIImageStyle)_style;

        public UIImage(string name, UIImageStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.Sprite);
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.Sprite.Update(gameTimer);
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            Style.Sprite.Draw(this, spriteBatch, _position, _size);
            base.Draw(spriteBatch);
        }

    } // UIImage
}
