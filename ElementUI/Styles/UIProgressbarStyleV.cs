using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIProgressbarStyleV : UIStyle
    {
        public UIImageStyle Background;
        public UIImageStyle Fill;
        public int FillPadding = 0;

        public UIProgressbarStyleV(UIProgressbarStyleV copyFrom, bool baseCopy = false)
        {
            Background = copyFrom.Background;
            Fill = copyFrom.Fill;
            FillPadding = copyFrom.FillPadding;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIProgressbarStyleV(
            UIImageStyle background,
            UIImageStyle fill,
            int fillPadding = 0)
        {
            Background = background;
            Fill = fill;
            FillPadding = fillPadding;
        }
    }
}
