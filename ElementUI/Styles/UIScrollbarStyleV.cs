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
        public UIButtonStyle Slider;
        public UIButtonStyle ButtonUp;
        public UIButtonStyle ButtonDown;
        public UIScrollbarButtonType ButtonType;
        public UIScrollbarSliderType SliderType;

        public UIScrollbarStyleV(
            UIImageStyle rail,
            UIButtonStyle slider,
            UIButtonStyle buttonUp = null,
            UIButtonStyle buttonDown = null,
            UIScrollbarButtonType buttonType = UIScrollbarButtonType.OutsideRail,
            UIScrollbarSliderType sliderType = UIScrollbarSliderType.Center)
        {
            Rail = rail;
            Slider = slider;
            ButtonUp = buttonUp;
            ButtonDown = buttonDown;
            ButtonType = buttonType;
            SliderType = sliderType;
        }
    } // UIScrollbarStyleV
}
