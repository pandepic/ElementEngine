using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UIPosition
    {
        public Vector2? Position;
        public bool CenterX, CenterY;
        public bool AnchorBottom;
        public bool AnchorRight;

        public Vector2 GetRelativePosition(UIObject obj)
        {
            return (Position ?? Vector2.Zero) + obj._margins.TopLeftF;
        }

        public Vector2 GetPosition(UIObject obj)
        {
            if (obj.Parent == null)
                return GetRelativePosition(obj);

            var position = obj.Parent._childOrigin + GetRelativePosition(obj);

            if (CenterX)
                position.X += (obj.Parent._size.X / 2f) - (obj._size.X / 2f);
            if (CenterY)
                position.Y += (obj.Parent._size.Y / 2f) - (obj._size.Y / 2f);

            position += obj._margins.TopLeftF;
            return position;
        }
    } // UIPosition
}
