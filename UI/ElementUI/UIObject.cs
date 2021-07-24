using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine.ElementUI
{
    public class UIObject
    {
        public UIObject Parent;

        public bool IsScreen = false;
        public Vector2 LocalPosition = Vector2.Zero;
        public Vector2 Offset = Vector2.Zero;
        public Vector2 Size = Vector2.Zero;
        public UISpacing Margins = new UISpacing();
        public UISpacing Padding = new UISpacing();
        public UIPositionFlags PositionFlags = new UIPositionFlags();

        public Vector2 Position
        {
            get
            {
                if (IsScreen)
                    return LocalPosition;

                return Parent.Position + CalculateLocalPosition() + Margins.TopLeftF;
            }
        }

        public Rectangle Bounds => new Rectangle(Position, Size);

        public virtual void Load(XElement el)
        {
        }

        protected Vector2 CalculateLocalPosition()
        {
            var position = PositionFlags.CalculateLocalPosition(this);
            return position;
        }

    } // UIElement
}
