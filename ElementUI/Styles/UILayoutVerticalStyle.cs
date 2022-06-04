using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutVerticalStyle : UIStyle
    {
        public int CellPaddingTop;
        public int CellPaddingBottom;

        public UILayoutVerticalStyle(UILayoutVerticalStyle copyFrom, bool baseCopy = false)
        {
            CellPaddingTop = copyFrom.CellPaddingTop;
            CellPaddingBottom = copyFrom.CellPaddingBottom;

            if (baseCopy)
                BaseCopy(copyFrom);
        }
        
        public UILayoutVerticalStyle(
            int cellPaddingTop = 0,
            int cellPaddingBottom = 0)
        {
            CellPaddingTop = cellPaddingTop;
            CellPaddingBottom = cellPaddingBottom;
        }
    }
}
