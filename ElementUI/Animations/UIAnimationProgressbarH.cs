using System;

namespace ElementEngine.ElementUI
{
    public class UIAnimationProgressbarH : UIAnimation
    {
        public UIProgressbarH ProgressbarH => Object as UIProgressbarH;

        public EasingType EasingType;
        public float TimePerValueChange;
        public float MaxDuration;

        private int _startWidth;
        private int _targetWidth;
        private int _changeDirection;
        private int _totalChangeAmount;

        public UIAnimationProgressbarH() { }

        public UIAnimationProgressbarH(UIObject obj) : base(obj)
        {
        }

        public UIAnimationProgressbarH Copy()
        {
            var copy = new UIAnimationProgressbarH()
            {
                EasingType = EasingType,
                TimePerValueChange = TimePerValueChange,
                MaxDuration = MaxDuration,
            };

            BaseCopy(copy);
            return copy;
        }

        public void Start(int startWidth, int targetWidth)
        {
            _startWidth = startWidth;
            _targetWidth = targetWidth;

            _changeDirection = _targetWidth > _startWidth ? 1 : -1;
            _totalChangeAmount = Math.Abs(_targetWidth - _startWidth);

            if (TimePerValueChange > 0)
                Duration = TimePerValueChange * _totalChangeAmount;

            if (MaxDuration > 0 && Duration > MaxDuration)
                Duration = MaxDuration;

            Start();
        }

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            var easingTime = MathHelper.Normalize(RunningTime, 0, Duration);
            var easingValue = Easings.Ease(easingTime, EasingType);

            var fillWidth = (float)_startWidth + ((_totalChangeAmount * easingValue) * (float)_changeDirection);
            ProgressbarH.SetFillWidth((int)fillWidth);
        }
    }
}
