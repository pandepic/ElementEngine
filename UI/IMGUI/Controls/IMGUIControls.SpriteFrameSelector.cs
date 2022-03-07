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
        private static RangeI? _selectedFrameRange = null;
        private static IntPtr? _spriteFrameSelectorTexturePtr = null;

        public static void ResetSpriteFrameSelector()
        {
            _selectedFrameRange = null;
            _spriteFrameSelectorTexturePtr = null;
        }

        public static RangeI? SpriteFrameSelector(string name, Texture2D texture, Vector2I frameSize, RangeI? selectedFrames = null)
        {
            var textureSizeFrames = texture.Size / frameSize;
            var open = true;

            if (!_selectedFrameRange.HasValue)
                _selectedFrameRange = selectedFrames ?? new RangeI(1, 2);

            CheckSelectedTextureLoaded();

            if (!_spriteFrameSelectorTexturePtr.HasValue)
                _spriteFrameSelectorTexturePtr = IMGUIManager.AddTexture(texture);

            ImGui.SetNextWindowSizeConstraints(new Vector2(200, 400), new Vector2(800, 800));
            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.AlwaysHorizontalScrollbar))
            {
                ImGui.Text($"Selected Frames: {_selectedFrameRange.Value}");

                if (ImGui.Button("Select Frames"))
                {
                    ImGui.CloseCurrentPopup();
                    return _selectedFrameRange.Value;
                }

                ImGui.NewLine();

                ImGui.Image(_spriteFrameSelectorTexturePtr.Value, texture.SizeF);
                var texturePos = ImGui.GetItemRectMin();

                if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)) && ImGui.IsItemHovered())
                {
                    var mousePos = IMGUIManager.ItemClickedRelativePosition().ToVector2I();
                    var mouseFrame = mousePos / frameSize;
                    var mouseFrameIndex = (mouseFrame.X + textureSizeFrames.X * mouseFrame.Y) + 1;

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        if (mouseFrameIndex <= _selectedFrameRange.Value.Max)
                            _selectedFrameRange = new RangeI(mouseFrameIndex, _selectedFrameRange.Value.Max);
                    }
                    else
                    {
                        if (mouseFrameIndex >= _selectedFrameRange.Value.Min)
                            _selectedFrameRange = new RangeI(_selectedFrameRange.Value.Min, mouseFrameIndex);
                    }
                }

                for (var i = _selectedFrameRange.Value.Min - 1; i <= _selectedFrameRange.Value.Max - 1; i++)
                {
                    var textureFramePos = new Vector2I(i % textureSizeFrames.X, i / textureSizeFrames.X) * frameSize;
                    var drawPos = texturePos + textureFramePos.ToVector2();

                    ImGui.GetWindowDrawList().AddImage(_selectedTextureBGPtr, drawPos, drawPos + frameSize.ToVector2());
                }

                ImGui.EndPopup();
            }

            return null;
        }
    }
}
