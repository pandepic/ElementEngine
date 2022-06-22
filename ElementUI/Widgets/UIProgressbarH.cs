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

        public UIProgressbarHAnimation ValueChangedAnimation;

        internal int _minValue;
        public int MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue == value)
                    return;

                _minValue = value;
                UpdateBar(true);
            }
        }

        internal int _maxValue;
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue == value)
                    return;

                _maxValue = value;
                UpdateBar(true);
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

            if (Style.Background.Sprite.Size.X == 1 && Style.Fill.Sprite.Size.X > 1)
                ApplyDefaultSize(Style.Fill.Sprite);
            else
                ApplyDefaultSize(Style.Background.Sprite);

            if (style.ValueChangedAnimation != null)
            {
                ValueChangedAnimation = style.ValueChangedAnimation.Copy();
                ValueChangedAnimation.Object = this;
            }

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
            UpdateBar(true);
        }

        public void SetValueNoAnimation(int value)
        {
            _currentValue = value;
            UpdateBar(true);
        }

        protected void UpdateBar(bool skipAnimation = false)
        {
            var baseFillWidth = Width - (Style.FillPadding * 2);
            _widthPerValue = baseFillWidth / (float)Math.Abs(_maxValue - _minValue);

            Background.Width = Width;
            Fill.Width = baseFillWidth;

            var fillWidth = GetFillWidthAtValue(CurrentValue);

            if (!skipAnimation && ValueChangedAnimation != null)
            {
                var currentFillWidth = GetFillWidth();
                ValueChangedAnimation.Start(currentFillWidth, fillWidth);
            }
            else
            {
                SetFillWidth(fillWidth);
            }
        }

        public void SetFillWidth(int width)
        {
            if (Fill.ScaleType == UIScaleType.Scale)
                Fill.Width = width;
            else if (Fill.ScaleType == UIScaleType.Crop)
                Fill.CropWidth = width;
        }

        public int GetFillWidthAtValue(int value)
        {
            var fillWidth = value * _widthPerValue;

            if (fillWidth < 0)
                return 0;

            return (int)fillWidth;
        }

        public int GetFillWidth()
        {
            if (Fill.ScaleType == UIScaleType.Scale)
                return Fill.Width;
            else if (Fill.ScaleType == UIScaleType.Crop)
                return Fill.CropWidth ?? 0;

            return 0;
        }

    } // UIProgressbarH
}
