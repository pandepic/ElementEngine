using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIContainerBackground
    {
        public UISprite Sprite;
        public UISize Size;
        public UIPosition UIPosition;
    }

    public class UIContainerStyle : UIStyle
    {
        public List<UIContainerBackground> Backgrounds = new List<UIContainerBackground>();
    }
}
