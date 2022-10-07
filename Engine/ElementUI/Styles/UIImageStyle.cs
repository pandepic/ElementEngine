using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIImageStyle : UIStyle
    {
        public UISprite Sprite;
        public UIScaleType? ScaleType;

        public UIImageStyle(UIImageStyle copyFrom, bool baseCopy = false)
        {
            Sprite = copyFrom.Sprite;
            ScaleType = copyFrom.ScaleType;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIImageStyle(UISprite sprite, UIScaleType? scaleType = null)
        {
            Sprite = sprite;
            ScaleType = scaleType;
        }

    } // UIImageStyle
}
