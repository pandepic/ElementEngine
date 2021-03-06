﻿using System;
using System.Collections.Generic;
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

        // Engine resources
        public List<IEngineService> EngineServices { get; set; } = new List<IEngineService>();
        public Dictionary<int, List<IEngineService>> EngineServiceMessageSubscriptions { get; set; } = new Dictionary<int, List<IEngineService>>();

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

        public void SetupWindow(Rectangle windowRect, string gameTitle, GraphicsBackend? graphicsBackend = null, bool vsync = false)
        {
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
            }, graphicsBackend ?? VeldridStartup.GetPlatformDefaultBackend());

            CreateGraphicsResources();
            GameTitle = gameTitle + " [" + ElementGlobals.GraphicsDevice.BackendType.ToString() + "]";

            ElementGlobals.Load(this);
            ElementGlobals.Viewport = new Viewport(0f, 0f, ElementGlobals.Window.Width, ElementGlobals.Window.Height, 0f, 1f);

            ElementGlobals.Window.Resized += () =>
            {
                OnWindowResized(new Rectangle(Window.X, Window.Y, Window.Width, Window.Height));
            };
        } // SetupWindow

        public void SetupAssets(string modsPath = "Mods")
        {
            AssetManager.Load(modsPath);
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

        public virtual void OnWindowResized(Rectangle windowRect)
        {
            ElementGlobals.GraphicsDevice.ResizeMainWindow((uint)windowRect.Width, (uint)windowRect.Height);
            ElementGlobals.Viewport = new Viewport(0f, 0f, windowRect.Width, windowRect.Height, 0f, 1f);
        }

        public void AddEngineService<T>(T service) where T : IEngineService
        {
            if (EngineServices.Contains(service))
                throw new ArgumentException("This service type has already been added.", "service");

            EngineServices.Add(service);
            service.Parent = this;

        } // AddEngineService

        public bool SubscribeEngineServiceMessages(int messageType, IEngineService service)
        {
            if (!EngineServices.Contains(service))
                throw new ArgumentException("Service has not been added.", "T");

            if (!EngineServiceMessageSubscriptions.ContainsKey(messageType))
                EngineServiceMessageSubscriptions.Add(messageType, new List<IEngineService>());

            if (!EngineServiceMessageSubscriptions[messageType].Contains(service))
            {
                EngineServiceMessageSubscriptions[messageType].Add(service);
                return true;
            }
            else
            {
                return false;
            }
        } // SubscribeEngineServiceMessages

        public bool UnsubscribeEngineServiceMessages(int messageType, IEngineService service)
        {
            if (!EngineServices.Contains(service))
                throw new ArgumentException("Service has not been added.", "T");

            if (!EngineServiceMessageSubscriptions.ContainsKey(messageType))
                return false;
            
            return EngineServiceMessageSubscriptions[messageType].Remove(service);
        } // UnsubscribeEngineServiceMessages

        public void SendServiceMessage(IServiceMessage message)
        {
            if (!EngineServiceMessageSubscriptions.ContainsKey(message.MessageType))
                return;

            foreach (var service in EngineServiceMessageSubscriptions[message.MessageType])
                service.HandleMessage(message);

        } // SendEngineServiceMessage

    } // BaseGame
}
