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

        public UIImageStyle() { }

        public UIImageStyle(UISprite sprite)
        {
            Sprite = sprite;
        }

    } // UIImageStyle
}
