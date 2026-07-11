using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ElementEngine;

internal sealed class X11GameWindow : GameWindow
{
    private const int NotifyInferior = 2;
    private const int TextBufferSize = 64;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly Dictionary<uint, Key> _pressedKeys = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];

    private nint _display;
    private nuint _window;
    private nuint _rootWindow;
    private int _screen;
    private X11Atoms _atoms;
    private nint _inputMethod;
    private nint _inputContext;
    private nuint _invisibleCursor;
    private X11ExclusiveDisplayMode _exclusiveDisplayMode;

    private bool _exists;
    private bool _focused;
    private bool _mapped;
    private bool _fullScreen;
    private bool _fullScreenObserved;
    private bool _ignoreFullscreenUntilRemoved;
    private bool _resizable = true;
    private bool _cursorVisible = true;
    private bool _disposed;
    private bool _closingRaised;
    private int _x;
    private int _y;
    private int _width;
    private int _height;
    private int _mouseX;
    private int _mouseY;
    private int _fullScreenTargetWidth;
    private int _fullScreenTargetHeight;
    private string _title;
    private WindowState _state;
    private WindowState _fullScreenState = WindowState.BorderlessFullScreen;

    public X11GameWindow(WindowCreateInfo createInfo)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("X11 windows can only be created on Linux.");
        if (createInfo.WindowWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window width must be greater than zero.");
        if (createInfo.WindowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window height must be greater than zero.");

        X11Native.EnsureThreadSupport();

        _display = X11Native.XOpenDisplay(0);
        if (_display == 0)
            throw new InvalidOperationException("XOpenDisplay failed. Check that DISPLAY points to a running X server.");

        try
        {
            _screen = X11Native.XDefaultScreen(_display);
            _rootWindow = X11Native.XRootWindow(_display, _screen);
            _x = createInfo.X;
            _y = createInfo.Y;
            _width = createInfo.WindowWidth;
            _height = createInfo.WindowHeight;
            _title = createInfo.WindowTitle ?? string.Empty;
            _state = WindowState.Normal;
            _atoms = new X11Atoms(_display);

            _window = X11Native.XCreateSimpleWindow(
                _display,
                _rootWindow,
                _x,
                _y,
                (uint)_width,
                (uint)_height,
                0,
                0,
                0);

            if (_window == 0)
                throw new InvalidOperationException("XCreateSimpleWindow failed.");

            _exists = true;

            X11Native.XSelectInput(
                _display,
                _window,
                X11Native.KeyPressMask |
                X11Native.KeyReleaseMask |
                X11Native.ButtonPressMask |
                X11Native.ButtonReleaseMask |
                X11Native.EnterWindowMask |
                X11Native.PointerMotionMask |
                X11Native.ExposureMask |
                X11Native.StructureNotifyMask |
                X11Native.FocusChangeMask |
                X11Native.PropertyChangeMask);

            var deleteWindow = _atoms.WmDeleteWindow;
            X11Native.XSetWMProtocols(_display, _window, ref deleteWindow, 1);
            SetTitle(_title);
            EnableDetectableKeyRepeat();
            CreateInputContext();

            if (createInfo.WindowInitialState == WindowState.Hidden)
            {
                _state = WindowState.Hidden;
            }
            else
            {
                X11Native.XMapWindow(_display, _window);
                _mapped = true;
                SetState(createInfo.WindowInitialState);
            }

            X11Native.XSync(_display, X11Native.False);
            RefreshGeometry(false);
            RefreshFocus(false, null);
            RefreshMousePosition();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public override NativeWindowKind NativeKind => NativeWindowKind.X11;

    public override nint NativeHandle => (nint)_window;

    public override nint NativeDisplayHandle => _display;

    public override bool Exists => _exists;

    public override bool Focused => _focused;

    public override int X => _x;

    public override int Y => _y;

    public override int Width => _width;

    public override int Height => _height;

    public override string Title
    {
        get => _title;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SetTitle(value ?? string.Empty);
        }
    }

    public override bool Resizable
    {
        get => _resizable;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_resizable == value)
                return;

            _resizable = value;
            ApplyResizeHints();
        }
    }

    public override bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cursorVisible == value)
                return;

            _cursorVisible = value;
            ApplyCursorVisibility();
        }
    }

    public override WindowState State
    {
        get => _state;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_state == value)
                return;

            SetState(value);
        }
    }

    public override InputSnapshot PumpEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var snapshot = new InputSnapshot
        {
            MousePosition = new Vector2(_mouseX, _mouseY),
        };

        if (_display == 0 || !_exists)
            return snapshot;

        while (_display != 0 && _exists && X11Native.XPending(_display) > 0)
        {
            X11Native.XNextEvent(_display, out var nativeEvent);

            var filtered = X11Native.XFilterEvent(ref nativeEvent, _window) != X11Native.False;
            if (filtered && nativeEvent.Type == X11Native.KeyPress)
            {
                ProcessKeyPress(ref nativeEvent.Key, snapshot, false);
                continue;
            }

            if (filtered && nativeEvent.Type == X11Native.KeyRelease)
            {
                ProcessKeyRelease(ref nativeEvent.Key, snapshot);
                continue;
            }

            if (filtered)
                continue;

            ProcessEvent(ref nativeEvent, snapshot);
        }

        SuppressScreenSaver();
        snapshot.MousePosition = new Vector2(_mouseX, _mouseY);
        return snapshot;
    }

    private long _nextScreenSaverResetTimestamp;

    private void SuppressScreenSaver()
    {
        // Game input does not reset X11's idle timer, so kick the screensaver
        // periodically while inhibition is active (the same approach SDL uses).
        if (AllowScreenSaver || _display == 0 || !_exists)
            return;

        var now = Stopwatch.GetTimestamp();
        if (now < _nextScreenSaverResetTimestamp)
            return;

        _nextScreenSaverResetTimestamp = now + Stopwatch.Frequency * 30;
        X11Native.XResetScreenSaver(_display);
    }

    public override DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var displayModes = X11Native.GetDisplayModes(_display);
        if (displayIndex < 0 || displayIndex >= displayModes.Count)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

        var mode = displayModes[displayIndex];
        return displayIndex == 0
            ? mode with { RefreshRate = X11Native.GetRefreshRate(_display, _screen) }
            : mode;
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _exists = false;
        DestroyInputContext();

        if (_display == 0)
            return;

        RestoreExclusiveDisplayMode(false);

        if (_invisibleCursor != 0)
        {
            X11Native.XFreeCursor(_display, _invisibleCursor);
            _invisibleCursor = 0;
        }

        if (_window != 0)
        {
            X11Native.XDestroyWindow(_display, _window);
            _window = 0;
        }

        X11Native.XCloseDisplay(_display);
        _display = 0;
        _rootWindow = 0;
        GC.SuppressFinalize(this);
    }

    private void EnableDetectableKeyRepeat()
    {
        try
        {
            X11Native.XkbSetDetectableAutoRepeat(_display, X11Native.True, out _);
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private void CreateInputContext()
    {
        try
        {
            if (!X11Native.TryConfigureLocale())
                return;

            _inputMethod = X11Native.XOpenIM(_display, 0, 0, 0);
            if (_inputMethod == 0)
                return;

            _inputContext = X11Native.XCreateIC(
                _inputMethod,
                "inputStyle",
                X11Native.XimPreeditNothing | X11Native.XimStatusNothing,
                "clientWindow",
                _window,
                "focusWindow",
                _window,
                0);

            if (_inputContext == 0)
            {
                X11Native.XCloseIM(_inputMethod);
                _inputMethod = 0;
            }
        }
        catch (EntryPointNotFoundException)
        {
            DestroyInputContext();
        }
    }

    private void DestroyInputContext()
    {
        if (_inputContext != 0)
        {
            X11Native.XDestroyIC(_inputContext);
            _inputContext = 0;
        }

        if (_inputMethod != 0)
        {
            X11Native.XCloseIM(_inputMethod);
            _inputMethod = 0;
        }
    }

    private void ProcessEvent(ref XEvent nativeEvent, InputSnapshot snapshot)
    {
        switch (nativeEvent.Type)
        {
            case X11Native.KeyPress:
                ProcessKeyPress(ref nativeEvent.Key, snapshot, true);
                break;
            case X11Native.KeyRelease:
                ProcessKeyRelease(ref nativeEvent.Key, snapshot);
                break;
            case X11Native.ButtonPress:
                ProcessButton(ref nativeEvent.Button, true, snapshot);
                break;
            case X11Native.ButtonRelease:
                ProcessButton(ref nativeEvent.Button, false, snapshot);
                break;
            case X11Native.MotionNotify:
                _mouseX = nativeEvent.Motion.X;
                _mouseY = nativeEvent.Motion.Y;
                break;
            case X11Native.EnterNotify:
                _mouseX = nativeEvent.Crossing.X;
                _mouseY = nativeEvent.Crossing.Y;
                break;
            case X11Native.FocusIn:
                if (nativeEvent.Focus.Detail != NotifyInferior)
                    SetFocused(true, snapshot);
                break;
            case X11Native.FocusOut:
                if (nativeEvent.Focus.Detail != NotifyInferior)
                    SetFocused(false, snapshot);
                break;
            case X11Native.ConfigureNotify:
                RefreshGeometry(true);
                break;
            case X11Native.MapNotify:
                _mapped = true;
                RefreshWindowState();
                break;
            case X11Native.UnmapNotify:
                _mapped = false;
                if (_state != WindowState.Hidden)
                    _state = WindowState.Minimized;
                break;
            case X11Native.PropertyNotify:
                if (nativeEvent.Property.Atom == _atoms.NetWmState)
                    RefreshWindowState();
                break;
            case X11Native.ClientMessage:
                ProcessClientMessage(ref nativeEvent.ClientMessage, snapshot);
                break;
            case X11Native.DestroyNotify:
                if (nativeEvent.DestroyWindow.Window == _window)
                    HandleNativeDestroy(snapshot);
                break;
            case X11Native.MappingNotify:
                X11Native.XRefreshKeyboardMapping(ref nativeEvent.Mapping);
                break;
        }
    }

    private void ProcessKeyPress(ref XKeyEvent keyEvent, InputSnapshot snapshot, bool appendText)
    {
        var key = TranslateKey(ref keyEvent);
        var repeat = _pressedKeys.ContainsKey(keyEvent.KeyCode);

        if (!repeat)
            _pressedKeys.Add(keyEvent.KeyCode, key);

        if (key != Key.Unknown)
            snapshot.KeyEvents.Add(new KeyEvent(key, true, repeat));

        if (appendText)
            AppendText(ref keyEvent, snapshot);
    }

    private void ProcessKeyRelease(ref XKeyEvent keyEvent, InputSnapshot snapshot)
    {
        if (IsRepeatRelease(ref keyEvent))
            return;

        if (!_pressedKeys.Remove(keyEvent.KeyCode, out var key))
            key = TranslateKey(ref keyEvent);

        if (key != Key.Unknown)
            snapshot.KeyEvents.Add(new KeyEvent(key, false));
    }

    private static Key TranslateKey(ref XKeyEvent keyEvent)
    {
        // Xorg keycodes are evdev scancodes offset by 8; translate positionally so keys
        // stay usable on non-Latin layouts, matching the Wayland backend. Fall back to
        // the layout's keysym for keycodes the positional table does not cover.
        var key = keyEvent.KeyCode >= 8 ? WaylandKeyMap.Translate(keyEvent.KeyCode - 8) : Key.Unknown;

        if (key == Key.Unknown)
            key = X11KeyMap.Translate(X11Native.XLookupKeysym(ref keyEvent, 0));

        return key;
    }

    private bool IsRepeatRelease(ref XKeyEvent keyEvent)
    {
        if (X11Native.XPending(_display) == 0)
            return false;

        X11Native.XPeekEvent(_display, out var nextEvent);
        return nextEvent.Type == X11Native.KeyPress &&
               nextEvent.Key.KeyCode == keyEvent.KeyCode &&
               nextEvent.Key.Time == keyEvent.Time;
    }

    private void AppendText(ref XKeyEvent keyEvent, InputSnapshot snapshot)
    {
        var buffer = new byte[TextBufferSize];
        int length;

        if (_inputContext != 0)
        {
            length = X11Native.Xutf8LookupString(
                _inputContext,
                ref keyEvent,
                buffer,
                buffer.Length,
                out _,
                out var status);

            if (status == X11Native.XBufferOverflow && length > buffer.Length)
            {
                buffer = new byte[length];
                length = X11Native.Xutf8LookupString(
                    _inputContext,
                    ref keyEvent,
                    buffer,
                    buffer.Length,
                    out _,
                    out status);
            }

            if (status is not (X11Native.XLookupChars or X11Native.XLookupBoth))
                return;
        }
        else
        {
            length = X11Native.XLookupString(
                ref keyEvent,
                buffer,
                buffer.Length,
                out _,
                0);
        }

        if (length <= 0)
            return;

        var text = DecodeText(buffer, Math.Min(length, buffer.Length));
        foreach (var character in text)
        {
            if (!char.IsControl(character))
                snapshot.KeyCharPresses.Add(character);
        }
    }

    private static string DecodeText(byte[] buffer, int length)
    {
        try
        {
            return StrictUtf8.GetString(buffer, 0, length);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(buffer, 0, length);
        }
    }

    private void ProcessButton(ref XButtonEvent buttonEvent, bool down, InputSnapshot snapshot)
    {
        _mouseX = buttonEvent.X;
        _mouseY = buttonEvent.Y;

        if (buttonEvent.Button is 4 or 5)
        {
            if (down)
                snapshot.WheelDelta += buttonEvent.Button == 4 ? 1f : -1f;

            return;
        }

        if (!TryTranslateMouseButton(buttonEvent.Button, out var mouseButton))
            return;

        if (down)
            _pressedMouseButtons.Add(mouseButton);
        else
            _pressedMouseButtons.Remove(mouseButton);

        snapshot.MouseEvents.Add(new MouseEvent(mouseButton, down));
    }

    private static bool TryTranslateMouseButton(uint nativeButton, out MouseButton mouseButton)
    {
        switch (nativeButton)
        {
            case 1:
                mouseButton = MouseButton.Left;
                return true;
            case 2:
                mouseButton = MouseButton.Middle;
                return true;
            case 3:
                mouseButton = MouseButton.Right;
                return true;
            case >= 8 and <= 15:
                mouseButton = (MouseButton)((int)MouseButton.Button1 + nativeButton - 8);
                return true;
            default:
                mouseButton = default;
                return false;
        }
    }

    private void SetFocused(bool focused, InputSnapshot snapshot)
    {
        if (_focused == focused)
            return;

        _focused = focused;

        if (_inputContext != 0)
        {
            if (focused)
                X11Native.XSetICFocus(_inputContext);
            else
                X11Native.XUnsetICFocus(_inputContext);
        }

        if (!focused)
            ReleasePressedInputs(snapshot);

        OnFocusChanged();
    }

    private void ReleasePressedInputs(InputSnapshot snapshot)
    {
        foreach (var key in _pressedKeys.Values)
        {
            if (key != Key.Unknown)
                snapshot.KeyEvents.Add(new KeyEvent(key, false));
        }

        _pressedKeys.Clear();

        foreach (var mouseButton in _pressedMouseButtons)
            snapshot.MouseEvents.Add(new MouseEvent(mouseButton, false));

        _pressedMouseButtons.Clear();
    }

    private void ProcessClientMessage(ref XClientMessageEvent clientMessage, InputSnapshot snapshot)
    {
        if (clientMessage.MessageType != _atoms.WmProtocols ||
            (nuint)clientMessage.Data.Long0 != _atoms.WmDeleteWindow)
        {
            return;
        }

        CloseFromWindowManager(snapshot);
    }

    private void CloseFromWindowManager(InputSnapshot snapshot)
    {
        var display = _display;
        var window = _window;

        try
        {
            RaiseClosing();
        }
        finally
        {
            if (_display == display && _window == window && display != 0 && window != 0)
            {
                ReleasePressedInputs(snapshot);
                DestroyInputContext();
                RestoreExclusiveDisplayMode(false);
                _exists = false;
                _mapped = false;
                _window = 0;
                X11Native.XDestroyWindow(display, window);
                X11Native.XFlush(display);
            }
        }
    }

    private void HandleNativeDestroy(InputSnapshot snapshot)
    {
        try
        {
            RaiseClosing();
        }
        finally
        {
            ReleasePressedInputs(snapshot);
            DestroyInputContext();
            RestoreExclusiveDisplayMode(false);
            _exists = false;
            _mapped = false;
            _window = 0;
        }
    }

    private void RaiseClosing()
    {
        if (_closingRaised)
            return;

        _closingRaised = true;
        OnClosing();
    }

    private void SetTitle(string title)
    {
        _title = title;
        if (_display == 0 || _window == 0)
            return;

        X11Native.XStoreName(_display, _window, title);
        var utf8Title = Encoding.UTF8.GetBytes(title);
        X11Native.XChangeProperty(
            _display,
            _window,
            _atoms.NetWmName,
            _atoms.Utf8String,
            8,
            X11Native.PropModeReplace,
            utf8Title,
            utf8Title.Length);
        X11Native.XFlush(_display);
    }

    private void ApplyResizeHints()
    {
        if (_display == 0 || _window == 0)
            return;

        var hints = new XSizeHints();
        if (!_resizable)
        {
            hints.Flags = X11Native.PMinSize | X11Native.PMaxSize;
            hints.MinWidth = _width;
            hints.MinHeight = _height;
            hints.MaxWidth = _width;
            hints.MaxHeight = _height;
        }

        X11Native.XSetWMNormalHints(_display, _window, ref hints);
        X11Native.XFlush(_display);
    }

    private void ApplyCursorVisibility()
    {
        if (_display == 0 || _window == 0)
            return;

        if (_cursorVisible)
        {
            X11Native.XUndefineCursor(_display, _window);
        }
        else
        {
            EnsureInvisibleCursor();
            X11Native.XDefineCursor(_display, _window, _invisibleCursor);
        }

        X11Native.XFlush(_display);
    }

    private void EnsureInvisibleCursor()
    {
        if (_invisibleCursor != 0)
            return;

        var emptyBitmap = X11Native.XCreateBitmapFromData(_display, _window, [0], 1, 1);
        if (emptyBitmap == 0)
            throw new InvalidOperationException("XCreateBitmapFromData failed while hiding the cursor.");

        try
        {
            var black = new XColor();
            _invisibleCursor = X11Native.XCreatePixmapCursor(
                _display,
                emptyBitmap,
                emptyBitmap,
                ref black,
                ref black,
                0,
                0);

            if (_invisibleCursor == 0)
                throw new InvalidOperationException("XCreatePixmapCursor failed while hiding the cursor.");
        }
        finally
        {
            X11Native.XFreePixmap(_display, emptyBitmap);
        }
    }

    private void SetState(WindowState state)
    {
        if (_display == 0 || _window == 0)
            return;

        if (state is WindowState.FullScreen or WindowState.BorderlessFullScreen)
        {
            SetFullScreenState(state);
            X11Native.XFlush(_display);
            return;
        }

        LeaveFullScreen();

        switch (state)
        {
            case WindowState.Hidden:
                X11Native.XUnmapWindow(_display, _window);
                _mapped = false;
                break;
            case WindowState.Normal:
                EnsureMapped();
                SendNetWmState(
                    X11Native.NetWmStateRemove,
                    _atoms.NetWmStateMaximizedHorz,
                    _atoms.NetWmStateMaximizedVert);
                break;
            case WindowState.Maximized:
                EnsureMapped();
                SendNetWmState(
                    X11Native.NetWmStateAdd,
                    _atoms.NetWmStateMaximizedHorz,
                    _atoms.NetWmStateMaximizedVert);
                break;
            case WindowState.Minimized:
                EnsureMapped();
                X11Native.XIconifyWindow(_display, _window, _screen);
                _mapped = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

        _state = state;
        X11Native.XFlush(_display);
    }

    private void SetFullScreenState(WindowState state)
    {
        if (!_fullScreen)
        {
            RefreshGeometry(false);
            _fullScreenTargetWidth = Math.Max(1, _width);
            _fullScreenTargetHeight = Math.Max(1, _height);
            EnsureMapped();
            SendNetWmState(
                X11Native.NetWmStateRemove,
                _atoms.NetWmStateMaximizedHorz,
                _atoms.NetWmStateMaximizedVert);

            ApplyExclusiveModeTransition(state);

            try
            {
                SendNetWmState(X11Native.NetWmStateAdd, _atoms.NetWmStateFullscreen);
            }
            catch
            {
                RestoreExclusiveDisplayMode(false);
                throw;
            }

            _fullScreen = true;
            _fullScreenObserved = false;
        }
        else
        {
            ApplyExclusiveModeTransition(state);
        }

        EnsureMapped();
        _ignoreFullscreenUntilRemoved = false;
        _fullScreenState = state;
        _state = state;
    }

    private void LeaveFullScreen()
    {
        var exclusiveModeActive = _exclusiveDisplayMode is not null;
        ApplyExclusiveModeTransition(WindowState.Normal);

        if (exclusiveModeActive)
        {
            _fullScreenState = X11ExclusiveModeTransition.GetStateAfterRestore(_fullScreenState);
            if (_fullScreen)
                _state = _fullScreenState;
        }

        if (_fullScreen)
        {
            SendNetWmState(X11Native.NetWmStateRemove, _atoms.NetWmStateFullscreen);
            _ignoreFullscreenUntilRemoved = true;
        }

        _fullScreen = false;
        _fullScreenObserved = false;
        _fullScreenTargetWidth = 0;
        _fullScreenTargetHeight = 0;
    }

    private void EnterExclusiveDisplayMode()
    {
        _exclusiveDisplayMode = X11ExclusiveDisplayMode.Enter(
            _display,
            _rootWindow,
            _x,
            _y,
            _width,
            _height,
            _fullScreenTargetWidth,
            _fullScreenTargetHeight);
    }

    private void ApplyExclusiveModeTransition(WindowState requestedState)
    {
        switch (X11ExclusiveModeTransition.GetAction(_exclusiveDisplayMode is not null, requestedState))
        {
            case X11ExclusiveModeAction.Enter:
                EnterExclusiveDisplayMode();
                break;
            case X11ExclusiveModeAction.Restore:
                RestoreExclusiveDisplayMode(true);
                break;
        }
    }

    private void RestoreExclusiveDisplayMode(bool throwOnFailure)
    {
        if (_exclusiveDisplayMode is null)
            return;

        if (_exclusiveDisplayMode.TryRestore(out var status))
        {
            _exclusiveDisplayMode = null;
            return;
        }

        if (throwOnFailure)
        {
            throw new InvalidOperationException(
                $"RandR failed to restore the original X11 display mode (status {status}).");
        }

        Logging.Error(
            "RandR failed to restore the original X11 display mode before closing the display (status {status}).",
            status);
    }

    private void EnsureMapped()
    {
        if (_mapped)
        {
            X11Native.XRaiseWindow(_display, _window);
            return;
        }

        X11Native.XMapRaised(_display, _window);
        _mapped = true;
    }

    private void SendNetWmState(int action, nuint firstAtom, nuint secondAtom = 0)
    {
        if (firstAtom == 0 || _atoms.NetWmState == 0)
            throw new InvalidOperationException("The X11 window manager state atoms are unavailable.");

        var clientMessage = new XClientMessageEvent
        {
            Type = X11Native.ClientMessage,
            SendEvent = X11Native.True,
            Display = _display,
            Window = _window,
            MessageType = _atoms.NetWmState,
            Format = 32,
            Data = new XClientMessageData
            {
                Long0 = action,
                Long1 = (nint)firstAtom,
                Long2 = (nint)secondAtom,
                Long3 = 1,
            },
        };

        var nativeEvent = new XEvent
        {
            ClientMessage = clientMessage,
        };

        if (X11Native.XSendEvent(
                _display,
                _rootWindow,
                X11Native.False,
                X11Native.SubstructureRedirectMask | X11Native.SubstructureNotifyMask,
                ref nativeEvent) == X11Native.False)
        {
            throw new InvalidOperationException("The X11 window manager rejected the requested window state change.");
        }
    }

    private void RefreshWindowState()
    {
        if (!_mapped || !TryGetNetWmState(out var hasFullscreen, out var hasMaximized, out var hasHidden))
            return;

        if (hasFullscreen)
        {
            if (_ignoreFullscreenUntilRemoved)
                return;

            if (!_fullScreen)
            {
                _fullScreen = true;
                _fullScreenState = WindowState.BorderlessFullScreen;
                _fullScreenTargetWidth = Math.Max(1, _width);
                _fullScreenTargetHeight = Math.Max(1, _height);
            }

            _fullScreenObserved = true;
            _state = _fullScreenState;
        }
        else
        {
            _ignoreFullscreenUntilRemoved = false;

            if (_fullScreen && !_fullScreenObserved)
                return;

            if (_fullScreen)
            {
                RestoreExclusiveDisplayMode(true);
                _fullScreen = false;
                _fullScreenObserved = false;
                _fullScreenTargetWidth = 0;
                _fullScreenTargetHeight = 0;
            }

            if (hasMaximized)
                _state = WindowState.Maximized;
            else if (hasHidden)
                _state = WindowState.Minimized;
            else if (_state != WindowState.Hidden)
                _state = WindowState.Normal;
        }
    }

    private bool TryGetNetWmState(out bool hasFullscreen, out bool hasMaximized, out bool hasHidden)
    {
        hasFullscreen = false;
        hasMaximized = false;
        hasHidden = false;

        var status = X11Native.XGetWindowProperty(
            _display,
            _window,
            _atoms.NetWmState,
            0,
            1024,
            X11Native.False,
            X11Native.AnyPropertyType,
            out _,
            out var actualFormat,
            out var itemCount,
            out _,
            out var propertyData);

        if (status != X11Native.Success)
            return false;

        try
        {
            if (actualFormat != 32 || propertyData == 0)
                return itemCount == 0;

            var hasMaximizedHorizontal = false;
            var hasMaximizedVertical = false;

            for (nuint index = 0; index < itemCount; index++)
            {
                var atom = (nuint)Marshal.ReadIntPtr(propertyData, checked((int)(index * (nuint)nint.Size)));
                hasFullscreen |= atom == _atoms.NetWmStateFullscreen;
                hasMaximizedHorizontal |= atom == _atoms.NetWmStateMaximizedHorz;
                hasMaximizedVertical |= atom == _atoms.NetWmStateMaximizedVert;
                hasHidden |= atom == _atoms.NetWmStateHidden;
            }

            hasMaximized = hasMaximizedHorizontal && hasMaximizedVertical;
            return true;
        }
        finally
        {
            if (propertyData != 0)
                X11Native.XFree(propertyData);
        }
    }

    private void RefreshGeometry(bool notify)
    {
        if (_display == 0 || _window == 0 ||
            X11Native.XGetWindowAttributes(_display, _window, out var attributes) == X11Native.False)
        {
            return;
        }

        var x = attributes.X;
        var y = attributes.Y;
        if (X11Native.XTranslateCoordinates(
                _display,
                _window,
                _rootWindow,
                0,
                0,
                out var rootX,
                out var rootY,
                out _) != X11Native.False)
        {
            x = rootX;
            y = rootY;
        }

        var moved = x != _x || y != _y;
        var resized = attributes.Width != _width || attributes.Height != _height;
        _x = x;
        _y = y;
        _width = attributes.Width;
        _height = attributes.Height;

        if (!notify)
            return;

        if (moved)
            OnMoved();

        if (resized)
            OnResized();
    }

    private void RefreshFocus(bool notify, InputSnapshot snapshot)
    {
        if (_display == 0 || _window == 0)
            return;

        X11Native.XGetInputFocus(_display, out var focus, out _);
        var focused = focus == _window;

        if (notify && snapshot is not null)
            SetFocused(focused, snapshot);
        else
            _focused = focused;

        if (_focused && _inputContext != 0)
            X11Native.XSetICFocus(_inputContext);
    }

    private void RefreshMousePosition()
    {
        if (_display == 0 || _window == 0)
            return;

        if (X11Native.XQueryPointer(
                _display,
                _window,
                out _,
                out _,
                out _,
                out _,
                out var windowX,
                out var windowY,
                out _) == X11Native.False)
        {
            return;
        }

        _mouseX = windowX;
        _mouseY = windowY;
    }

    private readonly struct X11Atoms
    {
        public X11Atoms(nint display)
        {
            WmProtocols = Intern(display, "WM_PROTOCOLS");
            WmDeleteWindow = Intern(display, "WM_DELETE_WINDOW");
            Utf8String = Intern(display, "UTF8_STRING");
            NetWmName = Intern(display, "_NET_WM_NAME");
            NetWmState = Intern(display, "_NET_WM_STATE");
            NetWmStateFullscreen = Intern(display, "_NET_WM_STATE_FULLSCREEN");
            NetWmStateMaximizedHorz = Intern(display, "_NET_WM_STATE_MAXIMIZED_HORZ");
            NetWmStateMaximizedVert = Intern(display, "_NET_WM_STATE_MAXIMIZED_VERT");
            NetWmStateHidden = Intern(display, "_NET_WM_STATE_HIDDEN");
        }

        public nuint WmProtocols { get; }
        public nuint WmDeleteWindow { get; }
        public nuint Utf8String { get; }
        public nuint NetWmName { get; }
        public nuint NetWmState { get; }
        public nuint NetWmStateFullscreen { get; }
        public nuint NetWmStateMaximizedHorz { get; }
        public nuint NetWmStateMaximizedVert { get; }
        public nuint NetWmStateHidden { get; }

        private static nuint Intern(nint display, string name) =>
            X11Native.XInternAtom(display, name, X11Native.False);
    }
}
