﻿using System;
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
        public Vector2I? MarginOffset;
        public bool CenterX, CenterY;
        public bool AnchorBottom;
        public bool AnchorRight;

        public bool IsAutoPosition => IsAutoPositionX || IsAutoPositionY;
        public bool IsAutoPositionX => CenterX || AnchorRight;
        public bool IsAutoPositionY => CenterY || AnchorBottom;

        internal Vector2I _internalOffset;

        public void StopAutoPositionX()
        {
            CenterX = false;
            AnchorRight = false;
        }

        public void StopAutoPositionY()
        {
            CenterY = false;
            AnchorBottom = false;
        }

        public Vector2I GetRelativePosition(UIObject obj)
        {
            return (Position ?? Vector2I.Zero) + (MarginOffset ?? Vector2I.Zero);
        }

        public Vector2I GetPosition(UIObject obj)
        {
            if (obj.Parent == null)
                return GetRelativePosition(obj);

            var position = GetRelativePosition(obj);
            var parentOrigin = obj.IgnoreParentPadding ? obj.Parent._position : obj.Parent._childOrigin;

            position.X += parentOrigin.X;
            position.Y += parentOrigin.Y;

            if (CenterX)
                position.X += (obj.Parent._size.X / 2) - (obj._size.X / 2);
            if (CenterY)
                position.Y += (obj.Parent._size.Y / 2) - (obj._size.Y / 2);

            if (AnchorRight)
                position.X = (obj.IgnoreParentPadding ? obj.Parent.Bounds.Right : obj.Parent.PaddingBounds.Right) - obj._size.X - obj._margins.Right;
            if (AnchorBottom)
                position.Y = (obj.IgnoreParentPadding ? obj.Parent.Bounds.Bottom : obj.Parent.PaddingBounds.Bottom) - obj._size.Y - obj._margins.Bottom;

            position += obj._margins.TopLeft;
            position += _internalOffset;

            return position;
        }

    } // UIPosition
}
