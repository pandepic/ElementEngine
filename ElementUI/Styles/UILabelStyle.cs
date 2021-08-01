using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UILabelStyle : UIStyle
    {
        public SpriteFont Font;
        public RgbaByte Color;
        public int FontSize;
        public int Outline;

        public UILabelStyle(
            SpriteFont font,
            RgbaByte color,
            int fontSize,
            int outline = 0)
        {
            Font = font;
            Color = color;
            FontSize = fontSize;
            Outline = outline;
        }

    } // UILabelStyle
}
