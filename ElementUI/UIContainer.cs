using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIContainer : UIObject
    {
        public new UIContainerStyle Style => (UIContainerStyle)_style;

        public UIContainer(string name, UIContainerStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(style.Background);
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.Background?.Update(gameTimer);
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            Style.Background?.Draw(this, spriteBatch, _position, _size);
            base.Draw(spriteBatch);
        }
    } // UIContainer
}
