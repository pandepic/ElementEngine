using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine
{
    public enum UILayoutGroupDirection
    {
        Vertical,
        Horizontal,
    }

    public class UILayoutGroup
    {
        public string Name { get; set; }
        public UILayoutGroupDirection Direction { get; set; }
        public int Spacing { get; set; }
        public UIFrame ParentFrame { get; set; }
        public List<UIWidget> Widgets { get; set; } = new List<UIWidget>();
        public WidgetPositionFlags PositionFlags { get; set; } = new WidgetPositionFlags();

        public UILayoutGroup(XElement el, UIFrame parentFrame)
        {
            ParentFrame = parentFrame;
            Name = el.Attribute("Name").Value;
            Direction = el.Attribute("Direction").Value.ToEnum<UILayoutGroupDirection>();
            Spacing = 0;

            var attSpacing = el.Attribute("Spacing");
            if (attSpacing != null)
                Spacing = int.Parse(attSpacing.Value);

            var valueX = el.Element("StartPosition").Attribute("X").Value;

            if (valueX.ToUpper() == "CENTER")
                PositionFlags.CenterX = true;
            else if (valueX.ToUpper() == "LEFT")
                PositionFlags.AnchorLeft = true;
            else if (valueX.ToUpper() == "RIGHT")
                PositionFlags.AnchorRight = true;
            else
                PositionFlags.SetX = int.Parse(valueX);

            var valueY = el.Element("StartPosition").Attribute("Y").Value;

            if (valueY.ToUpper() == "CENTER")
                PositionFlags.CenterY = true;
            else if (valueY.ToUpper() == "TOP")
                PositionFlags.AnchorTop = true;
            else if (valueY.ToUpper() == "BOTTOM")
                PositionFlags.AnchorBottom = true;
            else
                PositionFlags.SetY = int.Parse(valueY);
        }

        public void UpdateWidgetPositions()
        {
            var width = 0;
            var height = 0;

            foreach (var widget in Widgets)
            {
                var offset = widget.Offset.ToVector2I();

                if (Direction == UILayoutGroupDirection.Vertical)
                {
                    if (widget.Width > width)
                        width = widget.Width;

                    height += widget.Height + Spacing + offset.Y;
                }
                else if (Direction == UILayoutGroupDirection.Horizontal)
                {
                    if (widget.Height > height)
                        height = widget.Height;

                    width += widget.Width + Spacing + offset.X;
                }
            }

            if (ParentFrame.AutoWidth && ParentFrame.Width == 0)
                ParentFrame.Width = width;
            if (ParentFrame.AutoHeight && ParentFrame.Height == 0)
                ParentFrame.Height = height;

            var startPosition = Vector2I.Zero;

            if (PositionFlags.CenterX)
                startPosition.X = ParentFrame.Width / 2 - (Direction == UILayoutGroupDirection.Horizontal ? width / 2 : 0);
            else if (PositionFlags.AnchorLeft)
                startPosition.X = 0;
            else if (PositionFlags.AnchorRight)
                startPosition.X = ParentFrame.Width;
            else if (PositionFlags.SetX.HasValue)
                startPosition.X = PositionFlags.SetX.Value;

            if (PositionFlags.CenterY)
                startPosition.Y = ParentFrame.Height / 2 - (Direction == UILayoutGroupDirection.Vertical ? height / 2 : 0);
            else if (PositionFlags.AnchorTop)
                startPosition.Y = 0;
            else if (PositionFlags.AnchorBottom)
                startPosition.Y = ParentFrame.Height;
            else if (PositionFlags.SetY.HasValue)
                startPosition.Y = PositionFlags.SetY.Value;

            var currentPosition = startPosition;

            foreach (var widget in Widgets)
            {
                var offset = widget.Offset.ToVector2I();

                if (Direction == UILayoutGroupDirection.Vertical)
                {
                    if (widget.PositionFlags.CenterX)
                        currentPosition.X = startPosition.X - widget.Width / 2;
                    else if (widget.PositionFlags.AnchorLeft)
                        currentPosition.X = startPosition.X;
                    else if (widget.PositionFlags.AnchorRight)
                        currentPosition.X = startPosition.X - widget.Width;
                    else if (widget.PositionFlags.SetX.HasValue)
                        currentPosition.X = startPosition.X + widget.PositionFlags.SetX.Value;

                    widget.X = currentPosition.X;
                    widget.Y = currentPosition.Y;

                    currentPosition.Y += widget.Height + Spacing + offset.Y;
                }
                else if (Direction == UILayoutGroupDirection.Horizontal)
                {
                    if (widget.PositionFlags.CenterY)
                        currentPosition.Y = startPosition.Y - widget.Height / 2;
                    else if (widget.PositionFlags.AnchorTop)
                        currentPosition.Y = startPosition.Y;
                    else if (widget.PositionFlags.AnchorBottom)
                        currentPosition.Y = startPosition.Y - widget.Height;
                    else if (widget.PositionFlags.SetY.HasValue)
                        currentPosition.Y = startPosition.Y + widget.PositionFlags.SetY.Value;

                    widget.X = currentPosition.X;
                    widget.Y = currentPosition.Y;

                    currentPosition.X += widget.Width + Spacing + offset.X;
                }
            }

        } // UpdateWidgetPositions
    } // UILayoutGroup
}
