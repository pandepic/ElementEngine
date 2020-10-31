using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public static partial class Extensions
    {
        public static TextureDescription GetDescription(this Texture texture)
        {
            return new TextureDescription(texture.Width, texture.Height, texture.Depth, texture.MipLevels, texture.ArrayLayers, texture.Format, texture.Usage, texture.Type);
        }

        #region Veldrid color extensions
        public static RgbaByte ToRgbaByte(this RgbaFloat color)
        {
            return new RgbaByte((byte)(255f * color.R), (byte)(255f * color.G), (byte)(255f * color.B), (byte)(255f * color.A));
        }

        public static RgbaVector ToRgbaVector(this RgbaFloat color)
        {
            return new RgbaVector(color.R, color.G, color.G, color.A);
        }

        public static RgbaFloat ToRgbaFloat(this RgbaByte color)
        {
            return new RgbaFloat(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static RgbaVector ToRgbaVector(this RgbaByte color)
        {
            return new RgbaVector(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }
        #endregion

    } // Extensions
}
