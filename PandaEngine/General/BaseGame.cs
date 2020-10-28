using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace PandaEngine
{
    public class BaseGame : IDisposable
    {
        public GameTimer GameTimer { get; set; }

        public Sdl2Window Window
        {
            get => PandaGlobals.Window;
        }

        public GraphicsDevice GraphicsDevice
        {
            get => PandaGlobals.GraphicsDevice;
        }

        public CommandList CommandList
        {
            get => PandaGlobals.CommandList;
        }

        public SpriteBatch2D SpriteBatch2D
        {
            get => PandaGlobals.SpriteBatch2D;
        }

        public AssetManager AssetManager
        {
            get => PandaGlobals.AssetManager;
        }

        // Graphics settings
        public RgbaFloat ClearColour { get; set; } = RgbaFloat.Black;
        public Viewport Viewport { get; set; }

        // Timing
        public bool IsFixedTimeStep { get; set; } = false;
        protected Stopwatch _stopWatch;
        protected long _currentTicks, _prevTicks;
        protected TimeSpan _targetFrameTime = TimeSpan.Zero;
        protected TimeSpan _totalFrameTime = TimeSpan.Zero;

        // Window title
        public string GameTitle { get; set; }
        public bool TitleShowFPS { get; set; } = true;
        protected TimeSpan _fpsCounter = TimeSpan.Zero;
        protected int _frameCounter = 0;

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
                    PandaGlobals.Unload();
                }

                _disposed = true;
            }
        }
        #endregion

        public BaseGame()
        {
            Load();
        }

        ~BaseGame()
        {
            Dispose(false);
        }

        public void SetupWindow(Rectangle windowRect, string gameTitle, GraphicsBackend graphicsBackend, bool vsync = false)
        {
            GameTitle = gameTitle;

            var windowCI = new WindowCreateInfo()
            {
                X = windowRect.X,
                Y = windowRect.Y,
                WindowWidth = windowRect.Width,
                WindowHeight = windowRect.Height,
                WindowInitialState = WindowState.Normal,
                WindowTitle = GameTitle,
            };

            PandaGlobals.Window = VeldridStartup.CreateWindow(ref windowCI);
            PandaGlobals.GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Window, new GraphicsDeviceOptions()
            {
                SyncToVerticalBlank = vsync,
            }, graphicsBackend);

            CreateGraphicsResources();

            PandaGlobals.Load(this);

            Viewport = new Viewport(0f, 0f, PandaGlobals.Window.Width, PandaGlobals.Window.Height, 0f, 1f);
        }

        public void SetupAssets(string modsPath = "Mods")
        {
            PandaGlobals.AssetManager.Load(modsPath);
        }

        public virtual void CreateGraphicsResources()
        {
            var factory = GraphicsDevice.ResourceFactory;
            PandaGlobals.CommandList = factory.CreateCommandList();
        } // CreateGraphicsResources

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
            CurrentGameState?.Unload();
            CurrentGameState = newState;
            CurrentGameState?.Load(PandaGlobals.AssetManager);
        }

        public void Quit() => _quit = true;

        public void Run()
        {
            GameTimer = new GameTimer();
            _stopWatch = Stopwatch.StartNew();

            while (Window.Exists && !_quit)
            {
                var inputSnapshot = Window.PumpEvents();

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
                    }

                    var sleepTime = (_totalFrameTime - _targetFrameTime).TotalMilliseconds;
                    if (sleepTime > 1.0f)
                        System.Threading.Thread.Sleep((int)sleepTime);
                }
                else
                {
                    GameTimer.SetFrameTime(_totalFrameTime);
                    _totalFrameTime = TimeSpan.Zero;

                    Update(GameTimer);
                    CurrentGameState?.Update(GameTimer);
                }

                HandleDraw();

                if (TitleShowFPS)
                {
                    _fpsCounter += GameTimer.FrameTime;
                    _frameCounter += 1;

                    if (_fpsCounter >= TimeSpan.FromSeconds(1))
                    {
                        Window.Title = GameTitle + " " + _frameCounter + " fps";
                        _fpsCounter -= TimeSpan.FromSeconds(1);
                        _frameCounter = 0;
                    }
                }

                PandaGlobals.InputManager.Update(inputSnapshot, GameTimer);
            }
        } // Run

        protected void HandleDraw()
        {
            CommandList.Begin();
            CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
            CommandList.SetViewport(0, Viewport);
            CommandList.ClearColorTarget(0, ClearColour);

            Draw(GameTimer);
            CurrentGameState?.Draw(GameTimer);

            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.SwapBuffers();
            //GraphicsDevice.WaitForIdle();
        }

        public virtual void Load()
        {
        }

        public virtual void Update(GameTimer gameTimer)
        {
        }

        public virtual void Draw(GameTimer gameTimer)
        {
        }

    } // BaseGame
}
