using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public struct UIOnClickArgs
    {
        public UIObject Object;
        public MouseButton MouseButton;

        public UIOnClickArgs(UIObject obj, MouseButton mouseButton)
        {
            Object = obj;
            MouseButton = mouseButton;
        }
    }

    public struct UIOnValueChangedArgs<T>
    {
        public UIObject Object;
        public T PreviousValue;
        public T CurrentValue;

        public UIOnValueChangedArgs(UIObject obj, T prev, T current)
        {
            Object = obj;
            PreviousValue = prev;
            CurrentValue = current;
        }
    }

    public struct UIOnSelectedTabChangedArgs
    {
        public UIButton PreviousTab;
        public UIButton CurrentTab;

        public UIOnSelectedTabChangedArgs(UIButton prev, UIButton current)
        {
            PreviousTab = prev;
            CurrentTab = current;
        }
    }
}
