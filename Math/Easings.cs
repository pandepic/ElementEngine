using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    // https://easings.net/

    public enum EasingType
    {
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,

        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,

        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,

        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,

        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,

        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,

        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,

        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,

        EaseInBack,
        EaseOutBack,
        EaseInOutBack,

        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
    }

    public static class Easings
    {
        public static float Ease(float x, EasingType easingType)
        {
            return easingType switch
            {
                EasingType.EaseInSine => EaseInSine(x),
                EasingType.EaseOutSine => EaseOutSine(x),
                EasingType.EaseInOutSine => EaseInOutSine(x),

                EasingType.EaseInCubic => EaseInCubic(x),
                EasingType.EaseOutCubic => EaseOutCubic(x),
                EasingType.EaseInOutCubic => EaseInOutCubic(x),

                EasingType.EaseInQuint => EaseInQuint(x),
                EasingType.EaseOutQuint => EaseOutQuint(x),
                EasingType.EaseInOutQuint => EaseInOutQuint(x),

                EasingType.EaseInCirc => EaseInCirc(x),
                EasingType.EaseOutCirc => EaseOutCirc(x),
                EasingType.EaseInOutCirc => EaseInOutCirc(x),

                EasingType.EaseInElastic => EaseInElastic(x),
                EasingType.EaseOutElastic => EaseOutElastic(x),
                EasingType.EaseInOutElastic => EaseInOutElastic(x),

                EasingType.EaseInQuad => EaseInQuad(x),
                EasingType.EaseOutQuad => EaseOutQuad(x),
                EasingType.EaseInOutQuad => EaseInOutQuad(x),

                EasingType.EaseInQuart => EaseInQuart(x),
                EasingType.EaseOutQuart => EaseOutQuart(x),
                EasingType.EaseInOutQuart => EaseInOutQuart(x),

                EasingType.EaseInExpo => EaseInExpo(x),
                EasingType.EaseOutExpo => EaseOutExpo(x),
                EasingType.EaseInOutExpo => EaseInOutExpo(x),

                EasingType.EaseInBack => EaseInBack(x),
                EasingType.EaseOutBack => EaseOutBack(x),
                EasingType.EaseInOutBack => EaseInOutBack(x),

                EasingType.EaseInBounce => EaseInBounce(x),
                EasingType.EaseOutBounce => EaseOutBounce(x),
                EasingType.EaseInOutBounce => EaseInOutBounce(x),

                _ => throw new NotImplementedException(),
            };
        }

        public static float EaseInSine(float x)
        {
            return 1f - MathF.Cos((x * MathF.PI) / 2f);
        }

        public static float EaseOutSine(float x)
        {
            return MathF.Sin((x * MathF.PI) / 2f);
        }

        public static float EaseInOutSine(float x)
        {
            return -(MathF.Cos(MathF.PI * x) - 1f) / 2f;
        }

        public static float EaseInCubic(float x)
        {
            return x * x * x;
        }

        public static float EaseOutCubic(float x)
        {
            return 1f - MathF.Pow(1f - x, 3f);
        }

        public static float EaseInOutCubic(float x)
        {
            return x < 0.5f ? 4f * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f;
        }

        public static float EaseInQuint(float x)
        {
            return x * x * x * x * x;
        }

        public static float EaseOutQuint(float x)
        {
            return 1f - MathF.Pow(1f - x, 5f);
        }

        public static float EaseInOutQuint(float x)
        {
            return x < 0.5f ? 16f * x * x * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 5f) / 2f;
        }

        public static float EaseInCirc(float x)
        {
            return 1f - MathF.Sqrt(1f - MathF.Pow(x, 2f));
        }

        public static float EaseOutCirc(float x)
        {
            return MathF.Sqrt(1f - MathF.Pow(x - 1f, 2f));
        }

        public static float EaseInOutCirc(float x)
        {
            return x < 0.5f
              ? (1f - MathF.Sqrt(1f - MathF.Pow(2f * x, 2f))) / 2f
              : (MathF.Sqrt(1f - MathF.Pow(-2f * x + 2f, 2f)) + 1f) / 2f;
        }

        public static float EaseInElastic(float x)
        {
            var c4 = (2f * MathF.PI) / 3f;

            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : -MathF.Pow(2f, 10f * x - 10f) * MathF.Sin((x * 10f - 10.75f) * c4);
        }

        public static float EaseOutElastic(float x)
        {
            var c4 = (2f * MathF.PI) / 3f;

            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : MathF.Pow(2f, -10f * x) * MathF.Sin((x * 10f - 0.75f) * c4) + 1;
        }

        public static float EaseInOutElastic(float x)
        {
            var c5 = (2f * MathF.PI) / 4.5f;

            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : x < 0.5f
              ? -(MathF.Pow(2, 20 * x - 10) * MathF.Sin((20f * x - 11.125f) * c5)) / 2f
              : (MathF.Pow(2, -20 * x + 10) * MathF.Sin((20f * x - 11.125f) * c5)) / 2f + 1f;
        }

        public static float EaseInQuad(float x)
        {
            return x * x;
        }

        public static float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        public static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2f * x * x : 1f - MathF.Pow(-2f * x + 2f, 2f) / 2f;
        }

        public static float EaseInQuart(float x)
        {
            return x * x * x * x;
        }

        public static float EaseOutQuart(float x)
        {
            return 1f - MathF.Pow(1f - x, 4f);
        }

        public static float EaseInOutQuart(float x)
        {
            return x < 0.5f ? 8f * x * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 4f) / 2f;
        }

        public static float EaseInExpo(float x)
        {
            return x == 0f ? 0f : MathF.Pow(2f, 10f * x - 10f);
        }

        public static float EaseOutExpo(float x)
        {
            return x == 1f ? 1f : 1f - MathF.Pow(2f, -10f * x);
        }

        public static float EaseInOutExpo(float x)
        {
            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : x < 0.5f ? MathF.Pow(2f, 20f * x - 10f) / 2f
              : (2f - MathF.Pow(2f, -20f * x + 10f)) / 2f;
        }

        public static float EaseInBack(float x)
        {
            var c1 = 1.70158f;
            var c3 = c1 + 1f;

            return c3 * x * x * x - c1 * x * x;
        }

        public static float EaseOutBack(float x)
        {
            var c1 = 1.70158f;
            var c3 = c1 + 1f;

            return 1 + c3 * MathF.Pow(x - 1f, 3f) + c1 * MathF.Pow(x - 1f, 2f);
        }

        public static float EaseInOutBack(float x)
        {
            var c1 = 1.70158f;
            var c2 = c1 * 1.525f;

            return x < 0.5f
              ? (MathF.Pow(2f * x, 2f) * ((c2 + 1f) * 2f * x - c2)) / 2f
              : (MathF.Pow(2f * x - 2f, 2f) * ((c2 + 1f) * (x * 2f - 2f) + c2) + 2f) / 2f;
        }

        public static float EaseInBounce(float x)
        {
            return 1 - EaseOutBounce(1 - x);
        }

        public static float EaseOutBounce(float x)
        {
            var n1 = 7.5625f;
            var d1 = 2.75f;

            if (x < 1f / d1)
                return n1 * x * x;
            else if (x < 2f / d1)
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            else if (x < 2.5f / d1)
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            else
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }

        public static float EaseInOutBounce(float x)
        {
            return x < 0.5f
              ? (1f - EaseOutBounce(1f - 2f * x)) / 2f
              : (1f + EaseOutBounce(2f * x - 1)) / 2f;
        }
    }
}
