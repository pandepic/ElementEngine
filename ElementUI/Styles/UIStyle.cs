using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIStyle
    {
        public UIPosition? UIPosition;
        public UISize? UISize;
        public UISpacing? Margins;
        public UISpacing? Padding;
        public OverflowType OverflowType = OverflowType.Show;
    }
}
