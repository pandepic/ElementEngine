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

        public UIContainerStyle(UISprite background, Rectangle? draggableRect = null, bool fullDraggableRect = false)
        {
            Background = background;
            DraggableRect = draggableRect;
            IsFullDraggableRect = fullDraggableRect;
        }
    }
}
