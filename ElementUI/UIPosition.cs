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
        public bool AnchorTop, AnchorBottom;
        public bool AnchorLeft, AnchorRight;

        public Vector2 GetRelativePosition(UIObject obj)
        {
            return (Position ?? Vector2.Zero) + obj._margins.TopLeftF;
        }

        public Vector2 GetPosition(UIObject obj)
        {
            if (obj.Parent == null)
                return GetRelativePosition(obj);

            var parentOrigin = obj.Parent._childOrigin;
            var position = obj.Parent._childOrigin + GetRelativePosition(obj);

            if (CenterX)
                position.X += (obj.Parent._size.X / 2f) - (obj._size.X / 2f);
            if (CenterY)
                position.Y += (obj.Parent._size.Y / 2f) - (obj._size.Y / 2f);

            //if (position.X < parentOrigin.X + child._margins.Left)
            //    position.X = parentOrigin.X + child._margins.Left;
            //if (position.Y < parentOrigin.Y + child._margins.Top)
            //    position.Y = parentOrigin.Y + child._margins.Top;

            position += obj._margins.TopLeftF;
            return position;
        }
    } // UIPosition
}
