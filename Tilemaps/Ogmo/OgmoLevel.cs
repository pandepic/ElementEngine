using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElementEngine.Ogmo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External JSON format")]
    public class OgmoLayer
    {
        public string name { get; set; }
        public string _eid { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public int gridCellWidth { get; set; }
        public int gridCellHeight { get; set; }
        public int gridCellsX { get; set; }
        public int gridCellsY { get; set; }
        public string tileset { get; set; }
        public int[] data { get; set; }
        public int exportMode { get; set; }
        public int arrayMode { get; set; }
        public string[] grid { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External JSON format")]
    public class OgmoLevelData
    {
        public string ogmoVersion { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public Dictionary<string, string> values { get; set; }
        public List<OgmoLayer> layers { get; set; }
    }

    public class OgmoLevel
    {
        public OgmoLevelData Data { get; set; }

        public Vector2I LevelSize { get; protected set; }
        public Vector2I TileSize { get; protected set; }
        public Vector2I LevelPixelSize { get; protected set; }

        public OgmoLevel(FileStream fs)
        {
            using var streamReader = new StreamReader(fs);
            using var jsonTextReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();
            Data = serializer.Deserialize<OgmoLevelData>(jsonTextReader);

            var firstLayer = Data.layers[0];
            LevelSize = new Vector2I(firstLayer.gridCellsX, firstLayer.gridCellsY);
            TileSize = new Vector2I(firstLayer.gridCellWidth, firstLayer.gridCellHeight);
            LevelPixelSize = LevelSize * TileSize;
        }
    }
}
