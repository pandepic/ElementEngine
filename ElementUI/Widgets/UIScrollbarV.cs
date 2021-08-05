using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScrollbarV : UIObject
    {
        public new UIScrollbarStyleV Style => (UIScrollbarStyleV)_style;
        public event Action<UIOnValueChangedArgs<int>> OnValueChanged;

        public readonly UIImage Rail;
        public readonly UIButton Slider;
        public readonly UIButton ButtonUp;
        public readonly UIButton ButtonDown;

        public bool IsSliding { get; protected set; }

        internal int _sliderMinY;
        internal int _sliderMaxY;
        internal float _distancePerChange;
        internal float _distancePerStep => _distancePerChange * StepSize;

        internal Vector2I _slidingMouseOffset;

        internal int _minValue;
        public int MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                UpdateSizesPositions();
                CurrentValue = CurrentValue;
            }
        }

        internal int _maxValue;
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                UpdateSizesPositions();
                CurrentValue = CurrentValue;
            }
        }

        internal int _stepSize;
        public int StepSize
        {
            get => _stepSize;
            set
            {
                _stepSize = value;
                UpdateSizesPositions();
                CurrentValue = CurrentValue;
            }
        }

        internal int _currentValue;
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                var prev = _currentValue;
                _currentValue = Math.Clamp(value, MinValue, MaxValue);

                if (prev != _currentValue)
                {
                    OnValueChanged?.Invoke(new UIOnValueChangedArgs<int>(this, prev, _currentValue));
                    UpdateSliderPositionFromCurrentValue();
                }
            }
        }

        public UIScrollbarV(string name, UIScrollbarStyleV style, int minValue, int maxValue, int stepSize = 1, int currentValue = 0) : base(name)
        {
            ApplyStyle(style);

            _minValue = minValue;
            _maxValue = maxValue;
            _stepSize = stepSize;
            _currentValue = Math.Clamp(currentValue, minValue, maxValue);

            Rail = new UIImage(name + "_Rail", Style.Rail);
            Rail.CenterX = true;
            Rail.CenterY = true;

            Slider = new UIButton(name + "_Slider", Style.Slider);
            Slider.CenterX = true;

            AddChild(Rail);
            AddChild(Slider);

            if (Style.ButtonUp != null && Style.ButtonDown != null)
            {
                ButtonUp = new UIButton(name + "_ButtonUp", Style.ButtonUp);
                ButtonUp._uiPosition.StopAutoPositionX();
                ButtonUp._uiPosition.StopAutoPositionY();
                ButtonUp.CenterX = true;
                ButtonUp.AnchorTop = true;
                AddChild(ButtonUp);

                ButtonUp.OnClick += (args) =>
                {
                    CurrentValue -= StepSize;
                };

                ButtonDown = new UIButton(name + "_ButtonDown", Style.ButtonDown);
                ButtonDown._uiPosition.StopAutoPositionX();
                ButtonDown._uiPosition.StopAutoPositionY();
                ButtonDown.CenterX = true;
                ButtonDown.AnchorBottom = true;
                AddChild(ButtonDown);

                ButtonDown.OnClick += (args) =>
                {
                    CurrentValue += StepSize;
                };
            }

            UpdateSizesPositions();

        } // constructor

        protected void UpdateSizesPositions()
        {
            if (!IsVisible)
                return;

            var railHeight = Size.Y;
            Width = MathHelper.Max(Width, Rail.Width, Slider.Width);

            if (Style.ButtonUp != null && Style.ButtonDown != null)
            {
                Width = MathHelper.Max(Width, ButtonUp.Width, ButtonDown.Width);

                switch (Style.ButtonType)
                {
                    case UIScrollbarButtonType.OutsideRail:
                        {
                            railHeight -= ButtonUp.Size.Y + ButtonDown.Size.Y;
                        }
                        break;

                    case UIScrollbarButtonType.CenterRailEdge:
                        {
                            railHeight -= (ButtonUp.Size.Y / 2) + (ButtonDown.Size.Y / 2);
                        }
                        break;
                }
            }

            var railYOffset = (Height - railHeight) / 2;

            switch (Style.SliderType)
            {
                case UIScrollbarSliderType.Center:
                    {
                        _sliderMinY = railYOffset - (Slider.Height / 2);
                        _sliderMaxY = Height - railYOffset - (Slider.Height / 2);
                    }
                    break;

                case UIScrollbarSliderType.Contain:
                    {
                        _sliderMinY = railYOffset;
                        _sliderMaxY = Height - railYOffset - Slider.Height;
                    }
                    break;
            }

            _distancePerChange = (float)Math.Abs(_sliderMaxY - _sliderMinY) / Math.Abs(_maxValue - _minValue);

            Rail.Height = railHeight;
            Slider.SetPosition(new Vector2I());
            Slider._uiPosition.Position = new Vector2I(0, Math.Clamp(Slider._uiPosition.Position.Value.Y, _sliderMinY, _sliderMaxY));

            UpdateSliderPositionFromCurrentValue();

        } // UpdateSizesPositions

        protected void UpdateSliderPositionFromCurrentValue()
        {
            Slider.Y = (int)(_sliderMinY + (CurrentValue * _distancePerChange));
        }

        internal override void UpdateLayout()
        {
            base.UpdateLayout();
            UpdateSizesPositions();
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var val = base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            if (IsSliding)
            {
                var currentMouseOffset = mousePosition.ToVector2I() - Slider.Position;

                if (currentMouseOffset.Y == _slidingMouseOffset.Y)
                    return val;

                var diff = Math.Abs(currentMouseOffset.Y - _slidingMouseOffset.Y);
                if (diff < _distancePerStep)
                    return val;

                var direction = currentMouseOffset.Y > _slidingMouseOffset.Y ? 1 : -1;
                CurrentValue += StepSize * (int)(diff / _distancePerStep) * direction;
            }

            return val;
        }

        internal override void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            IsSliding = false;
            base.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var val = base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (Slider.IsPressed)
            {
                _slidingMouseOffset = mousePosition.ToVector2I() - Slider.Position;
                IsSliding = true;
            }
            else if (!ButtonUp.IsPressed && !ButtonDown.IsPressed)
            {
                _slidingMouseOffset = new Vector2I(0, Slider.Height / 2);
                var currentMouseOffset = mousePosition.ToVector2I() - Slider.Position;

                if (currentMouseOffset.Y == _slidingMouseOffset.Y)
                    return val;

                var diff = Math.Abs(currentMouseOffset.Y - _slidingMouseOffset.Y);
                if (diff < _distancePerStep)
                    return val;

                var direction = currentMouseOffset.Y > _slidingMouseOffset.Y ? 1 : -1;
                CurrentValue += StepSize * (int)(diff / _distancePerStep) * direction;
                IsSliding = true;
            }

            return val;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            IsSliding = false;
            return base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);
        }

    } // UIScrollbarV
}
