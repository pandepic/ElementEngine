using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine.UI
{
    public class UIPanel
    {
        public readonly UIFrame Parent;
        public readonly List<UIWidget> Widgets = new List<UIWidget>();

        public UIWHScrollBar HScrollBar;
        public UIWVScrollBar VScrollBar;

        public Rectangle FullPanelRect;
        public Vector2 ScrollOffset;
        public Rectangle PanelRect;
        public Vector2 Offset;
        public Rectangle ScissorRect => new Rectangle(
            Parent.Position + PanelRect.LocationF,
            PanelRect.SizeF - new Vector2(
                VScrollBar != null ? VScrollBar.Width : 0,
                HScrollBar != null ? HScrollBar.Height : 0));

        public bool CenterX;
        public bool CenterY;
        public bool AnchorTop;
        public bool AnchorBottom;
        public bool AnchorLeft;
        public bool AnchorRight;

        public Vector2 Position
        {
            get => new Vector2(PanelRect.X, PanelRect.Y) + Offset;
        }

        public int X { get => PanelRect.X; set { PanelRect.X = value; } }
        public int Y { get => PanelRect.Y; set { PanelRect.Y = value; } }
        public int Width { get => PanelRect.Width; set { PanelRect.Width = value; } }
        public int Height { get => PanelRect.Height; set { PanelRect.Height = value; } }

        public UIPanel(UIFrame parent, XElement el)
        {
            Parent = parent;

            var panelSize = el.Element("Size");
            var panelSizeW = panelSize.Attribute("Width").Value;
            var panelSizeH = panelSize.Attribute("Height").Value;

            if (panelSizeW.ToUpper() == "FILL")
                Width = Parent.Width;
            else
                Width = int.Parse(panelSizeW);

            if (panelSizeH.ToUpper() == "FILL")
                Height = Parent.Height;
            else
                Height = int.Parse(panelSizeH);

            var elPosition = el.Element("Position");
            var elOffset = el.Element("Offset");

            if (elPosition != null)
            {
                var panelX = elPosition.Attribute("X").Value.ToUpper();
                var panelY = elPosition.Attribute("Y").Value.ToUpper();

                if (panelX == "CENTER")
                    CenterX = true;
                else if (panelX == "LEFT")
                    AnchorLeft = true;
                else if (panelX == "RIGHT")
                    AnchorRight = true;
                else
                    X = int.Parse(panelX);

                if (panelY == "CENTER")
                    CenterY = true;
                else if (panelY == "TOP")
                    AnchorTop = true;
                else if (panelY == "BOTTOM")
                    AnchorBottom = true;
                else
                    Y = int.Parse(panelY);
            }

            if (elOffset != null)
                Offset = new Vector2(int.Parse(elOffset.Attribute("X").Value), int.Parse(elOffset.Attribute("Y").Value));

            var scrollBarH = el.Element("HScrollBar");
            var scrollBarV = el.Element("VScrollBar");

            if (scrollBarH != null)
            {
                HScrollBar = new UIWHScrollBar();
                HScrollBar.Init(Parent, scrollBarH);
                HScrollBar.Load(Parent, scrollBarH);
                HScrollBar.Panel = this;
                HScrollBar.IgnorePanelScissorRect = true;
            }

            if (scrollBarV != null)
            {
                VScrollBar = new UIWVScrollBar();
                VScrollBar.Init(Parent, scrollBarV);
                VScrollBar.Load(Parent, scrollBarV);
                VScrollBar.Panel = this;
                VScrollBar.IgnorePanelScissorRect = true;
            }

            FullPanelRect.Location = PanelRect.Location;

        } // UIPanel

        public void AddWidget(UIWidget widget)
        {
            Widgets.AddIfNotContains(widget);
        }

        public void Update(GameTimer gameTimer)
        {
            var prevFullRect = FullPanelRect;

            foreach (var widget in Widgets)
            {
                var checkRight = (int)(widget.X + widget.Width);
                var checkBottom = (int)(widget.Y + widget.Height);

                if (widget.X < FullPanelRect.X)
                    FullPanelRect.X = widget.X;
                if (widget.Y < FullPanelRect.Y)
                    FullPanelRect.Y = widget.Y;

                if (checkRight > FullPanelRect.Right)
                    FullPanelRect.Right = checkRight;

                if (checkBottom > FullPanelRect.Bottom)
                    FullPanelRect.Bottom = checkBottom;
            }

            if (prevFullRect != FullPanelRect)
            {
                var offsetX = 0;
                if (FullPanelRect.X < 0)
                    offsetX = FullPanelRect.X * -1;

                var offsetY = 0;
                if (FullPanelRect.Y < 0)
                    offsetY = FullPanelRect.Y * -1;

                var checkRect = PanelRect;
                checkRect.Width = ScissorRect.Width;
                checkRect.Height = ScissorRect.Height;

                var scrollWidth = (FullPanelRect.Right + Parent.PaddingLeft + offsetX) - checkRect.Right;
                var scrollHeight = (FullPanelRect.Bottom + Parent.PaddingTop + offsetY) - checkRect.Bottom;

                HScrollBar?.SetMaxValue(scrollWidth);
                VScrollBar?.SetMaxValue(scrollHeight);
            }

            if (HScrollBar != null)
            {
                var offset = 0;
                if (FullPanelRect.X < 0)
                    offset  = FullPanelRect.X * -1;

                ScrollOffset.X = HScrollBar.Value * -1f + offset;
            }

            if (VScrollBar != null)
            {
                var offset = 0;
                if (FullPanelRect.Y < 0)
                    offset = FullPanelRect.Y * -1;

                ScrollOffset.Y = VScrollBar.Value * -1f + offset;
            }
        }

        public void Draw(SpriteBatch2D spriteBatch)
        {
        }

    } // UIPanel
}
