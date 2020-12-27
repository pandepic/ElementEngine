using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ElementEngine
{
    internal class AssetManagerHotReloadWatcher
    {
        public string AssetName { get; set; }
        public string ExeFilePath { get; set; }
        public string DevFilePath { get; set; }
        public FileSystemWatcher Watcher { get; set; }
        public Action<string, string, string> Action { get; set; }
    }

    public static class AssetManagerHotReload
    {
        internal static Dictionary<string, AssetManagerHotReloadWatcher> AssetWatchers { get; set; } = new Dictionary<string, AssetManagerHotReloadWatcher>();
        
        public static void AddHotReloadAsset(string assetName, Action<string, string, string> action)
        {
            if (AssetWatchers.ContainsKey(assetName))
                return;

            var path = AssetManager.GetAssetPath(assetName);

            var fileInfo = new FileInfo(path);
            var exeDir = new DirectoryInfo(fileInfo.DirectoryName);
            var exeFile = new FileInfo(Path.Combine(exeDir.FullName, fileInfo.Name));
            
            var devDirPath = Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, exeFile.Directory.FullName.Replace(AppDomain.CurrentDomain.BaseDirectory, ""));
            var devFilePath = Path.Combine(devDirPath, exeFile.Name);
            var devFile = new FileInfo(devFilePath);

            var watcher = new AssetManagerHotReloadWatcher()
            {
                AssetName = assetName,
                DevFilePath = devFilePath,
                ExeFilePath = exeFile.FullName,
                Watcher = new FileSystemWatcher(devFile.DirectoryName, devFile.Name),
                Action = action,
            };

            watcher.Watcher.NotifyFilter = NotifyFilters.LastWrite;

            watcher.Watcher.Changed += (object source, FileSystemEventArgs e) =>
            {
                Thread.Sleep(2000);
                watcher.Action(assetName, devFilePath, exeFile.FullName);
            };

            watcher.Watcher.EnableRaisingEvents = true;
            AssetWatchers.Add(assetName, watcher);
        }
    }
}
