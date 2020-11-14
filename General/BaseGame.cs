using System;
using System.Diagnostics;
using System.Globalization;
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
                    ElementGlobals.Unload();
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

        public void SetupWindow(Rectangle windowRect, string gameTitle, GraphicsBackend graphicsBackend, bool vsync = false)
        {
            GameTitle = gameTitle + " [" + graphicsBackend.ToString() + "]";

            var windowCI = new WindowCreateInfo()
            {
                X = windowRect.X,
                Y = windowRect.Y,
                WindowWidth = windowRect.Width,
                WindowHeight = windowRect.Height,
                WindowInitialState = WindowState.Normal,
                WindowTitle = GameTitle,
            };

            ElementGlobals.Window = VeldridStartup.CreateWindow(ref windowCI);
            ElementGlobals.GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Window, new GraphicsDeviceOptions()
            {
                SyncToVerticalBlank = vsync,
                PreferStandardClipSpaceYDirection = true,
            }, graphicsBackend);

            CreateGraphicsResources();

            ElementGlobals.Load(this);
            ElementGlobals.Viewport = new Viewport(0f, 0f, ElementGlobals.Window.Width, ElementGlobals.Window.Height, 0f, 1f);
        } // SetupWindow

        public void SetupAssets(string modsPath = "Mods")
        {
            AssetManager.Load(modsPath);
        }

        public virtual void CreateGraphicsResources()
        {
            var factory = GraphicsDevice.ResourceFactory;
            ElementGlobals.CommandList = factory.CreateCommandList();
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
            CommandList.Begin();

            CurrentGameState?.DeRegister();
            CurrentGameState?.Unload();

            CurrentGameState = newState;
            CurrentGameState?.Register();
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

                var inputSnapshot = Window.PumpEvents();
                InputManager.Update(inputSnapshot, GameTimer);
                SoundManager.Update();
            }
        } // Run

        protected void HandleDraw()
        {
            CommandList.Begin();
            ElementGlobals.ResetFramebuffer();
            ElementGlobals.ResetViewport();
            CommandList.ClearColorTarget(0, ClearColor);

            Draw(GameTimer);
            CurrentGameState?.Draw(GameTimer);

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
