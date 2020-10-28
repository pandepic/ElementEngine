using System;
using System.Collections.Generic;
using System.Text;

namespace PandaEngine
{
    public class Animation
    {
        public float Duration { get; set; }
        public string Name { get; set; }
        public List<int> Frames { get; set; } = new List<int>();
        public int EndFrame { get; set; }
        public SpriteFlipType Flip { get; set; } = SpriteFlipType.None;

        public Animation() { }

        public void FrameSetRange(int min, int max)
        {
            if (min < max)
                throw new ArgumentException("min must be less than max.", "min");

            Frames.Clear();
            FrameAddRange(min, max);
        }

        public void FrameAddRange(int min, int max)
        {
            if (min < max)
                throw new ArgumentException("min must be less than max.", "min");

            for (var i = min; i <= max; i++)
                Frames.Add(i);
        }

    } // Animation
}
