using ElementEngine.Input;
using ElementEngine.Timer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ElementEngine
{
    public class BaseGame : IDisposable
    {
        public Sdl2Window Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;
        public SpriteBatch2D ScreenSpaceSpriteBatch2D => ElementGlobals.ScreenSpaceSpriteBatch2D;

        // Graphics settings
        public RgbaFloat ClearColor { get; set; } = RgbaFloat.Black;

        // Timing
        public GameTimer GameTimer { get; set; }
        public bool IsFixedTimeStep { get; set; } = false;
        protected Stopwatch _stopWatch;
        protected long _currentTicks, _prevTicks;
        protected TimeSpan _targetFrameTime = TimeSpan.Zero;
        protected TimeSpan _totalFrameTime = TimeSpan.Zero;

        // Window
        public bool Focused { get => ElementGlobals.Window.Focused; }
        public string GameTitle { get; set; }
        public bool TitleShowFPS { get; set; } = true;
        protected TimeSpan _fpsCounter = TimeSpan.Zero;
        protected int _frameCounter = 0;

        public PlatformType PlatformType = PlatformType.Unknown;

        // Game state
        public GameState CurrentGameState { get; set; } = null;
        protected bool _quit = false;

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ElementGlobals.Unload();
                    Unload();
                }

                _disposed = true;
            }
        }
        #endregion

        public BaseGame()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Logging.Load();
            Load();
        }

        ~BaseGame()
        {
            Dispose(false);
        }

        public unsafe void SetupWindow(Rectangle windowRect, string windowTitle, GraphicsBackend? graphicsBackend = null, bool vsync = false, WindowState? windowState = null)
        {
            windowState ??= WindowState.Normal;

            var windowInfo = new WindowCreateInfo()
            {
                X = windowRect.X,
                Y = windowRect.Y,
                WindowWidth = windowRect.Width,
                WindowHeight = windowRect.Height,
                WindowInitialState = windowState.Value,
                WindowTitle = windowTitle,
            };

            SetupWindow(windowInfo, graphicsBackend, vsync);

            var platformName = Marshal.PtrToStringUTF8((IntPtr)SDL2.SDL_GetPlatform());
            PlatformType = PlatformMapping.GetPlatformTypeBySDLName(platformName);
        }

        public void SetupWindow(WindowCreateInfo windowInfo, GraphicsBackend? graphicsBackend = null, bool vsync = false)
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(windowInfo, new GraphicsDeviceOptions()
            {
                SyncToVerticalBlank = vsync,
                PreferStandardClipSpaceYDirection = true,
            }, graphicsBackend ?? VeldridStartup.GetPlatformDefaultBackend(), out ElementGlobals.Window, out ElementGlobals.GraphicsDevice);

            CreateGraphicsResources();

            GameTitle = $"{windowInfo.WindowTitle} [{ElementGlobals.GraphicsDevice.BackendType}]";

            ElementGlobals.Load(this);
            ElementGlobals.Viewport = new Viewport(0f, 0f, ElementGlobals.Window.Width, ElementGlobals.Window.Height, 0f, 1f);

            ElementGlobals.Window.Resized += () =>
            {
                OnWindowResized(new Rectangle(Window.X, Window.Y, Window.Width, Window.Height));
            };

        } // SetupWindow

        public void EnableGameControllers()
        {
            Sdl2Native.SDL_Init(SDLInitFlags.GameController);

            GameControllerMapping.ApplyMappings(PlatformType);
            InputManager.LoadGameControllers();

            Sdl2Events.Subscribe(InputManager.ProcessGameControllerEvents);

            Logging.Information("Controller input enabled.");
        }

        public unsafe SDL_DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
        {
            Sdl2Native.SDL_Init(SDLInitFlags.Video);

            var displayMode = new SDL_DisplayMode();
            var result = Sdl2Native.SDL_GetCurrentDisplayMode(displayIndex, &displayMode);

            if (result == -1)
            {
                var error = StringHelper.GetString(Sdl2Native.SDL_GetError());
                throw new Exception($"GetCurrentDisplayMode SDL error: {error}");
            }

            return displayMode;
        }

        public void SetupAssets(string modsPath = "Mods")
        {
            AssetManager.Instance.Load(modsPath);
        }

        public void CreateGraphicsResources()
        {
            var factory = GraphicsDevice.ResourceFactory;
            ElementGlobals.CommandList = factory.CreateCommandList();
        }

        public void EnableFixedTimeStep(int targetFPS)
        {
            IsFixedTimeStep = true;
            _targetFrameTime = TimeSpan.FromMilliseconds(1000.0f / (float)targetFPS);
        }

        public void DisableFixedTimeStep()
        {
            IsFixedTimeStep = false;
        }

        public void SetGameState(GameState newState)
        {
            CommandList.Begin();

            CurrentGameState?.DeRegister();
            CurrentGameState?.Unload();

            CurrentGameState = newState;
            CurrentGameState?.Register();
            CurrentGameState?.DoInitialize();
            CurrentGameState?.Load();

            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
        }

        public void Quit() => _quit = true;

        public void Run()
        {
            GameTimer = new GameTimer();
            _stopWatch = Stopwatch.StartNew();

            while (Window.Exists && !_quit)
            {
                _currentTicks = _stopWatch.Elapsed.Ticks;
                var newTicks = TimeSpan.FromTicks(_currentTicks - _prevTicks);
                _totalFrameTime += newTicks;
                _prevTicks = _currentTicks;

                if (IsFixedTimeStep)
                {
                    while (_totalFrameTime > _targetFrameTime)
                    {
                        _totalFrameTime -= _targetFrameTime;

                        GameTimer.SetFrameTime(_targetFrameTime);

                        Update(GameTimer);
                        CurrentGameState?.Update(GameTimer);
                        TimerManager.Update(GameTimer);
                    }

                    var sleepTime = (_totalFrameTime - _targetFrameTime).TotalMilliseconds;
                    if (sleepTime > 1.0f)
                        Thread.Sleep((int)sleepTime);
                }
                else
                {
                    GameTimer.SetFrameTime(_totalFrameTime);
                    _totalFrameTime = TimeSpan.Zero;

                    Update(GameTimer);
                    CurrentGameState?.Update(GameTimer);
                    TimerManager.Update(GameTimer);
                }

                HandleDraw();

                CurrentGameState?.EndOfFrame(GameTimer);
                EndOfFrame(GameTimer);

                if (TitleShowFPS)
                {
                    _fpsCounter += GameTimer.FrameTime;
                    _frameCounter += 1;

                    if (_fpsCounter >= TimeSpan.FromSeconds(1))
                    {
                        var fps = _frameCounter * GameTimer.TimeWarpFactor;

                        Window.Title = $"{GameTitle} {(int)fps}fps";
                        _fpsCounter -= TimeSpan.FromSeconds(1);
                        _frameCounter = 0;
                    }
                }

                var inputSnapshot = Window.PumpEvents();
                InputManager.Update(inputSnapshot, GameTimer);
                SoundManager.Update();
            }

            CurrentGameState?.Unload();
            AssetManager.Instance.Clear();
            Exit();

        } // Run

        protected void HandleDraw()
        {
            CommandList.Begin();
            ElementGlobals.ResetFramebuffer();
            ElementGlobals.ResetViewport();
            CommandList.ClearColorTarget(0, ClearColor);

            CurrentGameState?.Draw(GameTimer);
            Draw(GameTimer);

            if (ElementGlobals.ScreenSpaceSpriteBatch2D != null && ElementGlobals.ScreenSpaceDrawList.Count > 0)
            {
                ScreenSpaceSpriteBatch2D.Begin(SamplerType.Point);
                for (var i = 0; i < ElementGlobals.ScreenSpaceDrawList.Count; i++)
                    ElementGlobals.ScreenSpaceDrawList[i]();
                ScreenSpaceSpriteBatch2D.End();
            }

            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.SwapBuffers();
        }

        public virtual void Load() { }
        public virtual void Unload() { }
        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw(GameTimer gameTimer) { }
        public virtual void EndOfFrame(GameTimer gameTimer) { }
        public virtual void Exit() { }

        public virtual void OnWindowResized(Rectangle windowRect)
        {
            ElementGlobals.GraphicsDevice.ResizeMainWindow((uint)windowRect.Width, (uint)windowRect.Height);
            ElementGlobals.Viewport = new Viewport(0f, 0f, windowRect.Width, windowRect.Height, 0f, 1f);
            ElementGlobals.ScreenSpaceSpriteBatch2D?.SetViewSize(ElementGlobals.TargetResolutionSizeF);

            CurrentGameState.OnWindowResized(windowRect);
        }

    } // BaseGame
}
