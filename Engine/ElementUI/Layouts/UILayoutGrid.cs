using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UILayoutGrid : UIObject
    {
        public UISpacing CellPadding;
        public int? MaxColumns;

        public UILayoutGrid(string name, UILayoutGridStyle style) : base(name)
        {
            ApplyStyle(style);

            CellPadding = style.CellPadding;
            MaxColumns = style.MaxColumns;
        }

        public override void UpdateLayout(bool secondCheck = true, bool updateScrollbars = true)
        {
            base.UpdateLayout(secondCheck, updateScrollbars);
            UpdateChildPositions();
        }

        public void UpdateChildPositions()
        {
            var currentRowColumns = 0;

            var currentPos = Vector2I.Zero;
            var currentRowHeight = 0;

            foreach (var child in Children)
            {
                if ((!AutoWidth && (currentPos + new Vector2I(CellPadding.Left + CellPadding.Right + child.Width, 0)).X > Width) || (MaxColumns.HasValue && currentRowColumns >= MaxColumns.Value))
                {
                    currentPos.Y += CellPadding.Top;
                    currentPos.Y += currentRowHeight;
                    currentPos.X = 0;
                    currentRowHeight = 0;
                    currentRowColumns = 0;
                }

                if ((child.Height + child.MarginBottom + CellPadding.Bottom) > currentRowHeight)
                    currentRowHeight = child.Height + child.MarginBottom + CellPadding.Bottom;

                currentPos.X += CellPadding.Left;

                child.SetPosition(currentPos);

                currentPos.X += CellPadding.Right + child.Width;

                currentRowColumns += 1;
            }
        }
    }
}
