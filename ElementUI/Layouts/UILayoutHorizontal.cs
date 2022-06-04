using ElementEngine.ElementUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutHorizontal : UIObject
    {
        public int CellPaddingLeft;
        public int CellPaddingRight;

        public UILayoutHorizontal(string name, UILayoutHorizontalStyle style) : base(name)
        {
            ApplyStyle(style);

            CellPaddingLeft = style.CellPaddingLeft;
            CellPaddingRight = style.CellPaddingRight;
        }

        public override void UpdateLayout(bool secondCheck = true, bool updateScrollbars = true)
        {
            base.UpdateLayout(secondCheck, updateScrollbars);
            UpdateChildPositions();
        }

        public void UpdateChildPositions()
        {
            var currentX = 0;

            foreach (var child in Children)
            {
                currentX += CellPaddingLeft;

                child.X = currentX;

                currentX += child.Width + child.MarginRight;
                currentX += CellPaddingRight;
            }
        }
    }
}
