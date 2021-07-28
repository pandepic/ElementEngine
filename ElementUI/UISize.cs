using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UISize
    {
        public Vector2I? Size;
        public bool AutoWidth;
        public bool AutoHeight;
        public bool ParentWidth;
        public bool ParentHeight;
        public float? ParentWidthRatio;
        public float? ParentHeightRatio;

        public bool IsAutoSized => AutoWidth || ParentWidth || ParentWidthRatio.HasValue || AutoHeight || ParentHeight || ParentHeightRatio.HasValue;
        public bool IsAutoSizedX => AutoWidth || ParentWidth || ParentWidthRatio.HasValue;
        public bool IsAutoSizedY => AutoHeight || ParentHeight || ParentHeightRatio.HasValue;

        public Vector2I GetSize(UIObject obj)
        {
            var size = Size ?? Vector2I.One;

            if (AutoWidth || AutoHeight)
            {
                var width = 1;
                var height = 1;

                foreach (var child in obj.Children)
                {
                    var checkBounds = child._uiPosition.GetRelativePosition(child) + child._size;

                    width = Math.Max((int)checkBounds.X, width);
                    height = Math.Max((int)checkBounds.Y, height);
                }

                if (AutoWidth)
                    size.X = width + obj.PaddingLeft + obj.PaddingRight;
                if (AutoHeight)
                    size.Y = height + obj.PaddingTop + obj.PaddingBottom;
            }

            if (ParentWidth)
                size.X = obj.Parent.Size.X;
            if (ParentHeight)
                size.Y = obj.Parent.Size.Y;

            if (ParentWidthRatio.HasValue)
                size.X = (int)(obj.Parent.Size.X * ParentWidthRatio.Value);
            if (ParentHeightRatio.HasValue)
                size.Y = (int)(obj.Parent.Size.Y * ParentHeightRatio.Value);

            return size;
        }
    }
}
