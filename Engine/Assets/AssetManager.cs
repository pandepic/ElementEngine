using ElementEngine.EndlessTiles;
using ElementEngine.Ogmo;
using ElementEngine.TexturePacker;
using ElementEngine.Tiled;
using ElementEngine.Util;
using NAudio.Vorbis;
using NAudio.Wave;
using SharpGen.Runtime.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace ElementEngine;

public enum TexturePremultiplyType
{
    None,
    Premultiply,
    UnPremultiply
}

public enum LoadAssetsMode
{
    None = 0,
    AutoPrependDir = 1,
    AutoFind = 2,
}

public class Asset
{
    public string Name;
    public string FilePath;
    public DirectoryInfo Directory;
}

public class AssetManager
{
    public static AssetManager Instance = new();

    private readonly Dictionary<string, Asset> _assetData = new Dictionary<string, Asset>();
    private readonly Dictionary<string, object> _assetCache = new Dictionary<string, object>();
    private readonly List<string> _removeList = new List<string>();

    public string ModsPath { get; private set; }

    protected bool _useCache;
    public bool UseCache { get => _useCache; }

    public void Load(string modsPath, LoadAssetsMode? mode = null, bool useCache = true)
    {
        ModsPath = modsPath;
        _useCache = useCache;

        var stopWatch = Stopwatch.StartNew();

        var modsFilePath = Path.Combine(modsPath, "Mods.xml");

        if (!File.Exists(modsFilePath))
        {
            LoadAssetsFile(modsPath, mode);
        }
        else
        {
            using var fs = File.OpenRead(Path.Combine(modsPath, "Mods.xml"));
            var modsDoc = XDocument.Load(fs);

            foreach (var mod in modsDoc.Root.Elements("Mod"))
            {
                var modPath = mod.Attribute("Path").Value;
                LoadAssetsFile(Path.Combine(modsPath, modPath), mode);
            }
        }

        stopWatch.Stop();
        Logging.Information("[{component}] {count} mod assets loaded from {path} in {time:0.00} ms.", "AssetManager", _assetData.Count, modsPath, stopWatch.Elapsed.TotalMilliseconds);
    } // Load

    private void LoadAssetsFile(string path = null, LoadAssetsMode? mode = null)
    {
        var assetsFilePath = Path.Combine(path, "Assets.xml");
        var autoFind = false;
        var autoPrependDir = false;

        if (File.Exists(assetsFilePath))
        {
            using var assetsFS = File.OpenRead(assetsFilePath);
            var assetsDoc = XDocument.Load(assetsFS);

            mode ??= LoadAssetsMode.None;

            if (mode.Value.HasFlag(LoadAssetsMode.AutoFind))
            {
                autoFind = true;
            }
            else
            {
                var autoFindAtt = assetsDoc.Root.Attribute("AutoFind");
                if (autoFindAtt != null)
                    autoFind = bool.Parse(autoFindAtt.Value);
            }

            if (mode.Value.HasFlag(LoadAssetsMode.AutoPrependDir))
            {
                autoPrependDir = true;
            }
            else
            {
                var autoPrependDirAtt = assetsDoc.Root.Attribute("AutoPrependDir");
                if (autoPrependDirAtt != null)
                    autoPrependDir = bool.Parse(autoPrependDirAtt.Value);
            }

            foreach (var asset in assetsDoc.Root.Elements("Asset"))
            {
                var assetName = asset.Attribute("Name").Value;
                var assetPath = asset.Attribute("FilePath").Value;

                if (!_assetData.ContainsKey(assetName))
                    AddAsset(assetName, Path.Combine(path, assetPath));
                else
                    _assetData[assetName].FilePath = Path.Combine(path, assetPath);
            } // foreach asset
        }
        else
        {
            mode ??= LoadAssetsMode.AutoFind;

            if (mode.Value.HasFlag(LoadAssetsMode.AutoFind))
                autoFind = true;
            if (mode.Value.HasFlag(LoadAssetsMode.AutoPrependDir))
                autoPrependDir = true;
        }

        if (autoFind)
        {
            var directoryPaths = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            var directoryList = new List<DirectoryInfo>
            {
                new DirectoryInfo(path)
            };

            directoryList.AddRange(directoryPaths.Select(d => new DirectoryInfo(d)).ToList());
            var baseDirProcessed = false;

            foreach (var dir in directoryList)
            {
                foreach (var file in dir.GetFiles())
                {
                    var assetName = (autoPrependDir && baseDirProcessed ? dir.Name + "/" : "") + file.Name;

                    if (!_assetData.ContainsKey(assetName))
                        AddAsset(assetName, Path.Combine(path, Path.GetRelativePath(path, file.FullName)), dir);
                }

                baseDirProcessed = true;
            }
        } // if autoFind
    } // LoadAssetsFile

    public void AddAsset(string name, string filePath, DirectoryInfo dir = null)
    {
        var fileInfo = new FileInfo(filePath);

        _assetData.Add(
            name,
            new Asset()
            {
                Name = name,
                FilePath = filePath,
                Directory = dir ?? fileInfo.Directory
            });
    }

    public List<string> GetAssetsByExtension(string extension, string pathFilter = null)
    {
        var assets = new List<string>();

        foreach (var (name, asset) in _assetData)
        {
            if ((pathFilter == null || asset.FilePath.Contains(pathFilter)) && asset.FilePath.ToUpper().EndsWith(extension.ToUpper()))
                assets.Add(name);
        }

        return assets;
    }

    public bool Contains(string assetName)
    {
        return _assetData.ContainsKey(assetName);
    }

    public bool IsLoaded(string assetName)
    {
        return _assetCache.ContainsKey(assetName);
    }

    public void Clear()
    {
        foreach (var kvp in _assetCache)
        {
            if (kvp.Value is IDisposable disposable)
                disposable?.Dispose();
        }

        _assetCache.Clear();
    }

    public void Unload(string assetName)
    {
        if (!_assetCache.TryGetValue(assetName, out var asset))
            return;

        _assetCache.Remove(assetName);

        if (asset is IDisposable disposable)
            disposable?.Dispose();
    }

    public void Unload<T>()
    {
        foreach (var kvp in _assetCache)
        {
            if (kvp.Value is T)
                _removeList.Add(kvp.Key);

            if (kvp.Value is IDisposable disposable)
                disposable?.Dispose();
        }

        _removeList.Clear();
    }

    public List<Asset> GetAllAssetsByPath(string pathContains, string pathDoesntContain = null)
    {
        var assets = new List<Asset>();

        foreach (var (name, asset) in _assetData)
        {
            if (!string.IsNullOrEmpty(pathDoesntContain) && asset.FilePath.Contains(pathDoesntContain))
                continue;

            if (asset.FilePath.Contains(pathContains))
                assets.Add(asset);
        }

        return assets;
    }

    public List<Asset> GetAllAssetsByName(string nameContains, string nameDoesntContain = null)
    {
        var assets = new List<Asset>();

        foreach (var (name, asset) in _assetData)
        {
            if (!string.IsNullOrEmpty(nameDoesntContain) && asset.Name.Contains(nameDoesntContain))
                continue;

            if (asset.Name.Contains(nameContains))
                assets.Add(asset);
        }

        return assets;
    }

    public Asset GetAsset(string assetName)
    {
        return _assetData[assetName];
    }

    public T GetAsset<T>(string assetName)
    {
        return (T)_assetCache[assetName];
    }

    public Asset GetAssetInfo(string assetName)
    {
        if (!_assetData.TryGetValue(assetName, out var asset))
            throw new Exception($"{assetName} was not found in this asset manager.");

        return asset;
    }

    public string GetAssetPath(string assetName)
    {
        return _assetData[assetName].FilePath;
    }

    public FileStream GetAssetStream(
        string assetName,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read)
    {
        return new FileStream(GetAssetPath(assetName), mode, access);
    }

    public FileStream GetFileStream(
        string path,
        FileMode mode = FileMode.Open,
        FileAccess access = FileAccess.Read)
    {
        return new FileStream(path, mode, access);
    }

    public void LogLoaded(string type, string assetName, Stopwatch stopWatch)
    {
        stopWatch.Stop();

        Logging.Information(
            "[{component}] {type} loaded from asset {name} in {time:0.00} ms.",
            "AssetManager",
            type,
            assetName,
            stopWatch.Elapsed.TotalMilliseconds);
    }

    public T GetFromCache<T>(string assetName)
    {
        if (TryGetFromCache<T>(assetName, out T t))
            return t;

        return default;
    }

    public bool TryGetFromCache<T>(string assetName, out T asset)
    {
        asset = default;

        if (!UseCache)
            return false;
        if (!_assetCache.TryGetValue(assetName, out var assetObj))
            return false;

        if (assetObj is T t)
        {
            asset = t;
            return true;
        }

        return false;
    }

    public bool TryAddToCache<T>(string assetName, T asset)
    {
        if (!UseCache)
            return false;
        if (_assetCache.ContainsKey(assetName))
            return false;

        _assetCache.Add(assetName, asset);
        return true;
    }

    public T LoadJSON<T>(
        string assetName,
        JsonSerializerOptions serializerOptions = null,
        List<JsonConverter> converters = null)
    {
        if (TryGetFromCache<T>(assetName, out var cachedAsset))
            return cachedAsset;

        var stopWatch = Stopwatch.StartNew();

        var path = GetAssetPath(assetName);
        var obj = JSONUtil.LoadJSON<T>(path, serializerOptions, converters);

        TryAddToCache(assetName, obj);
        LogLoaded("JSON (" + typeof(T).ToString() + ")", assetName, stopWatch);

        return obj;
    }

    #region Texture2D
    public Texture2D LoadTexture2D(string assetName, TexturePremultiplyType premultiply = TexturePremultiplyType.None)
    {
        if (TryGetFromCache<Texture2D>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(assetName);
        var newTexture = LoadTexture2DFromStream(fs, assetName, premultiply, false);

        TryAddToCache(assetName, newTexture);
        LogLoaded("Texture2D", assetName, stopWatch);

        return newTexture;
    }

    public Texture2D LoadTexture2DFromPath(
        string path,
        TexturePremultiplyType premultiply = TexturePremultiplyType.None,
        string name = null,
        bool log = true)
    {
        using var fs = File.OpenRead(path);
        return LoadTexture2DFromStream(fs, fs.Name, premultiply, log);
    }

    public Texture2D LoadTexture2DFromStream(
        Stream fs,
        string name = null,
        TexturePremultiplyType premultiply = TexturePremultiplyType.None,
        bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<Texture2D>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();

        using var textureData = Image.Load<Rgba32>(fs);
        var newTexture = new Texture2D(textureData.Width, textureData.Height, name);
        newTexture.SetData(textureData.GetPixelMemoryGroup()[0].Span, new Rectangle(0, 0, textureData.Width, textureData.Height), premultiply);

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("Texture2D", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newTexture);
        }

        return newTexture;
    }
    #endregion

    #region SpriteFont
    public SpriteFont LoadSpriteFont(string assetName)
    {
        if (TryGetFromCache<SpriteFont>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        using var fs = GetAssetStream(assetName);
        var newFont = LoadSpriteFontFromStream(fs, assetName, true);

        TryAddToCache(assetName, newFont);

        return newFont;
    }

    public SpriteFont LoadSpriteFontFromStream(Stream stream, string name = null, bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<SpriteFont>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();
        var newFont = new SpriteFont(stream);

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("SpriteFont", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newFont);
        }

        return newFont;
    }
    #endregion

    #region Tiled
    #region Tiled Map
    public TiledMap LoadTiledMap(string assetName)
    {
        if (TryGetFromCache<TiledMap>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        using var fs = GetAssetStream(assetName);
        var newMap = LoadTiledMapFromStream(fs, assetName, true);

        TryAddToCache(assetName, newMap);

        return newMap;
    }

    public TiledMap LoadTiledMapFromStream(Stream stream, string name = null, bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<TiledMap>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();
        var newMap = new TiledMap(stream);

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("TiledMap", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newMap);
        }

        return newMap;
    }
    #endregion

    #region Tiled Tileset
    public TiledTileset LoadTiledTileset(string assetName)
    {
        if (TryGetFromCache<TiledTileset>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        using var fs = GetAssetStream(assetName);
        var newSet = LoadTiledTilesetFromStream(fs, assetName, true);

        TryAddToCache(assetName, newSet);

        return newSet;
    }

    public TiledTileset LoadTiledTilesetFromStream(Stream stream, string name = null, bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<TiledTileset>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();
        var newSet = new TiledTileset(stream);

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("TiledTileset", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newSet);
        }

        return newSet;
    }
    #endregion
    #endregion

    #region Ogmo
    public OgmoLevel LoadOgmoLevel(string assetName)
    {
        if (TryGetFromCache<OgmoLevel>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(assetName);
        var newLevel = new OgmoLevel(fs);

        TryAddToCache(assetName, newLevel);
        LogLoaded("OgmoLevel", assetName, stopWatch);

        return newLevel;
    }
    #endregion

    #region Audio Source
    /// <summary>
    /// Try to auto detect the audio format and load from the correct source type
    /// </summary>
    public AudioSource LoadAudioSourceByExtension(string assetName)
    {
        if (TryGetFromCache<AudioSource>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var path = GetAssetPath(assetName);
        var extension = Path.GetExtension(path);

        return extension.ToUpper() switch
        {
            ".WAV" => LoadAudioSourceWAV(assetName),
            ".OGG" => LoadAudioSourceOggVorbis(assetName),
            _ => throw new Exception("Couldn't load audio source from unknown or unsupported audio format " + assetName),
        };
    }

    public AudioSource LoadAudioSourceWAV(string assetName)
    {
        if (TryGetFromCache<AudioSource>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(assetName);
        using var wav = new WaveFileReader(fs);

        var newSource = new AudioSource(wav)
        {
            AssetName = assetName
        };

        TryAddToCache(assetName, newSource);
        LogLoaded("AudioSource", assetName, stopWatch);

        return newSource;
    }

    public AudioSource LoadAudioSourceWAVFromStream(Stream stream, string name = null, bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<AudioSource>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();

        using var wav = new WaveFileReader(stream);

        var newSource = new AudioSource(wav)
        {
            AssetName = name ?? ""
        };

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("AudioSource", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newSource);
        }

        return newSource;
    }

    public AudioSource LoadAudioSourceOggVorbis(string assetName)
    {
        if (TryGetFromCache<AudioSource>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(assetName);
        using var vorbis = new VorbisWaveReader(fs);

        var newSource = new AudioSource(vorbis)
        {
            AssetName = assetName
        };

        TryAddToCache(assetName, newSource);
        LogLoaded("AudioSource", assetName, stopWatch);

        return newSource;
    }

    public AudioSource LoadAudioSourceOggVorbisStream(Stream stream, string name = null, bool log = true)
    {
        if (UseCache & !string.IsNullOrEmpty(name))
        {
            if (TryGetFromCache<AudioSource>(name, out var cachedAsset))
                return cachedAsset;
        }

        var stopWatch = Stopwatch.StartNew();

        using var vorbis = new VorbisWaveReader(stream);

        var newSource = new AudioSource(vorbis)
        {
            AssetName = name ?? ""
        };

        if (!string.IsNullOrEmpty(name))
        {
            if (log)
                LogLoaded("AudioSource", name, stopWatch);

            if (UseCache)
                TryAddToCache(name, newSource);
        }

        return newSource;
    }
    #endregion

    #region EndlessTiles
    public EndlessTilesWorld LoadEndlessTilesWorld(string assetName)
    {
        if (TryGetFromCache<EndlessTilesWorld>(assetName, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(assetName))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(assetName);
        var newWorld = new EndlessTilesWorld(fs);

        TryAddToCache(assetName, newWorld);
        LogLoaded("EndlessTilesWorld", assetName, stopWatch);

        return newWorld;
    }
    #endregion

    #region Texture Packer
    public TexturePackerAtlas LoadTexturePackerAtlas(string textureAsset, string dataAsset)
    {
        if (TryGetFromCache<TexturePackerAtlas>(dataAsset, out var cachedAsset))
            return cachedAsset;
        if (!_assetData.ContainsKey(dataAsset))
            return null;

        var stopWatch = Stopwatch.StartNew();

        using var fs = GetAssetStream(dataAsset);
        var newAtlas = new TexturePackerAtlas(fs, textureAsset, dataAsset, this);

        TryAddToCache(dataAsset, newAtlas);
        LogLoaded("TexturePackerAtlas", dataAsset, stopWatch);

        return newAtlas;
    }
    #endregion
}
