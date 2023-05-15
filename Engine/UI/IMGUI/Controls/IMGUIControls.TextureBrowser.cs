using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace ElementEngine.UI
{
    public static partial class IMGUIControls
    {
        private static List<string> _textureExtensions = new List<string>() { "png", "jpg", "jpeg" };
        private static List<string> _tempStringList = new List<string>();

        private static Dictionary<string, IntPtr> _cachedTexturePtrs = new();
        private static Dictionary<string, List<string>> _selectedAssets = new();
        private static Dictionary<string, string> _nameFilters = new();

        private static Texture2D _selectedTextureBG = new Texture2D(1, 1, new Veldrid.RgbaByte(255, 0, 0, 80));
        private static IntPtr _selectedTextureBGPtr;
        private static bool _selectedTextureBGLoaded = false;

        private static void CheckSelectedTextureLoaded()
        {
            if (!_selectedTextureBGLoaded)
            {
                _selectedTextureBGPtr = IMGUIManager.AddTexture(_selectedTextureBG);
                _selectedTextureBGLoaded = true;
            }
        }

        private static List<string> GetAssetsByExtension(List<string> extensions, string pathFilter = null)
        {
            _tempStringList.Clear();

            foreach (var extension in extensions)
            {
                var assets = AssetManager.Instance.GetAssetsByExtension(extension, pathFilter);
                _tempStringList.AddIfNotContains(assets);
            }

            return _tempStringList;
        }

        public static string TextureBrowser(string name, Vector2? previewSize = null, int texturesPerRow = 10, string pathFilter = null, Vector2? previewScale = null)
        {
            string selectedTexture = null;
            var open = true;
            var textureAssets = GetAssetsByExtension(_textureExtensions, pathFilter);

            CheckSelectedTextureLoaded();

            ImGui.SetNextWindowSizeConstraints(new Vector2(1, 1), new Vector2(800, 800));
            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.HorizontalScrollbar))
            {
                if (!_selectedAssets.TryGetValue(name, out var selectedTextures))
                {
                    _selectedAssets.Add(name, new List<string>());
                    selectedTextures = _selectedAssets[name];
                }

                if (!_nameFilters.TryGetValue(name, out var nameFilter))
                {
                    nameFilter = "";
                    _nameFilters.Add(name, nameFilter);
                }

                ImGui.InputText("Name Filter", ref nameFilter, 200);
                _nameFilters[name] = nameFilter;
                ImGui.NewLine();

                var textures = 0;

                foreach (var asset in textureAssets)
                {
                    if (!string.IsNullOrEmpty(nameFilter) && !asset.ToUpper().Contains(nameFilter.ToUpper()))
                        continue;

                    var texture = AssetManager.Instance.LoadTexture2D(asset);

                    if (!_cachedTexturePtrs.TryGetValue(asset, out var texturePtr))
                    {
                        texturePtr = IMGUIManager.AddTexture(texture);
                        _cachedTexturePtrs.Add(asset, texturePtr);
                    }

                    var renderSize = previewSize.HasValue ? previewSize.Value : texture.SizeF;

                    if (previewScale.HasValue)
                        renderSize *= previewScale.Value;

                    ImGui.Image(texturePtr, renderSize);

                    if (ImGui.IsItemClicked())
                    {
                        if (selectedTextures.Contains(asset))
                        {
                            selectedTexture = selectedTextures[0];
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            selectedTextures.Clear();
                            selectedTextures.Add(asset);
                        }
                    }

                    if (selectedTextures.Contains(asset))
                    {
                        var imagePos = ImGui.GetItemRectMin();
                        ImGui.GetWindowDrawList().AddImage(_selectedTextureBGPtr, imagePos, imagePos + renderSize);
                    }

                    textures += 1;
                    if (textures % texturesPerRow != 0)
                        ImGui.SameLine();
                }

                ImGui.NewLine();

                if (ImGui.Button("Confirm Selection") && selectedTextures.Count > 0)
                {
                    selectedTexture = selectedTextures[0];
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            return selectedTexture;
        }
    }
}
