using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIAnimationBlink : UIAnimation
    {
        public float TimePerBlink;
        public float BlinkTime;
        public int Blinks;

        private bool _hiddenOnComplete;
        private float _blinkTimer;

        public UIAnimationBlink() { }

        public UIAnimationBlink(UIObject obj) : base(obj)
        {
        }

        public UIAnimationBlink Copy()
        {
            var copy = new UIAnimationBlink()
            {
            };

            BaseCopy(copy);
            return copy;
        }

        protected override void InternalStart()
        {
            _hiddenOnComplete = Object.IsHidden;
            Duration = TimePerBlink * (Blinks * 2);
        }

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            _blinkTimer += gameTimer.DeltaS;

            if (_blinkTimer >= (Object.IsHidden ? BlinkTime : TimePerBlink))
            {
                _blinkTimer -= (Object.IsHidden ? BlinkTime : TimePerBlink);
                Object.IsHidden = !Object.IsHidden;
            }
        }

        protected override void InternalComplete()
        {
            Object.IsHidden = _hiddenOnComplete;
        }
    }
}
