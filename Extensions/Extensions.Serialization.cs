using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public static partial class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, ref Vector2 vec)
        {
            writer.Write(vec.X);
            writer.Write(vec.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, ref Vector2I vec)
        {
            writer.Write(vec.X);
            writer.Write(vec.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2I ReadVector2I(this BinaryReader reader)
        {
            return new Vector2I(reader.ReadInt32(), reader.ReadInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, ref Rectangle rect)
        {
            writer.Write(rect.X);
            writer.Write(rect.Y);
            writer.Write(rect.Width);
            writer.Write(rect.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rectangle ReadRectangle(this BinaryReader reader)
        {
            return new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
    } // Extensions
}
