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

        protected virtual void InternalStart() { }
        protected virtual void InternalUpdate(GameTimer gameTimer) { }
        protected virtual void InternalComplete() { }

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
    }
}
