using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementEngine;
using ElementEngine.UI;
using ImGuiNET;
using Veldrid;

namespace Samples
{
    public enum GameStateType
    {
        SpriteMovement,
    }

    internal class Game : BaseGame
    {
        public Dictionary<GameStateType, GameState> GameStates = new Dictionary<GameStateType, GameState>();

        public override void Load()
        {
            var displayMode = GetCurrentDisplayMode();
            SettingsManager.LoadFromPath("Settings.xml");

            var strWidth = SettingsManager.GetSetting<string>("Window", "Width");
            var strHeight = SettingsManager.GetSetting<string>("Window", "Height");

            // default to fullscreen borderless
            if (string.IsNullOrWhiteSpace(strWidth) || string.IsNullOrWhiteSpace(strHeight))
            {
                SettingsManager.UpdateSetting("Window", "Width", displayMode.w);
                SettingsManager.UpdateSetting("Window", "Height", displayMode.h);
                SettingsManager.UpdateSetting("Window", "BorderlessFullscreen", true);
            }

            var vsync = SettingsManager.GetSetting<bool>("Window", "Vsync");
            var borderless = SettingsManager.GetSetting<bool>("Window", "BorderlessFullscreen");

            var windowPosition = new Vector2I(100, 100);

            var windowRect = new Rectangle()
            {
                X = windowPosition.X,
                Y = windowPosition.Y,
                Width = SettingsManager.GetSetting<int>("Window", "Width"),
                Height = SettingsManager.GetSetting<int>("Window", "Height")
            };

            var windowState = borderless ? WindowState.BorderlessFullScreen : WindowState.Normal;

            if (borderless)
            {
                windowRect.Width = displayMode.w;
                windowRect.Height = displayMode.h;
            }

            // remove resolutions bigger than the current screen
            for (var i = Globals.PossibleResolutions.Count - 1; i >= 0; i--)
            {
                var resolution = Globals.PossibleResolutions[i];

                if (resolution.Width > displayMode.w || resolution.Height > displayMode.h)
                    Globals.PossibleResolutions.RemoveAt(i);
            }

            SetupWindow(windowRect, "Element Engine Samples", GraphicsBackend.Direct3D11, vsync: vsync, windowState: windowState);
            SetupAssets("Mods");

            IMGUIManager.Setup();
            Globals.SetLanguage(SettingsManager.GetSetting<string>("UI", "Language"));
            InputManager.LoadGameControls();

            EnableGameControllers();
            UpdateAudioVolume();

            Window.Resizable = false;
            Globals.DebugFont = AssetManager.Instance.LoadSpriteFont("Fonts/LatoBlack.ttf");

            GameStates.Add(GameStateType.SpriteMovement, new GameStateSpriteMovement());

            SetGameState(GameStateType.SpriteMovement);
        }

        public void SetGameState(GameStateType state)
        {
            SetGameState(GameStates[state]);
        }

        public static void UpdateAudioVolume()
        {
            SoundManager.SetMasterVolume(SettingsManager.GetSetting<float>("Sound", "MasterVolume"));
            SoundManager.SetVolume((int)AudioVolumeType.Music, SettingsManager.GetSetting<float>("Sound", "MusicVolume"));
            SoundManager.SetVolume((int)AudioVolumeType.SFX, SettingsManager.GetSetting<float>("Sound", "SFXVolume"));
            SoundManager.SetVolume((int)AudioVolumeType.UI, SettingsManager.GetSetting<float>("Sound", "UIVolume"));
        }

        public override void Update(GameTimer gameTimer)
        {
            IMGUIManager.Update(gameTimer);
        }

        public override void Draw(GameTimer gameTimer)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Language"))
                {
                    foreach (var language in Globals.Languages)
                    {
                        if (ImGui.MenuItem(language, "", language == Globals.CurrentLanguage))
                            Globals.SetLanguage(language);
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            IMGUIManager.Draw();
        }
    }
}
