using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public class AnimatedSprite : Sprite
    {
        public const int LOOP_FOREVER = -1;

        public Vector2i FrameSize { get; set; }
        public int TotalFrames { get; set; }
        public int CurrentFrame { get; set; }
        public int CurrentFrameIndex { get; set; }
        public Animation CurrentAnimation { get; set; }

        protected float _currentFrameTime = 0.0f;
        protected float _timePerFrame = 0.0f;
        protected int _animationLoopCount = 0;
        protected SpriteFlipType _prevFlip;

        public AnimatedSprite(Texture2D texture, Vector2i? frameSize, bool centerOrigin = false) : base(texture, centerOrigin)
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

        public void PlayAnimation(Animation animation, int loopCount = LOOP_FOREVER)
        {
            if (animation.Frames.Count <= 0)
                throw new ArgumentException("Animation has no frames.", "animation");

            CurrentAnimation = animation;

            CurrentFrameIndex = 0;
            _currentFrameTime = 0.0f;
            _timePerFrame = animation.Duration / (float)animation.Frames.Count;
            _animationLoopCount = loopCount;

            if (_animationLoopCount != LOOP_FOREVER)
                _animationLoopCount -= 1;

            SetFrame(CurrentAnimation.Frames[CurrentFrameIndex]);
            _prevFlip = Flip;
            Flip = animation.Flip;
        }

        public void StopAnimation()
        {
            if (CurrentAnimation == null)
                return;

            if (CurrentAnimation.EndFrame != Animation.NO_ENDFRAME)
                SetFrame(CurrentAnimation.EndFrame);

            CurrentAnimation = null;
            Flip = _prevFlip;
        }

        public override void Update(GameTimer gameTimer)
        {
            base.Update(gameTimer);

            if (CurrentAnimation == null)
                return;

            _currentFrameTime += gameTimer.DeltaMS;

            if (_currentFrameTime >= _timePerFrame)
            {
                _currentFrameTime -= _timePerFrame;

                CurrentFrameIndex++;

                if (CurrentFrameIndex >= CurrentAnimation.Frames.Count)
                {
                    if (_animationLoopCount == LOOP_FOREVER || _animationLoopCount > 0)
                    {
                        CurrentFrameIndex = 0;

                        if (_animationLoopCount != LOOP_FOREVER)
                            _animationLoopCount--;

                        SetFrame((int)CurrentAnimation.Frames[CurrentFrameIndex]);
                    }
                    else if (_animationLoopCount <= 0)
                    {
                        StopAnimation();
                    }
                }
                else
                {
                    SetFrame((int)CurrentAnimation.Frames[CurrentFrameIndex]);
                }
            }
        }
    } // AnimatedSprite
}
