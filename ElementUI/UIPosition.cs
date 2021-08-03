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
        public Vector2I? Position;
        public bool CenterX, CenterY;
        public bool AnchorBottom;
        public bool AnchorRight;

        public bool IsAutoPosition => IsAutoPositionX || IsAutoPositionY;
        public bool IsAutoPositionX => CenterX || AnchorRight;
        public bool IsAutoPositionY => CenterY || AnchorBottom;

        public Vector2I GetRelativePosition(UIObject obj)
        {
            return (Position ?? Vector2I.Zero);
        }

        public Vector2I GetPosition(UIObject obj)
        {
            if (obj.Parent == null)
                return GetRelativePosition(obj);

            var position = GetRelativePosition(obj);
            var parentOrigin = obj.IgnoreParentPadding ? Vector2I.Zero : obj.Parent._childOrigin;

            if (IsAutoPositionX)
                position.X += obj.Parent._position.X;
            else
                position.X += parentOrigin.X;

            if (IsAutoPositionY)
                position.Y += obj.Parent._position.Y;
            else
                position.Y += parentOrigin.Y;

            if (CenterX)
                position.X += (obj.Parent._size.X / 2) - (obj._size.X / 2);
            if (CenterY)
                position.Y += (obj.Parent._size.Y / 2) - (obj._size.Y / 2);

            position += obj._margins.TopLeft;
            return position;
        }

    } // UIPosition
}
