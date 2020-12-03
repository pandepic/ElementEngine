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
        private static Dictionary<string, IMGUIModal> Modals { get; set; } = new Dictionary<string, IMGUIModal>();

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
            foreach (var (_, modal) in Modals)
            {
                if (modal.Begin())
                {
                    modal.Draw();
                    modal.End();
                }
            }
            
            Renderer.Render(ElementGlobals.GraphicsDevice, ElementGlobals.CommandList);
        }

        public static IntPtr AddTexture(Texture2D texture)
        {
            return Renderer.GetOrCreateImGuiBinding(ElementGlobals.GraphicsDevice.ResourceFactory, texture.TextureView);
        }

        public static void RemoveTexture(Texture2D texture)
        {
            Renderer.RemoveImGuiBinding(texture.TextureView);
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

        public static void AddModal<T>(T type, IMGUIModal modal) where T : Enum
        {
            AddModal(type.ToString(), modal);
        }

        public static void AddModal(string type, IMGUIModal modal)
        {
            Modals.Add(type, modal);
        } // AddModal

        public static void OpenModal<T>(T type) where T : Enum
        {
            OpenModal(type.ToString());
        }

        public static void OpenModal(string type)
        {
            if (Modals.TryGetValue(type, out var modal))
            {
                modal.Open();
            }
        }

        public static void CloseModal<T>(T type) where T : Enum
        {
            CloseModal(type.ToString());
        }

        public static void CloseModal(string type)
        {
            if (Modals.TryGetValue(type, out var modal))
            {
                modal.Close();
            }
        }

    } // IMGUIManager
}
