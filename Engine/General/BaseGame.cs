using ElementEngine.Timer;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace ElementEngine
{
    public class BaseGame : IDisposable
    {
        public GameWindow Window => ElementGlobals.Window;
        public GraphicsDevice GraphicsDevice => ElementGlobals.GraphicsDevice;
        public CommandList CommandList => ElementGlobals.CommandList;
        public SpriteBatch2D ScreenSpaceSpriteBatch2D => ElementGlobals.ScreenSpaceSpriteBatch2D;

        // Graphics settings
        public RgbaFloat ClearColor { get; set; } = RgbaFloat.Black;

        // Timing
        public GameTimer GameTimer;
        public bool IsFixedTimeStep = false;
        protected Stopwatch _stopWatch;
        protected long _currentTicks, _prevTicks;
        protected TimeSpan _targetFrameTime = TimeSpan.Zero;
        protected TimeSpan _totalFrameTime = TimeSpan.Zero;

        // Window
        public bool Focused { get => ElementGlobals.Window.Focused; }
        public string GameTitle;
        public bool TitleShowFPS = true;
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
            try
            {
                Dispose(true);
            }
            catch (Exception disposeException)
            {
                // Throwing from Dispose would replace an in-flight exception unwinding
                // through a using block, hiding the root cause; log teardown failures.
                Logging.Error("Game disposal failed: {exception}", disposeException.ToString());
            }
            finally
            {
                Logging.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (!disposing)
            {
                _disposed = true;
                return;
            }

            var collector = new TeardownCollector();
            collector.Run(Unload);
            TeardownEngine(collector);
            _disposed = true;
            collector.ThrowIfFailed("Game teardown failed.");
        }
        #endregion

        public BaseGame()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Load();
            }
            catch (Exception loadException)
            {
                var collector = new TeardownCollector(loadException);
                TeardownEngine(collector);
                collector.ThrowIfFailed("Game loading and cleanup both failed.");
                throw;
            }
        }

        ~BaseGame()
        {
            Dispose(false);
        }

        public void SetupWindow(
            Rectangle windowRect,
            string windowTitle,
            GraphicsBackend? graphicsBackend = null,
            bool vsync = false,
            WindowState? windowState = null,
            bool debug = false)
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

            SetupWindow(windowInfo, graphicsBackend, vsync, debug);
        }

        public void SetupWindow(
            WindowCreateInfo windowInfo,
            GraphicsBackend? graphicsBackend = null,
            bool vsync = false,
            bool debug = false)
        {
            if (ElementGlobals.Window != null || ElementGlobals.GraphicsDevice != null)
                throw new InvalidOperationException("The game window has already been created.");

            ElementGlobals.Window = NativeWindowFactory.Create(windowInfo);
            try
            {
                ElementGlobals.GraphicsDevice = GraphicsDevice.Create(
                    ElementGlobals.Window,
                    new GraphicsDeviceOptions(Debug: debug, SyncToVerticalBlank: vsync),
                    graphicsBackend);
            }
            catch
            {
                ElementGlobals.Window.Dispose();
                ElementGlobals.Window = null;
                throw;
            }
            PlatformType = PlatformMapping.Current;

            CreateGraphicsResources();

            GameTitle = $"{windowInfo.WindowTitle} [{ElementGlobals.GraphicsDevice.BackendType}]";

            ElementGlobals.Load(this);
            ElementGlobals.Viewport = new Viewport(
                0f,
                0f,
                ElementGlobals.Window.FramebufferWidth,
                ElementGlobals.Window.FramebufferHeight,
                0f,
                1f);

            ElementGlobals.Window.Resized += () =>
            {
                OnWindowResized(new Rectangle(Window.X, Window.Y, Window.Width, Window.Height));
            };

        } // SetupWindow

        /// <summary>
        /// Enables gamepad input. <paramref name="applyMappings"/> applies the SDL-style
        /// controller mapping database and is currently honored on Linux only; Windows
        /// (XInput) and macOS (GameController framework) report standardized layouts,
        /// and generic HID pads on those platforms use a heuristic default layout.
        /// </summary>
        public void EnableGamepads(bool applyMappings = false)
        {
            if (applyMappings && !OperatingSystem.IsLinux())
                Logging.Warning("Gamepad mappings are only applied on Linux; the applyMappings flag has no effect on this platform.");

            InputManager.LoadGamepads(applyMappings);

            Logging.Information("Controller input enabled.");
        }

        public DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
        {
            return Window?.GetCurrentDisplayMode(displayIndex) ?? NativeDisplay.GetCurrentDisplayMode(displayIndex);
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
            GraphicsDevice.WaitForIdle();
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

        private static void SleepUntilNextUpdate(TimeSpan remaining)
        {
            // Thread.Sleep can overshoot by the OS timer period (~15.6ms on Windows),
            // so sleep coarsely and spin the final stretch to hold high target rates.
            var start = Stopwatch.GetTimestamp();
            var coarseSleep = remaining - TimeSpan.FromMilliseconds(OperatingSystem.IsWindows() ? 16 : 2);

            if (coarseSleep > TimeSpan.Zero)
                Thread.Sleep(coarseSleep);

            while (Stopwatch.GetElapsedTime(start) < remaining)
                Thread.Yield();
        }

        public void Run()
        {
            GameTimer = new GameTimer();
            _stopWatch = Stopwatch.StartNew();

            while (Window.Exists && !_quit)
            {
                var inputSnapshot = Window.PumpEvents();
                if (!Window.Exists)
                    break;

                InputManager.Update(inputSnapshot, GameTimer);

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

                    var sleepTime = _targetFrameTime - _totalFrameTime;
                    if (sleepTime.TotalMilliseconds > 1.0)
                        SleepUntilNextUpdate(sleepTime);
                }
                else
                {
                    GameTimer.SetFrameTime(_totalFrameTime);
                    _totalFrameTime = TimeSpan.Zero;

                    Update(GameTimer);
                    CurrentGameState?.Update(GameTimer);
                    TimerManager.Update(GameTimer);
                }

                var renderedFrame = Window.FramebufferWidth > 0 &&
                                    Window.FramebufferHeight > 0 &&
                                    Window.State is not WindowState.Minimized and not WindowState.Hidden;
                if (renderedFrame)
                {
                    HandleDraw();

                    if (TitleShowFPS)
                    {
                        _fpsCounter += GameTimer.RawFrameTime;
                        _frameCounter += 1;

                        if (_fpsCounter >= TimeSpan.FromSeconds(1))
                        {
                            Window.Title = $"{GameTitle} {_frameCounter}fps";
                            _fpsCounter -= TimeSpan.FromSeconds(1);
                            _frameCounter = 0;
                        }
                    }
                }
                else if (!IsFixedTimeStep)
                {
                    Thread.Sleep(16);
                }

                CurrentGameState?.EndOfFrame(GameTimer);
                EndOfFrame(GameTimer);

                SoundManager.Update();
            }

            GraphicsDevice.WaitForIdle();
            CurrentGameState?.DeRegister();
            CurrentGameState?.Unload();
            AssetManager.Instance.Clear();
            InputManager.Dispose();
            SoundManager.Shutdown();

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
            ElementGlobals.GraphicsDevice.ResizeMainWindow(
                (uint)Window.FramebufferWidth,
                (uint)Window.FramebufferHeight);
            ElementGlobals.Viewport = new Viewport(
                0f,
                0f,
                Window.FramebufferWidth,
                Window.FramebufferHeight,
                0f,
                1f);
            ElementGlobals.ScreenSpaceSpriteBatch2D?.SetViewSize(ElementGlobals.TargetResolutionSizeF);
            UI.IMGUIManager.WindowResized(windowRect.Width, windowRect.Height);

            CurrentGameState?.OnWindowResized(windowRect);
        }

        // Every engine subsystem is torn down here, in order, on both the dispose
        // and the load-failure paths, so the two cannot drift apart.
        private static void TeardownEngine(TeardownCollector collector)
        {
            collector.Run(ElementGlobals.Unload);
            collector.Run(AssetManager.Instance.Clear);
            collector.Run(InputManager.Dispose);
            collector.Run(SoundManager.Shutdown);
        }

    } // BaseGame
}
