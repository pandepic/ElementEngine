using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UIPositionFlags
    {
        public bool CenterX, CenterY;
        public bool AnchorTop, AnchorBottom;
        public bool AnchorLeft, AnchorRight;

        public Vector2 CalculateLocalPosition(UIObject obj)
        {
            var position = obj.LocalPosition;
            var parentSize = obj.Parent.Size;

            position += obj.Parent.Padding.TopLeftF;

            return position;
        }

    } // UIPositionFlags
}
