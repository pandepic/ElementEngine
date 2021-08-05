using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIScrollbarStyleH : UIStyle
    {
        public UIImageStyle Rail;
        public UIButtonStyle Slider;
        public UIButtonStyle ButtonLeft;
        public UIButtonStyle ButtonRight;
        public UIScrollbarButtonType ButtonType;
        public UIScrollbarSliderType SliderType;

        public UIScrollbarStyleH(
            UIImageStyle rail,
            UIButtonStyle slider,
            UIButtonStyle buttonLeft = null,
            UIButtonStyle buttonRight = null,
            UIScrollbarButtonType buttonType = UIScrollbarButtonType.OutsideRail,
            UIScrollbarSliderType sliderType = UIScrollbarSliderType.Center)
        {
            Rail = rail;
            Slider = slider;
            ButtonLeft = buttonLeft;
            ButtonRight = buttonRight;
            ButtonType = buttonType;
            SliderType = sliderType;
        }
    } // UIScrollbarStyleH
}
