using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIContainerStyle : UIStyle
    {
        public UISprite Background;
        public Rectangle? DraggableRect;
        public bool IsFullDraggableRect;

        public UIScrollbarStyleV ScrollbarV;
        public UIScrollbarStyleH ScrollbarH;

        public UIContainerStyle(UIContainerStyle copyFrom, bool baseCopy = false)
        {
            Background = copyFrom.Background;
            DraggableRect = copyFrom.DraggableRect;
            IsFullDraggableRect = copyFrom.IsFullDraggableRect;
            ScrollbarV = copyFrom.ScrollbarV;
            ScrollbarH = copyFrom.ScrollbarH;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UIContainerStyle(
            UISprite background,
            Rectangle? draggableRect = null,
            bool fullDraggableRect = false,
            UIScrollbarStyleV scrollbarV = null,
            UIScrollbarStyleH scrollbarH = null)
        {
            Background = background;
            DraggableRect = draggableRect;
            IsFullDraggableRect = fullDraggableRect;
            ScrollbarV = scrollbarV;
            ScrollbarH = scrollbarH;
        }
    }
}
