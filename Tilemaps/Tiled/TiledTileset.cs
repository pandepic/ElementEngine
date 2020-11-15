using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace ElementEngine.Tiled
{
    public class TiledTileset
    {
        public Dictionary<int, TileAnimation> TileAnimations { get; set; } = new Dictionary<int, TileAnimation>();

        public TiledTileset(FileStream fs)
        {
            var doc = XDocument.Load(fs);
            
            foreach (var tile in doc.Root.Elements("tile"))
            {
                var tileID = int.Parse(tile.Attribute("id").Value);
                var animation = tile.Element("animation");

                if (animation != null)
                {
                    var newAnimation = new TileAnimation()
                    {
                        TileID = tileID,
                    };

                    foreach (var animTile in animation.Elements("frame"))
                    {
                        newAnimation.Frames.Add(int.Parse(animTile.Attribute("tileid").Value));
                        newAnimation.DurationPerFrame = int.Parse(animTile.Attribute("duration").Value);
                    }

                    TileAnimations.Add(newAnimation.TileID, newAnimation);
                }
            }
        }
    } // TiledTileset
}
