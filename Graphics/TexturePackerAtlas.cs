using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.TexturePacker
{
    public struct TexturePackerAtlasSpriteSourceSize
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }

        public Rectangle Rect => new Rectangle(x, y, w, h);
    }

    public struct TexturePackerAtlasSourceSize
    {
        public int w { get; set; }
        public int h { get; set; }
    }

    public struct TexturePackerAtlasSprite
    {
        public string filename;
        public TexturePackerAtlasSpriteSourceSize frame;
        public bool rotated;
        public bool trimmed;
        public TexturePackerAtlasSpriteSourceSize spriteSourceSize;
        public TexturePackerAtlasSourceSize sourceSize;
    }

    public struct TexturePackerAtlasSize
    {
        public int w;
        public int h;
    }

    public struct TexturePackerAtlasMeta
    {
        public string app;
        public string version;
        public string image;
        public string format;
        public TexturePackerAtlasSize size;
        public string scale;
        public string smartupdate;
    }

    public class TexturePackerAtlasData
    {
        public List<TexturePackerAtlasSprite> frames { get; set; }
        public TexturePackerAtlasMeta meta { get; set; }
    }

    public class TexturePackerAtlas
    {
        public Texture2D Texture;
        public TexturePackerAtlasData Data;

        public Dictionary<string, TexturePackerAtlasSprite> Sprites = new Dictionary<string, TexturePackerAtlasSprite>();

        public TexturePackerAtlas(FileStream fs, Texture2D texture)
        {
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(fs);
            using var jsonTextReader = new JsonTextReader(sr);

            Data = serializer.Deserialize<TexturePackerAtlasData>(jsonTextReader);
            Texture = texture;

            foreach (var frame in Data.frames)
                Sprites.TryAdd(frame.filename, frame);
        }

        public Rectangle GetSpriteRect(string sprite)
        {
            if (Sprites.TryGetValue(sprite, out var spriteData))
            {
                return spriteData.frame.Rect;
            }
            else
            {
                throw new ArgumentException("Sprite doesn't exist within the atlas.", "sprite");
            }
        }
    }
}
