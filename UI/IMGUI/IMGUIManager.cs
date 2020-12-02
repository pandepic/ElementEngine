using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ElementEngine.UI
{
    public static class IMGUIManager
    {
        public static ImGuiRenderer Renderer { get; set; }

        public static void Setup()
        {
            Renderer = new ImGuiRenderer(ElementGlobals.GraphicsDevice,
                ElementGlobals.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight);

            Renderer.RecreateFontDeviceTexture();
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
    }
}
