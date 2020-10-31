using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace PandaEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2DPositionTexCoordsColor
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public RgbaFloat Color;
    }
}
