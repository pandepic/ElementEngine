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
            return Renderer.GetOrCreateImGuiBinding(ElementGlobals.GraphicsDevice.ResourceFactory, texture.Texture);
        }

        public static void RemoveTexture(Texture2D texture)
        {
            Renderer.RemoveImGuiBinding(texture.Texture);
        }

        public static void PushStyleColor(ImGuiCol type, Vector4 val)
        {
            ImGui.PushStyleColor(type, val);
            _pushedStyleColors.Add(type);
        }

        public static void PopStyleColor(int count = 1)
        {
            ImGui.PopStyleColor(count);
            _pushedStyleColors.RemoveRange(_pushedStyleColors.Count - count, count);
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

        public static bool IsMouseHoveringRect(Rectangle rect)
        {
            return ImGui.IsMouseHoveringRect(rect.LocationF, rect.BottomRightF);
        }

        public static Rectangle GetWindowRect()
        {
            return new Rectangle(ImGui.GetWindowPos(), ImGui.GetWindowSize());
        }

        public static Rectangle GetItemRect()
        {
            return new Rectangle(ImGui.GetItemRectMin(), ImGui.GetItemRectSize());
        }

        public static Vector2 ItemRelativePosition()
        {
            return ImGui.GetItemRectMin() - ImGui.GetWindowPos();
        }

        public static Vector2 ItemClickedRelativePosition()
        {
            return ImGui.GetMousePos() - ImGui.GetWindowPos() - ItemRelativePosition();
        }

    } // IMGUIManager
}
