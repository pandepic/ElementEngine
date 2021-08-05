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
        public UILabelStyle TextStyle;

        public int? CursorHeight; // if null then text height
        public int CursorWidth = 1;
        public RgbaByte CursorColor;
        public RgbaByte SelectionColor;

        public UITextboxStyle(
            UILabelStyle textStyle,
            UISprite backgroundNormal,
            UISprite backgroundDisabled = null,
            int? cursorHeight = null,
            int cursorWidth = 1,
            RgbaByte? cursorColor = null,
            RgbaByte? selectionColor = null)
        {
            TextStyle = textStyle;
            BackgroundNormal = backgroundNormal;
            BackgroundDisabled = backgroundDisabled;
            CursorHeight = cursorHeight;
            CursorWidth = cursorWidth;

            OverflowType = OverflowType.Hide;
            CursorColor = cursorColor ?? RgbaByte.White;
            SelectionColor = selectionColor ?? new RgbaByte(0, 0, 255, 120);
        }
    } // UITextboxStyle
}
