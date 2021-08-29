using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public enum LabelDisplayMode
    {
        Normal,
        Password,
    }

    public class UILabelStyle : UIStyle
    {
        public UIFontFamily FontFamily;
        public RgbaByte Color;
        public int FontSize;
        public int Outline;
        public int? WordWrapWidth;
        public LabelDisplayMode LabelDisplayMode;

        public UIFontStyle? FontStyle;
        public UIFontWeight? FontWeight;

        public UILabelStyle(UILabelStyle copyFrom, bool baseCopy = false)
        {
            FontFamily = copyFrom.FontFamily;
            Color = copyFrom.Color;
            FontSize = copyFrom.FontSize;
            Outline = copyFrom.Outline;
            FontWeight = copyFrom.FontWeight;
            FontStyle = copyFrom.FontStyle;
            WordWrapWidth = copyFrom.WordWrapWidth;
            LabelDisplayMode = copyFrom.LabelDisplayMode;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UILabelStyle(
            UIFontFamily fontFamily,
            RgbaByte color,
            int fontSize,
            int outline = 0,
            UIFontStyle? fontStyle = null,
            UIFontWeight? fontWeight = null,
            int? wordWrapWidth = null,
            LabelDisplayMode? labelDisplayMode = null)
        {
            FontFamily = fontFamily;
            Color = color;
            FontSize = fontSize;
            Outline = outline;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
            WordWrapWidth = wordWrapWidth;
            LabelDisplayMode = labelDisplayMode ?? LabelDisplayMode;
        }

    } // UILabelStyle
}
