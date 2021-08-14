using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIScrollbarStyleV : UIStyle
    {
        public UIImageStyle Rail;
        public UIImageStyle RailFill;
        public UIButtonStyle Slider;
        public UIButtonStyle ButtonUp;
        public UIButtonStyle ButtonDown;
        public UIScrollbarButtonType ButtonType;
        public UIScrollbarSliderType SliderType;
        public int RailFillPadding = 0;

        public UIScrollbarStyleV(UIScrollbarStyleV copyFrom, bool baseCopy = false)
        {
            Rail = copyFrom.Rail;
            RailFill = copyFrom.RailFill;
            Slider = copyFrom.Slider;
            ButtonUp = copyFrom.ButtonUp;
            ButtonDown = copyFrom.ButtonDown;
            ButtonType = copyFrom.ButtonType;
            SliderType = copyFrom.SliderType;
            RailFillPadding = copyFrom.RailFillPadding;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIScrollbarStyleV(
            UIImageStyle rail,
            UIButtonStyle slider,
            UIButtonStyle buttonUp = null,
            UIButtonStyle buttonDown = null,
            UIScrollbarButtonType buttonType = UIScrollbarButtonType.OutsideRail,
            UIScrollbarSliderType sliderType = UIScrollbarSliderType.Center,
            UIImageStyle railFill = null,
            int railFillPadding = 0)
        {
            Rail = rail;
            RailFill = railFill;
            Slider = slider;
            ButtonUp = buttonUp;
            ButtonDown = buttonDown;
            ButtonType = buttonType;
            SliderType = sliderType;
            RailFillPadding = railFillPadding;
        }
    } // UIScrollbarStyleV
}
