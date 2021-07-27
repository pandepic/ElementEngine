using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UISpacing
    {
        public int Top;
        public int Bottom;
        public int Left;
        public int Right;

        public Vector2I TopLeft => new Vector2I(Left, Top);
        public Vector2I BottomRight => new Vector2I(Right, Bottom);
        public Vector2 TopLeftF => new Vector2(Left, Top);
        public Vector2 BottomRightF => new Vector2(Right, Bottom);

        public bool IsZero => Top == 0 && Bottom == 0 && Left == 0 && Right == 0;

        public UISpacing(int top, int bottom, int left, int right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
    } // UIMargins
}
