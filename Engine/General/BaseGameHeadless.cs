using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElementEngine.Timer;
using Serilog.Core;

namespace ElementEngine
{
    public class BaseGameHeadless : IDisposable
    {
        protected bool _quit = false;

        // Timing
        public GameTimer GameTimer;
        public bool IsFixedTimeStep = false;
        protected Stopwatch _stopWatch;
        protected long _currentTicks, _prevTicks;
        protected TimeSpan _targetFrameTime = TimeSpan.Zero;
        protected TimeSpan _totalFrameTime = TimeSpan.Zero;

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
                    Unload();
                }

                _disposed = true;
            }
        }
        #endregion

        public BaseGameHeadless() : this(null) { }

        public BaseGameHeadless(Logger logger)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (logger is null)
            {
                Logging.Load();
            }
            else
            {
                Logging.Load(logger);
            }
            
            Load();
        }

        public void SetupAssets(string modsPath = "Mods")
        {
            AssetManager.Instance.Load(modsPath);
        }

        public virtual void Load() { }
        public virtual void Unload() { }
        public virtual void Update(GameTimer gameTimer) { }
        public virtual void EndOfFrame(GameTimer gameTimer) { }
        public virtual void Exit() { }

        public void EnableFixedTimeStep(int targetFPS)
        {
            IsFixedTimeStep = true;
            _targetFrameTime = TimeSpan.FromMilliseconds(1000.0f / (float)targetFPS);
        }

        public void DisableFixedTimeStep()
        {
            IsFixedTimeStep = false;
        }

        public void Quit() => _quit = true;

        public void Run()
        {
            GameTimer = new GameTimer();
            _stopWatch = Stopwatch.StartNew();

            while (!_quit)
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
                    TimerManager.Update(GameTimer);
                }

                EndOfFrame(GameTimer);
            }

            AssetManager.Instance.Clear();
            InputManager.Dispose();

            Exit();
        }
    }
}
