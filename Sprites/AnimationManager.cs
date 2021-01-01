using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace ElementEngine
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
                    var newAnimation = new Animation(animation);
                    _animations.Add(newAnimation.Name, newAnimation);
                    loadedCount += 1;
                }
            }

            stopWatch.Stop();
            Logging.Information("[{component}] loaded {count} animations from {asset} in {time:0.00} ms.", "AnimationManager", loadedCount, assetName, stopWatch.Elapsed.TotalMilliseconds);
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
