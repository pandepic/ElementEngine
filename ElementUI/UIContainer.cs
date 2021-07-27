using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIContainer : UIObject
    {
        public UIContainer(string name, UIContainerStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(style.Background);
        }
    } // UIContainer
}
