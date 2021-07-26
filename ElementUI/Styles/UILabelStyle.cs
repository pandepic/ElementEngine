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
        public int Size;
        public int Outline;

        public UILabelStyle(
            SpriteFont font,
            RgbaByte color,
            int size,
            int outline = 0)
        {
            Font = font;
            Color = color;
            Size = size;
            Outline = outline;
        }

    } // UILabelStyle
}
