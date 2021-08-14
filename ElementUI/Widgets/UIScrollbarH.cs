using System;
using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScrollbarH : UIObject
    {
        public new UIScrollbarStyleH Style => (UIScrollbarStyleH)_style;
        public event Action<UIOnValueChangedArgs<int>> OnValueChanged;

        public readonly UIImage Rail;
        public readonly UIImage RailFill;
        public readonly UIButton Slider;
        public readonly UIButton ButtonLeft;
        public readonly UIButton ButtonRight;

        public bool IsSliding { get; protected set; }

        internal int _sliderMinX;
        internal int _sliderMaxX;
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

        public float NormalizedValue
        {
            get => ((float)_currentValue - _minValue) / (_maxValue - (float)_minValue);
            set
            {
                CurrentValue = (int)((((float)_maxValue - _minValue) * value) + _minValue);
            }
        }

        internal bool _buttonLeftHeld = false;
        internal bool _buttonRightHeld = false;

        public UIScrollbarH(string name, UIScrollbarStyleH style, int minValue, int maxValue, int stepSize = 1, int currentValue = 0) : base(name)
        {
            ApplyStyle(style);

            _minValue = minValue;
            _maxValue = maxValue;
            _stepSize = stepSize;
            _currentValue = Math.Clamp(currentValue, minValue, maxValue);

            Rail = new UIImage(name + "_Rail", Style.Rail);
            Rail.CenterX = true;
            Rail.CenterY = true;
            AddChild(Rail);

            if (Style.RailFill != null)
            {
                RailFill = new UIImage(name + "_RailFill", Style.RailFill);
                RailFill.X = Style.RailFillPadding;
                RailFill.CenterY = true;
                AddChild(RailFill);
            }

            Slider = new UIButton(name + "_Slider", Style.Slider);
            Slider.CenterY = true;
            AddChild(Slider);

            if (Style.ButtonLeft != null && Style.ButtonRight != null)
            {
                ButtonLeft = new UIButton(name + "_ButtonLeft", Style.ButtonLeft);
                ButtonLeft._uiPosition.StopAutoPositionX();
                ButtonLeft._uiPosition.StopAutoPositionY();
                ButtonLeft.CenterY = true;
                ButtonLeft.AnchorLeft = true;
                AddChild(ButtonLeft);

                ButtonLeft.OnClick += (args) =>
                {
                    if (_buttonLeftHeld)
                    {
                        _buttonLeftHeld = false;
                        return;
                    }

                    CurrentValue -= StepSize;
                };

                ButtonLeft.OnMouseDown += (args) =>
                {
                    CurrentValue -= StepSize;
                    _buttonLeftHeld = true;
                };

                ButtonRight = new UIButton(name + "_ButtonRight", Style.ButtonRight);
                ButtonRight._uiPosition.StopAutoPositionX();
                ButtonRight._uiPosition.StopAutoPositionY();
                ButtonRight.CenterY = true;
                ButtonRight.AnchorRight = true;
                AddChild(ButtonRight);

                ButtonRight.OnClick += (args) =>
                {
                    if (_buttonRightHeld)
                    {
                        _buttonRightHeld = false;
                        return;
                    }

                    CurrentValue += StepSize;
                };

                ButtonRight.OnMouseDown += (args) =>
                {
                    CurrentValue += StepSize;
                    _buttonRightHeld = true;
                };
            }

            UpdateSizesPositions();

        } // constructor

        protected void UpdateSizesPositions()
        {
            if (!IsVisible || Width <= 0)
                return;

            var railWidth = Size.X;
            Height = MathHelper.Max(Height, Rail.Height, Slider.Height);

            if (Style.ButtonLeft != null && Style.ButtonRight != null)
            {
                Height = MathHelper.Max(Height, ButtonLeft.Height, ButtonRight.Height);

                switch (Style.ButtonType)
                {
                    case UIScrollbarButtonType.OutsideRail:
                        {
                            railWidth -= ButtonLeft.Size.X + ButtonRight.Size.X;
                        }
                        break;

                    case UIScrollbarButtonType.CenterRailEdge:
                        {
                            railWidth -= (ButtonLeft.Size.X / 2) + (ButtonRight.Size.X / 2);
                        }
                        break;
                }
            }

            var railXOffset = (Width - railWidth) / 2;

            switch (Style.SliderType)
            {
                case UIScrollbarSliderType.Center:
                    {
                        _sliderMinX = railXOffset - (Slider.Width / 2);
                        _sliderMaxX = Width - railXOffset - (Slider.Width / 2);
                    }
                    break;

                case UIScrollbarSliderType.Contain:
                    {
                        _sliderMinX = railXOffset;
                        _sliderMaxX = Width - railXOffset - Slider.Width;
                    }
                    break;
            }

            if (_sliderMinX > _sliderMaxX)
                return;

            _distancePerChange = (float)Math.Abs(_sliderMaxX - _sliderMinX) / Math.Abs(_maxValue - _minValue);

            Rail.Width = railWidth;
            Slider.SetPosition(new Vector2I());
            Slider._uiPosition.Position = new Vector2I(Math.Clamp(Slider._uiPosition.Position.Value.X, _sliderMinX, _sliderMaxX), 0);

            if (RailFill != null && RailFill.ScaleType == UIScaleType.Crop)
                RailFill.Width = railWidth - Style.RailFillPadding;

            UpdateSliderPositionFromCurrentValue();

        } // UpdateSizesPositions

        protected void UpdateSliderPositionFromCurrentValue()
        {
            Slider.X = (int)(_sliderMinX + (CurrentValue * _distancePerChange));

            if (RailFill != null)
            {
                var fillWidth = Slider.X - RailFill.X + (Slider.Width / 2);

                if (RailFill.ScaleType == UIScaleType.Scale)
                    RailFill.Width = fillWidth - Style.RailFillPadding;
                else if (RailFill.ScaleType == UIScaleType.Crop)
                    RailFill.CropWidth = fillWidth;
            }
        }

        public override void UpdateLayout(bool secondCheck = true)
        {
            base.UpdateLayout(secondCheck);
            UpdateSizesPositions();
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (Width < 0)
                return;

            base.Draw(spriteBatch);
        }

        public override void Update(GameTimer gameTimer)
        {
            if (Width < 0)
                return;

            base.Update(gameTimer);
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var val = base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            if (IsSliding)
            {
                var currentMouseOffset = mousePosition.ToVector2I() - Slider.Position;

                if (currentMouseOffset.X == _slidingMouseOffset.X)
                    return val;

                var diff = Math.Abs(currentMouseOffset.X - _slidingMouseOffset.X);
                if (diff < _distancePerStep)
                    return val;

                var direction = currentMouseOffset.X > _slidingMouseOffset.X ? 1 : -1;
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
            else if ((ButtonLeft == null || !ButtonLeft.IsPressed) && (ButtonRight == null || !ButtonRight.IsPressed))
            {
                _slidingMouseOffset = new Vector2I(Slider.Width / 2, 0);
                var currentMouseOffset = mousePosition.ToVector2I() - Slider.Position;

                if (currentMouseOffset.X == _slidingMouseOffset.X)
                    return val;

                var diff = Math.Abs(currentMouseOffset.X - _slidingMouseOffset.X);
                if (diff < _distancePerStep)
                    return val;

                var direction = currentMouseOffset.X > _slidingMouseOffset.X ? 1 : -1;
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

    } // UIScrollbarH
}
