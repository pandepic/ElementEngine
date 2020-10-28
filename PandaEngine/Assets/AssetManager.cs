using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Veldrid.ImageSharp;

namespace PandaEngine
{
    public class Asset
    {
        public string Name;
        public string FilePath;
    }

    public class AssetManager
    {
        protected Dictionary<string, Asset> _assetData = new Dictionary<string, Asset>();
        protected Dictionary<string, object> _assetCache = new Dictionary<string, object>();
        protected List<IDisposable> _disposableAssets = new List<IDisposable>();

        ~AssetManager()
        {
            Clear();
        }

        public void Load(string modsPath)
        {
            using (var fs = File.OpenRead(modsPath + "Mods.xml"))
            {
                var modsDoc = XDocument.Load(fs);

                foreach (var mod in modsDoc.Root.Elements("Mod"))
                {
                    var modName = mod.Attribute("Name").Value;
                    var modPath = mod.Attribute("Path").Value;

                    using (var modFS = File.OpenRead(modsPath + modPath + "Assets.xml"))
                    {
                        var assetsDoc = XDocument.Load(modFS);

                        foreach (var asset in assetsDoc.Root.Elements("Asset"))
                        {
                            var assetName = asset.Attribute("Name").Value;
                            var assetPath = asset.Attribute("FilePath").Value;

                            if (!_assetData.ContainsKey(assetName))
                                _assetData.Add(assetName, new Asset() { Name = assetName, FilePath = modsPath + modPath + assetPath });
                            else
                                _assetData[assetName].FilePath = modsPath + modPath + assetPath;
                        } // foreach asset
                    }
                } // foreach mod
            }
        } // Load

        public void Clear()
        {
            for (var i = 0; i < _disposableAssets.Count; i++)
                _disposableAssets[i].Dispose();

            _disposableAssets.Clear();
            _assetCache.Clear();
        }

        public string GetAssetPath(string assetName)
        {
            return _assetData[assetName].FilePath;
        }

        public FileStream GetAssetStream(string assetName, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            return new FileStream(GetAssetPath(assetName), mode, access);
        }

        public Texture2D LoadTexture2D(string assetName, bool mipmap = false)
        {
            if (_assetCache.ContainsKey(assetName))
                return (Texture2D)_assetCache[assetName];

            using (var fs = GetAssetStream(assetName))
            {
                var textureData = new ImageSharpTexture(fs, mipmap);
                var deviceTexture = textureData.CreateDeviceTexture(PandaGlobals.GraphicsDevice, PandaGlobals.GraphicsDevice.ResourceFactory);
                var newTexture = new Texture2D(deviceTexture);
                newTexture.AssetName = assetName;

                _assetCache.Add(assetName, newTexture);
                _disposableAssets.Add(newTexture);
                
                return newTexture;
            }
        } // LoadTexture2D
    }
}
