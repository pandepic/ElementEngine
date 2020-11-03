using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public class GameTimer
    {
        protected TimeSpan _frameTime;
        public TimeSpan FrameTime { get => _frameTime; }

        public float DeltaS { get => (float)FrameTime.TotalSeconds; }
        public float DeltaMS { get => (float)FrameTime.TotalMilliseconds; }

        public GameTimer()
        {
            _frameTime = TimeSpan.Zero;
        }

        internal void SetFrameTime(TimeSpan frameTime)
        {
            _frameTime = frameTime;
        }
    }
}
