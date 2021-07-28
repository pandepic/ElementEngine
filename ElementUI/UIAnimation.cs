using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public struct UIAnimation
    {
        public UITexture Texture;
        public Vector2I FrameSize;
        public bool Loop;

        public int StartFrame;
        public int EndFrame;
        public int TotalFrames => EndFrame - StartFrame + 1;
        public int TotalTextureFrames;

        public float TotalSeconds;
        public float SecondsPerFrame => TotalSeconds / TotalFrames;

        public UIAnimation(string assetName, Vector2I frameSize, int startFrame, int endFrame, float totalTimeSeconds, bool loop)
            : this(new UITexture(AssetManager.LoadTexture2D(assetName)), frameSize, startFrame, endFrame, totalTimeSeconds, loop)
        {
        }

        public UIAnimation(UITexture texture, Vector2I frameSize, int startFrame, int endFrame, float totalTimeSeconds, bool loop)
        {
            Texture = texture;
            FrameSize = frameSize;
            TotalSeconds = totalTimeSeconds;
            Loop = loop;

            StartFrame = startFrame;
            EndFrame = endFrame;

            TotalTextureFrames = (int)(((float)texture.SourceRect.Width / frameSize.X) * ((float)texture.SourceRect.Height / frameSize.Y));
        }
    }
}
