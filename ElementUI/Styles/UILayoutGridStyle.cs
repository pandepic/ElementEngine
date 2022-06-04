using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutGridStyle : UIStyle
    {
        public UISpacing CellPadding;
        public int? MaxColumns;

        public UILayoutGridStyle(UILayoutGridStyle copyFrom, bool baseCopy = false)
        {
            CellPadding = copyFrom.CellPadding;
            MaxColumns = copyFrom.MaxColumns;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UILayoutGridStyle(
            UISpacing cellPadding,
            int? maxColumns = null)
        {
            CellPadding = cellPadding;
            MaxColumns = maxColumns;
        }
    }
}
