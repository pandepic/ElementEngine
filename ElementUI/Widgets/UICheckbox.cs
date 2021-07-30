using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UICheckbox : UIObject
    {
        public new UICheckboxStyle Style => (UICheckboxStyle)_style;

        public UICheckbox(string name, UICheckboxStyle style) : base(name)
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
    } // UICheckbox
}
