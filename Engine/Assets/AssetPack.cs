using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ElementEngine
{
    public class AssetPackFile
    {
        public string Name;
        public byte[] Bytes;
        public string CustomData;
    }

    public class AssetPack
    {
        public string Name;
        public bool IsCompressed { get; protected set; }

        public List<AssetPackFile> Files = new();

        public AssetPack() { }

        public AssetPack(string name, bool isCompressed)
        {
            Name = name;
            IsCompressed = isCompressed;
        }

        public AssetPack(string filePath)
        {
            Load(filePath);
        }

        public AssetPack(FileStream fs)
        {
            Load(fs);
        }

        public void Load(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            Load(fs);
        }

        public void Load(FileStream fs)
        {
            Files.Clear();

            using var reader = new BinaryReader(fs);

            IsCompressed = reader.ReadBoolean();
            Name = reader.ReadString();

            var bytesLength = reader.ReadInt32();
            var bytes = reader.ReadBytes(bytesLength);

            if (IsCompressed)
                bytes = Compression.Unzip(bytes);

            using var ms = new MemoryStream(bytes);
            using var msReader = new BinaryReader(ms);

            var fileCount = msReader.ReadInt32();

            for (var i = 0; i < fileCount; i++)
            {
                var fileName = msReader.ReadString();
                var fileLength = msReader.ReadInt32();
                var fileBytes = msReader.ReadBytes(fileLength);
                var fileCustomData = msReader.ReadString();

                var packFile = new AssetPackFile()
                {
                    Name = fileName,
                    Bytes = fileBytes,
                    CustomData = fileCustomData,
                };

                Files.Add(packFile);
            }
        }

        public void Save(string filePath)
        {
            using var fs = File.Open(filePath, FileMode.Create);
            Save(fs);
        }

        public void Save(FileStream fs)
        {
            using var writer = new BinaryWriter(fs);
            writer.Write(IsCompressed);
            writer.Write(Name);

            using var ms = new MemoryStream();
            using var msWriter = new BinaryWriter(ms);

            msWriter.Write(Files.Count);

            foreach (var file in Files)
            {
                msWriter.Write(file.Name);
                msWriter.Write(file.Bytes.Length);
                msWriter.Write(file.Bytes);
                msWriter.Write(file.CustomData ?? "");
            }

            var bytes = ms.ToArray();

            if (IsCompressed)
                bytes = Compression.Zip(bytes);

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public void AddFile(string fileName, byte[] bytes, string customData = null)
        {
            Files.Add(new()
            {
                Name = fileName,
                Bytes = bytes,
                CustomData = customData,
            });
        }

        public void AddFile(string filePath, string customData = null)
        {
            using var fs = File.OpenRead(filePath);
            AddFile(fs, customData);
        }

        public void AddFile(FileStream fs, string customData = null)
        {
            var fileName = Path.GetFileName(fs.Name);
            var bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);

            var packFile = new AssetPackFile()
            {
                Name = fileName,
                Bytes = bytes,
                CustomData = customData,
            };

            Files.Add(packFile);
        }

        /// <summary>
        /// Tries to auto detect file types from their extensions and import them as the correct type.
        /// </summary>
        public void ImportToAssetManagerAuto(AssetManager assetManager, bool log = true)
        {
            foreach (var file in Files)
            {
                var lastDot = file.Name.LastIndexOf('.');
                if (lastDot < 0)
                    continue;

                var extension = file.Name.Substring(lastDot).ToLower();
                var assetName = $"{file.Name}/{Name}";

                using var ms = new MemoryStream(file.Bytes);

                switch (extension)
                {
                    case ".jpeg":
                    case ".jpg":
                    case ".png":
                    case ".bmp":
                        assetManager.LoadTexture2DFromStream(ms, assetName, log: log) ;
                        break;

                    case ".ttf":
                        assetManager.LoadSpriteFontFromStream(ms, assetName, log: log);
                        break;

                    case ".tmx":
                        assetManager.LoadTiledMapFromStream(ms, assetName, log: log);
                        break;

                    case ".tsx":
                        assetManager.LoadTiledTilesetFromStream(ms, assetName, log: log);
                        break;

                    case ".wav":
                        assetManager.LoadAudioSourceWAVFromStream(ms, assetName, log: log);
                        break;

                    case ".ogg":
                        assetManager.LoadAudioSourceOggVorbisStream(ms, assetName, log: log);
                        break;
                }
            }
        }

        /// <summary>
        /// Assumes all files in the pack have the same type T.
        /// </summary>
        public void ImportToAssetManager<T>(AssetManager assetManager, bool log = true)
        {
            foreach (var file in Files)
            {
                var assetName = $"{file.Name}/{Name}";
                using var ms = new MemoryStream(file.Bytes);

                if (typeof(T) == typeof(Texture2D))
                    assetManager.LoadTexture2DFromStream(ms, assetName, log: log);
                else
                    throw new ArgumentException($"Unsupported generic type: {typeof(T).FullName}", "T");
            }
        }
    }
}
