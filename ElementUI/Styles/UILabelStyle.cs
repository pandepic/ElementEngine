﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UILabelStyle : UIStyle
    {
        public UIFontFamily FontFamily;
        public RgbaByte Color;
        public int FontSize;
        public int Outline;

        public UIFontStyle? FontStyle;
        public UIFontWeight? FontWeight;

        public UILabelStyle(
            UIFontFamily fontFamily,
            RgbaByte color,
            int fontSize,
            int outline = 0,
            UIFontStyle? fontStyle = null,
            UIFontWeight? fontWeight = null)
        {
            FontFamily = fontFamily;
            Color = color;
            FontSize = fontSize;
            Outline = outline;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
        }

    } // UILabelStyle
}
