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

        public UITextbox(string name, UITextboxStyle style) : base(name)
        {
            ApplyStyle(style);
        }

        public override void Update(GameTimer gameTimer)
        {
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            base.Draw(spriteBatch);
        }
    } // UITextbox
}
