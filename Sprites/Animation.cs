using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public class Animation
    {
        public static readonly int NO_ENDFRAME = -1;

        public float Duration { get; set; }
        public string Name { get; set; }
        public List<int> Frames { get; set; } = new List<int>();
        public int EndFrame { get; set; } = NO_ENDFRAME;
        public SpriteFlipType Flip { get; set; } = SpriteFlipType.None;

        public float DurationPerFrame
        {
            get => Duration / Frames.Count;
            set => Duration = Frames.Count * value;
        }

        public Animation() { }
        public Animation(int min, int max, float duration)
        {
            Duration = duration;
            FrameAddRange(min, max);
        }

        public void FrameSetRange(int min, int max)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than max.", "min");

            Frames.Clear();
            FrameAddRange(min, max);
        }

        public void FrameAddRange(int min, int max)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than max.", "min");

            for (var i = min; i <= max; i++)
                Frames.Add(i);
        }

    } // Animation
}
