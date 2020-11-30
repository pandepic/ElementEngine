namespace ElementEngine.Tiled
{
    public class TiledMapRenderer
    {
        public TiledMap Map { get; set; }
        public TiledTileset TileSet { get; set; }

        protected TileBatch2D _tileBatch;
        public bool HasAnimatedTiles { get; protected set; }

        public TiledMapRenderer(TiledMap map, TiledTileset tileSet = null, Texture2D tilesTexture = null)
        {
            Map = map;
            TileSet = tileSet;

            if (tilesTexture == null)
                tilesTexture = AssetManager.LoadTexture2D(Map.GetCustomProperty("Tilesheet").Value);

            _tileBatch = new TileBatch2D(Map.MapSize.X, Map.MapSize.Y, Map.TileSize.X, Map.TileSize.Y, tilesTexture, TileSet?.TileAnimations);

            var belowLayers = Map.GetLayersByCustomProperty("Below", "true");
            var aboveLayers = Map.GetLayersByCustomProperty("Below", "false");

            if (TileSet != null && TileSet.TileAnimations.Count > 0)
                HasAnimatedTiles = true;

            _tileBatch.BeginBuild();

            foreach (var layer in map.Layers)
                BuildTilebatchLayer(layer);

            _tileBatch.EndBuild();
        } // TiledMapRenderer

        public void BuildTilebatchLayer(TiledMapLayer layer)
        {
            for (int y = 0; y < Map.MapSize.Y; y++)
            {
                for (int x = 0; x < Map.MapSize.X; x++)
                {
                    var tileID = layer.Tiles[x + Map.MapSize.X * y];

                    if (tileID == 0)
                        continue;

                    _tileBatch.SetTileAtPosition(x, y, tileID - 1);
                }
            }

            _tileBatch.EndLayer();
        } // BuildTilebatchLayer

        public void Update(GameTimer gameTimer)
        {
            _tileBatch.Update(gameTimer);
        }

        public void DrawAllLayers(Camera2D camera)
        {
            _tileBatch.DrawAll(camera);
        }

        public void DrawLayersFrom(int from, Camera2D camera)
        {
            _tileBatch.DrawLayersFrom(from, camera);
        }

        public void DrawLayersTo(int to, Camera2D camera)
        {
            _tileBatch.DrawLayersTo(to, camera);
        }

        public void DrawLayers(int start, int end, Camera2D camera)
        {
            _tileBatch.DrawLayers(start, end, camera);
        }
    }
}
