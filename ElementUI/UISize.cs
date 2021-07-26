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
        public Vector2? Size;
        public bool AutoWidth;
        public bool AutoHeight;
        public bool ParentWidth;
        public bool ParentHeight;

        public bool IsAutoSized => (AutoWidth || ParentWidth) && (AutoHeight || ParentHeight);

        public Vector2 GetSize(UIObject obj)
        {
            var size = Size ?? Vector2.One;

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
                    size.X = width + obj.PaddingRight;
                if (AutoHeight)
                    size.Y = height + obj.PaddingBottom;
            }

            return size;
        }
    }
}
