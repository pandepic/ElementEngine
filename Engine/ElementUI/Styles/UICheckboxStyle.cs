using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UICheckboxStyle : UIStyle
    {
        public UISprite SpriteUnchecked;
        public UISprite SpriteChecked;
        public UISprite SpritePressed;
        public UISprite SpriteHover;
        public UISprite SpriteDisabledUnchecked;
        public UISprite SpriteDisabledChecked;

        public UILabelStyle TextStyleNormal;
        public UILabelStyle TextStyleHover;
        public int TextPadding;

        public UICheckboxStyle(UICheckboxStyle copyFrom, bool baseCopy = false)
        {
            TextPadding = copyFrom.TextPadding;
            TextStyleNormal = copyFrom.TextStyleNormal;
            TextStyleHover = copyFrom.TextStyleHover;
            SpriteUnchecked = copyFrom.SpriteUnchecked;
            SpriteChecked = copyFrom.SpriteChecked;
            SpritePressed = copyFrom.SpritePressed;
            SpriteHover = copyFrom.SpriteHover;
            SpriteDisabledUnchecked = copyFrom.SpriteDisabledUnchecked;
            SpriteDisabledChecked = copyFrom.SpriteDisabledChecked;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UICheckboxStyle(
            UILabelStyle textStyleNormal,
            UISprite spriteUnchecked,
            UISprite spriteChecked,
            int textPadding = 0,
            UILabelStyle textStyleHover = null,
            UISprite spritePressed = null,
            UISprite spriteHover = null,
            UISprite spriteDisabledUnchecked = null,
            UISprite spriteDisabledChecked = null)
        {
            TextPadding = textPadding;
            TextStyleNormal = textStyleNormal;
            TextStyleHover = textStyleHover;
            SpriteUnchecked = spriteUnchecked;
            SpriteChecked = spriteChecked;
            SpritePressed = spritePressed;
            SpriteHover = spriteHover;
            SpriteDisabledUnchecked = spriteDisabledUnchecked;
            SpriteDisabledChecked = spriteDisabledChecked;
        }
    } // UICheckboxStyle
}
