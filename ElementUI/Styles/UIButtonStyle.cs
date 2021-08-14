using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIButtonStyle : UIStyle
    {
        public UIImageStyle ImageNormal;
        public UIImageStyle ImagePressed;
        public UIImageStyle ImageHover;
        public UIImageStyle ImageDisabled;
        public UIImageStyle ImageSelected;

        public UIButtonStyle(UIButtonStyle copyFrom, bool baseCopy = false)
        {
            ImageNormal = copyFrom.ImageNormal;
            ImagePressed = copyFrom.ImagePressed;
            ImageHover = copyFrom.ImageHover;
            ImageDisabled = copyFrom.ImageDisabled;
            ImageSelected = copyFrom.ImageSelected;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIButtonStyle(
            UIImageStyle spriteNormal,
            UIImageStyle spritePressed = null,
            UIImageStyle spriteHover = null,
            UIImageStyle spriteDisabled = null,
            UIImageStyle spriteSelected = null)
        {
            ImageNormal = spriteNormal;
            ImagePressed = spritePressed;
            ImageHover = spriteHover;
            ImageDisabled = spriteDisabled;
            ImageSelected = spriteSelected;
        }

    } // UIButtonStyle
}
