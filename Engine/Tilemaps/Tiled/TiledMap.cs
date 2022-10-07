using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace ElementEngine.Tiled
{
    public class TiledCustomProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Type) && string.IsNullOrEmpty(Value);

        public TiledCustomProperty(XAttribute name, XAttribute type, XAttribute value)
        {
            Name = name == null ? "" : name.Value;
            Type = type == null ? "" : type.Value;
            Value = value == null ? "" : value.Value;
        }
    }

    public class TiledMapLayer
    {
        public string Name { get; set; }
        public List<TiledCustomProperty> CustomProperties { get; set; } = new List<TiledCustomProperty>();
        public int[] Tiles;
    }

    public class TiledMap
    {
        public const int NO_TILE = 0;

        public List<TiledCustomProperty> CustomProperties { get; set; } = new List<TiledCustomProperty>();
        public List<TiledMapLayer> Layers { get; set; } = new List<TiledMapLayer>();

        public Vector2I TileSize { get; set; } = Vector2I.Zero; // in pixels
        public Vector2I MapSize { get; set; } = Vector2I.Zero; // in tiles
        public Vector2I MapPixelSize => MapSize * TileSize;

        public TiledMap(FileStream fs)
        {
            var doc = XDocument.Load(fs);

            TileSize = new Vector2I(int.Parse(doc.Root.Attribute("tilewidth").Value), int.Parse(doc.Root.Attribute("tileheight").Value));
            MapSize = new Vector2I(int.Parse(doc.Root.Attribute("width").Value), int.Parse(doc.Root.Attribute("height").Value));

            var elMapCustomProperties = doc.Root.Element("properties");

            if (elMapCustomProperties != null)
            {
                foreach (var mapProperty in elMapCustomProperties.Elements("property"))
                    CustomProperties.Add(new TiledCustomProperty(mapProperty.Attribute("name"), mapProperty.Attribute("type"), mapProperty.Attribute("value")));
            }

            foreach (var elLayer in doc.Root.Elements("layer"))
            {
                var newLayer = new TiledMapLayer()
                {
                    Name = elLayer.Attribute("name").Value,
                    Tiles = new int[MapSize.X * MapSize.Y],
                };

                var elLayerCustomProperties = elLayer.Element("properties");

                if (elLayerCustomProperties != null)
                {
                    foreach (var layerProperty in elLayerCustomProperties.Elements("property"))
                        newLayer.CustomProperties.Add(new TiledCustomProperty(layerProperty.Attribute("name"), layerProperty.Attribute("type"), layerProperty.Attribute("value")));
                }

                var tileRows = elLayer.Element("data").Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var tileIndex = 0;

                foreach (var row in tileRows)
                {
                    if (string.IsNullOrWhiteSpace(row))
                        continue;

                    var tiles = row.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var tile in tiles)
                    {
                        newLayer.Tiles[tileIndex] = int.Parse(tile);
                        tileIndex += 1;
                    }
                }

                Layers.Add(newLayer);
            } // foreach layer
        } // TiledMap

        public TiledCustomProperty GetCustomProperty(string name)
        {
            foreach (var property in CustomProperties)
            {
                if (property.Name == name)
                    return property;
            }

            return null;
        } // GetCustomProperty

        public List<TiledMapLayer> GetLayersByCustomProperty(string name, string value)
        {
            var layers = new List<TiledMapLayer>();

            foreach (var layer in Layers)
            {
                foreach (var property in layer.CustomProperties)
                {
                    if (property.Name == name && property.Value == value)
                        layers.Add(layer);
                }
            }

            return layers;
        } // GetLayersByCustomProperty

        public TiledMapLayer GetLayerByName(string name)
        {
            foreach (var layer in Layers)
            {
                if (layer.Name == name)
                    return layer;
            }

            return null;
        } // GetLayerByName

    } // TiledMap
}
