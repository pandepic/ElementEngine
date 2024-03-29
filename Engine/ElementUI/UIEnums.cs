﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public enum OverflowType
    {
        Show,
        Hide,
        Scroll,
    }

    public enum UIEventType
    {
        OnClick,
        OnValueChanged,
    }

    public enum UISizeFillType
    {
        Contain,
        Cover,
        Stretch,
    }

    public enum UIFontStyle
    {
        Normal,
        Italic,
    }

    public enum UIFontWeight
    {
        Normal,
        Thin,
        Light,
        Medium,
        Bold,
        Black,
    }

    public enum UIScrollbarButtonType
    {
        OutsideRail,
        CenterRailEdge,
        InsideRail,
    }

    public enum UIScrollbarSliderType
    {
        Contain,
        Center,
    }
    
    public enum UIScaleType
    {
        Scale,
        Crop,
    }

    public enum TooltipPositionType
    {
        Auto,
        Top,
        Bottom,
        Left,
        Right,
        Center,
    }
}
