using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine.EndlessTiles
{
    public class EndlessTilesRendererChunk
    {
        public EndlessTilesWorldChunk ChunkData { get; set; }
        public Rectangle ChunkRect;
        public TileBatch2D TileBatch { get; set; }

        public EndlessTilesRendererChunk(EndlessTilesWorldChunk chunkData, Texture2D tilesheet)
        {
            ChunkData = chunkData;
            ChunkRect = new Rectangle(
                ChunkData.Position * (ChunkData.World.TileSize * ChunkData.World.ChunkSize),
                ChunkData.World.TileSize * ChunkData.World.ChunkSize);

            TileBatch = new TileBatch2D(ChunkData.World.ChunkSize.X, ChunkData.World.ChunkSize.Y, ChunkData.World.TileSize.X, ChunkData.World.TileSize.Y, tilesheet, TileBatch2DWrapMode.None, ChunkData.World.TileAnimations);

            TileBatch.BeginBuild();

            foreach (var layer in ChunkData.Layers)
                BuildTilebatchLayer(layer);

            TileBatch.EndBuild();
        }

        public void BuildTilebatchLayer(EndlessTilesWorldLayer layer)
        {
            layer.DecompressTiles();

            for (int y = 0; y < ChunkData.World.ChunkSize.Y; y++)
            {
                for (int x = 0; x < ChunkData.World.ChunkSize.X; x++)
                {
                    var tileID = layer.Tiles[x + ChunkData.World.ChunkSize.X * y];

                    if (tileID == 0)
                        continue;

                    TileBatch.SetTileAtPosition(x, y, tileID);
                }
            }

            TileBatch.EndLayer();
            layer.ClearTiles();
        }
    } // EndlessTilesRendererChunk

    public class EndlessTilesRenderer
    {
        public EndlessTilesWorld World { get; set; }
        public Texture2D Tilesheet { get; set; }
        public Dictionary<Vector2I, EndlessTilesRendererChunk> Chunks { get; set; } = new Dictionary<Vector2I, EndlessTilesRendererChunk>();

        public EndlessTilesRenderer(EndlessTilesWorld world, Texture2D tilesheet)
        {
            World = world;
            Tilesheet = tilesheet;

            foreach (var (_, chunk) in world.Chunks)
                Chunks.Add(chunk.Position, new EndlessTilesRendererChunk(chunk, tilesheet));
        }

        public void Update(GameTimer gameTimer)
        {
            foreach (var (_, chunk) in Chunks)
                chunk.TileBatch.Update(gameTimer);
        }

        public void DrawLayers(int start, int end, Camera2D camera)
        {
            foreach (var (pos, chunk) in Chunks)
            {
                if (camera.ScaledView.Intersects(chunk.ChunkRect))
                    chunk.TileBatch.DrawLayers(start, end, camera, chunk.ChunkRect.LocationF);
            }
        } // DrawLayers

    } // EndlessTilesRenderer
}
