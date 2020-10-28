using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace PandaEngine
{
    public static class AnimationManager
    {
        private static readonly Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        public static void LoadAnimations(string assetName)
        {
            var stopWatch = Stopwatch.StartNew();
            var loadedCount = 0;

            using (var fs = AssetManager.GetAssetStream(assetName))
            {
                var animationsDoc = XDocument.Load(fs);

                foreach (var animation in animationsDoc.Root.Elements("Animation"))
                {
                    var name = animation.Attribute("Name").Value;
                    var frames = animation.Attribute("Frames").Value;
                    var flip = animation.Attribute("Flip").Value.ToEnum<SpriteFlipType>();

                    var durationAtt = animation.Attribute("Duration");
                    var durationPerFrameAtt = animation.Attribute("DurationPerFrame");
                    var endFrameAtt = animation.Attribute("EndFrame");

                    if (durationAtt == null && durationPerFrameAtt == null)
                        throw new Exception("Animation " + name + " must have either Duration or DurationPerFrame.");

                    var newAnimation = new Animation()
                    {
                        Flip = flip
                    };

                    if (endFrameAtt != null)
                        newAnimation.EndFrame = int.Parse(endFrameAtt.Value);

                    if (durationAtt != null)
                        newAnimation.Duration = float.Parse(durationAtt.Value);

                    foreach (var frameString in frames.Split(",", StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (frameString.Contains(">"))
                        {
                            var frameRange = frameString.Split(">", StringSplitOptions.RemoveEmptyEntries);

                            if (frameRange.Length < 2)
                                throw new FormatException("Animation " + name + " has an invalid frame range " + frameString);

                            var min = int.Parse(frameRange[0]);
                            var max = int.Parse(frameRange[1]);

                            newAnimation.FrameAddRange(min, max);
                        }
                        else
                        {
                            newAnimation.Frames.Add(int.Parse(frameString));
                        }
                    }

                    if (durationPerFrameAtt != null)
                        newAnimation.DurationPerFrame = float.Parse(durationPerFrameAtt.Value);

                    _animations.Add(name, newAnimation);
                    loadedCount += 1;
                }
            }

            stopWatch.Stop();
            Logging.Logger.Information("[{component}] loaded {count} animations from {asset} in {time:0.00} ms.", "AnimationManager", loadedCount, assetName, stopWatch.Elapsed.TotalMilliseconds);
        } // LoadAnimations

        public static Animation GetAnimation(string name)
        {
            if (_animations.TryGetValue(name, out Animation animation))
                return animation;
            else
                throw new KeyNotFoundException(name + " not found in the AnimationManager");
        } // GetAnimation
    }
}
