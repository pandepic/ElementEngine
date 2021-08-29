using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.Timer
{
    public class CallbackTimer : ITimer
    {
        public TimerStatus TimerStatus { get; set; }

        public float BaseTime;
        public float CurrentTime;
        public bool Loop;
        public Action Callback;

        public CallbackTimer(float timeSeconds, bool loop, Action callback)
        {
            BaseTime = timeSeconds;
            CurrentTime = 0f;
            Loop = loop;
            Callback = callback;
        }

        public void Update(GameTimer gameTimer)
        {
            CurrentTime += gameTimer.DeltaS;

            if (CurrentTime >= BaseTime)
            {
                Callback();

                if (Loop)
                    CurrentTime -= BaseTime;
                else
                    TimerStatus = TimerStatus.Finished;
            }
        }

        public void Start() => TimerStatus = TimerStatus.Running;
        public void Pause() => TimerStatus = TimerStatus.Paused;
        public void Stop() => TimerStatus = TimerStatus.Finished;
        
    } // CallbackTimer
}
