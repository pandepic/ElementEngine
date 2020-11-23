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

        public OgmoLevelRenderer(OgmoLevel level, Texture2D tilesTexture = null)
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

                _tileBatch.EndLayer();
            }

            _tileBatch.EndBuild();
        }

        public void Update(GameTimer gameTimer)
        {
            _tileBatch.Update(gameTimer);
        }

        public void DrawAllLayers(Camera2D camera)
        {
            _tileBatch.DrawAll(camera.Position, camera.Zoom);
        }

        public void DrawLayersFrom(int from, Camera2D camera)
        {
            _tileBatch.DrawLayersFrom(from, camera.Position, camera.Zoom);
        }

        public void DrawLayersTo(int to, Camera2D camera)
        {
            _tileBatch.DrawLayersTo(to, camera.Position, camera.Zoom);
        }

        public void DrawLayers(int start, int end, Camera2D camera)
        {
            _tileBatch.DrawLayers(start, end, camera.Position, camera.Zoom);
        }

    } // OgmoLevelRenderer
}
