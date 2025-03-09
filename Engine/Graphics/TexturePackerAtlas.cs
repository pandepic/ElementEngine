using ElementEngine.ElementUI;
using ElementEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

        public Rectangle GetSpriteRect(string sprite)
        {
            foreach (var frame in frames)
            {
                if (frame.filename == sprite)
                    return frame.frame.Rect;
            }

            throw new Exception($"TexturePackerAtlasData doesn't contain frame {sprite}");
        }
    }

    public class TexturePackerAtlas
    {
        public Texture2D Texture;
        public string DataAsset;
        public string TextureAsset;
        public TexturePackerAtlasData Data;

        public Dictionary<string, TexturePackerAtlasSprite> Sprites = new Dictionary<string, TexturePackerAtlasSprite>();

        public TexturePackerAtlas(FileStream fs, string textureAsset, string dataAsset, AssetManager assetManager = null)
        {
            if (assetManager == null)
                assetManager = AssetManager.Instance;

            Data = JSONUtil.LoadJSON<TexturePackerAtlasData>(fs, new JsonSerializerOptions()
            {
                IncludeFields = true,
            });

            TextureAsset = textureAsset;
            DataAsset = dataAsset;

            Texture = assetManager.LoadTexture2D(TextureAsset);

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
                throw new ArgumentException($"Sprite {sprite} doesn't exist within the atlas.", "sprite");
            }
        }

        public UITexture GetUITexture(string sprite)
        {
            if (Sprites.TryGetValue(sprite, out var spriteData))
            {
                return new UITexture(Texture, spriteData.frame.Rect);
            }
            else
            {
                throw new ArgumentException($"Sprite {sprite} doesn't exist within the atlas.", "sprite");
            }
        }
    }
}
