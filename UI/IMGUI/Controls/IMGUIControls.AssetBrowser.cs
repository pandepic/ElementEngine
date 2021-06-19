using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.UI
{
    public static partial class IMGUIControls
    {
        private static List<string> _textureExtensions = new List<string>() { "png", "jpg", "jpeg" };
        private static List<string> _tempStringList = new List<string>();

        private static Dictionary<string, IntPtr> _cachedTexturePtrs = new Dictionary<string, IntPtr>();
        private static Dictionary<string, List<string>> _selectedAssets = new Dictionary<string, List<string>>();

        private static Texture2D _selectedTextureBG = new Texture2D(1, 1, new Veldrid.RgbaByte(255, 0, 0, 80));
        private static IntPtr _selectedTextureBGPtr;
        private static bool _selectedTextureBGLoaded = false;

        private static List<string> GetAssetsByExtension(List<string> extensions)
        {
            _tempStringList.Clear();

            foreach (var extension in extensions)
            {
                var assets = AssetManager.GetAssetsByExtension(extension);
                _tempStringList.AddIfNotContains(assets);
            }

            return _tempStringList;
        }

        public static string TextureBrowser(string name, Vector2 previewSize)
        {
            string selectedTexture = null;
            var open = true;
            var textureAssets = GetAssetsByExtension(_textureExtensions);
            var texturesPerRow = 10;

            if (!_selectedTextureBGLoaded)
            {
                _selectedTextureBGPtr = IMGUIManager.AddTexture(_selectedTextureBG);
                _selectedTextureBGLoaded = true;
            }

            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (!_selectedAssets.TryGetValue(name, out var selectedTextures))
                {
                    _selectedAssets.Add(name, new List<string>());
                    selectedTextures = _selectedAssets[name];
                }

                var textures = 0;

                foreach (var asset in textureAssets)
                {
                    if (!_cachedTexturePtrs.TryGetValue(asset, out var texturePtr))
                        texturePtr = IMGUIManager.AddTexture(AssetManager.LoadTexture2D(asset));

                    ImGui.Image(texturePtr, previewSize);
                    
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
                        ImGui.GetWindowDrawList().AddImage(_selectedTextureBGPtr, imagePos, imagePos + previewSize);
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
