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

        public UITextboxStyle(UITextboxStyle copyFrom, bool baseCopy = false)
        {
            TextStyle = copyFrom.TextStyle;
            BackgroundNormal = copyFrom.BackgroundNormal;
            BackgroundDisabled = copyFrom.BackgroundDisabled;
            CursorHeight = copyFrom.CursorHeight;
            CursorWidth = copyFrom.CursorWidth;
            OverflowType = copyFrom.OverflowType;
            CursorColor = copyFrom.CursorColor;
            SelectionColor = copyFrom.SelectionColor;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

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
            SelectionColor = selectionColor ?? new RgbaByte(100, 149, 237, 120);
        }
    } // UITextboxStyle
}
