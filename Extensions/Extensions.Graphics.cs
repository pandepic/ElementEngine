using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Globalization;
using Veldrid;

namespace ElementEngine
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
        
        public static System.Drawing.Color ToDrawingColor(this RgbaByte color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static RgbaByte FromHex(this RgbaByte color, string hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString))
                return color;
            if (hexString.StartsWith("#"))
                hexString = hexString.Substring(1);

            uint hex = uint.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            if (hexString.Length == 8)
            {
                return new RgbaByte(
                    (byte)(hex >> 16), // R
                    (byte)(hex >> 8), // G
                    (byte)(hex), // B
                    (byte)(hex >> 24)); // A
            }
            else if (hexString.Length == 6)
            {
                return new RgbaByte(
                    (byte)(hex >> 16), // R
                    (byte)(hex >> 8), // G
                    (byte)(hex), // B
                    255); // A
            }
            else
            {
                throw new InvalidOperationException("Invald hex representation of an ARGB or RGB color value.");
            }
        } // FromHex

        public static RgbaFloat FromHex(string hexString)
        {
            return new RgbaByte().FromHex(hexString).ToRgbaFloat();
        }
        #endregion

        #region Veldrid buffer extensions
        public static RgbaByte[] ToRgbaByte(this byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new Exception("Byte buffer has invalid format, must be 4 bytes per pixel in RGBA order.");

            var data = new RgbaByte[buffer.Length / 4];

            // convert flat byte array to RGBA with a pixel every 4 bytes   
            for (var i = 0; i < data.Length; i++)
                data[i] = new RgbaByte(buffer[(i * 4)], buffer[(i * 4) + 1], buffer[(i * 4) + 2], buffer[(i * 4) + 3]);

            return data;
        }

        public static RgbaFloat[] ToRgbaFloat(this byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new Exception("Byte buffer has invalid format, must be 4 bytes per pixel in RGBA order.");

            var data = new RgbaFloat[buffer.Length / 4];

            // convert flat byte array to RGBA with a pixel every 4 bytes   
            for (var i = 0; i < data.Length; i++)
                data[i] = new RgbaFloat(buffer[(i * 4)] / 255f, buffer[(i * 4) + 1] / 255f, buffer[(i * 4) + 2] / 255f, buffer[(i * 4) + 3] / 255f);

            return data;
        }

        public static Rgba32[] ToRgba32(this byte[] buffer)
        {
            if (buffer.Length % 4 != 0)
                throw new Exception("Byte buffer has invalid format, must be 4 bytes per pixel in RGBA order.");

            var data = new Rgba32[buffer.Length / 4];

            // convert flat byte array to RGBA with a pixel every 4 bytes   
            for (var i = 0; i < data.Length; i++)
                data[i] = new Rgba32(buffer[(i * 4)], buffer[(i * 4) + 1], buffer[(i * 4) + 2], buffer[(i * 4) + 3]);

            return data;
        }

        public static RgbaByte[] ToBuffer(this RgbaByte color, int size)
        {
            var data = new RgbaByte[size];
            for (var i = 0; i < data.Length; i++)
                data[i] = color;

            return data;
        }

        public static RgbaFloat[] ToBuffer(this RgbaFloat color, int size)
        {
            var data = new RgbaFloat[size];
            for (var i = 0; i < data.Length; i++)
                data[i] = color;

            return data;
        }

        #endregion

    } // Extensions
}
