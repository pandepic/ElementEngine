using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine.UI
{
    public static class IMGUIManager
    {
        public static ImGuiRenderer Renderer { get; set; }
        private static List<ImGuiCol> _pushedStyleColors = new List<ImGuiCol>();

        public static void Setup()
        {
            Renderer = new ImGuiRenderer(ElementGlobals.GraphicsDevice,
                ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight);
        }

        public static void Update(GameTimer gameTimer)
        {
            if (InputManager.PrevSnapshot == null)
                return;

            Renderer.Update(gameTimer.DeltaS, InputManager.PrevSnapshot);
        }

        public static void Draw()
        {
            Renderer.Render(ElementGlobals.GraphicsDevice, ElementGlobals.CommandList);
        }

        public static void PushStyleColor(ImGuiCol type, Vector4 val)
        {
            ImGui.PushStyleColor(type, val);
            _pushedStyleColors.Add(type);
        }

        public static void PopStyleColor(int count = 1)
        {
            ImGui.PopStyleColor(count);
        }

        public static void PopAllStyleColors()
        {
            ImGui.PopStyleColor(_pushedStyleColors.Count);
            _pushedStyleColors.Clear();
        }
    }
}
