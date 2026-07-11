using System;

namespace ElementEngine;

public enum WindowState
{
    Normal,
    FullScreen,
    Maximized,
    Minimized,
    BorderlessFullScreen,
    Hidden,
}

public enum NativeWindowKind
{
    Win32,
    X11,
    Wayland,
    Cocoa,
}

public readonly record struct DisplayMode(int Width, int Height, int RefreshRate)
{
    public int w => Width;
    public int h => Height;
    public int refresh_rate => RefreshRate;
}

public struct WindowCreateInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public WindowState WindowInitialState { get; set; }
    public string WindowTitle { get; set; }

    public WindowCreateInfo(
        int x,
        int y,
        int windowWidth,
        int windowHeight,
        WindowState windowInitialState,
        string windowTitle)
    {
        X = x;
        Y = y;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
        WindowInitialState = windowInitialState;
        WindowTitle = windowTitle;
    }
}

public abstract class GameWindow : IDisposable
{
    public abstract NativeWindowKind NativeKind { get; }
    public abstract nint NativeHandle { get; }
    public virtual nint NativeDisplayHandle => 0;
    public virtual nint NativeViewHandle => NativeHandle;
    public abstract bool Exists { get; }
    public abstract bool Focused { get; }
    public abstract int X { get; }
    public abstract int Y { get; }
    public abstract int Width { get; }
    public abstract int Height { get; }
    public virtual int FramebufferWidth => Width;
    public virtual int FramebufferHeight => Height;
    public virtual float ScaleFactor => Width == 0 ? 1f : (float)FramebufferWidth / Width;
    public abstract string Title { get; set; }
    public abstract bool Resizable { get; set; }
    public abstract bool CursorVisible { get; set; }
    public abstract WindowState State { get; set; }

    private bool _allowScreenSaver;

    /// <summary>
    /// Game input (gamepads especially) does not reset the OS idle timer, so the
    /// screensaver and display sleep are inhibited by default while a window exists.
    /// </summary>
    public bool AllowScreenSaver
    {
        get => _allowScreenSaver;
        set
        {
            if (_allowScreenSaver == value)
                return;

            _allowScreenSaver = value;
            OnAllowScreenSaverChanged();
        }
    }

    protected virtual void OnAllowScreenSaverChanged() { }

    public event Action Resized;
    public event Action Moved;
    public event Action FocusChanged;
    public event Action ScaleFactorChanged;
    public event Action Closing;

    public abstract InputSnapshot PumpEvents();
    public abstract DisplayMode GetCurrentDisplayMode(int displayIndex = 0);
    public abstract void Dispose();

    protected void OnResized() => Resized?.Invoke();
    protected void OnMoved() => Moved?.Invoke();
    protected void OnFocusChanged() => FocusChanged?.Invoke();
    protected void OnScaleFactorChanged() => ScaleFactorChanged?.Invoke();
    protected void OnClosing() => Closing?.Invoke();
}
