using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIProgressbarV : UIObject
    {
        public new UIProgressbarStyleV Style => (UIProgressbarStyleV)_style;

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

        internal float _heightPerValue;

        public UIProgressbarV(string name, UIProgressbarStyleV style, int minValue, int maxValue, int currentValue = 0) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.Background.Sprite);

            _minValue = minValue;
            _maxValue = maxValue;
            _currentValue = currentValue;

            Background = new UIImage(name + "_Background", Style.Background);
            AddChild(Background);

            Fill = new UIImage(name + "_Fill", Style.Fill);
            Fill.Y = Style.FillPadding;
            Fill.CenterX = true;
            AddChild(Fill);

            UpdateLayout();
            UpdateBar();
        }

        protected void UpdateBar()
        {
            var baseFillHeight = Height - (Style.FillPadding * 2);
            _heightPerValue = baseFillHeight / (float)Math.Abs(_maxValue - _minValue);

            Background.Height = Height;
            Fill.Height = baseFillHeight;

            var fillHeight = CurrentValue * _heightPerValue;

            if (Fill.ScaleType == UIScaleType.Scale)
                Fill.Height = (int)fillHeight;
            else if (Fill.ScaleType == UIScaleType.Crop)
                Fill.CropHeight = (int)fillHeight;

            Fill.Y = Height - (Style.FillPadding + (int)fillHeight);
        }

    } // UIProgressbarV
}
