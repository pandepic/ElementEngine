using FontStashSharp;
using NAudio.Vorbis;
using NAudio.Wave;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;
using Veldrid.ImageSharp;

namespace ElementEngine
{
    public enum TexturePremultiplyType
    {
        None,
        Premultiply,
        UnPremultiply
    }

    public class Asset
    {
        public string Name;
        public string FilePath;
    }

    public static class AssetManager
    {
        private static readonly Dictionary<string, Asset> _assetData = new Dictionary<string, Asset>();
        private static readonly Dictionary<string, object> _assetCache = new Dictionary<string, object>();

        public static void Load(string modsPath)
        {
            var stopWatch = Stopwatch.StartNew();

            using (var fs = File.OpenRead(Path.Combine(modsPath, "Mods.xml")))
            {
                var modsDoc = XDocument.Load(fs);

                foreach (var mod in modsDoc.Root.Elements("Mod"))
                {
                    var modName = mod.Attribute("Name").Value;
                    var modPath = mod.Attribute("Path").Value;

                    using var modFS = File.OpenRead(Path.Combine(modsPath, modPath, "Assets.xml"));
                    var assetsDoc = XDocument.Load(modFS);

                    var autoFind = false;
                    var autoFindAtt = assetsDoc.Root.Attribute("AutoFind");
                    if (autoFindAtt != null)
                        autoFind = bool.Parse(autoFindAtt.Value);

                    var autoAppendFolderFind = false;
                    var autoAppendFolderFindAtt = assetsDoc.Root.Attribute("AutoAppendFolder");
                    if (autoAppendFolderFindAtt != null)
                        autoAppendFolderFind = bool.Parse(autoAppendFolderFindAtt.Value);

                    foreach (var asset in assetsDoc.Root.Elements("Asset"))
                    {
                        var assetName = asset.Attribute("Name").Value;
                        var assetPath = asset.Attribute("FilePath").Value;

                        if (!_assetData.ContainsKey(assetName))
                            _assetData.Add(assetName, new Asset() { Name = assetName, FilePath = Path.Combine(modsPath, modPath, assetPath) });
                        else
                            _assetData[assetName].FilePath = modsPath + modPath + assetPath;
                    } // foreach asset

                    if (autoFind)
                    {
                        var dirPath = Path.Combine(modsPath, modPath);
                        var directoryPaths = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories);

                        var directoryList = new List<DirectoryInfo>
                        {
                            new DirectoryInfo(Path.Combine(modsPath, modPath))
                        };

                        directoryList.AddRange(directoryPaths.Select(d => new DirectoryInfo(d)).ToList());
                        var baseDirProcessed = false;

                        foreach (var dir in directoryList)
                        {
                            foreach (var file in dir.GetFiles())
                            {
                                var assetName = (autoAppendFolderFind && baseDirProcessed ? dir.Name + "/" : "") + file.Name;

                                if (!_assetData.ContainsKey(assetName))
                                {
                                    _assetData.Add(assetName, new Asset() { Name = assetName, FilePath = Path.Combine(modsPath, modPath, Path.GetRelativePath(dirPath, file.FullName)) });
                                    Logging.Information("[{component}] loaded asset {name} from {mod}.", "AssetManager", file.Name, modName);
                                }
                            }

                            baseDirProcessed = true;
                        }
                    } // if autoFind
                } // foreach mod
            }

            stopWatch.Stop();
            Logging.Information("[{component}] {count} mod assets loaded from {path} in {time:0.00} ms.", "AssetManager", _assetData.Count, modsPath, stopWatch.Elapsed.TotalMilliseconds);
        } // Load

        public static void Clear()
        {
            foreach (var kvp in _assetCache)
            {
                if (kvp.Value is IDisposable disposable)
                    disposable?.Dispose();
            }

            _assetCache.Clear();
        }

        public static string GetAssetPath(string assetName)
        {
            return _assetData[assetName].FilePath;
        }

        public static FileStream GetAssetStream(string assetName, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return new FileStream(GetAssetPath(assetName), mode, access);
        }

        public static FileStream GetFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return new FileStream(path, mode, access);
        }

        private static void LogLoaded(string type, string assetName, Stopwatch stopWatch)
        {
            stopWatch.Stop();
            Logging.Information("[{component}] {type} loaded from asset {name} in {time:0.00} ms.", "AssetManager", type, assetName, stopWatch.Elapsed.TotalMilliseconds);
        }

        public static Texture2D LoadTexture2D(string assetName, TexturePremultiplyType premultiply = TexturePremultiplyType.None, bool mipmap = false)
        {
            if (_assetCache.ContainsKey(assetName))
                return (Texture2D)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);

            var textureData = Image.Load<Rgba32>(fs);
            var newTexture = new Texture2D((uint)textureData.Width, (uint)textureData.Height, assetName);
            newTexture.SetData<Rgba32>(textureData.GetPixelMemoryGroup()[0].Span, new Rectangle(0, 0, textureData.Width, textureData.Height));

            _assetCache.Add(assetName, newTexture);

            LogLoaded("Texture2D", assetName, stopWatch);
            return newTexture;

        } // LoadTexture2D

        public static SpriteFont LoadSpriteFont(string assetName)
        {
            if (_assetCache.ContainsKey(assetName))
                return (SpriteFont)_assetCache[assetName];

            var stopWatch = Stopwatch.StartNew();

            using var fs = GetAssetStream(assetName);
            var newFont = new SpriteFont(fs);

            _assetCache.Add(assetName, newFont);
            LogLoaded("SpriteFont", assetName, stopWatch);

            return newFont;

        } // LoadSpriteFont

        /// <summary>
        /// Try to auto detect the audio format and load from the correct source type
        /// </summary>
        public static AudioSource LoadAudioSourceByExtension(string assetName)
        {
            var path = GetAssetPath(assetName);
            var extension = Path.GetExtension(path);

            switch (extension)
            {
                case ".wav":
                    return LoadAudioSourceWAV(assetName);

                case ".ogg":
                    return LoadAudioSourceOggVorbis(assetName);

                default:
                    throw new Exception("Couldn't load audio source from unknown or unsupported audio format " + assetName);
            }
        } // LoadAudioSourceByExtension

        public static AudioSource LoadAudioSourceWAV(string assetName)
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

        public static AudioSource LoadAudioSourceOggVorbis(string assetName)
        {
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

    } // AssetManager
}
