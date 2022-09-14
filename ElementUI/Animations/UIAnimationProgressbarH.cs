using System;

namespace ElementEngine.ElementUI
{
    public class UIAnimationProgressbarH : UIAnimation
    {
        public UIProgressbarH ProgressbarH => Object as UIProgressbarH;

        public EasingType EasingType;
        public float TimePerValueChange;
        public float MaxDuration;

        public int StartWidth;
        public int TargetWidth;
        public int ChangeDirection;
        public int TotalChangeAmount;

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
            StartWidth = startWidth;
            TargetWidth = targetWidth;

            ChangeDirection = TargetWidth > StartWidth ? 1 : -1;
            TotalChangeAmount = Math.Abs(TargetWidth - StartWidth);

            if (TimePerValueChange > 0)
                Duration = TimePerValueChange * TotalChangeAmount;

            if (MaxDuration > 0 && Duration > MaxDuration)
                Duration = MaxDuration;

            Start();
        }

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            var easingTime = MathHelper.Normalize(RunningTime, 0, Duration);
            var easingValue = Easings.Ease(easingTime, EasingType);

            var fillWidth = (float)StartWidth + ((TotalChangeAmount * easingValue) * (float)ChangeDirection);
            ProgressbarH.SetFillWidth((int)fillWidth);
        }
    }
}
