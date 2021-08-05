using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIScrollbarH : UIObject
    {
        public new UIScrollbarStyleH Style => (UIScrollbarStyleH)_style;
        public event Action<UIOnValueChangedArgs<int>> OnValueChanged;

        internal int _minValue;
        public int MinValue
        {
            get => _minValue;
            set
            {
            }
        }

        internal int _maxValue;
        public int MaxValue
        {
            get => _maxValue;
            set
            {
            }
        }

        internal int _stepSize;
        public int StepSize
        {
            get => _stepSize;
            set
            {
            }
        }

        internal int _currentValue;
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
            }
        }

        public UIScrollbarH(string name, UIScrollbarStyleH style, int minValue, int maxValue, int stepSize = 1, int currentValue = 0) : base(name)
        {
            ApplyStyle(style);

            _minValue = minValue;
            _maxValue = maxValue;
            _stepSize = stepSize;
            _currentValue = currentValue;
        }
    } // UIScrollbarH
}
