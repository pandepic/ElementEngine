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

        public UIButtonStyle(
            UIImageStyle spriteNormal,
            UIImageStyle spritePressed = null,
            UIImageStyle spriteHover = null,
            UIImageStyle spriteDisabled = null)
        {
            ImageNormal = spriteNormal;
            ImagePressed = spritePressed;
            ImageHover = spriteHover;
            ImageDisabled = spriteDisabled;
        }

    } // UIButtonStyle
}
