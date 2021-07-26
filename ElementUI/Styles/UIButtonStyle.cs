using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIButtonStyle : UIStyle
    {
        public UISprite SpriteNormal;
        public UISprite SpritePressed;
        public UISprite SpriteHover;
        public UISprite SpriteDisabled;

        public UIButtonStyle(
            UISprite spriteNormal,
            UISprite spritePressed = null,
            UISprite spriteHover = null,
            UISprite spriteDisabled = null)
        {
            SpriteNormal = spriteNormal;
            SpritePressed = spritePressed;
            SpriteHover = spriteHover;
            SpriteDisabled = spriteDisabled;
        }

    } // UIButtonStyle
}
