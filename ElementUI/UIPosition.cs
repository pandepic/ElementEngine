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

        public bool IsAutoPosition => CenterX || CenterY || AnchorBottom || AnchorRight;
        public bool IsAutoPositionX => CenterX || AnchorRight;
        public bool IsAutoPositionY => CenterY || AnchorBottom;

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

        //public void HandleMargins(UIObject obj)
        //{
        //    if (obj.HasMargin && Position.HasValue)
        //    {
        //        foreach (var sibling in obj.Parent.Children)
        //        {
        //            if (sibling == obj)
        //                continue;
        //            if (sibling is UIContainer)
        //                continue;
        //            if (!sibling.HasMargin)
        //                continue;

        //            //var intersect = obj.MarginBounds.Intersect(sibling.MarginBounds);

        //            //if (intersect.IsZero)
        //            //    continue;

        //            if ((obj.MarginTop > 0 || sibling.MarginBottom > 0) && !IsAutoPositionY)
        //            {
        //                if (obj.MarginBounds.Top > sibling.MarginBounds.Bottom && obj.MarginBounds.Bottom > sibling.MarginBounds.Top)
        //                {
        //                    Position += new Vector2(0, obj.MarginBounds.Top - sibling.MarginBounds.Bottom);
        //                    obj.UpdateLayout();
        //                }
        //            }
        //        }
        //    }
        //} // HandleMargins

    } // UIPosition
}
