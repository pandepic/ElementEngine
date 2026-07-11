using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal sealed class Win32GameWindow : GameWindow
{
    private const string WindowClassName = "ElementEngine.Win32GameWindow";

    private const uint CsOwnDc = 0x0020;
    private const uint CsDoubleClicks = 0x0008;

    private const long WsOverlapped = 0x00000000L;
    private const long WsPopup = 0x80000000L;
    private const long WsCaption = 0x00C00000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsClipSiblings = 0x04000000L;
    private const long WsClipChildren = 0x02000000L;
    private const long WsOverlappedWindow = WsOverlapped | WsCaption | WsSysMenu | WsThickFrame | WsMinimizeBox | WsMaximizeBox;
    private const long WsExAppWindow = 0x00040000L;

    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int GwlpUserData = -21;

    private const uint WmCreate = 0x0001;
    private const uint WmDestroy = 0x0002;
    private const uint WmMove = 0x0003;
    private const uint WmSize = 0x0005;
    private const uint WmSetFocus = 0x0007;
    private const uint WmKillFocus = 0x0008;
    private const uint WmClose = 0x0010;
    private const uint WmQuit = 0x0012;
    private const uint WmEraseBackground = 0x0014;
    private const uint WmSetCursor = 0x0020;
    private const uint WmCancelMode = 0x001F;
    private const uint WmInputLanguageChange = 0x0051;
    private const uint WmDisplayChange = 0x007E;
    private const uint WmNcCreate = 0x0081;
    private const uint WmNcDestroy = 0x0082;
    private const uint WmSysCommand = 0x0112;
    private const nuint SysCommandMask = 0xFFF0;
    private const nuint ScScreenSave = 0xF140;
    private const nuint ScMonitorPower = 0xF170;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmChar = 0x0102;
    private const uint WmSysKeyDown = 0x0104;
    private const uint WmSysKeyUp = 0x0105;
    private const uint WmUniChar = 0x0109;
    private const uint WmMouseMove = 0x0200;
    private const uint WmLeftButtonDown = 0x0201;
    private const uint WmLeftButtonUp = 0x0202;
    private const uint WmLeftButtonDoubleClick = 0x0203;
    private const uint WmRightButtonDown = 0x0204;
    private const uint WmRightButtonUp = 0x0205;
    private const uint WmRightButtonDoubleClick = 0x0206;
    private const uint WmMiddleButtonDown = 0x0207;
    private const uint WmMiddleButtonUp = 0x0208;
    private const uint WmMiddleButtonDoubleClick = 0x0209;
    private const uint WmMouseWheel = 0x020A;
    private const uint WmXButtonDown = 0x020B;
    private const uint WmXButtonUp = 0x020C;
    private const uint WmXButtonDoubleClick = 0x020D;
    private const uint WmCaptureChanged = 0x0215;
    private const uint WmDpiChanged = 0x02E0;
    private const uint WmAppDestroy = 0x8001;

    private const nuint UnicodeNoChar = 0xFFFF;
    private const int HtClient = 1;
    private const uint PmRemove = 0x0001;

    private const nuint SizeRestored = 0;
    private const nuint SizeMinimized = 1;
    private const nuint SizeMaximized = 2;

    private const int SwHide = 0;
    private const int SwShowNormal = 1;
    private const int SwShowMinimized = 2;
    private const int SwShowMaximized = 3;
    private const int SwRestore = 9;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoOwnerZOrder = 0x0200;

    private const uint MonitorDefaultToNearest = 2;
    private const uint EnumCurrentSettings = 0xFFFFFFFF;
    private const uint DisplayDeviceAttachedToDesktop = 0x00000001;
    private const uint DisplayDevicePrimaryDevice = 0x00000004;
    private const uint DisplayDeviceMirroringDriver = 0x00000008;
    private const uint DmBitsPerPixel = 0x00040000;
    private const uint DmPixelsWidth = 0x00080000;
    private const uint DmPixelsHeight = 0x00100000;
    private const uint DmDisplayFrequency = 0x00400000;
    private const uint CdsTest = 0x00000002;
    private const uint CdsFullScreen = 0x00000004;

    private const int DisplayChangeSuccessful = 0;
    private const int ErrorTimeout = 1460;
    private const uint SendMessageTimeoutBlock = 0x0001;
    private const uint SendMessageTimeoutAbortIfHung = 0x0002;
    private const uint SendMessageTimeoutErrorOnExit = 0x0020;
    private const uint CrossThreadDestroyTimeoutMilliseconds = 5000;

    private const int ErrorClassAlreadyExists = 1410;
    private const int IdcArrow = 32512;
    private const int WheelDelta = 120;
    private const uint DpiDefault = 96;
    private const nint DpiAwarenessContextUnaware = -1;
    private const nint DpiAwarenessContextPerMonitorV2 = -4;

    private const int VkBack = 0x08;
    private const int VkTab = 0x09;
    private const int VkClear = 0x0C;
    private const int VkReturn = 0x0D;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkPause = 0x13;
    private const int VkCapital = 0x14;
    private const int VkEscape = 0x1B;
    private const int VkSpace = 0x20;
    private const int VkPrior = 0x21;
    private const int VkNext = 0x22;
    private const int VkEnd = 0x23;
    private const int VkHome = 0x24;
    private const int VkLeft = 0x25;
    private const int VkUp = 0x26;
    private const int VkRight = 0x27;
    private const int VkDown = 0x28;
    private const int VkSnapshot = 0x2C;
    private const int VkInsert = 0x2D;
    private const int VkDelete = 0x2E;
    private const int Vk0 = 0x30;
    private const int Vk9 = 0x39;
    private const int VkA = 0x41;
    private const int VkZ = 0x5A;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkApps = 0x5D;
    private const int VkSleep = 0x5F;
    private const int VkNumpad0 = 0x60;
    private const int VkNumpad9 = 0x69;
    private const int VkMultiply = 0x6A;
    private const int VkAdd = 0x6B;
    private const int VkSubtract = 0x6D;
    private const int VkDecimal = 0x6E;
    private const int VkDivide = 0x6F;
    private const int VkF1 = 0x70;
    private const int VkF24 = 0x87;
    private const int VkNumLock = 0x90;
    private const int VkScroll = 0x91;
    private const int VkLShift = 0xA0;
    private const int VkRShift = 0xA1;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkLMenu = 0xA4;
    private const int VkRMenu = 0xA5;
    private const int VkOem1 = 0xBA;
    private const int VkOemPlus = 0xBB;
    private const int VkOemComma = 0xBC;
    private const int VkOemMinus = 0xBD;
    private const int VkOemPeriod = 0xBE;
    private const int VkOem2 = 0xBF;
    private const int VkOem3 = 0xC0;
    private const int VkOem4 = 0xDB;
    private const int VkOem5 = 0xDC;
    private const int VkOem6 = 0xDD;
    private const int VkOem7 = 0xDE;
    private const int VkOem102 = 0xE2;

    private static readonly object WindowClassLock = new();
    private static readonly WindowProcedure SharedWindowProcedure = StaticWindowProcedure;
    private static bool _windowClassRegistered;
    private static nint _arrowCursor;

    private readonly uint _ownerThreadId;
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];

    [ThreadStatic]
    private static int _threadWindowCount;

    private GCHandle _selfHandle;
    private bool _selfHandleAllocated;
    private nint _handle;
    private bool _exists;
    private bool _focused;
    private bool _resizable = true;
    private bool _cursorVisible = true;
    private bool _disposed;
    private bool _initializing = true;
    private bool _applyingState;
    private bool _fullScreen;
    private bool _closingRaised;
    private string _title;
    private int _x;
    private int _y;
    private int _width;
    private int _height;
    private int _framebufferWidth;
    private int _framebufferHeight;
    private uint _dpi = DpiDefault;
    private WindowState _state = WindowState.Normal;
    private WindowState _fullScreenState = WindowState.BorderlessFullScreen;
    private InputSnapshot _pendingInput = new();
    private Vector2 _lastMousePosition;
    private Exception _callbackException;
    private long _windowedStyle;
    private long _windowedExStyle;
    private WindowPlacement _windowedPlacement;
    private uint _fullScreenTargetWidth;
    private uint _fullScreenTargetHeight;
    private string _exclusiveDisplayDevice;

    public Win32GameWindow(WindowCreateInfo createInfo)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Win32 windows can only be created on Windows.");
        if (createInfo.WindowWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window width must be greater than zero.");
        if (createInfo.WindowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window height must be greater than zero.");

        _ownerThreadId = GetCurrentThreadId();
        _title = createInfo.WindowTitle ?? string.Empty;

        EnablePerMonitorDpiAwareness();
        EnsureWindowClassRegistered();

        _dpi = GetDpiAtPoint(createInfo.X, createInfo.Y);
        var style = WsOverlappedWindow | WsClipSiblings | WsClipChildren;
        var exStyle = WsExAppWindow;
        var outerBounds = GetOuterBounds(createInfo.WindowWidth, createInfo.WindowHeight, style, exStyle, _dpi);

        _selfHandle = GCHandle.Alloc(this);
        _selfHandleAllocated = true;

        try
        {
            _handle = CreateWindowExW(
                (uint)exStyle,
                WindowClassName,
                _title,
                (uint)style,
                createInfo.X,
                createInfo.Y,
                outerBounds.Width,
                outerBounds.Height,
                0,
                0,
                GetModuleHandleW(null),
                GCHandle.ToIntPtr(_selfHandle));

            if (_handle == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create the Win32 window.");

            _threadWindowCount++;

            _exists = true;
            _dpi = GetWindowDpi(_handle, _dpi);
            ResizeClientArea(createInfo.WindowWidth, createInfo.WindowHeight);

            switch (createInfo.WindowInitialState)
            {
                case WindowState.Normal:
                    _state = WindowState.Normal;
                    ShowWindow(_handle, SwShowNormal);
                    break;
                case WindowState.Maximized:
                    _state = WindowState.Maximized;
                    ShowWindow(_handle, SwShowMaximized);
                    break;
                case WindowState.Minimized:
                    _state = WindowState.Minimized;
                    ShowWindow(_handle, SwShowMinimized);
                    break;
                case WindowState.Hidden:
                    _state = WindowState.Hidden;
                    break;
                case WindowState.FullScreen:
                case WindowState.BorderlessFullScreen:
                    _state = WindowState.Normal;
                    ShowWindow(_handle, SwShowNormal);
                    EnterFullScreen(createInfo.WindowInitialState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(createInfo), "Unsupported initial window state.");
            }

            if (_state != WindowState.Hidden)
                UpdateWindow(_handle);

            UpdatePosition(_handle, false);
            UpdateClientSize(_handle, false);
            _focused = GetFocus() == _handle;
            UpdateMousePosition(_pendingInput);
            _initializing = false;
        }
        catch
        {
            if (_handle != 0 && IsWindow(_handle))
                DestroyWindow(_handle);

            ReleaseSelfHandle();
            throw;
        }
    }

    public override NativeWindowKind NativeKind => NativeWindowKind.Win32;
    public override nint NativeHandle => _handle;
    public override bool Exists => _exists;
    public override bool Focused => _focused;
    public override int X => _x;
    public override int Y => _y;
    public override int Width => _width;
    public override int Height => _height;
    public override int FramebufferWidth => _framebufferWidth;
    public override int FramebufferHeight => _framebufferHeight;
    public override float ScaleFactor => _dpi / (float)DpiDefault;

    public override string Title
    {
        get => _title;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _title = value ?? string.Empty;

            if (_handle != 0 && !SetWindowTextW(_handle, _title))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set the Win32 window title.");
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
            var resizableBits = WsThickFrame | WsMaximizeBox;

            if (_fullScreen)
            {
                _windowedStyle = value
                    ? _windowedStyle | resizableBits
                    : _windowedStyle & ~resizableBits;
                return;
            }

            if (_handle == 0)
                return;

            var style = GetWindowLong(_handle, GwlStyle);
            style = value ? style | resizableBits : style & ~resizableBits;
            SetWindowLong(_handle, GwlStyle, style);
            var resizeWindow = (_state is WindowState.Normal or WindowState.Hidden) && _width > 0 && _height > 0;
            var bounds = resizeWindow
                ? GetOuterBounds(_width, _height, style, GetWindowLong(_handle, GwlExStyle), _dpi)
                : default;
            var flags = SwpNoMove | SwpNoZOrder | SwpNoOwnerZOrder | SwpFrameChanged;

            if (!resizeWindow)
                flags |= SwpNoSize;

            if (!SetWindowPos(_handle, 0, 0, 0, bounds.Width, bounds.Height, flags))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to update Win32 window resizeability.");
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
            RefreshCursor();
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

            ApplyState(value);
        }
    }

    public override InputSnapshot PumpEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var windowHandle = _handle;

        if (windowHandle != 0)
        {
            // WM_QUIT is thread-scoped, while all other queued messages remain restricted to this window.
            if (PeekMessageW(out var quitMessage, 0, WmQuit, WmQuit, PmRemove))
            {
                HandleQuitMessage(quitMessage);
            }
            else
            {
                while (PeekMessageW(out var message, windowHandle, 0, 0, PmRemove))
                {
                    if (message.Message == WmQuit)
                    {
                        HandleQuitMessage(message);
                        break;
                    }

                    TranslateMessage(ref message);
                    DispatchMessageW(ref message);

                    if (_handle == 0)
                        break;
                }
            }
        }

        var snapshot = _pendingInput;
        _pendingInput = new InputSnapshot();
        UpdateMousePosition(snapshot);

        if (_callbackException is not null)
        {
            var callbackException = _callbackException;
            _callbackException = null;
            throw new InvalidOperationException("A Win32 window callback failed.", callbackException);
        }

        return snapshot;
    }

    private void HandleQuitMessage(NativeMessage message)
    {
        try
        {
            CloseFromMessage();
        }
        finally
        {
            // WM_QUIT is thread-scoped: keep it visible to sibling windows still pumping
            // on this thread, but let it drain with the last window so a window created
            // later on the same thread is not instantly destroyed by a stale quit flag.
            if (_threadWindowCount > 0)
                PostQuitMessage(unchecked((int)message.WParam));
        }
    }

    public override DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
    {
        if (displayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(displayIndex));

        var devices = new List<DisplayDevice>();

        for (uint deviceIndex = 0; ; deviceIndex++)
        {
            var device = new DisplayDevice
            {
                Size = (uint)Marshal.SizeOf<DisplayDevice>(),
                DeviceName = string.Empty,
                DeviceString = string.Empty,
                DeviceId = string.Empty,
                DeviceKey = string.Empty,
            };

            if (!EnumDisplayDevicesW(null, deviceIndex, ref device, 0))
                break;

            if ((device.StateFlags & DisplayDeviceAttachedToDesktop) == 0 ||
                (device.StateFlags & DisplayDeviceMirroringDriver) != 0)
            {
                continue;
            }

            if ((device.StateFlags & DisplayDevicePrimaryDevice) != 0)
                devices.Insert(0, device);
            else
                devices.Add(device);
        }

        if (displayIndex >= devices.Count)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

        var mode = new DeviceMode
        {
            DeviceName = string.Empty,
            FormName = string.Empty,
            Size = (ushort)Marshal.SizeOf<DeviceMode>(),
        };

        if (!EnumDisplaySettingsW(devices[displayIndex].DeviceName, EnumCurrentSettings, ref mode))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to query display {displayIndex}.");

        return new DisplayMode((int)mode.PixelsWidth, (int)mode.PixelsHeight, (int)mode.DisplayFrequency);
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var windowHandle = _handle;

        if (windowHandle != 0 && IsWindow(windowHandle))
        {
            bool destroyed;
            var destroyError = 0;

            if (GetCurrentThreadId() == _ownerThreadId)
            {
                destroyed = DestroyWindow(windowHandle);
                if (!destroyed)
                    destroyError = Marshal.GetLastWin32Error();
            }
            else
            {
                var flags = SendMessageTimeoutBlock | SendMessageTimeoutAbortIfHung | SendMessageTimeoutErrorOnExit;
                var sent = SendMessageTimeoutW(
                    windowHandle,
                    WmAppDestroy,
                    0,
                    0,
                    flags,
                    CrossThreadDestroyTimeoutMilliseconds,
                    out var destroyResult);
                var sendError = sent == 0 ? Marshal.GetLastWin32Error() : 0;
                destroyError = sendError;
                destroyed = (sent != 0 && destroyResult != 0) || !IsWindow(windowHandle);

                if (!destroyed && sendError == ErrorTimeout)
                {
                    _disposed = false;
                    throw new TimeoutException(
                        $"The Win32 owner thread did not destroy the window within " +
                        $"{CrossThreadDestroyTimeoutMilliseconds} milliseconds.");
                }
            }

            if (!destroyed && IsWindow(windowHandle))
            {
                _disposed = false;
                if (destroyError != 0)
                    throw new Win32Exception(destroyError, "Failed to destroy the Win32 window.");

                throw new InvalidOperationException("The Win32 owner thread did not destroy the window.");
            }
        }

        if (_handle == 0)
            ReleaseSelfHandle();

        GC.SuppressFinalize(this);
    }

    private static void EnsureWindowClassRegistered()
    {
        lock (WindowClassLock)
        {
            if (_windowClassRegistered)
                return;

            _arrowCursor = LoadCursorW(0, (nint)IdcArrow);
            if (_arrowCursor == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to load the Win32 arrow cursor.");

            var windowClass = new WindowClassEx
            {
                Size = (uint)Marshal.SizeOf<WindowClassEx>(),
                Style = CsOwnDc | CsDoubleClicks,
                WindowProcedure = SharedWindowProcedure,
                Instance = GetModuleHandleW(null),
                Cursor = _arrowCursor,
                ClassName = WindowClassName,
            };

            if (RegisterClassExW(ref windowClass) == 0)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorClassAlreadyExists)
                    throw new Win32Exception(error, "Failed to register the ElementEngine Win32 window class.");
            }

            _windowClassRegistered = true;
        }
    }

    private static nint StaticWindowProcedure(nint windowHandle, uint message, nuint wParam, nint lParam)
    {
        Win32GameWindow callbackTarget = null;

        try
        {
            if (message == WmNcCreate)
            {
                var creation = Marshal.PtrToStructure<CreateStructure>(lParam);
                SetWindowLong(windowHandle, GwlpUserData, creation.CreateParameters);
            }

            var userData = GetWindowLong(windowHandle, GwlpUserData);
            if (userData != 0)
            {
                var selfHandle = GCHandle.FromIntPtr((nint)userData);
                if (selfHandle.Target is Win32GameWindow window)
                {
                    callbackTarget = window;
                    window._handle = windowHandle;
                    window._exists = true;
                    return window.HandleWindowMessage(windowHandle, message, wParam, lParam);
                }
            }
        }
        catch (Exception exception)
        {
            if (callbackTarget is not null)
            {
                callbackTarget._callbackException ??= exception;
                return 0;
            }
        }

        return DefWindowProcW(windowHandle, message, wParam, lParam);
    }

    private nint HandleWindowMessage(nint windowHandle, uint message, nuint wParam, nint lParam)
    {
        switch (message)
        {
            case WmCreate:
                return 0;
            case WmSysCommand when !AllowScreenSaver &&
                                   (wParam & SysCommandMask) is ScScreenSave or ScMonitorPower:
                return 0;
            case WmClose:
                CloseFromMessage();
                return 0;
            case WmAppDestroy:
                return DestroyWindow(windowHandle) ? 1 : 0;
            case WmDestroy:
                _exists = false;
                _fullScreen = false;
                RestoreExclusiveDisplayMode(false);
                ReleasePressedInputs();
                return 0;
            case WmNcDestroy:
                _exists = false;
                _focused = false;
                _handle = 0;
                _threadWindowCount--;
                SetWindowLong(windowHandle, GwlpUserData, 0);
                ReleaseSelfHandle();
                return DefWindowProcW(windowHandle, message, wParam, lParam);
            case WmMove:
                if (!IsIconic(windowHandle))
                    UpdatePosition(windowHandle, true);
                return 0;
            case WmSize:
                HandleSizeChanged(windowHandle, wParam);
                return 0;
            case WmSetFocus:
                SetFocused(true);
                return 0;
            case WmKillFocus:
                SetFocused(false);
                return 0;
            case WmDpiChanged:
                HandleDpiChanged(windowHandle, wParam, lParam);
                return 0;
            case WmDisplayChange:
                if (_fullScreen)
                    FitToCurrentMonitor(windowHandle);
                return 0;
            case WmEraseBackground:
                return 1;
            case WmSetCursor:
                if ((ushort)(lParam.ToInt64() & 0xFFFF) == HtClient)
                {
                    SetCursor(_cursorVisible ? _arrowCursor : 0);
                    return 1;
                }

                break;
            case WmMouseMove:
                SetMousePositionFromClientMessage(lParam);
                RefreshCursor();
                return 0;
            case WmLeftButtonDown:
            case WmLeftButtonDoubleClick:
                HandleMouseButton(MouseButton.Left, true, lParam);
                return 0;
            case WmLeftButtonUp:
                HandleMouseButton(MouseButton.Left, false, lParam);
                return 0;
            case WmRightButtonDown:
            case WmRightButtonDoubleClick:
                HandleMouseButton(MouseButton.Right, true, lParam);
                return 0;
            case WmRightButtonUp:
                HandleMouseButton(MouseButton.Right, false, lParam);
                return 0;
            case WmMiddleButtonDown:
            case WmMiddleButtonDoubleClick:
                HandleMouseButton(MouseButton.Middle, true, lParam);
                return 0;
            case WmMiddleButtonUp:
                HandleMouseButton(MouseButton.Middle, false, lParam);
                return 0;
            case WmXButtonDown:
            case WmXButtonDoubleClick:
                HandleMouseButton(GetXButton(wParam), true, lParam);
                return 1;
            case WmXButtonUp:
                HandleMouseButton(GetXButton(wParam), false, lParam);
                return 1;
            case WmMouseWheel:
                HandleMouseWheel(wParam, lParam);
                return 0;
            case WmCaptureChanged:
            case WmCancelMode:
                ReleasePressedMouseButtons();
                return 0;
            case WmKeyDown:
            case WmSysKeyDown:
                HandleKeyMessage((int)wParam, lParam, true);
                break;
            case WmKeyUp:
            case WmSysKeyUp:
                HandleKeyMessage((int)wParam, lParam, false);
                break;
            case WmChar:
                HandleCharacter(wParam);
                return 0;
            case WmUniChar:
                if (wParam == UnicodeNoChar)
                    return 1;

                HandleUnicodeCharacter((uint)wParam);
                return 0;
            case WmInputLanguageChange:
                break;
        }

        return DefWindowProcW(windowHandle, message, wParam, lParam);
    }

    private void ApplyState(WindowState state)
    {
        if (_handle == 0)
            return;

        _applyingState = true;

        try
        {
            if (state is WindowState.FullScreen or WindowState.BorderlessFullScreen)
            {
                if (_fullScreen)
                    SwitchFullScreenMode(state);
                else
                    EnterFullScreen(state);

                return;
            }

            if (_fullScreen)
                LeaveFullScreen();

            switch (state)
            {
                case WindowState.Normal:
                    ShowWindow(_handle, SwRestore);
                    _state = WindowState.Normal;
                    break;
                case WindowState.Maximized:
                    ShowWindow(_handle, SwShowMaximized);
                    _state = WindowState.Maximized;
                    break;
                case WindowState.Minimized:
                    ShowWindow(_handle, SwShowMinimized);
                    _state = WindowState.Minimized;
                    break;
                case WindowState.Hidden:
                    ShowWindow(_handle, SwHide);
                    _state = WindowState.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
        finally
        {
            _applyingState = false;
        }
    }

    private void EnterFullScreen(WindowState state)
    {
        if (_handle == 0)
            return;

        _windowedStyle = GetWindowLong(_handle, GwlStyle);
        _windowedExStyle = GetWindowLong(_handle, GwlExStyle);
        _windowedPlacement = new WindowPlacement
        {
            Length = (uint)Marshal.SizeOf<WindowPlacement>(),
        };

        if (!GetWindowPlacement(_handle, ref _windowedPlacement))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to save the window placement before entering full screen.");

        _fullScreenTargetWidth = checked((uint)Math.Max(1, _framebufferWidth));
        _fullScreenTargetHeight = checked((uint)Math.Max(1, _framebufferHeight));

        if (state == WindowState.FullScreen)
            EnterExclusiveDisplayMode(_fullScreenTargetWidth, _fullScreenTargetHeight);

        try
        {
            ShowWindow(_handle, SwRestore);
            SetWindowLong(_handle, GwlStyle, (_windowedStyle & ~WsOverlappedWindow) | WsPopup);
            _fullScreen = true;
            _fullScreenState = state;
            FitToCurrentMonitor(_handle);
            _state = state;
        }
        catch
        {
            RestoreExclusiveDisplayMode(false);
            SetWindowLong(_handle, GwlStyle, _windowedStyle);
            SetWindowLong(_handle, GwlExStyle, _windowedExStyle);
            _fullScreen = false;
            throw;
        }
    }

    private void SwitchFullScreenMode(WindowState state)
    {
        if (_fullScreenState == state)
        {
            _state = state;
            return;
        }

        var previousFullScreenState = _fullScreenState;
        var previousState = _state;
        var displayModeChanged = false;
        var displayModeTransition = Win32FullScreenTransition.GetDisplayModeTransition(previousFullScreenState, state);

        try
        {
            ApplyDisplayModeTransition(displayModeTransition);

            displayModeChanged = true;
            ShowWindow(_handle, SwRestore);
            FitToCurrentMonitor(_handle);
            _fullScreenState = state;
            _state = state;
        }
        catch (Exception transitionException)
        {
            if (!displayModeChanged)
                throw;

            try
            {
                var rollbackTransition = Win32FullScreenTransition.GetDisplayModeTransition(state, previousFullScreenState);
                ApplyDisplayModeTransition(rollbackTransition);

                ShowWindow(_handle, SwRestore);
                FitToCurrentMonitor(_handle);
                _fullScreenState = previousFullScreenState;
                _state = previousState;
            }
            catch (Exception rollbackException)
            {
                var exclusiveModeActive = !string.IsNullOrEmpty(_exclusiveDisplayDevice);
                _fullScreenState = exclusiveModeActive
                    ? WindowState.FullScreen
                    : WindowState.BorderlessFullScreen;
                _state = _fullScreenState;
                throw new AggregateException(
                    "The Win32 full-screen transition and its rollback both failed.",
                    transitionException,
                    rollbackException);
            }

            throw;
        }
    }

    private void ApplyDisplayModeTransition(Win32DisplayModeTransition transition)
    {
        switch (transition)
        {
            case Win32DisplayModeTransition.None:
                return;
            case Win32DisplayModeTransition.EnterExclusive:
                EnterExclusiveDisplayMode(_fullScreenTargetWidth, _fullScreenTargetHeight);
                return;
            case Win32DisplayModeTransition.RestoreDesktop:
                RestoreExclusiveDisplayMode(true);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(transition));
        }
    }

    private void LeaveFullScreen()
    {
        if (_handle == 0 || !_fullScreen)
            return;

        RestoreExclusiveDisplayMode(true);
        SetWindowLong(_handle, GwlStyle, _windowedStyle);
        SetWindowLong(_handle, GwlExStyle, _windowedExStyle);

        var placement = _windowedPlacement;
        placement.ShowCommand = SwShowNormal;
        SetWindowPlacement(_handle, ref placement);
        SetWindowPos(
            _handle,
            0,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoOwnerZOrder | SwpFrameChanged);

        _fullScreen = false;
        _state = WindowState.Normal;
    }

    private void EnterExclusiveDisplayMode(uint width, uint height)
    {
        var monitorInfo = GetCurrentMonitorInfo(_handle);
        var currentMode = CreateDeviceMode();
        if (!EnumDisplaySettingsW(monitorInfo.DeviceName, EnumCurrentSettings, ref currentMode))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to query the current display mode for {monitorInfo.DeviceName}.");
        }

        var modes = new List<Win32DisplayMode>();
        for (uint modeIndex = 0; ; modeIndex++)
        {
            var mode = CreateDeviceMode();
            if (!EnumDisplaySettingsW(monitorInfo.DeviceName, modeIndex, ref mode))
                break;

            modes.Add(new Win32DisplayMode(
                mode.PixelsWidth,
                mode.PixelsHeight,
                mode.BitsPerPixel,
                mode.DisplayFrequency,
                modeIndex));
        }

        if (!Win32DisplayModeSelector.TrySelect(
                modes,
                width,
                height,
                currentMode.BitsPerPixel,
                currentMode.DisplayFrequency,
                out var selectedMode))
        {
            throw new NotSupportedException(
                $"Display {monitorInfo.DeviceName} does not support a {width}x{height} full-screen mode at 32-bit color.");
        }

        var nativeMode = CreateDeviceMode();
        if (!EnumDisplaySettingsW(monitorInfo.DeviceName, selectedMode.NativeIndex, ref nativeMode))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to query full-screen mode {selectedMode.NativeIndex} for {monitorInfo.DeviceName}.");
        }

        nativeMode.Fields = DmBitsPerPixel | DmPixelsWidth | DmPixelsHeight | DmDisplayFrequency;
        var testResult = ChangeDisplaySettingsExW(
            monitorInfo.DeviceName,
            ref nativeMode,
            0,
            CdsTest,
            0);
        ThrowIfDisplayChangeFailed(testResult, monitorInfo.DeviceName, width, height, "validate");

        var changeResult = ChangeDisplaySettingsExW(
            monitorInfo.DeviceName,
            ref nativeMode,
            0,
            CdsFullScreen,
            0);
        ThrowIfDisplayChangeFailed(changeResult, monitorInfo.DeviceName, width, height, "apply");
        _exclusiveDisplayDevice = monitorInfo.DeviceName;
    }

    private void RestoreExclusiveDisplayMode(bool throwOnFailure)
    {
        if (string.IsNullOrEmpty(_exclusiveDisplayDevice))
            return;

        var displayDevice = _exclusiveDisplayDevice;
        var result = ChangeDisplaySettingsExW(displayDevice, 0, 0, 0, 0);
        if (result == DisplayChangeSuccessful)
        {
            _exclusiveDisplayDevice = null;
            return;
        }

        if (throwOnFailure)
        {
            throw new InvalidOperationException(
                $"Failed to restore the desktop display mode for {displayDevice} (result {result}).");
        }
    }

    private static void ThrowIfDisplayChangeFailed(
        int result,
        string displayDevice,
        uint width,
        uint height,
        string operation)
    {
        if (result == DisplayChangeSuccessful)
            return;

        throw new InvalidOperationException(
            $"Failed to {operation} {width}x{height} full-screen mode on {displayDevice} (result {result}).");
    }

    private void FitToCurrentMonitor(nint windowHandle)
    {
        var monitorInfo = GetCurrentMonitorInfo(windowHandle);

        if (!SetWindowPos(
                windowHandle,
                0,
                monitorInfo.Monitor.Left,
                monitorInfo.Monitor.Top,
                monitorInfo.Monitor.Right - monitorInfo.Monitor.Left,
                monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top,
                SwpNoOwnerZOrder | SwpFrameChanged))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to fit the Win32 window to its current monitor.");
        }
    }

    private static MonitorInfo GetCurrentMonitorInfo(nint windowHandle)
    {
        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            Size = (uint)Marshal.SizeOf<MonitorInfo>(),
            DeviceName = string.Empty,
        };

        if (monitor == 0 || !GetMonitorInfoW(monitor, ref monitorInfo))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query the current monitor bounds.");

        return monitorInfo;
    }

    private static DeviceMode CreateDeviceMode()
    {
        return new DeviceMode
        {
            DeviceName = string.Empty,
            FormName = string.Empty,
            Size = (ushort)Marshal.SizeOf<DeviceMode>(),
        };
    }

    private void HandleSizeChanged(nint windowHandle, nuint sizeKind)
    {
        if (!_applyingState && _state != WindowState.Hidden)
        {
            if (sizeKind == SizeMinimized)
                _state = WindowState.Minimized;
            else if (_fullScreen)
                _state = _fullScreenState;
            else if (sizeKind == SizeMaximized)
                _state = WindowState.Maximized;
            else if (sizeKind == SizeRestored)
                _state = WindowState.Normal;
        }

        if (sizeKind != SizeMinimized)
            UpdateClientSize(windowHandle, true);
    }

    private void HandleDpiChanged(nint windowHandle, nuint wParam, nint lParam)
    {
        var newDpi = (uint)(wParam & 0xFFFF);
        if (newDpi == 0)
            newDpi = DpiDefault;

        var changed = _dpi != newDpi;
        _dpi = newDpi;

        if (changed && !_initializing)
            OnScaleFactorChanged();

        var suggestedBounds = Marshal.PtrToStructure<NativeRectangle>(lParam);
        SetWindowPos(
            windowHandle,
            0,
            suggestedBounds.Left,
            suggestedBounds.Top,
            suggestedBounds.Right - suggestedBounds.Left,
            suggestedBounds.Bottom - suggestedBounds.Top,
            SwpNoZOrder | SwpNoActivate);

        UpdatePosition(windowHandle, true);
        UpdateClientSize(windowHandle, true);
    }

    private void SetFocused(bool focused)
    {
        if (_focused == focused)
            return;

        _focused = focused;

        if (!focused)
            ReleasePressedInputs();

        if (!_initializing)
            OnFocusChanged();
    }

    private void UpdatePosition(nint windowHandle, bool raiseEvent)
    {
        if (!GetLogicalWindowRect(windowHandle, _dpi, out var bounds))
            return;

        if (_x == bounds.Left && _y == bounds.Top)
            return;

        _x = bounds.Left;
        _y = bounds.Top;

        if (raiseEvent && !_initializing)
            OnMoved();
    }

    private void UpdateClientSize(nint windowHandle, bool raiseEvent)
    {
        if (!GetClientRect(windowHandle, out var bounds))
            return;

        var framebufferWidth = Math.Max(0, bounds.Right - bounds.Left);
        var framebufferHeight = Math.Max(0, bounds.Bottom - bounds.Top);
        var width = PhysicalToLogical(framebufferWidth, _dpi);
        var height = PhysicalToLogical(framebufferHeight, _dpi);

        if (_width == width &&
            _height == height &&
            _framebufferWidth == framebufferWidth &&
            _framebufferHeight == framebufferHeight)
        {
            return;
        }

        _width = width;
        _height = height;
        _framebufferWidth = framebufferWidth;
        _framebufferHeight = framebufferHeight;

        if (raiseEvent && !_initializing)
            OnResized();
    }

    private void ResizeClientArea(int logicalWidth, int logicalHeight)
    {
        var style = GetWindowLong(_handle, GwlStyle);
        var exStyle = GetWindowLong(_handle, GwlExStyle);
        var bounds = GetOuterBounds(logicalWidth, logicalHeight, style, exStyle, _dpi);

        if (!SetWindowPos(
                _handle,
                0,
                0,
                0,
                bounds.Width,
                bounds.Height,
                SwpNoMove | SwpNoZOrder | SwpNoActivate))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set the Win32 client area size.");
        }
    }

    private static WindowSize GetOuterBounds(int logicalWidth, int logicalHeight, long style, long exStyle, uint dpi)
    {
        var clientWidth = LogicalToPhysical(logicalWidth, dpi);
        var clientHeight = LogicalToPhysical(logicalHeight, dpi);
        var bounds = new NativeRectangle
        {
            Right = clientWidth,
            Bottom = clientHeight,
        };

        try
        {
            if (!AdjustWindowRectExForDpi(ref bounds, (uint)style, false, (uint)exStyle, dpi))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to calculate the Win32 window bounds.");
        }
        catch (EntryPointNotFoundException)
        {
            if (!AdjustWindowRectEx(ref bounds, (uint)style, false, (uint)exStyle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to calculate the Win32 window bounds.");
        }

        return new WindowSize(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    private void HandleKeyMessage(int virtualKey, nint lParam, bool down)
    {
        var key = TranslateKey(virtualKey, lParam);
        if (key == Key.Unknown)
            return;

        var repeat = down && (lParam.ToInt64() & (1L << 30)) != 0;

        if (down)
            _pressedKeys.Add(key);
        else
            _pressedKeys.Remove(key);

        _pendingInput.KeyEvents.Add(new KeyEvent(key, down, repeat));
    }

    private static Key TranslateKey(int virtualKey, nint lParam)
    {
        var messageData = lParam.ToInt64();
        var scanCode = (int)((messageData >> 16) & 0xFF);
        var extended = (messageData & (1L << 24)) != 0;

        if (virtualKey >= VkA && virtualKey <= VkZ)
            return (Key)((int)Key.A + virtualKey - VkA);
        if (virtualKey >= Vk0 && virtualKey <= Vk9)
            return (Key)((int)Key.Number0 + virtualKey - Vk0);
        if (virtualKey >= VkNumpad0 && virtualKey <= VkNumpad9)
            return (Key)((int)Key.Keypad0 + virtualKey - VkNumpad0);
        if (virtualKey >= VkF1 && virtualKey <= VkF24)
            return (Key)((int)Key.F1 + virtualKey - VkF1);

        return virtualKey switch
        {
            VkShift => scanCode == 0x36 ? Key.ShiftRight : Key.ShiftLeft,
            VkLShift => Key.ShiftLeft,
            VkRShift => Key.ShiftRight,
            VkControl => extended ? Key.ControlRight : Key.ControlLeft,
            VkLControl => Key.ControlLeft,
            VkRControl => Key.ControlRight,
            VkMenu => extended ? Key.AltRight : Key.AltLeft,
            VkLMenu => Key.AltLeft,
            VkRMenu => Key.AltRight,
            VkLWin => Key.WinLeft,
            VkRWin => Key.WinRight,
            VkApps => Key.Menu,
            VkUp => extended ? Key.Up : Key.Keypad8,
            VkDown => extended ? Key.Down : Key.Keypad2,
            VkLeft => extended ? Key.Left : Key.Keypad4,
            VkRight => extended ? Key.Right : Key.Keypad6,
            VkReturn => extended ? Key.KeypadEnter : Key.Enter,
            VkEscape => Key.Escape,
            VkSpace => Key.Space,
            VkTab => Key.Tab,
            VkBack => Key.BackSpace,
            VkInsert => extended ? Key.Insert : Key.Keypad0,
            VkDelete => extended ? Key.Delete : Key.KeypadDecimal,
            VkPrior => extended ? Key.PageUp : Key.Keypad9,
            VkNext => extended ? Key.PageDown : Key.Keypad3,
            VkHome => extended ? Key.Home : Key.Keypad7,
            VkEnd => extended ? Key.End : Key.Keypad1,
            VkCapital => Key.CapsLock,
            VkScroll => Key.ScrollLock,
            VkSnapshot => Key.PrintScreen,
            VkPause => Key.Pause,
            VkNumLock => Key.NumLock,
            VkClear => extended ? Key.Clear : Key.Keypad5,
            VkSleep => Key.Sleep,
            VkDivide => Key.KeypadDivide,
            VkMultiply => Key.KeypadMultiply,
            VkSubtract => Key.KeypadSubtract,
            VkAdd => Key.KeypadAdd,
            VkDecimal => Key.KeypadDecimal,
            VkOem3 => Key.Tilde,
            VkOemMinus => Key.Minus,
            VkOemPlus => Key.Plus,
            VkOem4 => Key.BracketLeft,
            VkOem6 => Key.BracketRight,
            VkOem1 => Key.Semicolon,
            VkOem7 => Key.Quote,
            VkOemComma => Key.Comma,
            VkOemPeriod => Key.Period,
            VkOem2 => Key.Slash,
            VkOem5 => Key.BackSlash,
            VkOem102 => Key.NonUSBackSlash,
            _ => Key.Unknown,
        };
    }

    private void HandleCharacter(nuint characterValue)
    {
        var character = (char)characterValue;
        if (!char.IsControl(character))
            _pendingInput.KeyCharPresses.Add(character);
    }

    private void HandleUnicodeCharacter(uint codePoint)
    {
        if (codePoint > 0x10FFFF || codePoint is >= 0xD800 and <= 0xDFFF)
            return;

        var characters = char.ConvertFromUtf32((int)codePoint);
        foreach (var character in characters)
        {
            if (!char.IsControl(character))
                _pendingInput.KeyCharPresses.Add(character);
        }
    }

    private void HandleMouseButton(MouseButton button, bool down, nint lParam)
    {
        SetMousePositionFromClientMessage(lParam);

        if (down)
        {
            _pressedMouseButtons.Add(button);
            SetCapture(_handle);
        }
        else
        {
            _pressedMouseButtons.Remove(button);
            if (_pressedMouseButtons.Count == 0 && GetCapture() == _handle)
                ReleaseCapture();
        }

        _pendingInput.MouseEvents.Add(new MouseEvent(button, down));
    }

    private void HandleMouseWheel(nuint wParam, nint lParam)
    {
        var delta = unchecked((short)((wParam >> 16) & 0xFFFF));
        _pendingInput.WheelDelta += delta / (float)WheelDelta;

        var point = new NativePoint
        {
            X = GetSignedLowWord(lParam),
            Y = GetSignedHighWord(lParam),
        };

        if (_handle != 0 && ScreenToClient(_handle, ref point))
            SetMousePosition(point.X, point.Y);
    }

    private void SetMousePositionFromClientMessage(nint lParam)
    {
        SetMousePosition(GetSignedLowWord(lParam), GetSignedHighWord(lParam));
    }

    private void SetMousePosition(int x, int y)
    {
        _lastMousePosition = new Vector2(
            PhysicalToLogical(x, _dpi),
            PhysicalToLogical(y, _dpi));
        _pendingInput.MousePosition = _lastMousePosition;
    }

    private void UpdateMousePosition(InputSnapshot snapshot)
    {
        if (_handle != 0 && GetCursorPos(out var point) && ScreenToClient(_handle, ref point))
        {
            _lastMousePosition = new Vector2(
                PhysicalToLogical(point.X, _dpi),
                PhysicalToLogical(point.Y, _dpi));
        }

        snapshot.MousePosition = _lastMousePosition;
    }

    private void ReleasePressedInputs()
    {
        foreach (var key in _pressedKeys)
            _pendingInput.KeyEvents.Add(new KeyEvent(key, false));

        _pressedKeys.Clear();
        ReleasePressedMouseButtons();
    }

    private void ReleasePressedMouseButtons()
    {
        if (_pressedMouseButtons.Count == 0)
            return;

        foreach (var button in _pressedMouseButtons)
            _pendingInput.MouseEvents.Add(new MouseEvent(button, false));

        _pressedMouseButtons.Clear();

        if (_handle != 0 && GetCapture() == _handle)
            ReleaseCapture();
    }

    private void RefreshCursor()
    {
        if (_handle == 0 || !GetCursorPos(out var point))
            return;

        var clientPoint = point;
        if (!ScreenToClient(_handle, ref clientPoint) || !GetClientRect(_handle, out var clientBounds))
            return;

        if (clientPoint.X >= clientBounds.Left &&
            clientPoint.X < clientBounds.Right &&
            clientPoint.Y >= clientBounds.Top &&
            clientPoint.Y < clientBounds.Bottom)
        {
            SetCursor(_cursorVisible ? _arrowCursor : 0);
        }
    }

    private void CloseFromMessage()
    {
        var windowHandle = _handle;
        if (windowHandle == 0 || !IsWindow(windowHandle))
            return;

        try
        {
            if (!_closingRaised)
            {
                _closingRaised = true;
                OnClosing();
            }
        }
        finally
        {
            if (IsWindow(windowHandle))
                DestroyWindow(windowHandle);
        }
    }

    private void ReleaseSelfHandle()
    {
        if (!_selfHandleAllocated)
            return;

        _selfHandle.Free();
        _selfHandleAllocated = false;
    }

    private static MouseButton GetXButton(nuint wParam)
    {
        return ((wParam >> 16) & 0xFFFF) == 1
            ? MouseButton.Button1
            : MouseButton.Button2;
    }

    private static int GetSignedLowWord(nint value)
    {
        return unchecked((short)(value.ToInt64() & 0xFFFF));
    }

    private static int GetSignedHighWord(nint value)
    {
        return unchecked((short)((value.ToInt64() >> 16) & 0xFFFF));
    }

    private static int LogicalToPhysical(int value, uint dpi)
    {
        return (int)Math.Round(value * dpi / (double)DpiDefault, MidpointRounding.AwayFromZero);
    }

    private static int PhysicalToLogical(int value, uint dpi)
    {
        return (int)Math.Round(value * DpiDefault / (double)Math.Max(1, dpi), MidpointRounding.AwayFromZero);
    }

    private static void EnablePerMonitorDpiAwareness()
    {
        try
        {
            SetThreadDpiAwarenessContext(DpiAwarenessContextPerMonitorV2);
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static bool GetLogicalWindowRect(nint windowHandle, uint dpi, out NativeRectangle bounds)
    {
        nint previousContext;

        try
        {
            // Query under a 96-DPI context so position and client size use the same engine coordinate space.
            previousContext = SetThreadDpiAwarenessContext(DpiAwarenessContextUnaware);
        }
        catch (EntryPointNotFoundException)
        {
            previousContext = 0;
        }

        try
        {
            if (!GetWindowRect(windowHandle, out bounds))
                return false;

            if (previousContext == 0)
            {
                bounds.Left = PhysicalToLogical(bounds.Left, dpi);
                bounds.Top = PhysicalToLogical(bounds.Top, dpi);
                bounds.Right = PhysicalToLogical(bounds.Right, dpi);
                bounds.Bottom = PhysicalToLogical(bounds.Bottom, dpi);
            }

            return true;
        }
        finally
        {
            if (previousContext != 0)
                SetThreadDpiAwarenessContext(previousContext);
        }
    }

    private static uint GetDpiAtPoint(int x, int y)
    {
        var monitor = MonitorFromPoint(new NativePoint { X = x, Y = y }, MonitorDefaultToNearest);

        if (monitor != 0)
        {
            try
            {
                if (GetDpiForMonitor(monitor, 0, out var horizontalDpi, out _) == 0 && horizontalDpi != 0)
                    return horizontalDpi;
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        try
        {
            var dpi = GetDpiForSystem();
            return dpi == 0 ? DpiDefault : dpi;
        }
        catch (EntryPointNotFoundException)
        {
            return DpiDefault;
        }
    }

    private static uint GetWindowDpi(nint windowHandle, uint fallback)
    {
        try
        {
            var dpi = GetDpiForWindow(windowHandle);
            return dpi == 0 ? fallback : dpi;
        }
        catch (EntryPointNotFoundException)
        {
            return fallback;
        }
    }

    private static long GetWindowLong(nint windowHandle, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtrW64(windowHandle, index).ToInt64()
            : GetWindowLongW32(windowHandle, index);
    }

    private static nint SetWindowLong(nint windowHandle, int index, long value)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtrW64(windowHandle, index, (nint)value)
            : (nint)SetWindowLongW32(windowHandle, index, (int)value);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedure(nint windowHandle, uint message, nuint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClassEx
    {
        public uint Size;
        public uint Style;
        public WindowProcedure WindowProcedure;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint BackgroundBrush;
        public nint MenuName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string ClassName;

        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CreateStructure
    {
        public nint CreateParameters;
        public nint Instance;
        public nint Menu;
        public nint Parent;
        public int Height;
        public int Width;
        public int Y;
        public int X;
        public int Style;
        public nint Name;
        public nint Class;
        public uint ExtendedStyle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public uint Size;
        public NativeRectangle Monitor;
        public NativeRectangle WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPlacement
    {
        public uint Length;
        public uint Flags;
        public int ShowCommand;
        public NativePoint MinimumPosition;
        public NativePoint MaximumPosition;
        public NativeRectangle NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DeviceMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TrueTypeOption;
        public short Collate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;

        public ushort LogPixels;
        public uint BitsPerPixel;
        public uint PixelsWidth;
        public uint PixelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint IcmMethod;
        public uint IcmIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint WindowHandle;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public NativePoint Point;
        public uint Private;
    }

    private readonly record struct WindowSize(int Width, int Height);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern nint GetModuleHandleW(string moduleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WindowClassEx windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint windowHandle);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint DefWindowProcW(nint windowHandle, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowTextW(nint windowHandle, string text);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint windowHandle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateWindow(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
    private static extern nint GetWindowLongPtrW64(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, SetLastError = true)]
    private static extern int GetWindowLongW32(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
    private static extern nint SetWindowLongPtrW64(nint windowHandle, int index, nint value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", ExactSpelling = true, SetLastError = true)]
    private static extern int SetWindowLongW32(nint windowHandle, int index, int value);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out NativeMessage message, nint windowHandle, uint minimumMessage, uint maximumMessage, uint removeMessage);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref NativeMessage message);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint DispatchMessageW(ref NativeMessage message);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern nint SendMessageTimeoutW(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam,
        uint flags,
        uint timeoutMilliseconds,
        out nuint result);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint windowHandle, out NativeRectangle rectangle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint windowHandle, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint windowHandle);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint LoadCursorW(nint instance, nint cursorName);

    [DllImport("user32.dll")]
    private static extern nint SetCursor(nint cursor);

    [DllImport("user32.dll")]
    private static extern nint SetCapture(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint GetCapture();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint windowHandle, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevicesW(string device, uint deviceIndex, ref DisplayDevice displayDevice, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettingsW(string deviceName, uint modeNumber, ref DeviceMode deviceMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int ChangeDisplaySettingsExW(
        string deviceName,
        ref DeviceMode deviceMode,
        nint windowHandle,
        uint flags,
        nint parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int ChangeDisplaySettingsExW(
        string deviceName,
        nint deviceMode,
        nint windowHandle,
        uint flags,
        nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(nint windowHandle, ref WindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(nint windowHandle, ref WindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustWindowRectEx(ref NativeRectangle rectangle, uint style, [MarshalAs(UnmanagedType.Bool)] bool hasMenu, uint extendedStyle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustWindowRectExForDpi(
        ref NativeRectangle rectangle,
        uint style,
        [MarshalAs(UnmanagedType.Bool)] bool hasMenu,
        uint extendedStyle,
        uint dpi);

    [DllImport("user32.dll")]
    private static extern nint SetThreadDpiAwarenessContext(nint dpiContext);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint windowHandle);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);
}
