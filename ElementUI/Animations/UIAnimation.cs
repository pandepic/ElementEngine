using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public abstract class UIAnimation
    {
        public UIObject Object;

        public Action<UIAnimation> OnComplete;
        public Action<UIAnimation> OnUpdate;

        public bool IsRunning;
        public bool IsComplete;

        public float RunningTime;
        public float Duration;

        public UIAnimation() { }

        public UIAnimation(UIObject obj)
        {
            Object = obj;
        }

        protected virtual void BaseCopy(UIAnimation animation)
        {
            animation.Object = Object;
            animation.OnComplete = OnComplete;
            animation.OnUpdate = OnUpdate;
            animation.Duration = Duration;
        }

        public void Start()
        {
            RunningTime = 0;
            IsComplete = false;
            IsRunning = true;

            Object.UIAnimations.AddIfNotContains(this);

            InternalStart();
        }

        protected virtual void InternalStart() { }

        public void Update(GameTimer gameTimer)
        {
            if (Duration > 0)
            {
                RunningTime += gameTimer.DeltaS;

                InternalUpdate(gameTimer);
                OnUpdate?.Invoke(this);
            }

            if (RunningTime >= Duration)
            {
                IsComplete = true;
                InternalComplete();
                OnComplete?.Invoke(this);
            }
        }

        protected virtual void InternalUpdate(GameTimer gameTimer) { }
        protected virtual void InternalComplete() { }
    }

    public class UIProgressbarHAnimation : UIAnimation
    {
        public UIProgressbarH ProgressbarH => Object as UIProgressbarH;

        public EasingType EasingType;
        public float TimePerValueChange;
        public float MaxDuration;

        public int StartWidth;
        public int TargetWidth;
        public int ChangeDirection;
        public int TotalChangeAmount;

        public UIProgressbarHAnimation() { }

        public UIProgressbarHAnimation(UIObject obj) : base(obj)
        {
        }

        public UIProgressbarHAnimation Copy()
        {
            var copy = new UIProgressbarHAnimation()
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
