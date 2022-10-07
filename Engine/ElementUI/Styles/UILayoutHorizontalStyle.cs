using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutHorizontalStyle : UIStyle
    {
        public int CellPaddingLeft;
        public int CellPaddingRight;

        public UILayoutHorizontalStyle(UILayoutHorizontalStyle copyFrom, bool baseCopy = false)
        {
            CellPaddingLeft = copyFrom.CellPaddingLeft;
            CellPaddingRight = copyFrom.CellPaddingRight;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UILayoutHorizontalStyle(
            int cellPaddingLeft = 0,
            int cellPaddingRight = 0)
        {
            CellPaddingLeft = cellPaddingLeft;
            CellPaddingRight = cellPaddingRight;
        }
    }
}
