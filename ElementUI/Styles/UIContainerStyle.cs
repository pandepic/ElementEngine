using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIContainerStyle : UIStyle
    {
        public UISprite Background;

        public UIContainerStyle(UISprite background)
        {
            Background = background;
        }
    }
}
