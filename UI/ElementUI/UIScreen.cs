using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine.ElementUI
{
    public class UIScreen : UIObject
    {
        public readonly List<UIContainer> Containers = new List<UIContainer>();

        public UIScreen()
        {
            LocalPosition = Vector2.Zero;
            Size = new Vector2();
            IsScreen = true;
        }
    } // UIScreen
}
