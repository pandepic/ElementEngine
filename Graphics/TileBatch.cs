using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace ElementEngine.Graphics
{
    public class TileBatch : IDisposable
    {
        public Sdl2Window Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;

        // Graphics resources
        protected Pipeline _pipeline;
        protected DeviceBuffer _vertexBuffer;
        protected Vertex2DTileBatch[] _vertices;

        // Map data
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public Texture2D AtlasTexture { get; set; }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _pipeline?.Dispose();
                    _vertexBuffer?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public TileBatch(int mapWidth, int mapHeight, int tileWidth, int tileHeight, Texture2D atlasTexture)
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            TileWidth = tileWidth;
            TileHeight = tileHeight;

            AtlasTexture = atlasTexture;

            _vertices = new Vertex2DTileBatch[6];
            _vertices[0] = new Vertex2DTileBatch(-1f, -1f, 0f, 1f); // tri 1
            _vertices[0] = new Vertex2DTileBatch(1f, -1f, 1f, 1f);
            _vertices[0] = new Vertex2DTileBatch(1f, 1f, 1f, 0f);
            _vertices[0] = new Vertex2DTileBatch(-1f, -1f, 0f, 1f); // tri 2
            _vertices[0] = new Vertex2DTileBatch(1f, 1f, 1f, 0f);
            _vertices[0] = new Vertex2DTileBatch(-1f, 1f, 0f, 0f);
        }
    }
}
