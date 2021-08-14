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
        public UISizeFillType? FillType;
        public int? ScrollSpeed;
        public bool? IgnoreOverflow;
        public bool? IgnoreParentPadding;

        public void BaseCopy(UIStyle copyFrom)
        {
            UIPosition = copyFrom.UIPosition;
            UISize = copyFrom.UISize;
            Margins = copyFrom.Margins;
            Padding = copyFrom.Padding;
            OverflowType = copyFrom.OverflowType;
            FillType = copyFrom.FillType;
            ScrollSpeed = copyFrom.ScrollSpeed;
            IgnoreOverflow = copyFrom.IgnoreOverflow;
            IgnoreParentPadding = copyFrom.IgnoreParentPadding;
        }
    }
}
