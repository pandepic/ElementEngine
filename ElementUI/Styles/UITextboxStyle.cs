using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UITextboxStyle : UIStyle
    {
        public UISprite BackgroundNormal;
        public UISprite BackgroundDisabled;

        public SpriteFont Font;
        public RgbaByte TextColor;
        public int Size;
        public int Outline;
        
        public UITextboxStyle(
            SpriteFont font,
            RgbaByte textColor,
            int size,
            int outline,
            UISprite backgroundNormal,
            UISprite backgroundDisabled = null)
        {
            Font = font;
            TextColor = textColor;
            Size = size;
            Outline = outline;
            BackgroundNormal = backgroundNormal;
            BackgroundDisabled = backgroundDisabled;
        }
    } // UITextboxStyle
}
