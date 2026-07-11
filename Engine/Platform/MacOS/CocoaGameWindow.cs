using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal sealed class CocoaGameWindow : GameWindow
{
    private const nuint TitledWindowMask = 1u << 0;
    private const nuint ClosableWindowMask = 1u << 1;
    private const nuint MiniaturizableWindowMask = 1u << 2;
    private const nuint ResizableWindowMask = 1u << 3;
    private const nuint FullScreenWindowMask = 1u << 14;
    private const nuint DefaultWindowMask = TitledWindowMask | ClosableWindowMask | MiniaturizableWindowMask | ResizableWindowMask;
    private const nuint BackingStoreBuffered = 2;
    private const nuint ViewWidthSizable = 1u << 1;
    private const nuint ViewHeightSizable = 1u << 4;
    private const nuint FullScreenPrimaryCollectionBehavior = 1u << 7;
    private const nuint FullScreenNoneCollectionBehavior = 1u << 9;
    private const nint RegularApplicationActivationPolicy = 0;

    private const nuint CapsLockModifierMask = 1u << 16;
    private const nuint LeftControlDeviceMask = 0x0001;
    private const nuint LeftShiftDeviceMask = 0x0002;
    private const nuint RightShiftDeviceMask = 0x0004;
    private const nuint LeftCommandDeviceMask = 0x0008;
    private const nuint RightCommandDeviceMask = 0x0010;
    private const nuint LeftOptionDeviceMask = 0x0020;
    private const nuint RightOptionDeviceMask = 0x0040;
    private const nuint RightControlDeviceMask = 0x2000;

    private static readonly object RuntimeLock = new();
    private static readonly Dictionary<nint, CocoaGameWindow> CallbackTargets = [];
    private static readonly CocoaNative.VoidCallback WindowDidResizeCallback = WindowDidResize;
    private static readonly CocoaNative.VoidCallback WindowDidMoveCallback = WindowDidMove;
    private static readonly CocoaNative.VoidCallback WindowDidBecomeKeyCallback = WindowDidBecomeKey;
    private static readonly CocoaNative.VoidCallback WindowDidResignKeyCallback = WindowDidResignKey;
    private static readonly CocoaNative.VoidCallback WindowWillCloseCallback = WindowWillClose;
    private static readonly CocoaNative.VoidCallback WindowDidChangeBackingCallback = WindowDidChangeBacking;
    private static readonly CocoaNative.VoidCallback WindowDidMiniaturizeCallback = WindowDidMiniaturize;
    private static readonly CocoaNative.VoidCallback WindowDidDeminiaturizeCallback = WindowDidDeminiaturize;
    private static readonly CocoaNative.VoidCallback WindowDidEnterFullScreenCallback = WindowDidEnterFullScreen;
    private static readonly CocoaNative.VoidCallback WindowDidExitFullScreenCallback = WindowDidExitFullScreen;
    private static readonly CocoaNative.VoidCallback WindowDidFailToEnterFullScreenCallback = WindowDidFailToEnterFullScreen;
    private static readonly CocoaNative.VoidCallback WindowDidFailToExitFullScreenCallback = WindowDidFailToExitFullScreen;
    private static readonly CocoaNative.BoolCallback WindowShouldCloseCallback = WindowShouldClose;
    private static readonly CocoaNative.BoolCallback ReturnTrueWithValueCallback = ReturnTrueWithValue;
    private static readonly CocoaNative.BoolNoArgumentCallback ReturnTrueCallback = ReturnTrue;
    private static readonly CocoaNative.EventCallback KeyDownCallback = KeyDown;
    private static readonly CocoaNative.EventCallback KeyUpCallback = KeyUp;
    private static readonly CocoaNative.EventCallback FlagsChangedCallback = FlagsChanged;
    private static readonly CocoaNative.EventCallback MouseMovedCallback = MouseMoved;
    private static readonly CocoaNative.EventCallback LeftMouseDownCallback = LeftMouseDown;
    private static readonly CocoaNative.EventCallback LeftMouseUpCallback = LeftMouseUp;
    private static readonly CocoaNative.EventCallback RightMouseDownCallback = RightMouseDown;
    private static readonly CocoaNative.EventCallback RightMouseUpCallback = RightMouseUp;
    private static readonly CocoaNative.EventCallback OtherMouseDownCallback = OtherMouseDown;
    private static readonly CocoaNative.EventCallback OtherMouseUpCallback = OtherMouseUp;
    private static readonly CocoaNative.EventCallback ScrollWheelCallback = ScrollWheel;
    private static readonly CocoaNative.VoidNoArgumentCallback ViewBackingChangedCallback = ViewBackingChanged;
    private static readonly CocoaNative.TextCallback InsertTextCallback = InsertText;
    private static readonly CocoaNative.TextAndRangeCallback InsertTextWithRangeCallback = InsertTextWithRange;
    private static readonly CocoaNative.MarkedTextCallback SetMarkedTextCallback = SetMarkedText;
    private static readonly CocoaNative.VoidNoArgumentCallback UnmarkTextCallback = UnmarkText;
    private static readonly CocoaNative.EventCallback DoCommandCallback = IgnoreTextCommand;
    private static readonly CocoaNative.RangeCallback SelectedRangeCallback = SelectedRange;
    private static readonly CocoaNative.RangeCallback MarkedRangeCallback = MarkedRange;
    private static readonly CocoaNative.BoolNoArgumentCallback HasMarkedTextCallback = HasMarkedText;
    private static readonly CocoaNative.ObjectForRangeCallback AttributedSubstringCallback = AttributedSubstring;
    private static readonly CocoaNative.ObjectNoArgumentCallback ValidMarkedTextAttributesCallback = ValidMarkedTextAttributes;
    private static readonly CocoaNative.RectForRangeCallback FirstRectForCharacterRangeCallback = FirstRectForCharacterRange;
    private static readonly CocoaNative.IndexForPointCallback CharacterIndexForPointCallback = CharacterIndexForPoint;

    private static nint _application;
    private static nint _delegateClass;
    private static nint _viewClass;
    private static nint _appKitFramework;
    private static nint _quartzCoreFramework;
    private static bool _applicationInitialized;

    private readonly CocoaNativeCleanupCoordinator _nativeCleanup = new();
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];
    private nint _window;
    private nint _view;
    private nint _delegate;
    private nint _metalLayer;
    private bool _exists;
    private bool _focused;
    private bool _resizable = true;
    private bool _cursorVisible = true;
    private bool _cursorHiddenByWindow;
    private bool _disposed;
    private bool _closingRaised;
    private bool _initializing = true;
    private string _title;
    private int _x;
    private int _y;
    private int _width;
    private int _height;
    private int _framebufferWidth;
    private int _framebufferHeight;
    private float _scaleFactor = 1f;
    private WindowState _state = WindowState.Normal;
    private WindowState? _pendingWindowState;
    private bool _enteringFullScreen;
    private bool _exitingFullScreen;
    private CocoaDisplayModeSession _exclusiveDisplayModeSession;
    private CocoaNative.NativeRect _windowedFrame;
    private nuint _windowedStyleMask;
    private nuint _windowedCollectionBehavior;
    private nint _windowedLevel;
    private InputSnapshot _pendingInput = new();
    private Vector2 _lastMousePosition;
    private string _markedText = string.Empty;
    private CocoaNative.NativeRange _markedSelection;
    private Exception _callbackException;

    public CocoaGameWindow(WindowCreateInfo createInfo)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Cocoa windows can only be created on macOS.");
        if (createInfo.WindowWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window width must be greater than zero.");
        if (createInfo.WindowHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(createInfo), "Window height must be greater than zero.");

        EnsureFrameworksLoaded();
        EnsureMainThread();
        EnsureApplication();
        EnsureRuntimeClasses();

        _title = createInfo.WindowTitle ?? string.Empty;

        using var pool = new CocoaAutoreleasePool();

        try
        {
            var contentRect = new CocoaNative.NativeRect(0, 0, createInfo.WindowWidth, createInfo.WindowHeight);
            _window = CocoaNative.SendIntPtr(
                CocoaNative.SendIntPtr(CocoaNative.GetClass("NSWindow"), CocoaNative.Selector("alloc")),
                CocoaNative.Selector("initWithContentRect:styleMask:backing:defer:"),
                contentRect,
                DefaultWindowMask,
                BackingStoreBuffered,
                0);

            if (_window == 0)
                throw new InvalidOperationException("Failed to create the Cocoa window.");

            _view = CocoaNative.SendIntPtr(
                CocoaNative.SendIntPtr(_viewClass, CocoaNative.Selector("alloc")),
                CocoaNative.Selector("initWithFrame:"),
                contentRect);
            _delegate = CocoaNative.SendIntPtr(
                CocoaNative.SendIntPtr(_delegateClass, CocoaNative.Selector("alloc")),
                CocoaNative.Selector("init"));

            if (_view == 0 || _delegate == 0)
                throw new InvalidOperationException("Failed to create the Cocoa window support objects.");

            RegisterCallbackTarget(_view, this);
            RegisterCallbackTarget(_delegate, this);

            CocoaNative.SendVoid(_window, CocoaNative.Selector("setReleasedWhenClosed:"), (byte)0);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setRestorable:"), (byte)0);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setAcceptsMouseMovedEvents:"), (byte)1);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setCollectionBehavior:"), FullScreenPrimaryCollectionBehavior);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setDelegate:"), _delegate);
            CocoaNative.SendVoid(_view, CocoaNative.Selector("setAutoresizingMask:"), ViewWidthSizable | ViewHeightSizable);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setContentView:"), _view);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("makeFirstResponder:"), _view);

            SetNativeTitle();
            CreateMetalLayer();
            SetWindowTopLeft(createInfo.X, createInfo.Y);

            _exists = true;
            RefreshGeometry(false);
            _focused = CocoaNative.SendBool(_window, CocoaNative.Selector("isKeyWindow"));
            ApplyInitialState(createInfo.WindowInitialState);
            RefreshMousePosition(_pendingInput);
            _initializing = false;
            UpdateIdleSleepAssertion();
        }
        catch (Exception creationException)
        {
            Exception cleanupException = null;

            try
            {
                RestoreExclusiveDisplayMode();
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }

            try
            {
                ReleaseNativeObjects();
            }
            catch (Exception exception)
            {
                cleanupException = cleanupException is null
                    ? exception
                    : new AggregateException(cleanupException, exception);
            }

            if (cleanupException is not null)
            {
                throw new AggregateException(
                    "Cocoa window creation and its cleanup both failed.",
                    creationException,
                    cleanupException);
            }

            throw;
        }
    }

    public override NativeWindowKind NativeKind => NativeWindowKind.Cocoa;
    public override nint NativeHandle => _window;
    public override nint NativeDisplayHandle => _metalLayer;
    public override nint NativeViewHandle => _view;
    public nint NativeLayerHandle => _metalLayer;
    public override bool Exists => _exists;
    public override bool Focused => _focused;
    public override int X => _x;
    public override int Y => _y;
    public override int Width => _width;
    public override int Height => _height;
    public override int FramebufferWidth => _framebufferWidth;
    public override int FramebufferHeight => _framebufferHeight;
    public override float ScaleFactor => _scaleFactor;

    public override string Title
    {
        get => _title;
        set
        {
            ThrowIfUnavailable();
            _title = value ?? string.Empty;
            RunNativeOutboundCall(SetNativeTitle);
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

            RunNativeOutboundCall(() =>
            {
                _resizable = value;
                var styleMask = CocoaNative.SendNUInt(_window, CocoaNative.Selector("styleMask"));
                styleMask = value ? styleMask | ResizableWindowMask : styleMask & ~ResizableWindowMask;
                CocoaNative.SendVoid(_window, CocoaNative.Selector("setStyleMask:"), styleMask);
            });
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
            RunNativeOutboundCall(ApplyCursorVisibility);
        }
    }

    public override WindowState State
    {
        get => _state;
        set
        {
            ThrowIfUnavailable();

            if (_state == value)
                return;

            RunNativeOutboundCall(() => ApplyState(value));
        }
    }

    public override InputSnapshot PumpEvents()
    {
        ThrowIfUnavailable();
        using var pool = new CocoaAutoreleasePool();
        var distantPast = CocoaNative.SendIntPtr(CocoaNative.GetClass("NSDate"), CocoaNative.Selector("distantPast"));
        var runLoopMode = CreateNativeString("kCFRunLoopDefaultMode");

        try
        {
            while (true)
            {
                var nativeEvent = CocoaNative.SendIntPtr(
                    _application,
                    CocoaNative.Selector("nextEventMatchingMask:untilDate:inMode:dequeue:"),
                    nuint.MaxValue,
                    distantPast,
                    runLoopMode,
                    1);

                if (nativeEvent == 0)
                    break;

                RunNativeOutboundCall(() =>
                    CocoaNative.SendVoid(_application, CocoaNative.Selector("sendEvent:"), nativeEvent));

                if (_disposed || !_exists)
                    break;
            }

            if (!_disposed && _exists)
            {
                RunNativeOutboundCall(() =>
                    CocoaNative.SendVoid(_application, CocoaNative.Selector("updateWindows")));

                if (!_disposed && _exists)
                    RefreshMousePosition(_pendingInput);
            }

            var snapshot = _pendingInput;
            _pendingInput = new InputSnapshot();

            if (_callbackException is not null)
            {
                var callbackException = _callbackException;
                _callbackException = null;
                throw new InvalidOperationException("A Cocoa window callback failed.", callbackException);
            }

            return snapshot;
        }
        finally
        {
            CocoaNative.SendVoid(runLoopMode, CocoaNative.Selector("release"));
        }
    }

    public override DisplayMode GetCurrentDisplayMode(int displayIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMainThread();

        using var pool = new CocoaAutoreleasePool();
        var screens = CocoaNative.SendIntPtr(CocoaNative.GetClass("NSScreen"), CocoaNative.Selector("screens"));
        var screenCount = checked((int)CocoaNative.SendNUInt(screens, CocoaNative.Selector("count")));

        if (displayIndex < 0 || displayIndex >= screenCount)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), $"Display {displayIndex} does not exist.");

        var screen = CocoaNative.SendIntPtr(screens, CocoaNative.Selector("objectAtIndex:"), (nuint)displayIndex);
        var scale = (float)Math.Max(1d, CocoaNative.SendDouble(screen, CocoaNative.Selector("backingScaleFactor")));
        var screenFrame = CocoaNative.SendRect(screen, CocoaNative.Selector("frame"));
        var width = checked((int)Math.Round(screenFrame.Size.Width * scale));
        var height = checked((int)Math.Round(screenFrame.Size.Height * scale));
        var refreshRate = 0;
        var maximumFramesPerSecond = CocoaNative.Selector("maximumFramesPerSecond");

        if (CocoaNative.SendBool(screen, CocoaNative.Selector("respondsToSelector:"), maximumFramesPerSecond))
            refreshRate = checked((int)CocoaNative.SendNInt(screen, maximumFramesPerSecond));

        return new DisplayMode(width, height, refreshRate);
    }

    public override void Dispose()
    {
        if (_nativeCleanup.CleanupCompleted && _exclusiveDisplayModeSession is null)
            return;

        EnsureMainThread();

        if (_disposed)
        {
            var pendingTeardown = new TeardownCollector();
            pendingTeardown.Run(RestoreExclusiveDisplayMode);
            pendingTeardown.Run(DrainNativeCleanup);
            pendingTeardown.ThrowIfFailed("Cocoa window disposal failed.");
            return;
        }

        _disposed = true;
        _exists = false;
        _focused = false;
        _pendingWindowState = null;
        _enteringFullScreen = false;
        _exitingFullScreen = false;
        _nativeCleanup.RequestCleanup();

        var teardown = new TeardownCollector();
        teardown.Run(ReleaseIdleSleepAssertion);
        teardown.Run(RestoreExclusiveDisplayMode);
        if (_exclusiveDisplayModeSession is null)
            _state = WindowState.Normal;
        ReleasePressedInputs();
        teardown.Run(RestoreCursorIfHidden);
        teardown.Run(DiscardMarkedText);
        teardown.Run(DrainNativeCleanup);
        teardown.ThrowIfFailed("Cocoa window disposal failed.");
    }

    private void RunNativeOutboundCall(Action action)
    {
        _nativeCleanup.EnterOutboundCall();
        Exception callException = null;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            callException = exception;
        }

        var teardown = callException is null
            ? new TeardownCollector()
            : new TeardownCollector(callException);
        teardown.Run(_nativeCleanup.ExitOutboundCall);
        teardown.Run(DrainNativeCleanup);
        teardown.ThrowIfFailed("A Cocoa outbound call or its deferred cleanup failed.");
    }

    private void DrainNativeCleanup()
    {
        if (!_nativeCleanup.TryBeginCleanup())
            return;

        try
        {
            using var pool = new CocoaAutoreleasePool();
            ReleaseNativeObjects();
        }
        finally
        {
            _nativeCleanup.CompleteCleanup();
            GC.SuppressFinalize(this);
        }
    }

    private static void EnsureFrameworksLoaded()
    {
        lock (RuntimeLock)
        {
            _appKitFramework = _appKitFramework == 0
                ? NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit")
                : _appKitFramework;
            _quartzCoreFramework = _quartzCoreFramework == 0
                ? NativeLibrary.Load("/System/Library/Frameworks/QuartzCore.framework/QuartzCore")
                : _quartzCoreFramework;
        }
    }

    private static void EnsureApplication()
    {
        lock (RuntimeLock)
        {
            if (_applicationInitialized)
                return;

            _application = CocoaNative.SendIntPtr(CocoaNative.GetClass("NSApplication"), CocoaNative.Selector("sharedApplication"));
            if (_application == 0)
                throw new InvalidOperationException("Failed to initialize NSApplication.");

            CocoaNative.SendBool(_application, CocoaNative.Selector("setActivationPolicy:"), RegularApplicationActivationPolicy);
            CocoaNative.SendVoid(_application, CocoaNative.Selector("finishLaunching"));
            _applicationInitialized = true;
        }
    }

    private static void EnsureRuntimeClasses()
    {
        lock (RuntimeLock)
        {
            if (_delegateClass != 0 && _viewClass != 0)
                return;

            _delegateClass = CocoaNative.GetClass("ElementEngineCocoaWindowDelegate");
            if (_delegateClass == 0)
            {
                _delegateClass = CocoaNative.AllocateClass(CocoaNative.GetClass("NSObject"), "ElementEngineCocoaWindowDelegate");
                AddMethod(_delegateClass, "windowDidResize:", WindowDidResizeCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidMove:", WindowDidMoveCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidBecomeKey:", WindowDidBecomeKeyCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidResignKey:", WindowDidResignKeyCallback, "v@:@");
                AddMethod(_delegateClass, "windowWillClose:", WindowWillCloseCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidChangeBackingProperties:", WindowDidChangeBackingCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidMiniaturize:", WindowDidMiniaturizeCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidDeminiaturize:", WindowDidDeminiaturizeCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidEnterFullScreen:", WindowDidEnterFullScreenCallback, "v@:@");
                AddMethod(_delegateClass, "windowDidExitFullScreen:", WindowDidExitFullScreenCallback, "v@:@");
                AddMethod(
                    _delegateClass,
                    "windowDidFailToEnterFullScreen:",
                    WindowDidFailToEnterFullScreenCallback,
                    "v@:@");
                AddMethod(
                    _delegateClass,
                    "windowDidFailToExitFullScreen:",
                    WindowDidFailToExitFullScreenCallback,
                    "v@:@");
                AddMethod(_delegateClass, "windowShouldClose:", WindowShouldCloseCallback, "c@:@");
                CocoaNative.RegisterClass(_delegateClass);
            }

            _viewClass = CocoaNative.GetClass("ElementEngineCocoaMetalView");
            if (_viewClass == 0)
            {
                _viewClass = CocoaNative.AllocateClass(CocoaNative.GetClass("NSView"), "ElementEngineCocoaMetalView");
                AddMethod(_viewClass, "acceptsFirstResponder", ReturnTrueCallback, "c@:");
                AddMethod(_viewClass, "acceptsFirstMouse:", ReturnTrueWithValueCallback, "c@:@");
                AddMethod(_viewClass, "isFlipped", ReturnTrueCallback, "c@:");
                AddMethod(_viewClass, "keyDown:", KeyDownCallback, "v@:@");
                AddMethod(_viewClass, "keyUp:", KeyUpCallback, "v@:@");
                AddMethod(_viewClass, "flagsChanged:", FlagsChangedCallback, "v@:@");
                AddMethod(_viewClass, "mouseMoved:", MouseMovedCallback, "v@:@");
                AddMethod(_viewClass, "mouseDragged:", MouseMovedCallback, "v@:@");
                AddMethod(_viewClass, "rightMouseDragged:", MouseMovedCallback, "v@:@");
                AddMethod(_viewClass, "otherMouseDragged:", MouseMovedCallback, "v@:@");
                AddMethod(_viewClass, "mouseDown:", LeftMouseDownCallback, "v@:@");
                AddMethod(_viewClass, "mouseUp:", LeftMouseUpCallback, "v@:@");
                AddMethod(_viewClass, "rightMouseDown:", RightMouseDownCallback, "v@:@");
                AddMethod(_viewClass, "rightMouseUp:", RightMouseUpCallback, "v@:@");
                AddMethod(_viewClass, "otherMouseDown:", OtherMouseDownCallback, "v@:@");
                AddMethod(_viewClass, "otherMouseUp:", OtherMouseUpCallback, "v@:@");
                AddMethod(_viewClass, "scrollWheel:", ScrollWheelCallback, "v@:@");
                AddMethod(_viewClass, "viewDidChangeBackingProperties", ViewBackingChangedCallback, "v@:");
                AddMethod(_viewClass, "insertText:", InsertTextCallback, "v@:@");
                AddMethod(
                    _viewClass,
                    "insertText:replacementRange:",
                    InsertTextWithRangeCallback,
                    "v@:@{_NSRange=QQ}");
                AddMethod(
                    _viewClass,
                    "setMarkedText:selectedRange:replacementRange:",
                    SetMarkedTextCallback,
                    "v@:@{_NSRange=QQ}{_NSRange=QQ}");
                AddMethod(_viewClass, "unmarkText", UnmarkTextCallback, "v@:");
                AddMethod(_viewClass, "doCommandBySelector:", DoCommandCallback, "v@::");
                AddMethod(_viewClass, "selectedRange", SelectedRangeCallback, "{_NSRange=QQ}@:");
                AddMethod(_viewClass, "markedRange", MarkedRangeCallback, "{_NSRange=QQ}@:");
                AddMethod(_viewClass, "hasMarkedText", HasMarkedTextCallback, "c@:");
                AddMethod(
                    _viewClass,
                    "attributedSubstringForProposedRange:actualRange:",
                    AttributedSubstringCallback,
                    "@@:{_NSRange=QQ}^{_NSRange=QQ}");
                AddMethod(
                    _viewClass,
                    "validAttributesForMarkedText",
                    ValidMarkedTextAttributesCallback,
                    "@@:");
                AddMethod(
                    _viewClass,
                    "firstRectForCharacterRange:actualRange:",
                    FirstRectForCharacterRangeCallback,
                    "{CGRect={CGPoint=dd}{CGSize=dd}}@:{_NSRange=QQ}^{_NSRange=QQ}");
                AddMethod(
                    _viewClass,
                    "characterIndexForPoint:",
                    CharacterIndexForPointCallback,
                    "Q@:{CGPoint=dd}");

                var textInputClientProtocol = CocoaNative.GetProtocol("NSTextInputClient");
                if (textInputClientProtocol == 0 || !CocoaNative.AddProtocol(_viewClass, textInputClientProtocol))
                    throw new InvalidOperationException("Failed to add NSTextInputClient conformance to the Cocoa view.");

                CocoaNative.RegisterClass(_viewClass);
            }
        }
    }

    private static void AddMethod<TDelegate>(nint type, string selector, TDelegate callback, string signature)
        where TDelegate : Delegate
    {
        if (!CocoaNative.AddMethod(type, CocoaNative.Selector(selector), Marshal.GetFunctionPointerForDelegate(callback), signature))
            throw new InvalidOperationException($"Failed to add Cocoa method {selector}.");
    }

    private static void EnsureMainThread()
    {
        if (!CocoaNative.SendBool(CocoaNative.GetClass("NSThread"), CocoaNative.Selector("isMainThread")))
            throw new InvalidOperationException("Cocoa window operations must run on the macOS main thread.");
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMainThread();
    }

    private void ApplyInitialState(WindowState initialState)
    {
        switch (initialState)
        {
            case WindowState.Hidden:
                _state = WindowState.Hidden;
                break;
            case WindowState.Minimized:
                ShowWindow();
                CocoaNative.SendVoid(_window, CocoaNative.Selector("miniaturize:"), (nint)0);
                _state = WindowState.Minimized;
                break;
            case WindowState.Maximized:
                ShowWindow();
                CocoaNative.SendVoid(_window, CocoaNative.Selector("zoom:"), (nint)0);
                _state = WindowState.Maximized;
                break;
            case WindowState.FullScreen:
                EnterExclusiveFullScreen();
                break;
            case WindowState.BorderlessFullScreen:
                ShowWindow();
                _state = WindowState.BorderlessFullScreen;
                _enteringFullScreen = true;
                CocoaNative.SendVoid(_window, CocoaNative.Selector("toggleFullScreen:"), (nint)0);
                break;
            case WindowState.Normal:
                ShowWindow();
                _state = WindowState.Normal;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialState));
        }
    }

    private void ApplyState(WindowState state)
    {
        if (_enteringFullScreen || _exitingFullScreen)
        {
            _pendingWindowState = state;
            _state = state;
            return;
        }

        var nativeFullScreen = IsNativeFullScreen();
        var currentState = _exclusiveDisplayModeSession is not null
            ? WindowState.FullScreen
            : nativeFullScreen
                ? WindowState.BorderlessFullScreen
                : _state;
        var displayModeTransition = CocoaFullScreenTransition.GetDisplayModeTransition(currentState, state);

        if (displayModeTransition == CocoaDisplayModeTransition.RestoreDesktop)
            LeaveExclusiveFullScreen();

        if (state == WindowState.FullScreen)
        {
            if (nativeFullScreen)
            {
                _pendingWindowState = state;
                _state = state;
                _exitingFullScreen = true;
                CocoaNative.SendVoid(_window, CocoaNative.Selector("toggleFullScreen:"), (nint)0);
                return;
            }

            if (displayModeTransition == CocoaDisplayModeTransition.EnterExclusive)
                EnterExclusiveFullScreen();

            return;
        }

        if (state == WindowState.BorderlessFullScreen)
        {
            _state = state;
            ShowWindow();

            if (!nativeFullScreen)
            {
                _enteringFullScreen = true;
                CocoaNative.SendVoid(_window, CocoaNative.Selector("toggleFullScreen:"), (nint)0);
            }

            return;
        }

        if (nativeFullScreen)
        {
            _pendingWindowState = state;
            _state = state;
            _exitingFullScreen = true;
            CocoaNative.SendVoid(_window, CocoaNative.Selector("toggleFullScreen:"), (nint)0);
            return;
        }

        ApplyWindowedState(state);
    }

    private void ApplyWindowedState(WindowState state)
    {
        switch (state)
        {
            case WindowState.Hidden:
                CocoaNative.SendVoid(_window, CocoaNative.Selector("orderOut:"), (nint)0);
                break;
            case WindowState.Minimized:
                CocoaNative.SendVoid(_window, CocoaNative.Selector("miniaturize:"), (nint)0);
                break;
            case WindowState.Maximized:
                ShowWindow();
                if (!CocoaNative.SendBool(_window, CocoaNative.Selector("isZoomed")))
                    CocoaNative.SendVoid(_window, CocoaNative.Selector("zoom:"), (nint)0);
                break;
            case WindowState.Normal:
                CocoaNative.SendVoid(_window, CocoaNative.Selector("deminiaturize:"), (nint)0);
                ShowWindow();
                if (CocoaNative.SendBool(_window, CocoaNative.Selector("isZoomed")))
                    CocoaNative.SendVoid(_window, CocoaNative.Selector("zoom:"), (nint)0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }

        _state = state;
    }

    private void HandleDidEnterFullScreen()
    {
        _enteringFullScreen = false;
        _state = WindowState.BorderlessFullScreen;
        ApplyPendingWindowState();
    }

    private void HandleDidExitFullScreen()
    {
        _exitingFullScreen = false;
        _state = WindowState.Normal;
        ApplyPendingWindowState();
    }

    private void HandleDidFailToEnterFullScreen()
    {
        _enteringFullScreen = false;
        _pendingWindowState = null;
        _state = WindowState.Normal;
        throw new InvalidOperationException("macOS failed to enter borderless full screen.");
    }

    private void HandleDidFailToExitFullScreen()
    {
        _exitingFullScreen = false;
        _pendingWindowState = null;
        _state = WindowState.BorderlessFullScreen;
        throw new InvalidOperationException("macOS failed to exit borderless full screen.");
    }

    private void EnterExclusiveFullScreen()
    {
        if (_exclusiveDisplayModeSession is not null)
        {
            _state = WindowState.FullScreen;
            return;
        }

        CocoaNative.SendVoid(_window, CocoaNative.Selector("deminiaturize:"), (nint)0);

        var screen = CocoaNative.SendIntPtr(_window, CocoaNative.Selector("screen"));
        if (screen == 0)
            screen = CocoaNative.SendIntPtr(CocoaNative.GetClass("NSScreen"), CocoaNative.Selector("mainScreen"));
        if (screen == 0)
            throw new InvalidOperationException("Failed to determine the Cocoa window's display.");

        var display = GetDisplayId(screen);
        var windowedFrame = CocoaNative.SendRect(_window, CocoaNative.Selector("frame"));
        var windowedStyleMask = CocoaNative.SendNUInt(_window, CocoaNative.Selector("styleMask"));
        var windowedCollectionBehavior = CocoaNative.SendNUInt(_window, CocoaNative.Selector("collectionBehavior"));
        var windowedLevel = CocoaNative.SendNInt(_window, CocoaNative.Selector("level"));
        var session = CocoaDisplayModeSession.Enter(display, Math.Max(1, _width), Math.Max(1, _height));
        _exclusiveDisplayModeSession = session;

        try
        {
            _windowedFrame = windowedFrame;
            _windowedStyleMask = windowedStyleMask;
            _windowedCollectionBehavior = windowedCollectionBehavior;
            _windowedLevel = windowedLevel;
            CocoaNative.SendVoid(
                _window,
                CocoaNative.Selector("setCollectionBehavior:"),
                FullScreenNoneCollectionBehavior);
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setStyleMask:"), (nuint)0);
            CocoaNative.SendVoid(
                _window,
                CocoaNative.Selector("setLevel:"),
                CocoaDisplayNative.GetFullScreenWindowLevel());
            CocoaNative.SendVoid(
                _window,
                CocoaNative.Selector("setFrame:display:"),
                CocoaNative.SendRect(screen, CocoaNative.Selector("frame")),
                (byte)1);
            ShowWindow();
            _state = WindowState.FullScreen;
            RefreshGeometry(false);
        }
        catch (Exception transitionException)
        {
            Exception rollbackException = null;

            try
            {
                session.Restore();
                _exclusiveDisplayModeSession = null;
                CocoaNative.SendVoid(
                    _window,
                    CocoaNative.Selector("setCollectionBehavior:"),
                    windowedCollectionBehavior);
                CocoaNative.SendVoid(_window, CocoaNative.Selector("setStyleMask:"), windowedStyleMask);
                CocoaNative.SendVoid(_window, CocoaNative.Selector("setLevel:"), windowedLevel);
                CocoaNative.SendVoid(
                    _window,
                    CocoaNative.Selector("setFrame:display:"),
                    windowedFrame,
                    (byte)1);
                _state = WindowState.Normal;
                RefreshGeometry(false);
            }
            catch (Exception exception)
            {
                rollbackException = exception;
                _state = _exclusiveDisplayModeSession is null
                    ? WindowState.Normal
                    : WindowState.FullScreen;
            }

            if (rollbackException is not null)
            {
                throw new AggregateException(
                    "The macOS full-screen transition and its rollback both failed.",
                    transitionException,
                    rollbackException);
            }

            throw;
        }
    }

    private void LeaveExclusiveFullScreen()
    {
        if (_exclusiveDisplayModeSession is null)
            return;

        _exclusiveDisplayModeSession.Restore();
        _exclusiveDisplayModeSession = null;
        _state = WindowState.Normal;
        CocoaNative.SendVoid(
            _window,
            CocoaNative.Selector("setCollectionBehavior:"),
            _windowedCollectionBehavior);
        CocoaNative.SendVoid(_window, CocoaNative.Selector("setStyleMask:"), _windowedStyleMask);
        CocoaNative.SendVoid(_window, CocoaNative.Selector("setLevel:"), _windowedLevel);
        CocoaNative.SendVoid(
            _window,
            CocoaNative.Selector("setFrame:display:"),
            _windowedFrame,
            (byte)1);
        RefreshGeometry(false);
    }

    private void RestoreExclusiveDisplayMode()
    {
        if (_exclusiveDisplayModeSession is null)
            return;

        _exclusiveDisplayModeSession.Restore();
        _exclusiveDisplayModeSession = null;
        _state = WindowState.Normal;
    }

    private static uint GetDisplayId(nint screen)
    {
        using var pool = new CocoaAutoreleasePool();
        var deviceDescription = CocoaNative.SendIntPtr(screen, CocoaNative.Selector("deviceDescription"));
        var screenNumberKey = CreateNativeString("NSScreenNumber");

        try
        {
            var screenNumber = CocoaNative.SendIntPtr(
                deviceDescription,
                CocoaNative.Selector("objectForKey:"),
                screenNumberKey);
            if (screenNumber == 0)
                throw new InvalidOperationException("The Cocoa screen does not expose a CoreGraphics display ID.");

            return checked((uint)CocoaNative.SendNUInt(screenNumber, CocoaNative.Selector("unsignedIntValue")));
        }
        finally
        {
            CocoaNative.SendVoid(screenNumberKey, CocoaNative.Selector("release"));
        }
    }

    private void ApplyPendingWindowState()
    {
        if (!_pendingWindowState.HasValue)
            return;

        var state = _pendingWindowState.Value;
        _pendingWindowState = null;
        ApplyState(state);
    }

    private void ShowWindow()
    {
        CocoaNative.SendVoid(_window, CocoaNative.Selector("makeKeyAndOrderFront:"), (nint)0);
        CocoaNative.SendVoid(_window, CocoaNative.Selector("makeFirstResponder:"), _view);
        CocoaNative.SendVoid(_application, CocoaNative.Selector("activateIgnoringOtherApps:"), (byte)1);
    }

    private bool IsNativeFullScreen() =>
        (CocoaNative.SendNUInt(_window, CocoaNative.Selector("styleMask")) & FullScreenWindowMask) != 0;

    private void SetNativeTitle()
    {
        using var pool = new CocoaAutoreleasePool();
        var nativeTitle = CreateNativeString(_title);

        try
        {
            CocoaNative.SendVoid(_window, CocoaNative.Selector("setTitle:"), nativeTitle);
        }
        finally
        {
            CocoaNative.SendVoid(nativeTitle, CocoaNative.Selector("release"));
        }
    }

    private void CreateMetalLayer()
    {
        var metalLayerClass = CocoaNative.GetClass("CAMetalLayer");
        if (metalLayerClass == 0)
            throw new InvalidOperationException("CAMetalLayer is unavailable.");

        _metalLayer = CocoaNative.SendIntPtr(metalLayerClass, CocoaNative.Selector("layer"));
        if (_metalLayer == 0)
            throw new InvalidOperationException("Failed to create CAMetalLayer.");

        CocoaNative.SendVoid(_view, CocoaNative.Selector("setWantsLayer:"), (byte)1);
        CocoaNative.SendVoid(_view, CocoaNative.Selector("setLayer:"), _metalLayer);
        CocoaNative.SendVoid(_metalLayer, CocoaNative.Selector("setOpaque:"), (byte)1);
        CocoaNative.SendVoid(_metalLayer, CocoaNative.Selector("setFramebufferOnly:"), (byte)1);
        CocoaNative.SendVoid(_metalLayer, CocoaNative.Selector("setPresentsWithTransaction:"), (byte)0);
    }

    private void SetWindowTopLeft(int x, int y)
    {
        var top = GetPrimaryScreenTop();
        CocoaNative.SendVoid(
            _window,
            CocoaNative.Selector("setFrameTopLeftPoint:"),
            new CocoaNative.NativePoint(x, top - y));
    }

    private void RefreshGeometry(bool notify)
    {
        if (_window == 0 || _view == 0)
            return;

        var windowFrame = CocoaNative.SendRect(_window, CocoaNative.Selector("frame"));
        var viewBounds = CocoaNative.SendRect(_view, CocoaNative.Selector("bounds"));
        var width = Math.Max(0, checked((int)Math.Round(viewBounds.Size.Width)));
        var height = Math.Max(0, checked((int)Math.Round(viewBounds.Size.Height)));
        var scale = (float)Math.Max(1d, CocoaNative.SendDouble(_window, CocoaNative.Selector("backingScaleFactor")));
        var framebufferWidth = Math.Max(0, checked((int)Math.Round(width * scale)));
        var framebufferHeight = Math.Max(0, checked((int)Math.Round(height * scale)));
        var x = checked((int)Math.Round(windowFrame.Origin.X));
        var y = checked((int)Math.Round(GetPrimaryScreenTop() - windowFrame.Origin.Y - windowFrame.Size.Height));

        var moved = x != _x || y != _y;
        var resized = width != _width || height != _height ||
                      framebufferWidth != _framebufferWidth || framebufferHeight != _framebufferHeight;
        var scaleChanged = Math.Abs(scale - _scaleFactor) > 0.001f;

        _x = x;
        _y = y;
        _width = width;
        _height = height;
        _framebufferWidth = framebufferWidth;
        _framebufferHeight = framebufferHeight;
        _scaleFactor = scale;
        UpdateMetalLayer();
        RefreshWindowedState();

        if (!notify || _initializing)
            return;

        if (scaleChanged)
            OnScaleFactorChanged();
        if (_disposed)
            return;
        if (moved)
            OnMoved();
        if (_disposed)
            return;
        if (resized)
            OnResized();
    }

    private void UpdateMetalLayer()
    {
        if (_metalLayer == 0)
            return;

        CocoaNative.SendVoid(_metalLayer, CocoaNative.Selector("setContentsScale:"), (double)_scaleFactor);
        CocoaNative.SendVoid(
            _metalLayer,
            CocoaNative.Selector("setDrawableSize:"),
            new CocoaNative.NativeSize(_framebufferWidth, _framebufferHeight));
    }

    private void RefreshWindowedState()
    {
        if (_state == WindowState.Hidden ||
            _enteringFullScreen ||
            _exitingFullScreen ||
            _exclusiveDisplayModeSession is not null ||
            IsNativeFullScreen())
        {
            return;
        }

        if (CocoaNative.SendBool(_window, CocoaNative.Selector("isMiniaturized")))
            _state = WindowState.Minimized;
        else if (CocoaNative.SendBool(_window, CocoaNative.Selector("isZoomed")))
            _state = WindowState.Maximized;
        else
            _state = WindowState.Normal;
    }

    private static double GetPrimaryScreenTop()
    {
        var screens = CocoaNative.SendIntPtr(CocoaNative.GetClass("NSScreen"), CocoaNative.Selector("screens"));
        if (screens == 0 || CocoaNative.SendNUInt(screens, CocoaNative.Selector("count")) == 0)
            return 0;

        var primaryScreen = CocoaNative.SendIntPtr(screens, CocoaNative.Selector("objectAtIndex:"), (nuint)0);
        var primaryFrame = CocoaNative.SendRect(primaryScreen, CocoaNative.Selector("frame"));
        return primaryFrame.Origin.Y + primaryFrame.Size.Height;
    }

    private uint _idleSleepAssertion;

    protected override void OnAllowScreenSaverChanged() => UpdateIdleSleepAssertion();

    private void UpdateIdleSleepAssertion()
    {
        if (AllowScreenSaver || _disposed || !_exists)
        {
            ReleaseIdleSleepAssertion();
            return;
        }

        if (_idleSleepAssertion != 0)
            return;

        // NSString is toll-free bridged to CFString, so native strings work directly.
        var assertionType = CreateNativeString("PreventUserIdleDisplaySleep");
        var reason = CreateNativeString("ElementEngine game window active");

        try
        {
            if (IOPMAssertionCreateWithName(assertionType, IOPMAssertionLevelOn, reason, out var assertion) == 0)
                _idleSleepAssertion = assertion;
        }
        finally
        {
            CocoaNative.SendVoid(assertionType, CocoaNative.Selector("release"));
            CocoaNative.SendVoid(reason, CocoaNative.Selector("release"));
        }
    }

    private void ReleaseIdleSleepAssertion()
    {
        if (_idleSleepAssertion == 0)
            return;

        IOPMAssertionRelease(_idleSleepAssertion);
        _idleSleepAssertion = 0;
    }

    private const uint IOPMAssertionLevelOn = 255;

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    private static extern int IOPMAssertionCreateWithName(
        nint assertionType,
        uint assertionLevel,
        nint assertionName,
        out uint assertionId);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    private static extern int IOPMAssertionRelease(uint assertionId);

    private static nint CreateNativeString(string value)
    {
        var nativeString = CocoaNative.SendIntPtr(
            CocoaNative.SendIntPtr(CocoaNative.GetClass("NSString"), CocoaNative.Selector("alloc")),
            CocoaNative.Selector("initWithUTF8String:"),
            value ?? string.Empty);

        if (nativeString == 0)
            throw new InvalidOperationException("Failed to create an NSString.");

        return nativeString;
    }

    private void ApplyCursorVisibility()
    {
        if (!_cursorVisible && _focused && !_cursorHiddenByWindow)
        {
            CocoaNative.SendVoid(CocoaNative.GetClass("NSCursor"), CocoaNative.Selector("hide"));
            _cursorHiddenByWindow = true;
        }
        else if (_cursorVisible)
        {
            RestoreCursorIfHidden();
        }
    }

    private void RestoreCursorIfHidden()
    {
        if (!_cursorHiddenByWindow)
            return;

        _cursorHiddenByWindow = false;
        CocoaNative.SendVoid(CocoaNative.GetClass("NSCursor"), CocoaNative.Selector("unhide"));
    }

    private void SetFocused(bool focused)
    {
        if (_focused == focused)
            return;

        _focused = focused;
        if (!focused)
        {
            ReleasePressedInputs();
            DiscardMarkedText();
            RestoreCursorIfHidden();
        }
        else
        {
            ApplyCursorVisibility();
        }

        if (!_initializing)
            OnFocusChanged();
    }

    private void DiscardMarkedText()
    {
        try
        {
            if (_view != 0)
            {
                var inputContext = CocoaNative.SendIntPtr(_view, CocoaNative.Selector("inputContext"));
                if (inputContext != 0)
                    CocoaNative.SendVoid(inputContext, CocoaNative.Selector("discardMarkedText"));
            }
        }
        finally
        {
            ClearMarkedText();
        }
    }

    private void HandleKey(nint nativeEvent, bool down)
    {
        var keyCode = checked((int)CocoaNative.SendNUInt(nativeEvent, CocoaNative.Selector("keyCode")));
        var key = TranslateKeyCode(keyCode);

        if (key != Key.Unknown)
        {
            var repeat = down && CocoaNative.SendBool(nativeEvent, CocoaNative.Selector("isARepeat"));
            if (down)
                _pressedKeys.Add(key);
            else
                _pressedKeys.Remove(key);

            _pendingInput.KeyEvents.Add(new KeyEvent(key, down, repeat));
        }

        if (down)
            InterpretKeyEvent(nativeEvent);
    }

    private void HandleFlagsChanged(nint nativeEvent)
    {
        var keyCode = checked((int)CocoaNative.SendNUInt(nativeEvent, CocoaNative.Selector("keyCode")));
        var key = TranslateKeyCode(keyCode);
        if (key == Key.Unknown)
            return;

        var flags = CocoaNative.SendNUInt(nativeEvent, CocoaNative.Selector("modifierFlags"));
        var mask = GetModifierMask(key);
        var down = mask != 0 && (flags & mask) != 0;

        if (key == Key.CapsLock)
        {
            // The CapsLock flag reflects the lock state, not the physical key, so each
            // toggle emits an immediate press+release instead of a minutes-long hold.
            _pendingInput.KeyEvents.Add(new KeyEvent(key, true));
            _pendingInput.KeyEvents.Add(new KeyEvent(key, false));
            return;
        }

        if (down)
            _pressedKeys.Add(key);
        else
            _pressedKeys.Remove(key);

        _pendingInput.KeyEvents.Add(new KeyEvent(key, down));
    }

    private static nuint GetModifierMask(Key key) => key switch
    {
        Key.ControlLeft => LeftControlDeviceMask,
        Key.ControlRight => RightControlDeviceMask,
        Key.ShiftLeft => LeftShiftDeviceMask,
        Key.ShiftRight => RightShiftDeviceMask,
        Key.AltLeft => LeftOptionDeviceMask,
        Key.AltRight => RightOptionDeviceMask,
        Key.WinLeft => LeftCommandDeviceMask,
        Key.WinRight => RightCommandDeviceMask,
        Key.CapsLock => CapsLockModifierMask,
        _ => 0,
    };

    private void InterpretKeyEvent(nint nativeEvent)
    {
        var events = CocoaNative.SendIntPtr(
            CocoaNative.GetClass("NSArray"),
            CocoaNative.Selector("arrayWithObject:"),
            nativeEvent);
        if (events == 0)
            return;

        CocoaNative.SendVoid(_view, CocoaNative.Selector("interpretKeyEvents:"), events);
    }

    private void InsertCommittedText(nint nativeText)
    {
        var characters = GetNativeText(nativeText);
        ClearMarkedText();

        if (string.IsNullOrEmpty(characters))
            return;

        foreach (var character in characters)
        {
            if (!char.IsControl(character) && character is not (>= '\uF700' and <= '\uF8FF'))
                _pendingInput.KeyCharPresses.Add(character);
        }
    }

    private void SetMarkedText(nint nativeText, CocoaNative.NativeRange selectedRange)
    {
        _markedText = GetNativeText(nativeText) ?? string.Empty;
        _markedSelection = selectedRange;
    }

    private void ClearMarkedText()
    {
        _markedText = string.Empty;
        _markedSelection = default;
    }

    private CocoaNative.NativeRange GetSelectedRange() => _markedText.Length == 0
        ? new CocoaNative.NativeRange(0, 0)
        : _markedSelection;

    private CocoaNative.NativeRange GetMarkedRange() => _markedText.Length == 0
        ? new CocoaNative.NativeRange(nuint.MaxValue, 0)
        : new CocoaNative.NativeRange(0, checked((nuint)_markedText.Length));

    private CocoaNative.NativeRect GetFirstRectForCharacterRange(
        CocoaNative.NativeRange range,
        nint actualRange)
    {
        if (actualRange != 0)
            Marshal.StructureToPtr(new CocoaNative.NativeRange(0, 0), actualRange, false);

        var windowFrame = CocoaNative.SendRect(_window, CocoaNative.Selector("frame"));
        return new CocoaNative.NativeRect(
            windowFrame.Origin.X,
            windowFrame.Origin.Y + windowFrame.Size.Height,
            1,
            1);
    }

    private static string GetNativeText(nint nativeText)
    {
        if (nativeText == 0)
            return null;

        var stringSelector = CocoaNative.Selector("string");
        if (CocoaNative.SendBool(
                nativeText,
                CocoaNative.Selector("respondsToSelector:"),
                stringSelector))
        {
            nativeText = CocoaNative.SendIntPtr(nativeText, stringSelector);
        }

        var utf8Characters = nativeText == 0
            ? 0
            : CocoaNative.SendIntPtr(nativeText, CocoaNative.Selector("UTF8String"));
        return utf8Characters == 0 ? null : Marshal.PtrToStringUTF8(utf8Characters);
    }

    private void HandleMouseMove(nint nativeEvent)
    {
        UpdateMousePosition(nativeEvent);
    }

    private void HandleMouseButton(nint nativeEvent, MouseButton button, bool down)
    {
        UpdateMousePosition(nativeEvent);

        if (down)
            _pressedMouseButtons.Add(button);
        else
            _pressedMouseButtons.Remove(button);

        _pendingInput.MouseEvents.Add(new MouseEvent(button, down));
    }

    private void HandleOtherMouseButton(nint nativeEvent, bool down)
    {
        var buttonNumber = checked((int)CocoaNative.SendNInt(nativeEvent, CocoaNative.Selector("buttonNumber")));
        var button = buttonNumber switch
        {
            2 => MouseButton.Middle,
            >= 3 and <= 10 => (MouseButton)((int)MouseButton.Button1 + buttonNumber - 3),
            _ => (MouseButton?)null,
        };

        if (button.HasValue)
            HandleMouseButton(nativeEvent, button.Value, down);
    }

    private void HandleScrollWheel(nint nativeEvent)
    {
        UpdateMousePosition(nativeEvent);
        var delta = CocoaNative.SendDouble(nativeEvent, CocoaNative.Selector("scrollingDeltaY"));
        if (CocoaNative.SendBool(nativeEvent, CocoaNative.Selector("hasPreciseScrollingDeltas")))
            delta /= 10d;

        _pendingInput.WheelDelta += (float)delta;
    }

    private void UpdateMousePosition(nint nativeEvent)
    {
        if (_view == 0)
            return;

        var windowPoint = CocoaNative.SendPoint(nativeEvent, CocoaNative.Selector("locationInWindow"));
        var viewPoint = CocoaNative.SendPoint(
            _view,
            CocoaNative.Selector("convertPoint:fromView:"),
            windowPoint,
            0);
        _lastMousePosition = new Vector2((float)viewPoint.X, (float)viewPoint.Y);
        _pendingInput.MousePosition = _lastMousePosition;
    }

    private void RefreshMousePosition(InputSnapshot snapshot)
    {
        if (_window != 0 && _view != 0)
        {
            var windowPoint = CocoaNative.SendPoint(_window, CocoaNative.Selector("mouseLocationOutsideOfEventStream"));
            var viewPoint = CocoaNative.SendPoint(
                _view,
                CocoaNative.Selector("convertPoint:fromView:"),
                windowPoint,
                0);
            _lastMousePosition = new Vector2((float)viewPoint.X, (float)viewPoint.Y);
        }

        snapshot.MousePosition = _lastMousePosition;
    }

    private void ReleasePressedInputs()
    {
        foreach (var key in _pressedKeys)
            _pendingInput.KeyEvents.Add(new KeyEvent(key, false));
        _pressedKeys.Clear();

        foreach (var button in _pressedMouseButtons)
            _pendingInput.MouseEvents.Add(new MouseEvent(button, false));
        _pressedMouseButtons.Clear();
    }

    private void RaiseClosing()
    {
        if (_closingRaised)
            return;

        _closingRaised = true;
        OnClosing();
    }

    private void HandleNativeClose()
    {
        var teardown = new TeardownCollector();
        teardown.Run(RaiseClosing);

        _exists = false;
        _pendingWindowState = null;
        _enteringFullScreen = false;
        _exitingFullScreen = false;
        ReleasePressedInputs();
        teardown.Run(RestoreExclusiveDisplayMode);
        if (_exclusiveDisplayModeSession is null)
            _state = WindowState.Normal;

        var focusChanged = _focused;
        _focused = false;
        teardown.Run(DiscardMarkedText);
        teardown.Run(RestoreCursorIfHidden);

        if (focusChanged && !_initializing && !_disposed)
            teardown.Run(OnFocusChanged);

        teardown.ThrowIfFailed("Cocoa window close handling failed.");
    }

    private void ReleaseNativeObjects()
    {
        var window = _window;
        var view = _view;
        var windowDelegate = _delegate;

        _metalLayer = 0;
        _view = 0;
        _delegate = 0;
        _window = 0;

        UnregisterCallbackTarget(view);
        UnregisterCallbackTarget(windowDelegate);

        var teardown = new TeardownCollector();

        if (window != 0)
        {
            teardown.Run(() => CocoaNative.SendVoid(window, CocoaNative.Selector("setDelegate:"), (nint)0));
            teardown.Run(() => CocoaNative.SendVoid(window, CocoaNative.Selector("orderOut:"), (nint)0));
            teardown.Run(() => CocoaNative.SendVoid(window, CocoaNative.Selector("close")));
            teardown.Run(() => CocoaNative.SendVoid(window, CocoaNative.Selector("release")));
        }

        if (view != 0)
            teardown.Run(() => CocoaNative.SendVoid(view, CocoaNative.Selector("release")));
        if (windowDelegate != 0)
            teardown.Run(() => CocoaNative.SendVoid(windowDelegate, CocoaNative.Selector("release")));

        teardown.ThrowIfFailed("Cocoa native object cleanup failed.");
    }

    private void CaptureCallbackException(Exception exception)
    {
        _callbackException ??= exception;
    }

    private static void RegisterCallbackTarget(nint nativeObject, CocoaGameWindow window)
    {
        lock (RuntimeLock)
            CallbackTargets[nativeObject] = window;
    }

    private static void UnregisterCallbackTarget(nint nativeObject)
    {
        if (nativeObject == 0)
            return;

        lock (RuntimeLock)
            CallbackTargets.Remove(nativeObject);
    }

    private static bool TryGetCallbackTarget(nint nativeObject, out CocoaGameWindow window)
    {
        lock (RuntimeLock)
            return CallbackTargets.TryGetValue(nativeObject, out window);
    }

    private static void Dispatch(nint nativeObject, Action<CocoaGameWindow> action)
    {
        if (!TryGetCallbackTarget(nativeObject, out var window))
            return;

        window._nativeCleanup.EnterCallback();

        try
        {
            action(window);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
        finally
        {
            window._nativeCleanup.ExitCallback();
        }
    }

    private static void Dispatch(nint nativeObject, nint nativeEvent, Action<CocoaGameWindow, nint> action)
    {
        if (!TryGetCallbackTarget(nativeObject, out var window))
            return;

        window._nativeCleanup.EnterCallback();

        try
        {
            action(window, nativeEvent);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
        }
        finally
        {
            window._nativeCleanup.ExitCallback();
        }
    }

    private static T Dispatch<T>(nint nativeObject, Func<CocoaGameWindow, T> action, T fallback)
    {
        if (!TryGetCallbackTarget(nativeObject, out var window))
            return fallback;

        window._nativeCleanup.EnterCallback();

        try
        {
            return action(window);
        }
        catch (Exception exception)
        {
            window.CaptureCallbackException(exception);
            return fallback;
        }
        finally
        {
            window._nativeCleanup.ExitCallback();
        }
    }

    private static void WindowDidResize(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.RefreshGeometry(true));

    private static void WindowDidMove(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.RefreshGeometry(true));

    private static void WindowDidBecomeKey(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.SetFocused(true));

    private static void WindowDidResignKey(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.SetFocused(false));

    private static void WindowWillClose(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.HandleNativeClose());

    private static void WindowDidChangeBacking(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.RefreshGeometry(true));

    private static void WindowDidMiniaturize(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window._state = WindowState.Minimized);

    private static void WindowDidDeminiaturize(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.RefreshWindowedState());

    private static void WindowDidEnterFullScreen(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.HandleDidEnterFullScreen());

    private static void WindowDidExitFullScreen(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.HandleDidExitFullScreen());

    private static void WindowDidFailToEnterFullScreen(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.HandleDidFailToEnterFullScreen());

    private static void WindowDidFailToExitFullScreen(nint self, nint selector, nint notification) =>
        Dispatch(self, window => window.HandleDidFailToExitFullScreen());

    private static byte WindowShouldClose(nint self, nint selector, nint sender)
    {
        Dispatch(self, window => window.RaiseClosing());
        return 1;
    }

    private static byte ReturnTrue(nint self, nint selector) => 1;

    private static byte ReturnTrueWithValue(nint self, nint selector, nint value) => 1;

    private static void KeyDown(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleKey(value, true));

    private static void KeyUp(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleKey(value, false));

    private static void FlagsChanged(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleFlagsChanged(value));

    private static void InsertText(nint self, nint selector, nint nativeText) =>
        Dispatch(self, nativeText, static (window, value) => window.InsertCommittedText(value));

    private static void InsertTextWithRange(
        nint self,
        nint selector,
        nint nativeText,
        CocoaNative.NativeRange replacementRange) =>
        Dispatch(self, nativeText, static (window, value) => window.InsertCommittedText(value));

    private static void SetMarkedText(
        nint self,
        nint selector,
        nint nativeText,
        CocoaNative.NativeRange selectedRange,
        CocoaNative.NativeRange replacementRange) =>
        Dispatch(self, window => window.SetMarkedText(nativeText, selectedRange));

    private static void UnmarkText(nint self, nint selector) =>
        Dispatch(self, window => window.ClearMarkedText());

    private static void IgnoreTextCommand(nint self, nint selector, nint commandSelector)
    {
    }

    private static CocoaNative.NativeRange SelectedRange(nint self, nint selector) =>
        Dispatch(self, static window => window.GetSelectedRange(), new CocoaNative.NativeRange(0, 0));

    private static CocoaNative.NativeRange MarkedRange(nint self, nint selector) =>
        Dispatch(
            self,
            static window => window.GetMarkedRange(),
            new CocoaNative.NativeRange(nuint.MaxValue, 0));

    private static byte HasMarkedText(nint self, nint selector) =>
        Dispatch(self, static window => window._markedText.Length == 0 ? (byte)0 : (byte)1, (byte)0);

    private static nint AttributedSubstring(
        nint self,
        nint selector,
        CocoaNative.NativeRange proposedRange,
        nint actualRange) => 0;

    private static nint ValidMarkedTextAttributes(nint self, nint selector) =>
        CocoaNative.SendIntPtr(CocoaNative.GetClass("NSArray"), CocoaNative.Selector("array"));

    private static CocoaNative.NativeRect FirstRectForCharacterRange(
        nint self,
        nint selector,
        CocoaNative.NativeRange range,
        nint actualRange) =>
        Dispatch(
            self,
            window => window.GetFirstRectForCharacterRange(range, actualRange),
            new CocoaNative.NativeRect(0, 0, 1, 1));

    private static nuint CharacterIndexForPoint(
        nint self,
        nint selector,
        CocoaNative.NativePoint point) => 0;

    private static void MouseMoved(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleMouseMove(value));

    private static void LeftMouseDown(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleMouseButton(value, MouseButton.Left, true));

    private static void LeftMouseUp(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleMouseButton(value, MouseButton.Left, false));

    private static void RightMouseDown(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleMouseButton(value, MouseButton.Right, true));

    private static void RightMouseUp(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleMouseButton(value, MouseButton.Right, false));

    private static void OtherMouseDown(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleOtherMouseButton(value, true));

    private static void OtherMouseUp(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleOtherMouseButton(value, false));

    private static void ScrollWheel(nint self, nint selector, nint nativeEvent) =>
        Dispatch(self, nativeEvent, static (window, value) => window.HandleScrollWheel(value));

    private static void ViewBackingChanged(nint self, nint selector) =>
        Dispatch(self, window => window.RefreshGeometry(true));

    private static Key TranslateKeyCode(int keyCode) => keyCode switch
    {
        0 => Key.A,
        1 => Key.S,
        2 => Key.D,
        3 => Key.F,
        4 => Key.H,
        5 => Key.G,
        6 => Key.Z,
        7 => Key.X,
        8 => Key.C,
        9 => Key.V,
        10 => Key.NonUSBackSlash,
        11 => Key.B,
        12 => Key.Q,
        13 => Key.W,
        14 => Key.E,
        15 => Key.R,
        16 => Key.Y,
        17 => Key.T,
        18 => Key.Number1,
        19 => Key.Number2,
        20 => Key.Number3,
        21 => Key.Number4,
        22 => Key.Number6,
        23 => Key.Number5,
        24 => Key.Plus,
        25 => Key.Number9,
        26 => Key.Number7,
        27 => Key.Minus,
        28 => Key.Number8,
        29 => Key.Number0,
        30 => Key.BracketRight,
        31 => Key.O,
        32 => Key.U,
        33 => Key.BracketLeft,
        34 => Key.I,
        35 => Key.P,
        36 => Key.Enter,
        37 => Key.L,
        38 => Key.J,
        39 => Key.Quote,
        40 => Key.K,
        41 => Key.Semicolon,
        42 => Key.BackSlash,
        43 => Key.Comma,
        44 => Key.Slash,
        45 => Key.N,
        46 => Key.M,
        47 => Key.Period,
        48 => Key.Tab,
        49 => Key.Space,
        50 => Key.Tilde,
        51 => Key.BackSpace,
        53 => Key.Escape,
        54 => Key.WinRight,
        55 => Key.WinLeft,
        56 => Key.ShiftLeft,
        57 => Key.CapsLock,
        58 => Key.AltLeft,
        59 => Key.ControlLeft,
        60 => Key.ShiftRight,
        61 => Key.AltRight,
        62 => Key.ControlRight,
        64 => Key.F17,
        65 => Key.KeypadDecimal,
        67 => Key.KeypadMultiply,
        69 => Key.KeypadAdd,
        71 => Key.Clear,
        75 => Key.KeypadDivide,
        76 => Key.KeypadEnter,
        78 => Key.KeypadSubtract,
        79 => Key.F18,
        80 => Key.F19,
        81 => Key.Plus,
        82 => Key.Keypad0,
        83 => Key.Keypad1,
        84 => Key.Keypad2,
        85 => Key.Keypad3,
        86 => Key.Keypad4,
        87 => Key.Keypad5,
        88 => Key.Keypad6,
        89 => Key.Keypad7,
        90 => Key.F20,
        91 => Key.Keypad8,
        92 => Key.Keypad9,
        96 => Key.F5,
        97 => Key.F6,
        98 => Key.F7,
        99 => Key.F3,
        100 => Key.F8,
        101 => Key.F9,
        103 => Key.F11,
        105 => Key.F13,
        106 => Key.F16,
        107 => Key.F14,
        109 => Key.F10,
        111 => Key.F12,
        113 => Key.F15,
        114 => Key.Insert,
        115 => Key.Home,
        116 => Key.PageUp,
        117 => Key.Delete,
        118 => Key.F4,
        119 => Key.End,
        120 => Key.F2,
        121 => Key.PageDown,
        122 => Key.F1,
        123 => Key.Left,
        124 => Key.Right,
        125 => Key.Down,
        126 => Key.Up,
        _ => Key.Unknown,
    };

    private readonly struct CocoaAutoreleasePool : IDisposable
    {
        private readonly nint _pool;

        public CocoaAutoreleasePool()
        {
            _pool = CocoaNative.SendIntPtr(
                CocoaNative.SendIntPtr(CocoaNative.GetClass("NSAutoreleasePool"), CocoaNative.Selector("alloc")),
                CocoaNative.Selector("init"));
        }

        public void Dispose()
        {
            if (_pool != 0)
                CocoaNative.SendVoid(_pool, CocoaNative.Selector("drain"));
        }
    }
}

internal static class CocoaNative
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void VoidCallback(nint self, nint selector, nint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void VoidNoArgumentCallback(nint self, nint selector);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate byte BoolCallback(nint self, nint selector, nint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate byte BoolNoArgumentCallback(nint self, nint selector);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void EventCallback(nint self, nint selector, nint nativeEvent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void TextCallback(nint self, nint selector, nint nativeText);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void TextAndRangeCallback(
        nint self,
        nint selector,
        nint nativeText,
        NativeRange replacementRange);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void MarkedTextCallback(
        nint self,
        nint selector,
        nint nativeText,
        NativeRange selectedRange,
        NativeRange replacementRange);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate NativeRange RangeCallback(nint self, nint selector);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint ObjectNoArgumentCallback(nint self, nint selector);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint ObjectForRangeCallback(
        nint self,
        nint selector,
        NativeRange proposedRange,
        nint actualRange);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate NativeRect RectForRangeCallback(
        nint self,
        nint selector,
        NativeRange range,
        nint actualRange);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nuint IndexForPointCallback(nint self, nint selector, NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeRange
    {
        public readonly nuint Location;
        public readonly nuint Length;

        public NativeRange(nuint location, nuint length)
        {
            Location = location;
            Length = length;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativePoint
    {
        public readonly double X;
        public readonly double Y;

        public NativePoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeSize
    {
        public readonly double Width;
        public readonly double Height;

        public NativeSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeRect
    {
        public readonly NativePoint Origin;
        public readonly NativeSize Size;

        public NativeRect(double x, double y, double width, double height)
        {
            Origin = new NativePoint(x, y);
            Size = new NativeSize(width, height);
        }
    }

    internal static nint GetClass(string name) => objc_getClass(name);
    internal static nint GetProtocol(string name) => objc_getProtocol(name);
    internal static nint Selector(string name) => sel_registerName(name);

    internal static nint AllocateClass(nint superclass, string name)
    {
        var type = objc_allocateClassPair(superclass, name, 0);
        if (type == 0)
            throw new InvalidOperationException($"Failed to allocate Objective-C class {name}.");
        return type;
    }

    internal static void RegisterClass(nint type) => objc_registerClassPair(type);
    internal static bool AddMethod(nint type, nint selector, nint implementation, string signature) =>
        class_addMethod(type, selector, implementation, signature);
    internal static bool AddProtocol(nint type, nint protocol) => class_addProtocol(type, protocol);

    internal static NativeRect SendRect(nint receiver, nint selector)
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            SendRectStret(out var result, receiver, selector);
            return result;
        }

        return SendRectDirect(receiver, selector);
    }

    [DllImport(ObjectiveCLibrary, CharSet = CharSet.Ansi)]
    private static extern nint objc_getClass(string name);

    [DllImport(ObjectiveCLibrary, CharSet = CharSet.Ansi)]
    private static extern nint objc_getProtocol(string name);

    [DllImport(ObjectiveCLibrary, CharSet = CharSet.Ansi)]
    private static extern nint sel_registerName(string name);

    [DllImport(ObjectiveCLibrary, CharSet = CharSet.Ansi)]
    private static extern nint objc_allocateClassPair(nint superclass, string name, nuint extraBytes);

    [DllImport(ObjectiveCLibrary)]
    private static extern void objc_registerClassPair(nint type);

    [DllImport(ObjectiveCLibrary, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(nint type, nint selector, nint implementation, string signature);

    [DllImport(ObjectiveCLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addProtocol(nint type, nint protocol);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(nint receiver, nint selector, nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(nint receiver, nint selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend", CharSet = CharSet.Ansi)]
    internal static extern nint SendIntPtr(nint receiver, nint selector, string value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(nint receiver, nint selector, NativeRect frame);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(
        nint receiver,
        nint selector,
        NativeRect contentRect,
        nuint styleMask,
        nuint backingStore,
        byte defer);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendIntPtr(
        nint receiver,
        nint selector,
        nuint eventMask,
        nint expiration,
        nint runLoopMode,
        byte dequeue);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, byte value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, double value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, NativePoint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, NativeSize value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector, NativeRect value, byte display);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool SendBool(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool SendBool(nint receiver, nint selector, nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nint SendNInt(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern nuint SendNUInt(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern double SendDouble(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern NativePoint SendPoint(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    internal static extern NativePoint SendPoint(nint receiver, nint selector, NativePoint point, nint sourceView);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern NativeRect SendRectDirect(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend_stret")]
    private static extern void SendRectStret(out NativeRect result, nint receiver, nint selector);
}
