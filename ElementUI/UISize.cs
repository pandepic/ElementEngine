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
        public int? MinWidth, MaxWidth;
        public int? MinHeight, MaxHeight;
        public UISizeFillType? FillType;

        public bool IsAutoSized => IsAutoSizedX || IsAutoSizedY;
        public bool IsAutoSizedX => AutoWidth || ParentWidth || ParentWidthRatio.HasValue || FillType.HasValue;
        public bool IsAutoSizedY => AutoHeight || ParentHeight || ParentHeightRatio.HasValue || FillType.HasValue;

        internal Rectangle _fullChildBounds;

        public void StopAutoSizeX()
        {
            AutoWidth = false;
            ParentWidth = false;
            ParentWidthRatio = null;
            FillType = null;
        }

        public void StopAutoSizeY()
        {
            AutoHeight = false;
            ParentHeight = false;
            ParentHeightRatio = null;
            FillType = null;
        }

        public Vector2I GetRespectedParentSize(UIObject obj)
        {
            if (obj.Parent == null)
                return Vector2I.Zero;

            if (obj.IgnoreParentPadding)
                return obj.Parent.Size;
            else
                return obj.Parent.Size - new Vector2I(obj.Parent.PaddingLeft + obj.Parent.PaddingRight, obj.Parent.PaddingTop + obj.Parent.PaddingBottom);
        }

        public Vector2I GetSize(UIObject obj)
        {
            var size = Size ?? Vector2I.One;

            var width = 1;
            var height = 1;
            _fullChildBounds = new Rectangle();

            foreach (var child in obj.Children)
            {
                if (child.IgnoreOverflow)
                    continue;

                var checkBounds = new Rectangle(child._uiPosition.GetRelativePosition(child), child._size);

                width = Math.Max(checkBounds.Right, width);
                height = Math.Max(checkBounds.Bottom, height);

                if (checkBounds.Top < _fullChildBounds.Top)
                    _fullChildBounds.Top = checkBounds.Top;
                if (checkBounds.Bottom > _fullChildBounds.Bottom)
                    _fullChildBounds.Bottom = checkBounds.Bottom;
                if (checkBounds.Left < _fullChildBounds.Left)
                    _fullChildBounds.Left = checkBounds.Left;
                if (checkBounds.Right > _fullChildBounds.Right)
                    _fullChildBounds.Right = checkBounds.Right;
            }

            if (AutoWidth)
                size.X = width + obj.PaddingLeft + obj.PaddingRight;
            if (AutoHeight)
                size.Y = height + obj.PaddingTop + obj.PaddingBottom;

            var parentSize = GetRespectedParentSize(obj);

            if (ParentWidth)
                size.X = parentSize.X;
            if (ParentHeight)
                size.Y = parentSize.Y;

            if (ParentWidthRatio.HasValue)
                size.X = (int)(parentSize.X * ParentWidthRatio.Value);
            if (ParentHeightRatio.HasValue)
                size.Y = (int)(parentSize.Y * ParentHeightRatio.Value);

            if (MinWidth.HasValue && size.X < MinWidth.Value)
                size.X = MinWidth.Value;
            if (MinHeight.HasValue && size.Y < MinHeight.Value)
                size.Y = MinHeight.Value;

            if (MaxWidth.HasValue && size.X > MaxWidth.Value)
                size.X = MaxWidth.Value;
            if (MaxHeight.HasValue && size.Y > MaxHeight.Value)
                size.Y = MaxHeight.Value;

            if (FillType.HasValue)
            {
                switch (FillType)
                {
                    case UISizeFillType.Contain:
                        {
                            var aspectRatio = (float)size.X / size.Y;

                            if (size.X > size.Y)
                            {
                                var targetHeight = (float)parentSize.Y;
                                var targetWidth = targetHeight * aspectRatio;
                                size = new Vector2I(targetWidth, targetHeight);
                            }
                            else
                            {
                                var targetWidth = (float)parentSize.X;
                                var targetHeight = targetWidth / aspectRatio;
                                size = new Vector2I(targetWidth, targetHeight);
                            }
                        }
                        break;

                    case UISizeFillType.Cover:
                        {
                            var aspectRatio = (float)size.X / size.Y;

                            if (size.X < size.Y)
                            {
                                var targetHeight = (float)parentSize.Y;
                                var targetWidth = targetHeight * aspectRatio;

                                if (targetWidth < obj.Parent.Width)
                                {
                                    targetWidth = (float)parentSize.X;
                                    targetHeight = targetWidth / aspectRatio;
                                }

                                size = new Vector2I(targetWidth, targetHeight);
                            }
                            else
                            {
                                var targetWidth = (float)parentSize.X;
                                var targetHeight = targetWidth / aspectRatio;

                                if (targetHeight < obj.Parent.Height)
                                {
                                    targetHeight = (float)parentSize.Y;
                                    targetWidth = targetHeight * aspectRatio;
                                }

                                size = new Vector2I(targetWidth, targetHeight);
                            }
                        }
                        break;

                    case UISizeFillType.Stretch:
                        {
                            size = new Vector2I(obj.Parent.Width, obj.Parent.Height);
                        }
                        break;
                }
            }

            return size;

        } // GetSize
    } // UISize
}
