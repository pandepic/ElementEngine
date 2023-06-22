using System.Numerics;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISpriteAnimated : UISprite
    {
        public UITextureAnimation Animation;
        public int CurrentFrame = 0;
        public float CurrentFrameTime = 0f;
        public bool IsPlaying = false;

        public UISpriteAnimated(UISpriteAnimated copyFrom, bool baseCopy = false)
        {
            Animation = copyFrom.Animation;
            CurrentFrame = copyFrom.CurrentFrame;
            CurrentFrameTime = copyFrom.CurrentFrameTime;
            IsPlaying = copyFrom.IsPlaying;

            if (baseCopy)
                BaseCopy(copyFrom);
        }

        public UISpriteAnimated(UITextureAnimation animation, bool autoPlay)
        {
            Animation = animation;
            Size = Animation.FrameSize;

            if (autoPlay)
                Play();
        }

        public void Play()
        {
            IsPlaying = true;
            CurrentFrame = Animation.StartFrame;
            CurrentFrameTime = 0f;
        }

        public override void Update(GameTimer gameTimer)
        {
            if (IsPlaying)
            {
                CurrentFrameTime += gameTimer.DeltaS;

                if (CurrentFrameTime >= Animation.SecondsPerFrame)
                {
                    CurrentFrame += 1;
                    CurrentFrameTime -= Animation.SecondsPerFrame;

                    if (CurrentFrame > Animation.EndFrame)
                    {
                        if (Animation.Loop)
                            CurrentFrame = Animation.StartFrame;
                        else
                            IsPlaying = false;
                    }
                }
            }
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            var sourceRect = new Rectangle(
                ((CurrentFrame - 1) % (Animation.Texture.SourceRect.Width / Animation.FrameSize.X)) * Animation.FrameSize.X,
                ((CurrentFrame - 1) / (Animation.Texture.SourceRect.Width / Animation.FrameSize.X)) * Animation.FrameSize.Y,
                Animation.FrameSize.X,
                Animation.FrameSize.Y);

            var scale = new Vector2(1f);
            if (size.HasValue)
                scale = size.Value.ToVector2() / Size.ToVector2();

            Animation.Texture.Draw(
                spriteBatch,
                GetDrawPosition(parent, position, size ?? Size).ToVector2(),
                sourceRect,
                scale,
                rotation,
                null,
                Color ?? RgbaByte.White);
        }
    }
}
