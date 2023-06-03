using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public class GameTimer
    {
        /// <summary>
        /// Raw time ignoring time warp factor.
        /// </summary>
        public TimeSpan RawFrameTime;

        public TimeSpan FrameTime;
        public float TimeWarpFactor = 1f;

        public float DeltaS { get => (float)FrameTime.TotalSeconds; }
        public float DeltaMS { get => (float)FrameTime.TotalMilliseconds; }

        /// <summary>
        /// Raw time ignoring time warp factor.
        /// </summary>
        public float RawDeltaS { get => (float)RawFrameTime.TotalSeconds; }

        /// <summary>
        /// Raw time ignoring time warp factor.
        /// </summary>
        public float RawDeltaMS { get => (float)RawFrameTime.TotalMilliseconds; }

        public GameTimer()
        {
            FrameTime = TimeSpan.Zero;
        }

        public void SetFrameTime(TimeSpan frameTime)
        {
            RawFrameTime = frameTime;
            FrameTime = frameTime * TimeWarpFactor;
        }
    }
}
