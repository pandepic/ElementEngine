using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ElementEngine
{
    public class Animation
    {
        public const int NO_ENDFRAME = -1;

        public float Duration { get; set; }
        public string Name { get; set; }
        public List<int> Frames { get; set; } = new List<int>();
        public int EndFrame { get; set; } = NO_ENDFRAME;
        public SpriteFlipType? Flip { get; set; } = null;

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

        public Animation(XElement el)
        {
            Name = el.Attribute("Name").Value;

            var attFlip = el.Attribute("Flip");
            if (attFlip != null)
                Flip = attFlip.Value.ToEnum<SpriteFlipType>();

            var frames = el.Attribute("Frames").Value;
            var durationAtt = el.Attribute("Duration");
            var durationPerFrameAtt = el.Attribute("DurationPerFrame");
            var endFrameAtt = el.Attribute("EndFrame");

            if (durationAtt == null && durationPerFrameAtt == null)
                throw new Exception("Animation " + Name + " must have either Duration or DurationPerFrame.");

            if (endFrameAtt != null)
                EndFrame = int.Parse(endFrameAtt.Value);

            if (durationAtt != null)
                Duration = float.Parse(durationAtt.Value);

            foreach (var frameString in frames.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if (frameString.Contains(">"))
                {
                    var frameRange = frameString.Split(">", StringSplitOptions.RemoveEmptyEntries);

                    if (frameRange.Length < 2)
                        throw new FormatException("Animation " + Name + " has an invalid frame range " + frameString);

                    var min = int.Parse(frameRange[0]);
                    var max = int.Parse(frameRange[1]);

                    FrameAddRange(min, max);
                }
                else
                {
                    Frames.Add(int.Parse(frameString));
                }
            }

            if (durationPerFrameAtt != null)
                DurationPerFrame = float.Parse(durationPerFrameAtt.Value);
        }

        public XElement ToXElement()
        {
            var element = new XElement("Animation");

            element.SetAttributeValue("Name", Name);
            element.SetAttributeValue("DurationPerFrame", DurationPerFrame);
            element.SetAttributeValue("Frames", FramesToString());

            if (EndFrame != NO_ENDFRAME)
                element.SetAttributeValue("EndFrame", EndFrame);

            if (Flip.HasValue)
                element.SetAttributeValue("Flip", Flip.Value.ToString());

            return element;
        }

        public string FramesToString()
        {
            return string.Join(',', Frames);
        }

        public void SetFramesFromString(string str)
        {
            Frames.Clear();

            foreach (var frameString in str.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if (frameString.Contains(">"))
                {
                    var frameRange = frameString.Split(">", StringSplitOptions.RemoveEmptyEntries);

                    if (frameRange.Length < 2)
                        throw new FormatException("Invalid frame range " + frameString);

                    var min = int.Parse(frameRange[0]);
                    var max = int.Parse(frameRange[1]);

                    FrameAddRange(min, max);
                }
                else
                {
                    Frames.Add(int.Parse(frameString));
                }
            }
        } // SetFramesFromString

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
