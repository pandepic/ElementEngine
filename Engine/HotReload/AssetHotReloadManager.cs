using System;
using System.Collections.Generic;
using System.IO;

namespace ElementEngine
{
    public class AssetHotReloadManager : IDisposable
    {
        public static AssetHotReloadManager Instance = new(AssetManager.Instance);

        public AssetManager AssetManager;
        public FileSystemWatcher AssetWatcher;

        public Dictionary<Asset, List<Action>> AssetChangedEvents = new();

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            AssetWatcher?.Dispose();
            AssetWatcher = null;

            AssetChangedEvents.Clear();

            _isDisposed = true;
        }

        public AssetHotReloadManager(AssetManager assetManager)
        {
            AssetManager = assetManager;
        }

        public void Enable(string filter = null)
        {
            if (AssetWatcher != null)
                return;

            AssetWatcher = new(AssetManager.ModsPath, filter ?? "*");
            AssetWatcher.NotifyFilter = NotifyFilters.LastWrite;
            AssetWatcher.IncludeSubdirectories = true;
            AssetWatcher.EnableRaisingEvents = true;

            AssetWatcher.Changed += OnChanged;
        }

        public void WatchAsset(string assetName, Action onChanged)
        {
            if (AssetWatcher == null)
                throw new Exception("Call enable on this hot reload manager before watching assets.");
            if (!AssetManager.Contains(assetName))
                throw new ArgumentException($"AssetManager doesn't contain an asset with the name {assetName}.", nameof(assetName));

            var asset = AssetManager.GetAsset(assetName);

            if (!AssetChangedEvents.TryGetValue(asset, out var events))
            {
                events = new();
                AssetChangedEvents.Add(asset, events);
            }

            events.Add(onChanged);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            foreach (var (asset, events) in AssetChangedEvents)
            {
                if (asset.FilePath != e.FullPath)
                    continue;

                try
                {
                    using var fs = File.OpenRead(asset.FilePath);

                    foreach (var ev in events)
                        ev?.Invoke();
                }
                catch (Exception)
                {
                }
            }
        }

        public void Disable()
        {
            AssetWatcher?.Dispose();
            AssetWatcher = null;

            AssetChangedEvents.Clear();
        }
    }
}
