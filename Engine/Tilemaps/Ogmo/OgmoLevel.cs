using ElementEngine.Util;
using System.Collections.Generic;
using System.IO;

namespace ElementEngine.Ogmo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External JSON format")]
    public class OgmoEntity
    {
        public string name { get; set; }
        public int id { get; set; }
        public string _eid { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int originX { get; set; }
        public int originY { get; set; }
    }

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
        public OgmoEntity[] entities { get; set; }
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
            Data = JSONUtil.LoadJSON<OgmoLevelData>(fs);

            var firstLayer = Data.layers[0];
            LevelSize = new Vector2I(firstLayer.gridCellsX, firstLayer.gridCellsY);
            TileSize = new Vector2I(firstLayer.gridCellWidth, firstLayer.gridCellHeight);
            LevelPixelSize = LevelSize * TileSize;
        }
    }
}
