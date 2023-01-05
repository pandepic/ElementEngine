using System.Collections.Generic;
using System.Numerics;
using ElementEngine.TexturePacker;
using ImGuiNET;

namespace ElementEngine.UI
{
    public static partial class IMGUIControls
    {
        public static string TexturePackerBrowser(string name, TexturePackerAtlas atlas, Vector2? previewSize = null)
        {
            string selectedSprite = null;
            var open = true;
            var texturesPerRow = 10;

            if (!_cachedTexturePtrs.TryGetValue(atlas.TextureAsset, out var texturePtr))
            {
                var texture = AssetManager.Instance.LoadTexture2D(atlas.TextureAsset);
                texturePtr = IMGUIManager.AddTexture(texture);
                _cachedTexturePtrs.Add(atlas.TextureAsset, texturePtr);
            }

            if (!_selectedTextureBGLoaded)
            {
                _selectedTextureBGPtr = IMGUIManager.AddTexture(_selectedTextureBG);
                _selectedTextureBGLoaded = true;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(1, 1), new Vector2(800, 800));
            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.HorizontalScrollbar))
            {
                if (!_selectedAssets.TryGetValue(name, out var selectedTextures))
                {
                    _selectedAssets.Add(name, new List<string>());
                    selectedTextures = _selectedAssets[name];
                }

                var textures = 0;

                foreach (var (spriteName, sprite) in atlas.Sprites)
                {
                    var spriteRect = atlas.GetSpriteRect(spriteName);

                    ImGui.Image(
                        texturePtr,
                        previewSize ?? spriteRect.SizeF,
                        spriteRect.LocationF * atlas.Texture.TexelSize,
                        spriteRect.BottomRightF * atlas.Texture.TexelSize);

                    if (ImGui.IsItemClicked())
                    {
                        if (selectedTextures.Contains(spriteName))
                        {
                            selectedSprite = selectedTextures[0];
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            selectedTextures.Clear();
                            selectedTextures.Add(spriteName);
                        }
                    }

                    if (selectedTextures.Contains(spriteName))
                    {
                        var imagePos = ImGui.GetItemRectMin();
                        ImGui.GetWindowDrawList().AddImage(_selectedTextureBGPtr, imagePos, imagePos + (previewSize ?? spriteRect.SizeF));
                    }

                    textures += 1;
                    if (textures % texturesPerRow != 0)
                        ImGui.SameLine();
                }

                ImGui.NewLine();

                if (ImGui.Button("Confirm Selection") && selectedTextures.Count > 0)
                {
                    selectedSprite = selectedTextures[0];
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            return selectedSprite;
        }
    }
}
