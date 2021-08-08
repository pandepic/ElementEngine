using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIProgressbarH : UIObject
    {
        public new UIProgressbarStyleH Style => (UIProgressbarStyleH)_style;

        public readonly UIImage Background;
        public readonly UIImage Fill;

        internal int _minValue;
        public int MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                UpdateBar();
            }
        }

        internal int _maxValue;
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                UpdateBar();
            }
        }

        internal int _currentValue;
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                UpdateBar();
            }
        }

        public float NormalizedValue
        {
            get => ((float)_currentValue - _minValue) / (_maxValue - (float)_minValue);
            set
            {
                CurrentValue = (int)((((float)_maxValue - _minValue) * value) + _minValue);
            }
        }

        internal float _widthPerValue;

        public UIProgressbarH(string name, UIProgressbarStyleH style, int minValue, int maxValue, int currentValue = 0) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.Background.Sprite);

            _minValue = minValue;
            _maxValue = maxValue;
            _currentValue = currentValue;

            Background = new UIImage(name + "_Background", Style.Background);
            AddChild(Background);

            Fill = new UIImage(name + "_Fill", Style.Fill);
            Fill.X = Style.FillPadding;
            Fill.CenterY = true;
            AddChild(Fill);

            UpdateLayout();
            UpdateBar();
        }

        protected void UpdateBar()
        {
            var baseFillWidth = Width - (Style.FillPadding * 2);
            _widthPerValue = baseFillWidth / (float)Math.Abs(_maxValue - _minValue);

            Background.Width = Width;
            Fill.Width = baseFillWidth;

            var fillWidth = CurrentValue * _widthPerValue;

            if (Fill.ScaleType == UIScaleType.Scale)
                Fill.Width = (int)fillWidth;
            else if (Fill.ScaleType == UIScaleType.Crop)
                Fill.CropWidth = (int)fillWidth;
        }

    } // UIProgressbarH
}
