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
        public UISprite SpriteDisabled;

        public SpriteFont Font;
        public RgbaByte TextColorNormal;
        public RgbaByte TextColorHover;
        public int Size;
        public int Outline;

        public UICheckboxStyle(
            SpriteFont font,
            RgbaByte textColorNormal,
            int size,
            int outline,
            UISprite spriteUnchecked,
            UISprite spriteChecked,
            RgbaByte? textColorHover = null,
            UISprite spritePressed = null,
            UISprite spriteHover = null,
            UISprite spriteDisabled = null)
        {
            Font = font;
            TextColorNormal = textColorNormal;
            TextColorHover = textColorHover ?? textColorNormal;
            Size = size;
            Outline = outline;
            SpriteUnchecked = spriteUnchecked;
            SpriteChecked = spriteChecked;
            SpritePressed = spritePressed;
            SpriteHover = spriteHover;
            SpriteDisabled = spriteDisabled;
        }
    } // UICheckboxStyle
}
