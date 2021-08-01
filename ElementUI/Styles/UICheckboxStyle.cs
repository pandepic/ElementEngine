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

        public SpriteFont Font;
        public RgbaByte TextColorNormal;
        public RgbaByte TextColorHover;
        public int FontSize;
        public int Outline;
        public int TextPadding;

        public UICheckboxStyle(
            SpriteFont font,
            RgbaByte textColorNormal,
            int fontSize,
            int outline,
            int textPadding,
            UISprite spriteUnchecked,
            UISprite spriteChecked,
            RgbaByte? textColorHover = null,
            UISprite spritePressed = null,
            UISprite spriteHover = null,
            UISprite spriteDisabledUnchecked = null,
            UISprite spriteDisabledChecked = null)
        {
            Font = font;
            TextColorNormal = textColorNormal;
            TextColorHover = textColorHover ?? textColorNormal;
            FontSize = fontSize;
            Outline = outline;
            TextPadding = textPadding;
            SpriteUnchecked = spriteUnchecked;
            SpriteChecked = spriteChecked;
            SpritePressed = spritePressed;
            SpriteHover = spriteHover;
            SpriteDisabledUnchecked = spriteDisabledUnchecked;
            SpriteDisabledChecked = spriteDisabledChecked;
        }
    } // UICheckboxStyle
}
