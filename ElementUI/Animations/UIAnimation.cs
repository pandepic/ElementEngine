using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public enum UIAnimationType
    {
        ProgressbarHValue,
    }

    public delegate void UIAnimationEvent(ref UIAnimation animation);

    public struct UIAnimation
    {
        public UIObject UIObject;
        public UIAnimationType UIAnimationType;
        public EasingType EasingType;
        public float Duration; // in seconds
        public int TargetValue;
        public int ChangeDirection;
        public bool IsComplete;

        public UIAnimationEvent OnComplete;
        public UIAnimationEvent OnTick;

        private float SecondsPerTick;
        private float ToNextTick;
        public int TicksRemaining;

        public void Start()
        {
            switch (UIAnimationType)
            {
                case UIAnimationType.ProgressbarHValue:
                    {
                        var progressBarH = UIObject as UIProgressbarH;

                        if (TargetValue == progressBarH.CurrentValue)
                        {
                            IsComplete = true;
                            break;
                        }

                        ChangeDirection = TargetValue > progressBarH.CurrentValue ? 1 : -1;
                        TicksRemaining = Math.Abs(TargetValue - progressBarH.CurrentValue);
                        SecondsPerTick = Duration / TicksRemaining;
                    }
                    break;
            }

            if (IsComplete)
                OnComplete?.Invoke(ref this);
        }

        public void Update(GameTimer gameTimer)
        {
            if (IsComplete)
                return;

            ToNextTick += gameTimer.DeltaS;

            while (ToNextTick >= SecondsPerTick)
            {
                ToNextTick -= SecondsPerTick;
                TicksRemaining -= 1;

                Tick();

                if (TicksRemaining <= 0)
                    break;
            }

            if (TicksRemaining <= 0)
                IsComplete = true;

            if (IsComplete)
                OnComplete?.Invoke(ref this);
        }

        private void Tick()
        {
            switch (UIAnimationType)
            {
                case UIAnimationType.ProgressbarHValue:
                    {
                        var progressBar = UIObject as UIProgressbarH;
                        progressBar.CurrentValue += ChangeDirection;
                    }
                    break;
            }

            OnTick?.Invoke(ref this);
        }
    }
}
