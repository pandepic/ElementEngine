using System.Collections.Generic;

namespace ElementEngine
{
    public class TileAnimation
    {
        public int TileID { get; set; }
        public int DurationPerFrame { get; set; }
        public int TotalDuration => Frames.Count * DurationPerFrame;
        public List<int> Frames { get; set; } = new List<int>();
    }
}
