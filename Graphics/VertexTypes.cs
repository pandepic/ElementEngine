using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace ElementEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2DPositionTexCoordsColor
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaFloat Color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2DTileBatch
    {
        public Vector2 Position;
        public Vector2 Texture;

        public Vertex2DTileBatch(float x, float y, float u, float v)
        {
            Position = new Vector2(x, y);
            Texture = new Vector2(u, v);
        }
    }
}
