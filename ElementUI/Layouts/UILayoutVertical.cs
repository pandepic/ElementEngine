using ElementEngine.ElementUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutVertical : UIObject
    {
        public int CellPaddingTop;
        public int CellPaddingBottom;

        public UILayoutVertical(string name) : base(name)
        {
        }

        public override void UpdateLayout(bool secondCheck = true, bool updateScrollbars = true)
        {
            base.UpdateLayout(secondCheck, updateScrollbars);
            UpdateChildPositions();
        }

        public void UpdateChildPositions()
        {
            var currentY = 0;

            foreach (var child in Children)
            {
                currentY += CellPaddingTop;

                child.Y = currentY;

                currentY += child.Height + child.MarginBottom;
                currentY += CellPaddingBottom;
            }
        }
    }
}
