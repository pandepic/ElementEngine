using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ElementEngine;

internal sealed unsafe class WaylandGameWindow : GameWindow
{
    private const uint SeatCapabilityPointer = 1;
    private const uint SeatCapabilityKeyboard = 2;
    private const uint OutputModeCurrent = 1;
    private const uint PointerButtonPressed = 1;
    private const uint PointerAxisVertical = 0;
    private const uint KeyboardKeyPressed = 1;
    private const uint KeyboardKeymapFormatXkbV1 = 1;
    private const uint ServerSideDecorationMode = 2;
    private const uint WmCapabilityMaximize = 2;
    private const uint WmCapabilityFullScreen = 3;
    private const uint WmCapabilityMinimize = 4;
    private const int CursorBaseSize = 24;
    private const int FixedScale = 256;
    private const int MaximumRepeatedEventsPerPump = 32;

    private readonly Dictionary<uint, WaylandOutput> _outputsByName = [];
    private readonly Dictionary<nint, WaylandOutput> _outputsByProxy = [];
    private readonly HashSet<nint> _enteredOutputs = [];
    private readonly WaylandSeatRegistry _seatRegistry = new();
    private readonly WaylandThreadGuard _threadGuard = new();
    private readonly Dictionary<uint, Key> _pressedKeys = [];
    private readonly HashSet<uint> _inheritedPressedKeys = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];

    private GCHandle _selfHandle;
    private nint _callbackData;
    private nint _display;
    private nint _registry;
    private nint _compositor;
    private nint _surface;
    private nint _wmBase;
    private nint _xdgSurface;
    private nint _toplevel;
    private nint _decorationManager;
    private nint _idleInhibitManager;
    private nint _idleInhibitor;
    private nint _decoration;
    private nint _sharedMemory;
    private nint _seat;
    private nint _pointer;
    private nint _keyboard;
    private nint _cursorSurface;
    private nint _cursorTheme;
    private nint _xkbContext;
    private nint _xkbKeymap;
    private nint _xkbState;
    private nint _composeTable;
    private nint _composeState;

    private InputSnapshot _activeSnapshot;
    private Exception _callbackException;
    private uint[] _pendingToplevelStates = [];
    private uint[] _wmCapabilities = [];
    private int _pendingWidth;
    private int _pendingHeight;
    private uint _pointerEnterSerial;
    private uint _seatCapabilities;
    private uint _repeatKeyCode;
    private Key _repeatKey;
    private long _nextRepeatTimestamp;
    private long _repeatIntervalTicks;
    private int _repeatDelayMilliseconds = 400;
    private int _repeatRate = 25;
    private int _width;
    private int _height;
    private int _bufferScale = 1;
    private int _mouseX;
    private int _mouseY;
    private float _pendingAxisDelta;
    private float? _pendingDiscreteAxisDelta;
    private string _title;
    private bool _exists;
    private bool _focused;
    private bool _resizable = true;
    private bool _cursorVisible = true;
    private bool _pointerInside;
    private bool _configured;
    private bool _mapped;
    private bool _disposed;
    private bool _closingRaised;
    private bool _disposeRequested;
    private bool _wmCapabilitiesReceived;
    private int _eventDispatchDepth;
    private WindowState _state;
    private WindowState _requestedState;

    public WaylandGameWindow(WindowCreateInfo createInfo)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("Wayland windows can only be created on Linux.");
        if (createInfo.WindowWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window width must be greater than zero.");
        if (createInfo.WindowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window height must be greater than zero.");

        _width = createInfo.WindowWidth;
        _height = createInfo.WindowHeight;
        _title = createInfo.WindowTitle ?? string.Empty;
        _requestedState = createInfo.WindowInitialState;
        _state = createInfo.WindowInitialState;

        WaylandNative.EnsureRuntimeLibraries();
        _display = WaylandNative.DisplayConnect(null);
        if (_display == 0)
        {
            throw new InvalidOperationException(
                "wl_display_connect failed. Check WAYLAND_DISPLAY and XDG_RUNTIME_DIR.");
        }

        _selfHandle = GCHandle.Alloc(this);
        _callbackData = GCHandle.ToIntPtr(_selfHandle);

        try
        {
            _registry = WaylandNative.ProxyMarshalConstructor(
                _display,
                1,
                WaylandNative.Interfaces.Registry,
                WaylandNative.ProxyGetVersion(_display),
                0,
                0);
            EnsureProxy(_registry, "create the Wayland registry");
            AddListener(_registry, WaylandListeners.Registry, "listen to the Wayland registry");

            Roundtrip();
            Roundtrip();

            if (_compositor == 0)
                throw new NotSupportedException("The Wayland compositor does not advertise wl_compositor.");
            if (_wmBase == 0)
                throw new NotSupportedException("The Wayland compositor does not advertise xdg_wm_base.");
            if (_sharedMemory == 0)
                throw new NotSupportedException("The Wayland compositor does not advertise wl_shm for cursors.");

            CreateWindowRole();
            _exists = true;
            UpdateIdleInhibitor();

            if (_requestedState == WindowState.Hidden)
            {
                _state = WindowState.Hidden;
            }
            else
            {
                ApplyStateRequests(_requestedState);
                MapAndAwaitConfigure();
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public override NativeWindowKind NativeKind => NativeWindowKind.Wayland;

    public override nint NativeHandle => _surface;

    public override nint NativeDisplayHandle => _display;

    public override bool Exists => _exists;

    public override bool Focused => _focused;

    public override int X => 0;

    public override int Y => 0;

    public override int Width => _width;

    public override int Height => _height;

    public override int FramebufferWidth => checked(_width * _bufferScale);

    public override int FramebufferHeight => checked(_height * _bufferScale);

    public override float ScaleFactor => _bufferScale;

    public override string Title
    {
        get => _title;
        set
        {
            ThrowIfUnavailable();
            _title = value ?? string.Empty;
            if (_toplevel != 0)
                WaylandNative.MarshalString(_toplevel, 2, _title);
            Flush();
        }
    }

    public override bool Resizable
    {
        get => _resizable;
        set
        {
            ThrowIfUnavailable();
            if (_resizable == value)
                return;

            _resizable = value;
            ApplyResizeConstraints();
            Flush();
        }
    }

    public override bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            ThrowIfUnavailable();
            if (_cursorVisible == value)
                return;

            _cursorVisible = value;
            ApplyCursorVisibility();
            Flush();
        }
    }

    public override WindowState State
    {
        get => _state;
        set
        {
            ThrowIfUnavailable();
            if (_requestedState == value && _state == value)
                return;

            SetState(value);
        }
    }

    public override InputSnapshot PumpEvents()
    {
        ThrowIfUnavailable();

        var snapshot = new InputSnapshot
        {
            MousePosition = new Vector2(_mouseX, _mouseY),
        };

        if (_display == 0 || !_exists)
            return snapshot;

        _activeSnapshot = snapshot;
        BeginEventDispatch();
        try
        {
            DispatchAvailableEvents();
            AppendKeyRepeats(snapshot);
            ThrowCallbackException();
            snapshot.MousePosition = new Vector2(_mouseX, _mouseY);
            return snapshot;
        }
        finally
        {
            _activeSnapshot = null;
            EndEventDispatch();
        }
    }

    public override DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
    {
        ThrowIfUnavailable();
        if (displayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(displayIndex));

        var outputs = _outputsByName.Values
            .OrderBy(static output => output.GlobalName)
            .ToArray();
        if (displayIndex >= outputs.Length)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

        var output = outputs[displayIndex];
        if (output.Width <= 0 || output.Height <= 0)
            throw new InvalidOperationException($"Wayland output {displayIndex} has no current display mode.");

        return new DisplayMode(
            output.Width,
            output.Height,
            WaylandWindowSemantics.ConvertRefreshRate(output.RefreshRateInMillihertz));
    }

    public override void Dispose()
    {
        _threadGuard.EnsureCurrentThread();

        if (_disposed || _disposeRequested)
            return;

        if (_eventDispatchDepth > 0)
        {
            _disposeRequested = true;
            _exists = false;
            return;
        }

        DisposeCore();
    }

    private void DisposeCore()
    {
        if (_disposed)
            return;

        _disposeRequested = false;
        _disposed = true;
        _exists = false;

        DestroySeat();
        _seatRegistry.Clear();
        DestroyCursorResources();

        foreach (var output in _outputsByName.Values)
            DestroyVersionedProxy(output.Proxy, 0, 3);
        _outputsByName.Clear();
        _outputsByProxy.Clear();
        _enteredOutputs.Clear();

        DestroyProtocolProxy(ref _idleInhibitor, 0);
        DestroyProtocolProxy(ref _idleInhibitManager, 0);
        DestroyProtocolProxy(ref _decoration, 0);
        DestroyProtocolProxy(ref _decorationManager, 0);
        DestroyProtocolProxy(ref _toplevel, 0);
        DestroyProtocolProxy(ref _xdgSurface, 0);

        if (_surface != 0)
        {
            DestroyProtocolProxy(ref _surface, 0);
        }

        if (_sharedMemory != 0)
        {
            WaylandNative.ProxyDestroy(_sharedMemory);
            _sharedMemory = 0;
        }

        DestroyProtocolProxy(ref _wmBase, 0);

        if (_compositor != 0)
        {
            WaylandNative.ProxyDestroy(_compositor);
            _compositor = 0;
        }

        if (_registry != 0)
        {
            WaylandNative.ProxyDestroy(_registry);
            _registry = 0;
        }

        if (_display != 0)
        {
            WaylandNative.DisplayDisconnect(_display);
            _display = 0;
        }

        DestroyXkbState();

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
        _callbackData = 0;

        GC.SuppressFinalize(this);
    }

    private void CreateWindowRole()
    {
        _surface = WaylandNative.ProxyMarshalConstructor(
            _compositor,
            0,
            WaylandNative.Interfaces.Surface,
            WaylandNative.ProxyGetVersion(_compositor),
            0,
            0);
        EnsureProxy(_surface, "create a Wayland surface");
        AddListener(_surface, WaylandListeners.Surface, "listen to the Wayland surface");

        _xdgSurface = WaylandNative.ProxyMarshalConstructorWithObject(
            _wmBase,
            2,
            WaylandProtocol.Interfaces.XdgSurface,
            WaylandNative.ProxyGetVersion(_wmBase),
            0,
            0,
            _surface);
        EnsureProxy(_xdgSurface, "create an xdg_surface");
        AddListener(_xdgSurface, WaylandListeners.XdgSurface, "listen to the xdg_surface");

        _toplevel = WaylandNative.ProxyMarshalConstructor(
            _xdgSurface,
            1,
            WaylandProtocol.Interfaces.XdgToplevel,
            WaylandNative.ProxyGetVersion(_xdgSurface),
            0,
            0);
        EnsureProxy(_toplevel, "create an xdg_toplevel");
        AddListener(_toplevel, WaylandListeners.Toplevel, "listen to the xdg_toplevel");

        ApplyWindowMetadata();
        CreateDecoration();
        UpdateSeatDevices();
    }

    private void ApplyWindowMetadata()
    {
        WaylandNative.MarshalString(_toplevel, 2, _title);
        WaylandNative.MarshalString(_toplevel, 3, "elementengine");
        ApplyResizeConstraints();

        if (_decoration != 0)
        {
            WaylandNative.ProxyMarshalUInt32(
                _decoration,
                1,
                0,
                WaylandNative.ProxyGetVersion(_decoration),
                0,
                ServerSideDecorationMode);
        }
    }

    protected override void OnAllowScreenSaverChanged() => UpdateIdleInhibitor();

    private void UpdateIdleInhibitor()
    {
        if (AllowScreenSaver || !_exists || _surface == 0 || _idleInhibitManager == 0)
        {
            DestroyProtocolProxy(ref _idleInhibitor, 0);
            return;
        }

        if (_idleInhibitor != 0)
            return;

        _idleInhibitor = WaylandNative.ProxyMarshalConstructorWithObject(
            _idleInhibitManager,
            1,
            WaylandProtocol.Interfaces.IdleInhibitor,
            WaylandNative.ProxyGetVersion(_idleInhibitManager),
            0,
            0,
            _surface);
        EnsureProxy(_idleInhibitor, "create a Wayland idle inhibitor");
    }

    private void CreateDecoration()
    {
        if (_decorationManager == 0 || _toplevel == 0 || _decoration != 0)
            return;

        _decoration = WaylandNative.ProxyMarshalConstructorWithObject(
            _decorationManager,
            1,
            WaylandProtocol.Interfaces.ToplevelDecoration,
            WaylandNative.ProxyGetVersion(_decorationManager),
            0,
            0,
            _toplevel);
        EnsureProxy(_decoration, "create an xdg toplevel decoration");
        AddListener(_decoration, WaylandListeners.Decoration, "listen to the xdg toplevel decoration");
        WaylandNative.ProxyMarshalUInt32(
            _decoration,
            1,
            0,
            WaylandNative.ProxyGetVersion(_decoration),
            0,
            ServerSideDecorationMode);
    }

    private void SetState(WindowState state)
    {
        if (_requestedState == WindowState.Minimized && state is not (WindowState.Minimized or WindowState.Hidden))
        {
            throw new NotSupportedException(
                "Wayland does not permit clients to programmatically restore a minimized window. " +
                "The compositor or user must activate it first.");
        }

        if (state == WindowState.Hidden)
        {
            Unmap();
            _requestedState = WindowState.Hidden;
            _state = WindowState.Hidden;
            return;
        }

        var wasHidden = _requestedState == WindowState.Hidden || !_mapped;
        _requestedState = state;
        ApplyStateRequests(state);

        if (wasHidden)
            MapAndAwaitConfigure();
        else
            Flush();

        if (state == WindowState.Minimized)
            _state = WindowState.Minimized;
    }

    private void ApplyStateRequests(WindowState state)
    {
        if (_toplevel == 0)
            return;

        switch (state)
        {
            case WindowState.Normal:
                MarshalToplevelRequest(10);
                MarshalToplevelRequest(12);
                break;
            case WindowState.Maximized:
                RequireWmCapability(WmCapabilityMaximize, "maximize");
                MarshalToplevelRequest(12);
                MarshalToplevelRequest(9);
                break;
            case WindowState.Minimized:
                RequireWmCapability(WmCapabilityMinimize, "minimize");
                MarshalToplevelRequest(13);
                break;
            case WindowState.FullScreen:
            case WindowState.BorderlessFullScreen:
                RequireWmCapability(WmCapabilityFullScreen, "enter fullscreen");
                MarshalToplevelRequest(10);
                WaylandNative.ProxyMarshalObject(
                    _toplevel,
                    11,
                    0,
                    WaylandNative.ProxyGetVersion(_toplevel),
                    0,
                    0);
                break;
            case WindowState.Hidden:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }
    }

    private void RequireWmCapability(uint capability, string operation)
    {
        if (_wmCapabilitiesReceived && !_wmCapabilities.Contains(capability))
        {
            throw new NotSupportedException(
                $"The Wayland compositor reports that it cannot {operation} this window.");
        }
    }

    private void MarshalToplevelRequest(uint opcode)
    {
        WaylandNative.ProxyMarshalNoArguments(
            _toplevel,
            opcode,
            0,
            WaylandNative.ProxyGetVersion(_toplevel),
            0);
    }

    private void MapAndAwaitConfigure()
    {
        _configured = false;
        ApplyWindowMetadata();
        WaylandNative.ProxyMarshalNoArguments(
            _surface,
            6,
            0,
            WaylandNative.ProxyGetVersion(_surface),
            0);
        _mapped = true;

        if (_eventDispatchDepth > 0)
        {
            Flush();
            return;
        }

        for (var attempt = 0;
             attempt < 3 && !_configured && !_disposeRequested && !_disposed;
             attempt++)
        {
            Roundtrip();
        }

        if (_disposeRequested || _disposed)
            return;

        if (!_configured)
            throw new InvalidOperationException("The Wayland compositor did not configure the xdg_surface.");
    }

    private void Unmap()
    {
        if (!_mapped || _surface == 0)
            return;

        WaylandNative.ProxyMarshalSurfaceAttach(
            _surface,
            1,
            0,
            WaylandNative.ProxyGetVersion(_surface),
            0,
            0,
            0,
            0);
        WaylandNative.ProxyMarshalNoArguments(
            _surface,
            6,
            0,
            WaylandNative.ProxyGetVersion(_surface),
            0);
        _mapped = false;
        _configured = false;
        Flush();
    }

    private void ApplyResizeConstraints()
    {
        if (_toplevel == 0)
            return;

        var width = _resizable ? 0 : _width;
        var height = _resizable ? 0 : _height;
        WaylandNative.ProxyMarshalTwoInt32(
            _toplevel,
            7,
            0,
            WaylandNative.ProxyGetVersion(_toplevel),
            0,
            width,
            height);
        WaylandNative.ProxyMarshalTwoInt32(
            _toplevel,
            8,
            0,
            WaylandNative.ProxyGetVersion(_toplevel),
            0,
            width,
            height);
    }

    private void DispatchAvailableEvents()
    {
        var pendingResult = WaylandNative.DisplayDispatchPending(_display);
        CheckDisplayResult(pendingResult, "dispatch pending Wayland events");

        while (true)
        {
            while (WaylandNative.DisplayPrepareRead(_display) != 0)
            {
                pendingResult = WaylandNative.DisplayDispatchPending(_display);
                CheckDisplayResult(pendingResult, "dispatch pending Wayland events");
            }

            Flush();

            var descriptor = new WaylandNative.PollFileDescriptor
            {
                FileDescriptor = WaylandNative.DisplayGetFileDescriptor(_display),
                Events = WaylandNative.PollIn,
            };
            int pollResult;
            do
            {
                pollResult = WaylandNative.Poll(&descriptor, 1, 0);
            }
            while (pollResult < 0 && Marshal.GetLastPInvokeError() == WaylandNative.ErrorInterrupted);

            if (pollResult < 0)
            {
                WaylandNative.DisplayCancelRead(_display);
                throw new InvalidOperationException(
                    $"poll failed while reading Wayland events (errno {Marshal.GetLastPInvokeError()}).");
            }

            if (pollResult == 0)
            {
                WaylandNative.DisplayCancelRead(_display);
                break;
            }

            if ((descriptor.ReturnedEvents & (WaylandNative.PollError | WaylandNative.PollHangUp)) != 0)
            {
                WaylandNative.DisplayCancelRead(_display);
                throw new InvalidOperationException("The Wayland compositor connection closed unexpectedly.");
            }

            if ((descriptor.ReturnedEvents & WaylandNative.PollIn) == 0)
            {
                WaylandNative.DisplayCancelRead(_display);
                break;
            }

            CheckDisplayResult(WaylandNative.DisplayReadEvents(_display), "read Wayland events");
            CheckDisplayResult(
                WaylandNative.DisplayDispatchPending(_display),
                "dispatch Wayland events");
            ThrowCallbackException();
        }
    }

    private void Roundtrip()
    {
        BeginEventDispatch();
        try
        {
            CheckDisplayResult(WaylandNative.DisplayRoundtrip(_display), "perform a Wayland roundtrip");
            ThrowCallbackException();
        }
        finally
        {
            EndEventDispatch();
        }
    }

    private void BeginEventDispatch() => _eventDispatchDepth++;

    private void EndEventDispatch()
    {
        _eventDispatchDepth--;
        if (_disposeRequested && _eventDispatchDepth == 0)
            DisposeCore();
    }

    private void ThrowIfUnavailable()
    {
        _threadGuard.EnsureCurrentThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void Flush()
    {
        if (_display == 0)
            return;

        if (WaylandNative.DisplayFlush(_display) >= 0)
            return;

        var error = Marshal.GetLastPInvokeError();
        if (error != WaylandNative.ErrorAgain)
            throw new InvalidOperationException($"wl_display_flush failed (errno {error}).");
    }

    private void CheckDisplayResult(int result, string operation)
    {
        if (result >= 0)
            return;

        var error = _display == 0 ? 0 : WaylandNative.DisplayGetError(_display);
        throw new InvalidOperationException($"Failed to {operation} (Wayland error {error}).");
    }

    private static void EnsureProxy(nint proxy, string operation)
    {
        if (proxy == 0)
            throw new InvalidOperationException($"Failed to {operation}.");
    }

    private void AddListener(nint proxy, nint listener, string operation)
    {
        if (WaylandNative.ProxyAddListener(proxy, listener, _callbackData) != 0)
            throw new InvalidOperationException($"Failed to {operation}.");
    }

    private void CaptureCallbackException(Exception exception)
    {
        _callbackException ??= exception;
    }

    private void ThrowCallbackException()
    {
        if (_callbackException is null)
            return;

        var exception = _callbackException;
        _callbackException = null;
        throw new InvalidOperationException("A Wayland event callback failed.", exception);
    }

    private void ApplyPendingConfigure()
    {
        var resized = false;
        if (_pendingWidth > 0 && _pendingHeight > 0 &&
            (_pendingWidth != _width || _pendingHeight != _height))
        {
            _width = _pendingWidth;
            _height = _pendingHeight;
            resized = true;
        }

        _state = WaylandWindowSemantics.ResolveConfiguredState(
            _requestedState,
            _pendingToplevelStates);
        _pendingWidth = 0;
        _pendingHeight = 0;
        // _pendingToplevelStates persists: toplevel states are double-buffered and a bare
        // xdg_surface.configure must resolve against the last received set, not an empty one.
        _configured = true;
        _mapped = _requestedState != WindowState.Hidden;

        if (resized)
        {
            if (!_resizable)
                ApplyResizeConstraints();
            OnResized();
        }
    }

    private void UpdateBufferScale()
    {
        var newScale = WaylandWindowSemantics.SelectScale(
            _enteredOutputs
                .Select(output => _outputsByProxy.TryGetValue(output, out var info) ? info.Scale : 1));
        if (newScale == _bufferScale)
            return;

        _bufferScale = newScale;
        if (_surface != 0 && WaylandNative.ProxyGetVersion(_surface) >= 3)
        {
            WaylandNative.ProxyMarshalInt32(
                _surface,
                8,
                0,
                WaylandNative.ProxyGetVersion(_surface),
                0,
                _bufferScale);
        }

        DestroyCursorTheme();
        if (_pointerInside)
            ApplyCursorVisibility();

        OnScaleFactorChanged();
        OnResized();
    }

    private void ApplyCursorVisibility()
    {
        if (_pointer == 0 || !_pointerInside || _pointerEnterSerial == 0)
            return;

        if (!_cursorVisible)
        {
            WaylandNative.ProxyMarshalPointerCursor(
                _pointer,
                0,
                0,
                WaylandNative.ProxyGetVersion(_pointer),
                0,
                _pointerEnterSerial,
                0,
                0,
                0);
            return;
        }

        EnsureCursorResources();

        var cursorName = Encoding.UTF8.GetBytes("left_ptr\0");
        WaylandNative.WlCursor* cursor;
        fixed (byte* cursorNamePointer = cursorName)
            cursor = WaylandNative.CursorThemeGetCursor(_cursorTheme, cursorNamePointer);
        if (cursor == null || cursor->ImageCount == 0 || cursor->Images == null)
            throw new InvalidOperationException("The Wayland cursor theme does not provide left_ptr.");

        var image = cursor->Images[0];
        var buffer = WaylandNative.CursorImageGetBuffer(image);
        if (buffer == 0)
            throw new InvalidOperationException("The Wayland cursor image has no buffer.");

        WaylandNative.ProxyMarshalSurfaceAttach(
            _cursorSurface,
            1,
            0,
            WaylandNative.ProxyGetVersion(_cursorSurface),
            0,
            buffer,
            0,
            0);
        if (WaylandNative.ProxyGetVersion(_cursorSurface) >= 3)
        {
            WaylandNative.ProxyMarshalInt32(
                _cursorSurface,
                8,
                0,
                WaylandNative.ProxyGetVersion(_cursorSurface),
                0,
                _bufferScale);
        }

        if (WaylandNative.ProxyGetVersion(_cursorSurface) >= 4)
        {
            WaylandNative.ProxyMarshalFourInt32(
                _cursorSurface,
                9,
                0,
                WaylandNative.ProxyGetVersion(_cursorSurface),
                0,
                0,
                0,
                checked((int)image->Width),
                checked((int)image->Height));
        }
        else
        {
            WaylandNative.ProxyMarshalFourInt32(
                _cursorSurface,
                2,
                0,
                WaylandNative.ProxyGetVersion(_cursorSurface),
                0,
                0,
                0,
                checked((int)image->Width),
                checked((int)image->Height));
        }

        WaylandNative.ProxyMarshalNoArguments(
            _cursorSurface,
            6,
            0,
            WaylandNative.ProxyGetVersion(_cursorSurface),
            0);
        WaylandNative.ProxyMarshalPointerCursor(
            _pointer,
            0,
            0,
            WaylandNative.ProxyGetVersion(_pointer),
            0,
            _pointerEnterSerial,
            _cursorSurface,
            checked((int)image->HotSpotX / _bufferScale),
            checked((int)image->HotSpotY / _bufferScale));
    }

    private void EnsureCursorResources()
    {
        if (_sharedMemory == 0)
            throw new NotSupportedException("The Wayland compositor does not advertise wl_shm for cursor images.");

        if (_cursorSurface == 0)
        {
            _cursorSurface = WaylandNative.ProxyMarshalConstructor(
                _compositor,
                0,
                WaylandNative.Interfaces.Surface,
                WaylandNative.ProxyGetVersion(_compositor),
                0,
                0);
            EnsureProxy(_cursorSurface, "create the Wayland cursor surface");
        }

        if (_cursorTheme != 0)
            return;

        _cursorTheme = WaylandNative.CursorThemeLoad(
            null,
            checked(CursorBaseSize * _bufferScale),
            _sharedMemory);
        if (_cursorTheme == 0)
            throw new InvalidOperationException("Failed to load the default Wayland cursor theme.");
    }

    private void DestroyCursorTheme()
    {
        if (_cursorTheme == 0)
            return;

        if (_cursorSurface != 0)
        {
            WaylandNative.ProxyMarshalSurfaceAttach(
                _cursorSurface,
                1,
                0,
                WaylandNative.ProxyGetVersion(_cursorSurface),
                0,
                0,
                0,
                0);
            WaylandNative.ProxyMarshalNoArguments(
                _cursorSurface,
                6,
                0,
                WaylandNative.ProxyGetVersion(_cursorSurface),
                0);
        }

        WaylandNative.CursorThemeDestroy(_cursorTheme);
        _cursorTheme = 0;
    }

    private void DestroyCursorResources()
    {
        DestroyCursorTheme();
        DestroyProtocolProxy(ref _cursorSurface, 0);
    }

    private void CreatePointer()
    {
        if (_pointer != 0 || _seat == 0)
            return;

        _pointer = WaylandNative.ProxyMarshalConstructor(
            _seat,
            0,
            WaylandNative.Interfaces.Pointer,
            Math.Min(WaylandNative.ProxyGetVersion(_seat), 7),
            0,
            0);
        EnsureProxy(_pointer, "create the Wayland pointer");
        AddListener(_pointer, WaylandListeners.Pointer, "listen to the Wayland pointer");
    }

    private void UpdateSeatDevices()
    {
        if (_surface == 0)
            return;

        if ((_seatCapabilities & SeatCapabilityPointer) != 0)
            CreatePointer();
        else
            DestroyPointer();

        if ((_seatCapabilities & SeatCapabilityKeyboard) != 0)
            CreateKeyboard();
        else
            DestroyKeyboard();
    }

    private void DestroyPointer()
    {
        if (_pointer == 0)
            return;

        var pointer = _pointer;
        _pointer = 0;
        _pointerInside = false;
        _pointerEnterSerial = 0;
        try
        {
            ReleasePressedMouseButtons(_activeSnapshot);
        }
        finally
        {
            DestroyVersionedProxy(pointer, 1, 3);
        }
    }

    private void CreateKeyboard()
    {
        if (_keyboard != 0 || _seat == 0)
            return;

        _keyboard = WaylandNative.ProxyMarshalConstructor(
            _seat,
            1,
            WaylandNative.Interfaces.Keyboard,
            Math.Min(WaylandNative.ProxyGetVersion(_seat), 7),
            0,
            0);
        EnsureProxy(_keyboard, "create the Wayland keyboard");
        AddListener(_keyboard, WaylandListeners.Keyboard, "listen to the Wayland keyboard");
    }

    private void DestroyKeyboard()
    {
        var keyboard = _keyboard;
        _keyboard = 0;
        try
        {
            if (keyboard != 0)
            {
                if (_focused && !_disposed)
                    SetFocused(false);
                else
                    ReleasePressedKeys(_activeSnapshot);
            }
        }
        finally
        {
            try
            {
                DestroyVersionedProxy(keyboard, 0, 3);
            }
            finally
            {
                DestroyXkbState();
            }
        }
    }

    private void DestroySeat()
    {
        var seat = _seat;
        _seat = 0;
        _seatCapabilities = 0;

        try
        {
            DestroyKeyboard();
        }
        finally
        {
            try
            {
                DestroyPointer();
            }
            finally
            {
                DestroyVersionedProxy(seat, 3, 5);
            }
        }
    }

    private void BindAvailableSeat()
    {
        if (_seat != 0 || _disposed || _disposeRequested ||
            !_seatRegistry.TryGetAvailable(out var globalName, out var version))
        {
            return;
        }

        var seat = WaylandNative.Bind(
            _registry,
            globalName,
            "wl_seat",
            Math.Min(version, 7),
            WaylandNative.Interfaces.Seat);
        try
        {
            EnsureProxy(seat, "bind wl_seat");
            AddListener(seat, WaylandListeners.Seat, "listen to the Wayland seat");
            _seatRegistry.MarkBound(globalName);
            _seat = seat;
        }
        catch
        {
            DestroyVersionedProxy(seat, 3, 5);
            throw;
        }
    }

    private void LoadKeymap(int fileDescriptor, uint size)
    {
        nint mapping = 0;
        try
        {
            if (size == 0 || size > int.MaxValue)
                throw new InvalidOperationException("The Wayland keymap has an invalid size.");

            mapping = WaylandNative.MemoryMap(
                0,
                size,
                WaylandNative.MemoryProtectionRead,
                WaylandNative.MemoryMapPrivate,
                fileDescriptor,
                0);
            if (mapping == -1)
            {
                throw new InvalidOperationException(
                    $"Failed to map the Wayland keymap (errno {Marshal.GetLastPInvokeError()}).");
            }

            DestroyXkbState();
            _xkbContext = XkbCommonNative.ContextNew(0);
            if (_xkbContext == 0)
                throw new InvalidOperationException("xkb_context_new failed.");

            _xkbKeymap = XkbCommonNative.KeymapNewFromString(
                _xkbContext,
                (byte*)mapping,
                XkbCommonNative.KeymapFormatTextV1,
                0);
            if (_xkbKeymap == 0)
                throw new InvalidOperationException("xkb_keymap_new_from_string failed.");

            _xkbState = XkbCommonNative.StateNew(_xkbKeymap);
            if (_xkbState == 0)
                throw new InvalidOperationException("xkb_state_new failed.");

            CreateComposeState();
        }
        finally
        {
            if (mapping != 0 && mapping != -1)
                WaylandNative.MemoryUnmap(mapping, size);
            WaylandNative.Close(fileDescriptor);
        }
    }

    private void CreateComposeState()
    {
        var locale = Environment.GetEnvironmentVariable("LC_ALL");
        if (string.IsNullOrWhiteSpace(locale))
            locale = Environment.GetEnvironmentVariable("LC_CTYPE");
        if (string.IsNullOrWhiteSpace(locale))
            locale = Environment.GetEnvironmentVariable("LANG");
        if (string.IsNullOrWhiteSpace(locale))
            locale = "C";

        var localeBytes = Encoding.UTF8.GetBytes(locale + '\0');
        fixed (byte* localePointer = localeBytes)
            _composeTable = XkbCommonNative.ComposeTableNewFromLocale(_xkbContext, localePointer, 0);
        if (_composeTable == 0)
            return;

        _composeState = XkbCommonNative.ComposeStateNew(_composeTable, 0);
    }

    private void DestroyXkbState()
    {
        if (_composeState != 0)
        {
            XkbCommonNative.ComposeStateUnref(_composeState);
            _composeState = 0;
        }

        if (_composeTable != 0)
        {
            XkbCommonNative.ComposeTableUnref(_composeTable);
            _composeTable = 0;
        }

        if (_xkbState != 0)
        {
            XkbCommonNative.StateUnref(_xkbState);
            _xkbState = 0;
        }

        if (_xkbKeymap != 0)
        {
            XkbCommonNative.KeymapUnref(_xkbKeymap);
            _xkbKeymap = 0;
        }

        if (_xkbContext != 0)
        {
            XkbCommonNative.ContextUnref(_xkbContext);
            _xkbContext = 0;
        }
    }

    private void ProcessKey(uint keyCode, bool down)
    {
        var snapshot = _activeSnapshot;
        var key = WaylandKeyMap.Translate(keyCode);

        if (down)
        {
            var repeat = _pressedKeys.ContainsKey(keyCode);
            if (!repeat)
                _pressedKeys[keyCode] = key;

            if (key != Key.Unknown)
                snapshot?.KeyEvents.Add(new KeyEvent(key, true, repeat));

            if (!repeat)
                AppendText(keyCode, snapshot);

            StartRepeat(keyCode, key);
            return;
        }

        if (_pressedKeys.Remove(keyCode, out var pressedKey))
            key = pressedKey;
        var inherited = _inheritedPressedKeys.Remove(keyCode);
        if (!inherited && key != Key.Unknown)
            snapshot?.KeyEvents.Add(new KeyEvent(key, false));

        if (_repeatKeyCode == keyCode)
            StopRepeat();
    }

    private void StartRepeat(uint keyCode, Key key)
    {
        if (_repeatRate <= 0 || _xkbKeymap == 0 ||
            XkbCommonNative.KeymapKeyRepeats(_xkbKeymap, keyCode + 8) == 0)
        {
            return;
        }

        _repeatKeyCode = keyCode;
        _repeatKey = key;
        _repeatIntervalTicks = Math.Max(1, Stopwatch.Frequency / _repeatRate);
        _nextRepeatTimestamp = Stopwatch.GetTimestamp() +
                               (long)(_repeatDelayMilliseconds / 1000d * Stopwatch.Frequency);
    }

    private void StopRepeat()
    {
        _repeatKeyCode = 0;
        _repeatKey = Key.Unknown;
        _nextRepeatTimestamp = 0;
        _repeatIntervalTicks = 0;
    }

    private void AppendKeyRepeats(InputSnapshot snapshot)
    {
        if (_repeatKeyCode == 0 || _repeatIntervalTicks <= 0)
            return;

        var timestamp = Stopwatch.GetTimestamp();
        for (var count = 0;
             count < MaximumRepeatedEventsPerPump && timestamp >= _nextRepeatTimestamp;
             count++)
        {
            if (_repeatKey != Key.Unknown)
                snapshot.KeyEvents.Add(new KeyEvent(_repeatKey, true, true));
            AppendText(_repeatKeyCode, snapshot);
            _nextRepeatTimestamp += _repeatIntervalTicks;
        }

        if (timestamp >= _nextRepeatTimestamp)
            _nextRepeatTimestamp = timestamp + _repeatIntervalTicks;
    }

    private void AppendText(uint keyCode, InputSnapshot snapshot)
    {
        if (snapshot is null || _xkbState == 0)
            return;

        var xkbKeyCode = keyCode + 8;
        var keySym = XkbCommonNative.StateKeyGetOneSym(_xkbState, xkbKeyCode);

        if (_composeState != 0 && XkbCommonNative.ComposeStateFeed(_composeState, keySym) != 0)
        {
            switch (XkbCommonNative.ComposeStateGetStatus(_composeState))
            {
                case XkbCommonNative.ComposeComposing:
                    return;
                case XkbCommonNative.ComposeComposed:
                    AppendUtf8(snapshot, _composeState, true, xkbKeyCode);
                    XkbCommonNative.ComposeStateReset(_composeState);
                    return;
                case XkbCommonNative.ComposeCancelled:
                    XkbCommonNative.ComposeStateReset(_composeState);
                    return;
            }
        }

        AppendUtf8(snapshot, _xkbState, false, xkbKeyCode);
    }

    private static void AppendUtf8(InputSnapshot snapshot, nint state, bool compose, uint keyCode)
    {
        Span<byte> buffer = stackalloc byte[256];
        int length;
        fixed (byte* bufferPointer = buffer)
        {
            length = compose
                ? XkbCommonNative.ComposeStateGetUtf8(state, bufferPointer, (nuint)buffer.Length)
                : XkbCommonNative.StateKeyGetUtf8(state, keyCode, bufferPointer, (nuint)buffer.Length);
        }

        if (length <= 0)
            return;

        var text = Encoding.UTF8.GetString(buffer[..Math.Min(length, buffer.Length - 1)]);
        foreach (var character in text)
        {
            if (!char.IsControl(character))
                snapshot.KeyCharPresses.Add(character);
        }
    }

    private void SetFocused(bool focused)
    {
        if (_focused == focused)
            return;

        _focused = focused;
        if (!focused)
        {
            ReleasePressedKeys(_activeSnapshot);
            ReleasePressedMouseButtons(_activeSnapshot);
        }

        OnFocusChanged();
    }

    private void ObserveRestoredFromMinimize()
    {
        if (_requestedState != WindowState.Minimized)
            return;

        _requestedState = WindowState.Normal;
        _state = WindowState.Normal;
    }

    private void ReleasePressedKeys(InputSnapshot snapshot)
    {
        if (snapshot is not null)
        {
            foreach (var pressedKey in _pressedKeys)
            {
                if (!_inheritedPressedKeys.Contains(pressedKey.Key) && pressedKey.Value != Key.Unknown)
                    snapshot.KeyEvents.Add(new KeyEvent(pressedKey.Value, false));
            }
        }

        _pressedKeys.Clear();
        _inheritedPressedKeys.Clear();
        StopRepeat();
    }

    private void ReleasePressedMouseButtons(InputSnapshot snapshot)
    {
        if (snapshot is not null)
        {
            foreach (var button in _pressedMouseButtons)
                snapshot.MouseEvents.Add(new MouseEvent(button, false));
        }

        _pressedMouseButtons.Clear();
    }

    private static bool TryTranslateMouseButton(uint buttonCode, out MouseButton button)
    {
        switch (buttonCode)
        {
            case 0x110:
                button = MouseButton.Left;
                return true;
            case 0x111:
                button = MouseButton.Right;
                return true;
            case 0x112:
                button = MouseButton.Middle;
                return true;
            case >= 0x113 and <= 0x11A:
                button = (MouseButton)((int)MouseButton.Button1 + buttonCode - 0x113);
                return true;
            default:
                button = default;
                return false;
        }
    }

    private void RaiseClosing()
    {
        if (_closingRaised)
            return;

        _closingRaised = true;
        OnClosing();
    }

    private void CloseFromCompositor()
    {
        try
        {
            RaiseClosing();
        }
        finally
        {
            ReleasePressedKeys(_activeSnapshot);
            ReleasePressedMouseButtons(_activeSnapshot);
            _focused = false;
            _exists = false;
        }
    }

    private void DestroyVersionedProxy(nint proxy, uint destructorOpcode, uint destructorSinceVersion)
    {
        if (proxy == 0)
            return;

        if (WaylandNative.ProxyGetVersion(proxy) >= destructorSinceVersion)
        {
            WaylandNative.ProxyMarshalNoArguments(
                proxy,
                destructorOpcode,
                0,
                WaylandNative.ProxyGetVersion(proxy),
                WaylandNative.MarshalFlagDestroy);
        }
        else
        {
            WaylandNative.ProxyDestroy(proxy);
        }
    }

    private static void DestroyProtocolProxy(ref nint proxy, uint destructorOpcode)
    {
        if (proxy == 0)
            return;

        WaylandNative.ProxyMarshalNoArguments(
            proxy,
            destructorOpcode,
            0,
            WaylandNative.ProxyGetVersion(proxy),
            WaylandNative.MarshalFlagDestroy);
        proxy = 0;
    }

    private WaylandOutput GetOutput(nint proxy)
    {
        if (!_outputsByProxy.TryGetValue(proxy, out var output))
            throw new InvalidOperationException("Received an event for an unknown Wayland output.");
        return output;
    }

    private static WaylandGameWindow GetWindow(nint data)
    {
        var handle = GCHandle.FromIntPtr(data);
        return (WaylandGameWindow)handle.Target;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void RegistryGlobal(nint data, nint registry, uint name, byte* interfaceName, uint version)
    {
        var window = GetWindow(data);
        try
        {
            window.HandleRegistryGlobal(name, Marshal.PtrToStringUTF8((nint)interfaceName) ?? string.Empty, version);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    private void HandleRegistryGlobal(uint name, string interfaceName, uint version)
    {
        switch (interfaceName)
        {
            case "wl_compositor" when _compositor == 0:
                _compositor = WaylandNative.Bind(
                    _registry,
                    name,
                    interfaceName,
                    Math.Min(version, 4),
                    WaylandNative.Interfaces.Compositor);
                EnsureProxy(_compositor, "bind wl_compositor");
                break;
            case "xdg_wm_base" when _wmBase == 0:
                _wmBase = WaylandNative.Bind(
                    _registry,
                    name,
                    interfaceName,
                    Math.Min(version, 6),
                    WaylandProtocol.Interfaces.XdgWmBase);
                EnsureProxy(_wmBase, "bind xdg_wm_base");
                AddListener(_wmBase, WaylandListeners.WmBase, "listen to xdg_wm_base");
                break;
            case "wl_shm" when _sharedMemory == 0:
                _sharedMemory = WaylandNative.Bind(
                    _registry,
                    name,
                    interfaceName,
                    1,
                    WaylandNative.Interfaces.SharedMemory);
                EnsureProxy(_sharedMemory, "bind wl_shm");
                break;
            case "wl_seat":
                _seatRegistry.Add(name, version);
                BindAvailableSeat();
                break;
            case "wl_output":
                {
                    var proxy = WaylandNative.Bind(
                        _registry,
                        name,
                        interfaceName,
                        Math.Min(version, 4),
                        WaylandNative.Interfaces.Output);
                    EnsureProxy(proxy, "bind wl_output");
                    var output = new WaylandOutput(name, proxy);
                    _outputsByName.Add(name, output);
                    _outputsByProxy.Add(proxy, output);
                    AddListener(proxy, WaylandListeners.Output, "listen to the Wayland output");
                    break;
                }
            case "zxdg_decoration_manager_v1" when _decorationManager == 0:
                _decorationManager = WaylandNative.Bind(
                    _registry,
                    name,
                    interfaceName,
                    1,
                    WaylandProtocol.Interfaces.DecorationManager);
                EnsureProxy(_decorationManager, "bind zxdg_decoration_manager_v1");
                CreateDecoration();
                break;
            case "zwp_idle_inhibit_manager_v1" when _idleInhibitManager == 0:
                _idleInhibitManager = WaylandNative.Bind(
                    _registry,
                    name,
                    interfaceName,
                    1,
                    WaylandProtocol.Interfaces.IdleInhibitManager);
                EnsureProxy(_idleInhibitManager, "bind zwp_idle_inhibit_manager_v1");
                UpdateIdleInhibitor();
                break;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void RegistryGlobalRemove(nint data, nint registry, uint name)
    {
        var window = GetWindow(data);
        try
        {
            window.HandleRegistryGlobalRemove(name);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    private void HandleRegistryGlobalRemove(uint name)
    {
        var removedBoundSeat = _seatRegistry.BoundGlobalName == name;
        if (_seatRegistry.Remove(name))
        {
            if (removedBoundSeat)
            {
                try
                {
                    DestroySeat();
                }
                finally
                {
                    BindAvailableSeat();
                }
            }

            return;
        }

        if (!_outputsByName.Remove(name, out var output))
            return;

        _outputsByProxy.Remove(output.Proxy);
        _enteredOutputs.Remove(output.Proxy);
        DestroyVersionedProxy(output.Proxy, 0, 3);
        UpdateBufferScale();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void WmBasePing(nint data, nint wmBase, uint serial)
    {
        var window = GetWindow(data);
        try
        {
            WaylandNative.ProxyMarshalUInt32(
                wmBase,
                3,
                0,
                WaylandNative.ProxyGetVersion(wmBase),
                0,
                serial);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void XdgSurfaceConfigure(nint data, nint xdgSurface, uint serial)
    {
        var window = GetWindow(data);
        try
        {
            WaylandNative.ProxyMarshalUInt32(
                xdgSurface,
                4,
                0,
                WaylandNative.ProxyGetVersion(xdgSurface),
                0,
                serial);
            window.ApplyPendingConfigure();
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void ToplevelConfigure(
        nint data,
        nint toplevel,
        int width,
        int height,
        WaylandNative.WlArray* states)
    {
        var window = GetWindow(data);
        try
        {
            window._pendingWidth = width;
            window._pendingHeight = height;

            if (states == null || states->Data == 0 || states->Size == 0)
            {
                window._pendingToplevelStates = [];
                return;
            }

            var count = checked((int)(states->Size / sizeof(uint)));
            var configuredStates = new uint[count];
            new ReadOnlySpan<uint>((void*)states->Data, count).CopyTo(configuredStates);
            window._pendingToplevelStates = configuredStates;
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void ToplevelClose(nint data, nint toplevel)
    {
        var window = GetWindow(data);
        try
        {
            window.CloseFromCompositor();
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void ToplevelConfigureBounds(nint data, nint toplevel, int width, int height)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void ToplevelWmCapabilities(nint data, nint toplevel, WaylandNative.WlArray* capabilities)
    {
        var window = GetWindow(data);
        try
        {
            if (capabilities == null || capabilities->Data == 0 || capabilities->Size == 0)
            {
                window._wmCapabilities = [];
            }
            else
            {
                var count = checked((int)(capabilities->Size / sizeof(uint)));
                var values = new uint[count];
                new ReadOnlySpan<uint>((void*)capabilities->Data, count).CopyTo(values);
                window._wmCapabilities = values;
            }

            window._wmCapabilitiesReceived = true;
            if (window._requestedState == WindowState.Minimized &&
                !window._wmCapabilities.Contains(WmCapabilityMinimize))
            {
                window._requestedState = WindowState.Normal;
                window._state = WindowState.Normal;
            }
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void DecorationConfigure(nint data, nint decoration, uint mode)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void SeatCapabilities(nint data, nint seat, uint capabilities)
    {
        var window = GetWindow(data);
        try
        {
            if (seat != window._seat || window._disposed || window._disposeRequested)
                return;

            window._seatCapabilities = capabilities;
            window.UpdateSeatDevices();
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void SeatName(nint data, nint seat, byte* name)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerEnter(
        nint data,
        nint pointer,
        uint serial,
        nint surface,
        int surfaceX,
        int surfaceY)
    {
        var window = GetWindow(data);
        try
        {
            if (surface != window._surface)
                return;

            window._pointerInside = true;
            window._pointerEnterSerial = serial;
            window._mouseX = surfaceX / FixedScale;
            window._mouseY = surfaceY / FixedScale;
            window.ObserveRestoredFromMinimize();
            window.ApplyCursorVisibility();
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerLeave(nint data, nint pointer, uint serial, nint surface)
    {
        var window = GetWindow(data);
        try
        {
            if (surface != window._surface)
                return;

            window._pointerInside = false;
            window._pointerEnterSerial = 0;
            window.ReleasePressedMouseButtons(window._activeSnapshot);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerMotion(nint data, nint pointer, uint time, int surfaceX, int surfaceY)
    {
        var window = GetWindow(data);
        window._mouseX = surfaceX / FixedScale;
        window._mouseY = surfaceY / FixedScale;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerButton(
        nint data,
        nint pointer,
        uint serial,
        uint time,
        uint buttonCode,
        uint state)
    {
        var window = GetWindow(data);
        try
        {
            if (!TryTranslateMouseButton(buttonCode, out var button))
                return;

            var down = state == PointerButtonPressed;
            var changed = down
                ? window._pressedMouseButtons.Add(button)
                : window._pressedMouseButtons.Remove(button);
            if (changed)
                window._activeSnapshot?.MouseEvents.Add(new MouseEvent(button, down));
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerAxis(nint data, nint pointer, uint time, uint axis, int value)
    {
        if (axis != PointerAxisVertical)
            return;

        var window = GetWindow(data);
        var delta = -(float)value / FixedScale / 10f;
        if (WaylandNative.ProxyGetVersion(pointer) >= 5)
            window._pendingAxisDelta += delta;
        else if (window._activeSnapshot is not null)
            window._activeSnapshot.WheelDelta += delta;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerFrame(nint data, nint pointer)
    {
        var window = GetWindow(data);
        if (window._activeSnapshot is not null)
        {
            window._activeSnapshot.WheelDelta +=
                window._pendingDiscreteAxisDelta ?? window._pendingAxisDelta;
        }

        window._pendingAxisDelta = 0f;
        window._pendingDiscreteAxisDelta = null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerAxisSource(nint data, nint pointer, uint source)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerAxisStop(nint data, nint pointer, uint time, uint axis)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void PointerAxisDiscrete(nint data, nint pointer, uint axis, int discrete)
    {
        if (axis != PointerAxisVertical)
            return;

        GetWindow(data)._pendingDiscreteAxisDelta = -discrete;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardKeymap(nint data, nint keyboard, uint format, int fileDescriptor, uint size)
    {
        var window = GetWindow(data);
        try
        {
            if (format != KeyboardKeymapFormatXkbV1)
            {
                WaylandNative.Close(fileDescriptor);
                throw new NotSupportedException($"Unsupported Wayland keyboard keymap format {format}.");
            }

            window.LoadKeymap(fileDescriptor, size);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardEnter(
        nint data,
        nint keyboard,
        uint serial,
        nint surface,
        WaylandNative.WlArray* keys)
    {
        var window = GetWindow(data);
        try
        {
            if (surface != window._surface)
                return;

            window.ObserveRestoredFromMinimize();

            window.ReleasePressedKeys(window._activeSnapshot);
            if (keys != null && keys->Data != 0)
            {
                var count = checked((int)(keys->Size / sizeof(uint)));
                foreach (var keyCode in new ReadOnlySpan<uint>((void*)keys->Data, count))
                {
                    window._pressedKeys[keyCode] = WaylandKeyMap.Translate(keyCode);
                    window._inheritedPressedKeys.Add(keyCode);
                }
            }

            window.SetFocused(true);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardLeave(nint data, nint keyboard, uint serial, nint surface)
    {
        var window = GetWindow(data);
        try
        {
            if (surface == window._surface)
                window.SetFocused(false);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardKey(
        nint data,
        nint keyboard,
        uint serial,
        uint time,
        uint keyCode,
        uint state)
    {
        var window = GetWindow(data);
        try
        {
            window.ProcessKey(keyCode, state == KeyboardKeyPressed);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardModifiers(
        nint data,
        nint keyboard,
        uint serial,
        uint depressed,
        uint latched,
        uint locked,
        uint group)
    {
        var window = GetWindow(data);
        try
        {
            if (window._xkbState != 0)
            {
                XkbCommonNative.StateUpdateMask(
                    window._xkbState,
                    depressed,
                    latched,
                    locked,
                    0,
                    0,
                    group);
            }
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void KeyboardRepeatInfo(nint data, nint keyboard, int rate, int delay)
    {
        var window = GetWindow(data);
        window._repeatRate = Math.Max(0, rate);
        window._repeatDelayMilliseconds = Math.Max(0, delay);
        if (rate <= 0)
            window.StopRepeat();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void SurfaceEnter(nint data, nint surface, nint output)
    {
        var window = GetWindow(data);
        try
        {
            if (surface == window._surface && window._outputsByProxy.ContainsKey(output))
            {
                window._enteredOutputs.Add(output);
                window.UpdateBufferScale();
            }
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void SurfaceLeave(nint data, nint surface, nint output)
    {
        var window = GetWindow(data);
        try
        {
            if (surface == window._surface)
            {
                window._enteredOutputs.Remove(output);
                window.UpdateBufferScale();
            }
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputGeometry(
        nint data,
        nint outputProxy,
        int x,
        int y,
        int physicalWidth,
        int physicalHeight,
        int subpixel,
        byte* make,
        byte* model,
        int transform)
    {
        var window = GetWindow(data);
        try
        {
            var output = window.GetOutput(outputProxy);
            output.Make = Marshal.PtrToStringUTF8((nint)make) ?? string.Empty;
            output.Model = Marshal.PtrToStringUTF8((nint)model) ?? string.Empty;
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputMode(
        nint data,
        nint outputProxy,
        uint flags,
        int width,
        int height,
        int refreshRate)
    {
        var window = GetWindow(data);
        try
        {
            if ((flags & OutputModeCurrent) == 0)
                return;

            var output = window.GetOutput(outputProxy);
            output.Width = width;
            output.Height = height;
            output.RefreshRateInMillihertz = refreshRate;
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputDone(nint data, nint outputProxy)
    {
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputScale(nint data, nint outputProxy, int scale)
    {
        var window = GetWindow(data);
        try
        {
            var output = window.GetOutput(outputProxy);
            output.Scale = Math.Max(1, scale);
            if (window._enteredOutputs.Contains(outputProxy))
                window.UpdateBufferScale();
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputName(nint data, nint outputProxy, byte* name)
    {
        var window = GetWindow(data);
        try
        {
            window.GetOutput(outputProxy).Name = Marshal.PtrToStringUTF8((nint)name) ?? string.Empty;
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void OutputDescription(nint data, nint outputProxy, byte* description)
    {
        var window = GetWindow(data);
        try
        {
            window.GetOutput(outputProxy).Description =
                Marshal.PtrToStringUTF8((nint)description) ?? string.Empty;
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
    }

    private sealed class WaylandOutput(uint globalName, nint proxy)
    {
        public uint GlobalName { get; } = globalName;
        public nint Proxy { get; } = proxy;
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRateInMillihertz { get; set; }
        public int Scale { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }
}
