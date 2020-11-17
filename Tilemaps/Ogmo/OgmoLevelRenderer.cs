using System;
using System.Collections.Generic;

namespace ElementEngine.Ogmo
{
    public class OgmoLevelRenderer
    {
        public float Scale { get; set; } = 1f;
        public OgmoLevel Level { get; protected set; }

        protected TileBatch2D _tileBatch;
        public bool HasAnimatedTiles { get; protected set; }

        public OgmoLevelRenderer(OgmoLevel level, List<string> aboveLayers = null, Texture2D tilesTexture = null)
        {
            if (level.Data.layers.Count == 0)
                throw new ArgumentException("OgmoLevel must have at least one layer to be renderable.", "level");

            Level = level;

            if (tilesTexture == null)
                tilesTexture = AssetManager.LoadTexture2D(level.Data.values["Tilesheet"]);

            var firstLayer = level.Data.layers[0];
            _tileBatch = new TileBatch2D(firstLayer.gridCellsX, firstLayer.gridCellsY, firstLayer.gridCellWidth, firstLayer.gridCellHeight, tilesTexture);

            _tileBatch.BeginBuild();

            foreach (var layer in Level.Data.layers)
            {
                if (layer.data == null)
                    continue;

                var below = true;

                if (aboveLayers != null && aboveLayers.Contains(layer.name))
                    below = false;

                for (int y = 0; y < firstLayer.gridCellsY; y++)
                {
                    for (int x = 0; x < firstLayer.gridCellsX; x++)
                    {
                        var tileID = layer.data[x + firstLayer.gridCellsX * y];

                        if (tileID < 0)
                            continue;

                        _tileBatch.SetTileAtPosition(x, y, tileID);
                    }
                }

                _tileBatch.EndLayer(below);
            }

            _tileBatch.EndBuild(true);
        }

        public void Update(GameTimer gameTimer)
        {
            _tileBatch.Update(gameTimer);
        }

        public void Draw(Camera2D camera, bool below)
        {
            _tileBatch.Draw(camera, below, Scale);
        }

    } // OgmoLevelRenderer
}
