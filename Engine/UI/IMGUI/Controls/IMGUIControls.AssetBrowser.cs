using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace ElementEngine.UI
{
    public static partial class IMGUIControls
    {
        public static readonly List<string> ExtensionFilterTexturePacker = new() { ".json" };

        public static string AssetBrowser(string name, string nameFilter = null, List<string> assetExtensions = null)
        {
            string selectedAsset = null;
            var open = true;

            var assets = new List<string>();

            if (assetExtensions != null)
            {
                foreach (var extension in assetExtensions)
                {
                    var extensionAssets = AssetManager.Instance.GetAssetsByExtension(extension);

                    foreach (var asset in extensionAssets)
                    {
                        if (!string.IsNullOrEmpty(nameFilter) && !asset.Contains(nameFilter))
                            continue;

                        assets.AddIfNotContains(asset);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(nameFilter))
            {
                var nameAssets = AssetManager.Instance.GetAllAssetsByName(nameFilter);

                foreach (var asset in nameAssets)
                    assets.AddIfNotContains(asset.Name);
            }

            if (assets.Count == 0)
            {
                ImGui.CloseCurrentPopup();
                return "";
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(1, 1), new Vector2(800, 800));
            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.HorizontalScrollbar))
            {
                if (ImGui.Button("Close##Close"))
                {
                    ImGui.CloseCurrentPopup();
                    return "";
                }

                ImGui.NewLine();

                foreach (var asset in assets)
                {
                    if (ImGui.Button($"{asset}##Asset"))
                    {
                        ImGui.CloseCurrentPopup();
                        return asset;
                    }
                }

                ImGui.EndPopup();
            }

            return selectedAsset;
        }
    }
}
