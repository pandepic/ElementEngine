using ElementEngine.EndlessTiles;
using ElementEngine.Ogmo;
using ElementEngine.TexturePacker;
using ElementEngine.Tiled;
using NAudio.Vorbis;
using NAudio.Wave;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ElementEngine
{
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

        public void Load(string modsPath, LoadAssetsMode? mode = null)
        {
            ModsPath = modsPath;

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

        public List<Asset> GetAllAssets(string pathContains)
        {
            var assets = new List<Asset>();

            foreach (var (name, asset) in _assetData)
            {
                if (asset.FilePath.Contains(pathContains))
                    assets.Add(asset);
            }

            return assets;
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

        public FileStream GetAssetStream(string assetName, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return new FileStream(GetAssetPath(assetName), mode, access);
        }

        public FileStream GetFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return new FileStream(path, mode, access);
        }

        private void LogLoaded(string type, string assetName, Stopwatch stopWatch)
        {
            stopWatch.Stop();
            Logging.Information("[{component}] {type} loaded from asset {name} in {time:0.00} ms.", "AssetManager", type, assetName, stopWatch.Elapsed.TotalMilliseconds);
        }
        
        public T LoadJSON<T>(string assetName, JsonSerializer serializer = null, List<JsonConverter> converters = null)
        {
            if (_assetCache.ContainsKey(assetName))
                return (T)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var streamReader = new StreamReader(GetAssetStream(assetName));
            using var jsonTextReader = new JsonTextReader(streamReader);
            
            if (serializer == null)
                serializer = new JsonSerializer();

            if (converters != null)
            {
                foreach (var converter in converters)
                    serializer.Converters.Add(converter);
            }

            var obj = serializer.Deserialize<T>(jsonTextReader);

            _assetCache.Add(assetName, obj);

            LogLoaded("JSON (" + typeof(T).ToString() + ")", assetName, stopWatch);
            return obj;
        }

        public Texture2D LoadTexture2D(string assetName, TexturePremultiplyType premultiply = TexturePremultiplyType.None)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (Texture2D)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newTexture = LoadTexture2DFromStream(fs, premultiply, assetName, false);

            _assetCache.Add(assetName, newTexture);

            LogLoaded("Texture2D", assetName, stopWatch);
            return newTexture;

        } // LoadTexture2D

        public Texture2D LoadTexture2DFromPath(string path, TexturePremultiplyType premultiply = TexturePremultiplyType.None, string name = null, bool log = true)
        {
            using var fs = File.OpenRead(path);
            return LoadTexture2DFromStream(fs, premultiply, name, log);
        }

        public Texture2D LoadTexture2DFromStream(FileStream fs, TexturePremultiplyType premultiply = TexturePremultiplyType.None, string name = null, bool log = true)
        {
            var stopWatch = Stopwatch.StartNew();

            using var textureData = Image.Load<Rgba32>(fs);
            var newTexture = new Texture2D(textureData.Width, textureData.Height, name);
            newTexture.SetData<Rgba32>(textureData.GetPixelMemoryGroup()[0].Span, new Rectangle(0, 0, textureData.Width, textureData.Height), premultiply);

            if (log)
                LogLoaded("Texture2D", fs.Name, stopWatch);

            return newTexture;
        }

        public SpriteFont LoadSpriteFont(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (SpriteFont)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newFont = new SpriteFont(fs);

            _assetCache.Add(assetName, newFont);
            LogLoaded("SpriteFont", assetName, stopWatch);

            return newFont;

        } // LoadSpriteFont

        public TiledMap LoadTiledMap(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (TiledMap)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newMap = new TiledMap(fs);

            _assetCache.Add(assetName, newMap);
            LogLoaded("TiledMap", assetName, stopWatch);

            return newMap;

        } // LoadTiledMap

        public TiledTileset LoadTiledTileset(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (TiledTileset)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newSet = new TiledTileset(fs);

            _assetCache.Add(assetName, newSet);
            LogLoaded("TiledTileset", assetName, stopWatch);

            return newSet;

        } // LoadTiledTileset

        public OgmoLevel LoadOgmoLevel(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (OgmoLevel)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newLevel = new OgmoLevel(fs);

            _assetCache.Add(assetName, newLevel);
            LogLoaded("OgmoLevel", assetName, stopWatch);

            return newLevel;

        } // LoadOgmoLevel

        /// <summary>
        /// Try to auto detect the audio format and load from the correct source type
        /// </summary>
        public AudioSource LoadAudioSourceByExtension(string assetName)
        {
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
        } // LoadAudioSourceByExtension

        public AudioSource LoadAudioSourceWAV(string assetName)
        {
            if (_assetCache.ContainsKey(assetName))
                return (AudioSource)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            using var wav = new WaveFileReader(fs);

            var newSource = new AudioSource(wav)
            {
                AssetName = assetName
            };

            _assetCache.Add(assetName, newSource);
            LogLoaded("AudioSource", assetName, stopWatch);

            return newSource;

        } // LoadAudioSourceWAV

        public AudioSource LoadAudioSourceOggVorbis(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (AudioSource)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            using var vorbis = new VorbisWaveReader(fs);

            var newSource = new AudioSource(vorbis)
            {
                AssetName = assetName
            };

            _assetCache.Add(assetName, newSource);
            LogLoaded("AudioSource", assetName, stopWatch);

            return newSource;

        } // LoadAudioSourceOggVorbis

        public EndlessTilesWorld LoadEndlessTilesWorld(string assetName)
        {
            if (!_assetData.ContainsKey(assetName))
                return null;
            if (_assetCache.ContainsKey(assetName))
                return (EndlessTilesWorld)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newWorld = new EndlessTilesWorld(fs);

            _assetCache.Add(assetName, newWorld);
            LogLoaded("EndlessTilesWorld", assetName, stopWatch);

            return newWorld;
        }

        public TexturePackerAtlas LoadTexturePackerAtlas(string textureAsset, string dataAsset)
        {
            if (!_assetData.ContainsKey(dataAsset))
                return null;
            if (_assetCache.ContainsKey(dataAsset))
                return (TexturePackerAtlas)_assetCache[dataAsset];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(dataAsset);
            var newAtlas = new TexturePackerAtlas(fs, textureAsset, dataAsset);

            _assetCache.Add(dataAsset, newAtlas);
            LogLoaded("TexturePackerAtlas", dataAsset, stopWatch);

            return newAtlas;
        }

    } // AssetManager
}
