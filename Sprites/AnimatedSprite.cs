using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public class AnimatedSprite : Sprite
    {
        public Point FrameSize { get; set; }
        public int TotalFrames { get; set; }
        public int CurrentFrame;

        public AnimatedSprite(Texture2D texture, Point? frameSize, bool centerOrigin = false) : base(texture, centerOrigin)
        {
            if (!frameSize.HasValue)
                frameSize = texture.Size;

            FrameSize = frameSize.Value;

            if (centerOrigin)
                Origin = new Vector2(FrameSize.X / 2, FrameSize.Y / 2);

            TotalFrames = (texture.Size.X / FrameSize.X) * (texture.Size.Y / FrameSize.Y);
            SetFrame(1);
        }

        public void SetFrame(int frame)
        {
            if (frame < 1 || frame > TotalFrames)
                return;
            
            SourceRect.X = ((frame - 1) % (Texture.Width / FrameSize.X)) * FrameSize.X;
            SourceRect.Y = ((frame - 1) / (Texture.Width / FrameSize.X)) * FrameSize.Y;
            SourceRect.Width = FrameSize.X;
            SourceRect.Height = FrameSize.Y;

            CurrentFrame = frame;
        }
    } // AnimatedSprite
}
