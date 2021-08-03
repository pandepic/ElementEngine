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

        public UITextboxStyle(
            UILabelStyle textStyle,
            UISprite backgroundNormal,
            UISprite backgroundDisabled = null)
        {
            TextStyle = textStyle;
            BackgroundNormal = backgroundNormal;
            BackgroundDisabled = backgroundDisabled;
        }
    } // UITextboxStyle
}
