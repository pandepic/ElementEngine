using System;
using System.Collections.Generic;
using System.IO;

namespace ElementEngine
{
    public class AssetPackFile
    {
        public string Name;
        public byte[] Bytes;
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
            using var innerReader = new BinaryReader(ms);

            var fileCount = innerReader.ReadInt32();

            for (var i = 0; i < fileCount; i++)
            {
                var fileName = innerReader.ReadString();
                var fileLength = innerReader.ReadInt32();
                var fileBytes = innerReader.ReadBytes(fileLength);
                
                var packFile = new AssetPackFile()
                {
                    Name = fileName,
                    Bytes = fileBytes,
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
            }

            var bytes = ms.ToArray();

            if (IsCompressed)
                bytes = Compression.Zip(bytes);

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public void AddFile(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            AddFile(fs);
        }

        public void AddFile(FileStream fs)
        {
            var fileName = Path.GetFileName(fs.Name);
            var bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);

            var packFile = new AssetPackFile()
            {
                Name = fileName,
                Bytes = bytes,
            };

            Files.Add(packFile);
        }

        public void ImportToAssetManager<T>(AssetManager assetManager)
        {
            foreach (var file in Files)
            {
                if (typeof(T) == typeof(Texture2D))
                    assetManager.LoadTexture2DFromAssetPack(Name, file);
                else
                    throw new ArgumentException($"Unsupported generic type: {typeof(T).FullName}", "T");
            }
        }
    }
}
