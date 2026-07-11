using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal static class X11Native
{
    private const string X11 = "libX11.so.6";
    private const string XRandR = "libXrandr.so.2";
    private const int LocaleCategoryCharacterType = 0;

    private static readonly object InitializationLock = new();
    private static bool _threadSupportInitialized;

    internal const int False = 0;
    internal const int True = 1;
    internal const int None = 0;
    internal const int CurrentTime = 0;

    internal const int KeyPress = 2;
    internal const int KeyRelease = 3;
    internal const int ButtonPress = 4;
    internal const int ButtonRelease = 5;
    internal const int MotionNotify = 6;
    internal const int EnterNotify = 7;
    internal const int FocusIn = 9;
    internal const int FocusOut = 10;
    internal const int DestroyNotify = 17;
    internal const int UnmapNotify = 18;
    internal const int MapNotify = 19;
    internal const int ConfigureNotify = 22;
    internal const int PropertyNotify = 28;
    internal const int ClientMessage = 33;
    internal const int MappingNotify = 34;

    internal const int IsUnmapped = 0;
    internal const int IsViewable = 2;
    internal const int RevertToParent = 2;

    internal const int PropModeReplace = 0;
    internal const int Success = 0;
    internal const int AnyPropertyType = 0;

    internal const int NetWmStateRemove = 0;
    internal const int NetWmStateAdd = 1;
    internal const int NetWmStateToggle = 2;

    internal const int RandRSetConfigSuccess = 0;
    internal const nuint RandRModeFlagInterlace = 1 << 4;
    internal const nuint RandRModeFlagDoubleScan = 1 << 5;

    internal const int XLookupNone = 1;
    internal const int XLookupChars = 2;
    internal const int XLookupKeySym = 3;
    internal const int XLookupBoth = 4;
    internal const int XBufferOverflow = -1;

    internal const nuint XimPreeditNothing = 0x0008;
    internal const nuint XimStatusNothing = 0x0400;

    internal const nint KeyPressMask = 1 << 0;
    internal const nint KeyReleaseMask = 1 << 1;
    internal const nint ButtonPressMask = 1 << 2;
    internal const nint ButtonReleaseMask = 1 << 3;
    internal const nint EnterWindowMask = 1 << 4;
    internal const nint PointerMotionMask = 1 << 6;
    internal const nint ExposureMask = 1 << 15;
    internal const nint StructureNotifyMask = 1 << 17;
    internal const nint SubstructureNotifyMask = 1 << 19;
    internal const nint SubstructureRedirectMask = 1 << 20;
    internal const nint FocusChangeMask = 1 << 21;
    internal const nint PropertyChangeMask = 1 << 22;

    internal const nint PMinSize = 1 << 4;
    internal const nint PMaxSize = 1 << 5;

    internal static void EnsureThreadSupport()
    {
        lock (InitializationLock)
        {
            if (_threadSupportInitialized)
                return;

            if (XInitThreads() == False)
                throw new InvalidOperationException("XInitThreads failed to initialize Xlib thread support.");

            _threadSupportInitialized = true;
        }
    }

    internal static bool TryConfigureLocale()
    {
        lock (InitializationLock)
        {
            return setlocale(LocaleCategoryCharacterType, string.Empty) != 0 &&
                   XSupportsLocale() != False &&
                   XSetLocaleModifiers(string.Empty) != 0;
        }
    }

    internal static IReadOnlyList<DisplayMode> GetDisplayModes(nint display)
    {
        if (display == 0)
            throw new ArgumentException("An open X11 display is required.", nameof(display));

        try
        {
            var screenCount = XScreenCount(display);
            var primaryModes = new List<DisplayMode>();
            var secondaryModes = new List<DisplayMode>();

            for (var screen = 0; screen < screenCount; screen++)
            {
                var root = XRootWindow(display, screen);
                var monitors = XRRGetMonitors(display, root, True, out var monitorCount);
                if (monitors == 0)
                    continue;

                try
                {
                    var monitorSize = Marshal.SizeOf<XRRMonitorInfo>();
                    for (var index = 0; index < monitorCount; index++)
                    {
                        var monitor = Marshal.PtrToStructure<XRRMonitorInfo>(
                            monitors + checked(index * monitorSize));
                        if (monitor.Width <= 0 || monitor.Height <= 0)
                            continue;

                        var mode = new DisplayMode(monitor.Width, monitor.Height, 0);
                        if (monitor.Primary != False)
                            primaryModes.Add(mode);
                        else
                            secondaryModes.Add(mode);
                    }
                }
                finally
                {
                    XRRFreeMonitors(monitors);
                }
            }

            primaryModes.AddRange(secondaryModes);
            if (primaryModes.Count > 0)
                return primaryModes;
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        var fallbackScreenCount = XScreenCount(display);
        var modes = new List<DisplayMode>(fallbackScreenCount);
        for (var screen = 0; screen < fallbackScreenCount; screen++)
            modes.Add(new DisplayMode(XDisplayWidth(display, screen), XDisplayHeight(display, screen), 0));

        return modes;
    }

    internal static int GetRefreshRate(nint display, int screen)
    {
        nint configuration = 0;

        try
        {
            var root = XRootWindow(display, screen);
            configuration = XRRGetScreenInfo(display, root);
            return configuration == 0 ? 0 : XRRConfigCurrentRate(configuration);
        }
        catch (DllNotFoundException)
        {
            return 0;
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }
        finally
        {
            if (configuration != 0)
                XRRFreeScreenConfigInfo(configuration);
        }
    }

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XInitThreads();

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSupportsLocale();

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XOpenDisplay(nint displayName);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XCloseDisplay(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XResetScreenSaver(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XDefaultScreen(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XScreenCount(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XRootWindow(nint display, int screenNumber);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XDisplayWidth(nint display, int screenNumber);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XDisplayHeight(nint display, int screenNumber);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XCreateSimpleWindow(
        nint display,
        nuint parent,
        int x,
        int y,
        uint width,
        uint height,
        uint borderWidth,
        nuint border,
        nuint background);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XDestroyWindow(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSelectInput(nint display, nuint window, nint eventMask);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XMapWindow(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XMapRaised(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XUnmapWindow(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XIconifyWindow(nint display, nuint window, int screenNumber);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XMoveResizeWindow(
        nint display,
        nuint window,
        int x,
        int y,
        uint width,
        uint height);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XRaiseWindow(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XFlush(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSync(nint display, int discard);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XPending(nint display);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XNextEvent(nint display, out XEvent xevent);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XPeekEvent(nint display, out XEvent xevent);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XFilterEvent(ref XEvent xevent, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XInternAtom(
        nint display,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string atomName,
        int onlyIfExists);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSetWMProtocols(nint display, nuint window, ref nuint protocols, int count);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XStoreName(
        nint display,
        nuint window,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string windowName);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XChangeProperty(
        nint display,
        nuint window,
        nuint property,
        nuint type,
        int format,
        int mode,
        byte[] data,
        int elementCount);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XGetWindowProperty(
        nint display,
        nuint window,
        nuint property,
        nint longOffset,
        nint longLength,
        int delete,
        nuint requestedType,
        out nuint actualType,
        out int actualFormat,
        out nuint itemCount,
        out nuint bytesAfter,
        out nint propertyData);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XFree(nint data);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSendEvent(
        nint display,
        nuint window,
        int propagate,
        nint eventMask,
        ref XEvent eventSend);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XGetWindowAttributes(
        nint display,
        nuint window,
        out XWindowAttributes windowAttributes);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XTranslateCoordinates(
        nint display,
        nuint sourceWindow,
        nuint destinationWindow,
        int sourceX,
        int sourceY,
        out int destinationX,
        out int destinationY,
        out nuint child);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XQueryPointer(
        nint display,
        nuint window,
        out nuint root,
        out nuint child,
        out int rootX,
        out int rootY,
        out int windowX,
        out int windowY,
        out uint mask);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XGetInputFocus(nint display, out nuint focus, out int revertTo);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XSetWMNormalHints(nint display, nuint window, ref XSizeHints hints);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XCreateBitmapFromData(
        nint display,
        nuint drawable,
        byte[] data,
        uint width,
        uint height);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XCreatePixmapCursor(
        nint display,
        nuint source,
        nuint mask,
        ref XColor foregroundColor,
        ref XColor backgroundColor,
        uint x,
        uint y);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XFreePixmap(nint display, nuint pixmap);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XFreeCursor(nint display, nuint cursor);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XDefineCursor(nint display, nuint window, nuint cursor);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XUndefineCursor(nint display, nuint window);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint XLookupKeysym(ref XKeyEvent keyEvent, int index);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XLookupString(
        ref XKeyEvent keyEvent,
        [Out] byte[] buffer,
        int bufferLength,
        out nuint keySym,
        nint composeStatus);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XRefreshKeyboardMapping(ref XMappingEvent mappingEvent);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XkbSetDetectableAutoRepeat(nint display, int detectable, out int supported);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XOpenIM(nint display, nint resourceDatabase, nint resourceName, nint resourceClass);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XSetLocaleModifiers(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string modifierList);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XCloseIM(nint inputMethod);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl, EntryPoint = "XCreateIC")]
    internal static extern nint XCreateIC(
        nint inputMethod,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string inputStyleName,
        nuint inputStyle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string clientWindowName,
        nuint clientWindow,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string focusWindowName,
        nuint focusWindow,
        nint terminator);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XDestroyIC(nint inputContext);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XSetICFocus(nint inputContext);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XUnsetICFocus(nint inputContext);

    [DllImport(X11, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Xutf8LookupString(
        nint inputContext,
        ref XKeyEvent keyEvent,
        [Out] byte[] buffer,
        int bufferLength,
        out nuint keySym,
        out int status);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XRRGetScreenInfo(nint display, nuint drawable);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern short XRRConfigCurrentRate(nint configuration);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XRRFreeScreenConfigInfo(nint configuration);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XRRGetScreenResourcesCurrent(nint display, nuint window);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XRRGetScreenResources(nint display, nuint window);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XRRFreeScreenResources(nint resources);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XRRGetCrtcInfo(nint display, nint resources, nuint crtc);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XRRFreeCrtcInfo(nint crtcInfo);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint XRRGetOutputInfo(nint display, nint resources, nuint output);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void XRRFreeOutputInfo(nint outputInfo);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int XRRSetCrtcConfig(
        nint display,
        nint resources,
        nuint crtc,
        nuint timestamp,
        int x,
        int y,
        nuint mode,
        ushort rotation,
        [In] nuint[] outputs,
        int outputCount);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint XRRGetMonitors(
        nint display,
        nuint window,
        int getActive,
        out int monitorCount);

    [DllImport(XRandR, CallingConvention = CallingConvention.Cdecl)]
    private static extern void XRRFreeMonitors(nint monitors);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint setlocale(
        int category,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string locale);
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRRScreenResources
{
    public nuint Timestamp;
    public nuint ConfigurationTimestamp;
    public int CrtcCount;
    public nint Crtcs;
    public int OutputCount;
    public nint Outputs;
    public int ModeCount;
    public nint Modes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRRModeInfo
{
    public nuint Id;
    public uint Width;
    public uint Height;
    public nuint DotClock;
    public uint HorizontalSyncStart;
    public uint HorizontalSyncEnd;
    public uint HorizontalTotal;
    public uint HorizontalSkew;
    public uint VerticalSyncStart;
    public uint VerticalSyncEnd;
    public uint VerticalTotal;
    public nint Name;
    public uint NameLength;
    public nuint ModeFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRRCrtcInfo
{
    public nuint Timestamp;
    public int X;
    public int Y;
    public uint Width;
    public uint Height;
    public nuint Mode;
    public ushort Rotation;
    public int OutputCount;
    public nint Outputs;
    public ushort Rotations;
    public int PossibleOutputCount;
    public nint PossibleOutputs;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRROutputInfo
{
    public nuint Timestamp;
    public nuint Crtc;
    public nint Name;
    public int NameLength;
    public nuint PhysicalWidth;
    public nuint PhysicalHeight;
    public ushort Connection;
    public ushort SubpixelOrder;
    public int CrtcCount;
    public nint Crtcs;
    public int CloneCount;
    public nint Clones;
    public int ModeCount;
    public int PreferredModeCount;
    public nint Modes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XRRMonitorInfo
{
    public nuint Name;
    public int Primary;
    public int Automatic;
    public int OutputCount;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int PhysicalWidth;
    public int PhysicalHeight;
    public nint Outputs;
}

[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct XEvent
{
    [FieldOffset(0)] public int Type;
    [FieldOffset(0)] public XAnyEvent Any;
    [FieldOffset(0)] public XKeyEvent Key;
    [FieldOffset(0)] public XButtonEvent Button;
    [FieldOffset(0)] public XMotionEvent Motion;
    [FieldOffset(0)] public XCrossingEvent Crossing;
    [FieldOffset(0)] public XFocusChangeEvent Focus;
    [FieldOffset(0)] public XDestroyWindowEvent DestroyWindow;
    [FieldOffset(0)] public XUnmapEvent Unmap;
    [FieldOffset(0)] public XMapEvent Map;
    [FieldOffset(0)] public XConfigureEvent Configure;
    [FieldOffset(0)] public XPropertyEvent Property;
    [FieldOffset(0)] public XClientMessageEvent ClientMessage;
    [FieldOffset(0)] public XMappingEvent Mapping;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XAnyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XKeyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint Root;
    public nuint Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int RootX;
    public int RootY;
    public uint State;
    public uint KeyCode;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XButtonEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint Root;
    public nuint Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int RootX;
    public int RootY;
    public uint State;
    public uint Button;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMotionEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint Root;
    public nuint Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int RootX;
    public int RootY;
    public uint State;
    public byte IsHint;
    public int SameScreen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XCrossingEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint Root;
    public nuint Subwindow;
    public nuint Time;
    public int X;
    public int Y;
    public int RootX;
    public int RootY;
    public int Mode;
    public int Detail;
    public int SameScreen;
    public int Focus;
    public uint State;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XFocusChangeEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public int Mode;
    public int Detail;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XDestroyWindowEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Event;
    public nuint Window;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XUnmapEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Event;
    public nuint Window;
    public int FromConfigure;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMapEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Event;
    public nuint Window;
    public int OverrideRedirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Event;
    public nuint Window;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int BorderWidth;
    public nuint Above;
    public int OverrideRedirect;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XPropertyEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint Atom;
    public nuint Time;
    public int State;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public nuint MessageType;
    public int Format;
    public XClientMessageData Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageData
{
    public nint Long0;
    public nint Long1;
    public nint Long2;
    public nint Long3;
    public nint Long4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XMappingEvent
{
    public int Type;
    public nuint Serial;
    public int SendEvent;
    public nint Display;
    public nuint Window;
    public int Request;
    public int FirstKeyCode;
    public int Count;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XWindowAttributes
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int BorderWidth;
    public int Depth;
    public nint Visual;
    public nuint Root;
    public int Class;
    public int BitGravity;
    public int WinGravity;
    public int BackingStore;
    public nuint BackingPlanes;
    public nuint BackingPixel;
    public int SaveUnder;
    public nuint Colormap;
    public int MapInstalled;
    public int MapState;
    public nint AllEventMasks;
    public nint YourEventMask;
    public nint DoNotPropagateMask;
    public int OverrideRedirect;
    public nint Screen;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XSizeHints
{
    public nint Flags;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int MinWidth;
    public int MinHeight;
    public int MaxWidth;
    public int MaxHeight;
    public int WidthIncrement;
    public int HeightIncrement;
    public XAspect MinimumAspect;
    public XAspect MaximumAspect;
    public int BaseWidth;
    public int BaseHeight;
    public int WindowGravity;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XAspect
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XColor
{
    public nuint Pixel;
    public ushort Red;
    public ushort Green;
    public ushort Blue;
    public byte Flags;
    public byte Padding;
}
