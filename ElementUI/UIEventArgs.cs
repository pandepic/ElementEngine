using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UIOnClickArgs
    {
        public UIObject Object;

        public UIOnClickArgs(UIObject obj)
        {
            Object = obj;
        }
    }

    public struct UIOnValueChangedArgs<T>
    {
        public UIObject Object;
        public T PreviousValue;
        public T NextValue;

        public UIOnValueChangedArgs(UIObject obj, T prev, T next)
        {
            Object = obj;
            PreviousValue = prev;
            NextValue = next;
        }
    }
}
