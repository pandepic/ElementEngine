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
        public RgbaByte Color;

        public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4_Norm)
        );
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2DTileBatch
    {
        public Vector2 Position;
        public Vector2 Texture;

        public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("vTexture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
        );

        public Vertex2DTileBatch(float x, float y, float u, float v)
        {
            Position = new Vector2(x, y);
            Texture = new Vector2(u, v);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex2DPositionColor
    {
        public Vector2 Position;
        public Vector2 Padding;
        public RgbaFloat Color;

        public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("vPadding", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
        );
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex3DPositionTexCoordsColor
    {
        public Vector4 Position;
        public Vector4 TexCoords;
        public RgbaFloat Color;

        public static VertexLayoutDescription VertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("vPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("vTexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("vColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
        );
    }
}
