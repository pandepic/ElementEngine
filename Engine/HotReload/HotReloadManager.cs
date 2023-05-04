using System;
using System.Collections.Generic;
using System.IO;

namespace ElementEngine
{
    public class FileHotReloadManager
    {
        public class HotReloadFile
        {
            public string Path;
            public FileSystemWatcher Watcher;
            public Action OnChanged;
        }

        public List<HotReloadFile> Files = new();

        public void WatchAsset(string assetName, Action onChanged, AssetManager assetManager = null)
        {
            if (assetManager == null)
                assetManager = AssetManager.Instance;

            WatchAsset(assetManager.GetAsset(assetName), onChanged);
        }

        public void WatchAsset(Asset asset, Action onChanged)
        {
            WatchFile(asset.FilePath, onChanged);
        }

        public void WatchFile(string path, Action onChanged)
        {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
            watcher.Filter = Path.GetFileName(path);

            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.EnableRaisingEvents = true;

            var hotReloadFile = new HotReloadFile()
            {
                Path = path,
                Watcher = watcher,
                OnChanged = onChanged,
            };

            watcher.Changed += (_, _) =>
            {
                try
                {
                    using var fs = File.OpenRead(hotReloadFile.Path);
                    hotReloadFile.OnChanged();
                }
                catch (Exception)
                {
                }
            };

            Files.Add(hotReloadFile);
        }
    }
}
