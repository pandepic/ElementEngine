using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElementEngine.EndlessTiles
{
    public class EndlessTilesWorldLayerData
    {
        public int Index { get; set; }
        public string CompressedTiles { get; set; }
    }

    public class EndlessTilesWorldChunkData
    {
        public Vector2I Position { get; set; }
        public List<EndlessTilesWorldLayerData> Layers { get; set; }

    }

    public class EndlessTilesWorldData
    {
        public string Name { get; set; }
        public string TilesheetPath { get; set; }
        public string TilesheetName { get; set; }
        public Vector2I TileSize { get; set; }
        public Vector2I ChunkSize { get; set; }

        public Dictionary<string, EndlessTilesWorldChunkData> SavedChunks { get; set; }
    }

    public class EndlessTilesWorldLayer
    {
        public EndlessTilesWorldChunk Chunk { get; set; }

        public int Index { get; set; }
        public string CompressedTiles { get; set; }
        public int[] Tiles { get; set; }

        public bool TilesLoaded => Tiles != null;

        public EndlessTilesWorldLayer(int index, string compressedTiles, EndlessTilesWorldChunk chunk)
        {
            Index = index;
            CompressedTiles = compressedTiles;
            Chunk = chunk;
        }

        public void ClearTiles()
        {
            Tiles = null;
        }

        public void ResetTiles()
        {
            for (var i = 0; i < Chunk.TotalTiles; i++)
                Tiles[i] = EndlessTilesWorld.BLANK_TILE;
        }

        public void DecompressTiles()
        {
            if (Tiles == null)
                Tiles = new int[Chunk.TotalTiles];
            else
                ResetTiles();

            var split = CompressedTiles.Split(",", StringSplitOptions.RemoveEmptyEntries);
            var index = 0;

            foreach (var str in split)
            {
                var tileSplit = str.Split("x", StringSplitOptions.RemoveEmptyEntries);

                var tile = int.Parse(tileSplit[0]);
                var count = tileSplit.Length > 1 ? int.Parse(tileSplit[1]) : 1;

                for (var i = 0; i < count; i++)
                {
                    Tiles[index] = tile;
                    index += 1;
                }
            }
        } // DecompressTiles

    } // EndlessTilesWorldLayer

    public class EndlessTilesWorldChunk
    {
        public EndlessTilesWorld World { get; set; }
        public Vector2I Position { get; set; }
        public List<EndlessTilesWorldLayer> Layers { get; set; } = new List<EndlessTilesWorldLayer>();

        public int TotalTiles => World.ChunkSize.X * World.ChunkSize.Y;

        public EndlessTilesWorldChunk(Vector2I position, EndlessTilesWorld world)
        {
            Position = position;
            World = world;
        }

        public void AddLayer(int index, string compressedTiles)
        {
            Layers.Add(new EndlessTilesWorldLayer(index, compressedTiles, this));
        }

        public void ClearTiles()
        {
            foreach (var layer in Layers)
                layer.ClearTiles();
        }

        public void ResetTiles()
        {
            foreach (var layer in Layers)
                layer.ResetTiles();
        }

        public void DecompressTiles()
        {
            foreach (var layer in Layers)
                layer.DecompressTiles();
        }
    } // EndlessTilesWorldChunk

    public class EndlessTilesWorld
    {
        public const int BLANK_TILE = -1;

        public string Name { get; set; }
        public Vector2I TileSize { get; set; }
        public Vector2I ChunkSize { get; set; }
        public Dictionary<Vector2I, EndlessTilesWorldChunk> Chunks { get; set; } = new Dictionary<Vector2I, EndlessTilesWorldChunk>();

        public EndlessTilesWorld(string path)
        {
            LoadFromPath(path);
        }

        public EndlessTilesWorld(FileStream fs)
        {
            LoadFromStream(fs);
        }

        public void LoadFromPath(string path)
        {
            using var fs = File.OpenRead(path);
            LoadFromStream(fs);
        }

        public void LoadFromStream(FileStream fs)
        {
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(fs);
            using var jsonTextReader = new JsonTextReader(sr);
            var Data = serializer.Deserialize<EndlessTilesWorldData>(jsonTextReader);

            Name = Data.Name;
            Chunks = new Dictionary<Vector2I, EndlessTilesWorldChunk>();
            ChunkSize = Data.ChunkSize;
            TileSize = Data.TileSize;

            foreach (var (_, chunkData) in Data.SavedChunks)
            {
                var newChunk = new EndlessTilesWorldChunk(chunkData.Position, this);

                foreach (var layer in chunkData.Layers)
                    newChunk.AddLayer(layer.Index, layer.CompressedTiles);

                Chunks.Add(newChunk.Position, newChunk);
            }
        }

        public void ClearTiles()
        {
            foreach (var (_, chunk) in Chunks)
                chunk.ClearTiles();
        }

        public void ResetTiles()
        {
            foreach (var (_, chunk) in Chunks)
                chunk.ResetTiles();
        }

        public void DecompressTiles()
        {
            foreach (var (_, chunk) in Chunks)
                chunk.DecompressTiles();
        }

    } // EndlessTilesWorld
}
