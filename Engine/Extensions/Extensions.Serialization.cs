using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using ElementEngine.ECS;
using Veldrid;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, ref RgbaByte rgba)
        {
            writer.Write(rgba.R);
            writer.Write(rgba.G);
            writer.Write(rgba.B);
            writer.Write(rgba.A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RgbaByte ReadRgbaByte(this BinaryReader reader)
        {
            return new RgbaByte(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this BinaryWriter writer, ref RgbaFloat rgba)
        {
            writer.Write(rgba.R);
            writer.Write(rgba.G);
            writer.Write(rgba.B);
            writer.Write(rgba.A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RgbaFloat ReadRgbaFloat(this BinaryReader reader)
        {
            return new RgbaFloat(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this BinaryWriter writer, T enumVal) where T : struct, IConvertible
        {
            writer.Write(Unsafe.As<T, int>(ref enumVal));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadEnum<T>(this BinaryReader reader) where T : struct, IConvertible
        {
            var intVal = reader.ReadInt32();
            return Unsafe.As<int, T>(ref intVal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity ReadEntity(this BinaryReader reader, Registry registry)
        {
            var intVal = reader.ReadInt32();

            if (intVal == 0)
                return Entity.Empty;

            return registry.CreateEntity(intVal);
        }
    } // Extensions
}
