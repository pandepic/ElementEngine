using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine.Tiled
{
    public class TiledSpriteBatchRenderer
    {
        public TiledMap Map { get; set; }
        public Texture2D Tilesheet { get; set; }
        public List<TiledMapLayer> BelowLayers { get; set; }
        public List<TiledMapLayer> AboveLayers { get; set; }

        public TiledSpriteBatchRenderer(TiledMap map)
        {
            Map = map;

            BelowLayers = Map.GetLayersByCustomProperty("Below", "true");
            AboveLayers = Map.GetLayersByCustomProperty("Below", "false");

            Tilesheet = AssetManager.LoadTexture2D(Map.GetCustomProperty("Tilesheet").Value);
        }

        public void Draw(SpriteBatch2D spriteBatch, bool below, Camera2D camera = null, Vector2? offset = null)
        {
            var drawLayers = below ? BelowLayers : AboveLayers;
            var cameraView = camera.View - (offset ?? Vector2.Zero);

            var cameraWidth = cameraView.Width / camera.Zoom;
            var cameraHeight = cameraView.Height / camera.Zoom;

            var tileWidth = Map.TileSize.X;
            var tileHeight = Map.TileSize.Y;

            int loopX = cameraView.X / tileWidth;
            int loopY = cameraView.Y / tileHeight;
            int loopWidth = (int)((cameraView.X + cameraWidth) / tileWidth);
            int loopHeight = (int)((cameraView.Y + cameraHeight) / tileHeight);

            int padding = 2;

            loopX -= padding;
            loopY -= padding;
            loopWidth += padding * 2;
            loopHeight += padding * 2;

            var tilesheetWidth = Tilesheet.Width;

            for (var l = 0; l < drawLayers.Count; l++)
            {
                var layer = drawLayers[l];
                var tilePos = Vector2.Zero;

                for (int y = loopY; y < loopHeight; y++)
                {
                    if (y >= Map.MapSize.Y || y < 0)
                        continue;

                    for (int x = loopX; x < loopWidth; x++)
                    {
                        if (x >= Map.MapSize.X || x < 0)
                            continue;

                        var tileID = layer.Tiles[x + Map.MapSize.X * y];

                        if (tileID == TiledMap.NO_TILE)
                            continue;

                        tilePos.X = x * tileWidth;
                        tilePos.Y = y * tileHeight;

                        if (offset.HasValue)
                            tilePos += offset.Value;

                        var sheetX = ((tileID - 1) % (tilesheetWidth / tileWidth)) * tileWidth;
                        var sheetY = ((tileID - 1) / (tilesheetWidth / tileWidth)) * tileHeight;

                        spriteBatch.DrawTexture2D(Tilesheet, tilePos, new Rectangle(sheetX, sheetY, tileWidth, tileHeight));

                    } // x
                } // y
            } // for drawLayers
        } // Draw

    } // TiledSpriteBatchRenderer
}
