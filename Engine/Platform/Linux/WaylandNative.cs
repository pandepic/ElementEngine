using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ElementEngine;

internal static unsafe class WaylandNative
{
    public const uint MarshalFlagDestroy = 1;
    public const short PollIn = 0x0001;
    public const short PollError = 0x0008;
    public const short PollHangUp = 0x0010;
    public const int ErrorAgain = 11;
    public const int ErrorInterrupted = 4;
    public const int MemoryProtectionRead = 1;
    public const int MemoryMapPrivate = 2;

    private const string ClientLibrary = "libwayland-client.so.0";
    private const string CursorLibrary = "libwayland-cursor.so.0";
    private const string XkbLibrary = "libxkbcommon.so.0";
    private const string CLibrary = "libc.so.6";

    private static readonly Lazy<CoreInterfaces> LazyInterfaces = new(static () => new CoreInterfaces());

    public static CoreInterfaces Interfaces => LazyInterfaces.Value;

    public static void EnsureRuntimeLibraries()
    {
        EnsureLibrary(
            ClientLibrary,
            "wl_display_connect",
            "wl_proxy_marshal_flags",
            "wl_proxy_add_listener");
        EnsureLibrary(
            CursorLibrary,
            "wl_cursor_theme_load",
            "wl_cursor_theme_get_cursor",
            "wl_cursor_image_get_buffer");
        EnsureLibrary(
            XkbLibrary,
            "xkb_context_new",
            "xkb_keymap_new_from_string",
            "xkb_compose_state_new");
    }

    private static void EnsureLibrary(string libraryName, params string[] symbols)
    {
        var library = NativeLibrary.Load(libraryName);
        try
        {
            foreach (var symbol in symbols)
                NativeLibrary.GetExport(library, symbol);
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [DllImport(ClientLibrary, EntryPoint = "wl_display_connect", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint DisplayConnect(byte* name);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_disconnect", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisplayDisconnect(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_roundtrip", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayRoundtrip(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_dispatch_pending", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayDispatchPending(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_prepare_read", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayPrepareRead(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_read_events", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayReadEvents(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_cancel_read", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisplayCancelRead(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_flush", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int DisplayFlush(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_get_fd", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayGetFileDescriptor(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_display_get_error", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DisplayGetError(nint display);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_add_listener", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ProxyAddListener(nint proxy, nint implementation, nint data);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_get_version", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ProxyGetVersion(nint proxy);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProxyDestroy(nint proxy);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalConstructor(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        nint newIdentifier);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalConstructorWithObject(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        nint newIdentifier,
        nint argument);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalRegistryBind(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        uint name,
        byte* interfaceName,
        uint requestedVersion,
        nint newIdentifier);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalNoArguments(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalUInt32(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        uint argument);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalInt32(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        int argument);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalString(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        byte* argument);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalObject(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        nint argument);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalTwoInt32(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        int first,
        int second);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalFourInt32(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        int first,
        int second,
        int third,
        int fourth);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalSurfaceAttach(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        nint buffer,
        int x,
        int y);

    [DllImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_flags", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ProxyMarshalPointerCursor(
        nint proxy,
        uint opcode,
        nint interfacePointer,
        uint version,
        uint flags,
        uint serial,
        nint surface,
        int hotSpotX,
        int hotSpotY);

    [DllImport(CursorLibrary, EntryPoint = "wl_cursor_theme_load", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint CursorThemeLoad(byte* name, int size, nint sharedMemory);

    [DllImport(CursorLibrary, EntryPoint = "wl_cursor_theme_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CursorThemeDestroy(nint theme);

    [DllImport(CursorLibrary, EntryPoint = "wl_cursor_theme_get_cursor", CallingConvention = CallingConvention.Cdecl)]
    public static extern WlCursor* CursorThemeGetCursor(nint theme, byte* name);

    [DllImport(CursorLibrary, EntryPoint = "wl_cursor_image_get_buffer", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint CursorImageGetBuffer(WlCursorImage* image);

    [DllImport(CLibrary, EntryPoint = "poll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int Poll(PollFileDescriptor* descriptors, nuint count, int timeout);

    [DllImport(CLibrary, EntryPoint = "close", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int Close(int fileDescriptor);

    [DllImport(CLibrary, EntryPoint = "mmap", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern nint MemoryMap(
        nint address,
        nuint length,
        int protection,
        int flags,
        int fileDescriptor,
        nint offset);

    [DllImport(CLibrary, EntryPoint = "munmap", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    public static extern int MemoryUnmap(nint address, nuint length);

    public static nint Bind(nint registry, uint name, string interfaceName, uint version, nint interfacePointer)
    {
        var utf8Name = Encoding.UTF8.GetBytes(interfaceName + '\0');
        fixed (byte* namePointer = utf8Name)
        {
            return ProxyMarshalRegistryBind(
                registry,
                0,
                interfacePointer,
                version,
                0,
                name,
                namePointer,
                version,
                0);
        }
    }

    public static void MarshalString(nint proxy, uint opcode, string value)
    {
        var utf8Value = Encoding.UTF8.GetBytes((value ?? string.Empty) + '\0');
        fixed (byte* valuePointer = utf8Value)
        {
            ProxyMarshalString(proxy, opcode, 0, ProxyGetVersion(proxy), 0, valuePointer);
        }
    }

    public sealed class CoreInterfaces
    {
        private readonly nint _library;

        internal CoreInterfaces()
        {
            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException("Wayland interfaces are only available on Linux.");

            _library = NativeLibrary.Load(ClientLibrary);
            Registry = Get("wl_registry_interface");
            Compositor = Get("wl_compositor_interface");
            Surface = Get("wl_surface_interface");
            Output = Get("wl_output_interface");
            Seat = Get("wl_seat_interface");
            Pointer = Get("wl_pointer_interface");
            Keyboard = Get("wl_keyboard_interface");
            SharedMemory = Get("wl_shm_interface");
        }

        public nint Registry { get; }
        public nint Compositor { get; }
        public nint Surface { get; }
        public nint Output { get; }
        public nint Seat { get; }
        public nint Pointer { get; }
        public nint Keyboard { get; }
        public nint SharedMemory { get; }

        private nint Get(string name) => NativeLibrary.GetExport(_library, name);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFileDescriptor
    {
        public int FileDescriptor;
        public short Events;
        public short ReturnedEvents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WlArray
    {
        public nuint Size;
        public nuint Allocation;
        public nint Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WlCursor
    {
        public uint ImageCount;
        public WlCursorImage** Images;
        public byte* Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WlCursorImage
    {
        public uint Width;
        public uint Height;
        public uint HotSpotX;
        public uint HotSpotY;
        public uint Delay;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WlInterface
    {
        public nint Name;
        public int Version;
        public int MethodCount;
        public nint Methods;
        public int EventCount;
        public nint Events;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WlMessage
    {
        public nint Name;
        public nint Signature;
        public nint Types;
    }
}

internal static unsafe class WaylandProtocol
{
    private static readonly Lazy<ProtocolInterfaces> LazyInterfaces = new(static () => new ProtocolInterfaces());

    public static ProtocolInterfaces Interfaces => LazyInterfaces.Value;

    internal sealed class ProtocolInterfaces
    {
        public ProtocolInterfaces()
        {
            XdgPositioner = AllocateInterface("xdg_positioner", 6);
            XdgPopup = AllocateInterface("xdg_popup", 6);
            XdgWmBase = AllocateInterface("xdg_wm_base", 6);
            XdgSurface = AllocateInterface("xdg_surface", 6);
            XdgToplevel = AllocateInterface("xdg_toplevel", 6);
            DecorationManager = AllocateInterface("zxdg_decoration_manager_v1", 1);
            ToplevelDecoration = AllocateInterface("zxdg_toplevel_decoration_v1", 1);
            IdleInhibitManager = AllocateInterface("zwp_idle_inhibit_manager_v1", 1);
            IdleInhibitor = AllocateInterface("zwp_idle_inhibitor_v1", 1);

            SetMessages(
                XdgWmBase,
                [
                    new("destroy", "", []),
                    new("create_positioner", "n", [XdgPositioner]),
                    new("get_xdg_surface", "no", [XdgSurface, WaylandNative.Interfaces.Surface]),
                    new("pong", "u", [0]),
                ],
                [new("ping", "u", [0])]);

            SetMessages(
                XdgSurface,
                [
                    new("destroy", "", []),
                    new("get_toplevel", "n", [XdgToplevel]),
                    new("get_popup", "n?oo", [XdgPopup, XdgSurface, XdgPositioner]),
                    new("set_window_geometry", "iiii", [0, 0, 0, 0]),
                    new("ack_configure", "u", [0]),
                ],
                [new("configure", "u", [0])]);

            SetMessages(
                XdgToplevel,
                [
                    new("destroy", "", []),
                    new("set_parent", "?o", [XdgToplevel]),
                    new("set_title", "s", [0]),
                    new("set_app_id", "s", [0]),
                    new("show_window_menu", "ouii", [WaylandNative.Interfaces.Seat, 0, 0, 0]),
                    new("move", "ou", [WaylandNative.Interfaces.Seat, 0]),
                    new("resize", "ouu", [WaylandNative.Interfaces.Seat, 0, 0]),
                    new("set_max_size", "ii", [0, 0]),
                    new("set_min_size", "ii", [0, 0]),
                    new("set_maximized", "", []),
                    new("unset_maximized", "", []),
                    new("set_fullscreen", "?o", [WaylandNative.Interfaces.Output]),
                    new("unset_fullscreen", "", []),
                    new("set_minimized", "", []),
                ],
                [
                    new("configure", "iia", [0, 0, 0]),
                    new("close", "", []),
                    new("configure_bounds", "4ii", [0, 0]),
                    new("wm_capabilities", "5a", [0]),
                ]);

            SetMessages(
                DecorationManager,
                [
                    new("destroy", "", []),
                    new("get_toplevel_decoration", "no", [ToplevelDecoration, XdgToplevel]),
                ],
                []);

            SetMessages(
                ToplevelDecoration,
                [
                    new("destroy", "", []),
                    new("set_mode", "u", [0]),
                    new("unset_mode", "", []),
                ],
                [new("configure", "u", [0])]);

            SetMessages(
                IdleInhibitManager,
                [
                    new("destroy", "", []),
                    new("create_inhibitor", "no", [IdleInhibitor, WaylandNative.Interfaces.Surface]),
                ],
                []);

            SetMessages(
                IdleInhibitor,
                [new("destroy", "", [])],
                []);
        }

        public nint XdgWmBase { get; }
        public nint XdgPositioner { get; }
        public nint XdgSurface { get; }
        public nint XdgToplevel { get; }
        public nint XdgPopup { get; }
        public nint DecorationManager { get; }
        public nint ToplevelDecoration { get; }
        public nint IdleInhibitManager { get; }
        public nint IdleInhibitor { get; }

        private static nint AllocateInterface(string name, int version)
        {
            var value = (WaylandNative.WlInterface*)NativeMemory.AllocZeroed(
                (nuint)sizeof(WaylandNative.WlInterface));
            value->Name = Marshal.StringToCoTaskMemUTF8(name);
            value->Version = version;
            return (nint)value;
        }

        private static void SetMessages(
            nint interfacePointer,
            MessageDefinition[] methods,
            MessageDefinition[] events)
        {
            var value = (WaylandNative.WlInterface*)interfacePointer;
            value->MethodCount = methods.Length;
            value->Methods = AllocateMessages(methods);
            value->EventCount = events.Length;
            value->Events = AllocateMessages(events);
        }

        private static nint AllocateMessages(MessageDefinition[] definitions)
        {
            if (definitions.Length == 0)
                return 0;

            var messages = (WaylandNative.WlMessage*)NativeMemory.AllocZeroed(
                checked((nuint)(definitions.Length * sizeof(WaylandNative.WlMessage))));

            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];
                messages[index].Name = Marshal.StringToCoTaskMemUTF8(definition.Name);
                messages[index].Signature = Marshal.StringToCoTaskMemUTF8(definition.Signature);

                if (definition.Types.Length == 0)
                    continue;

                var types = (nint*)NativeMemory.AllocZeroed(
                    checked((nuint)(definition.Types.Length * sizeof(nint))));
                definition.Types.CopyTo(new Span<nint>(types, definition.Types.Length));
                messages[index].Types = (nint)types;
            }

            return (nint)messages;
        }

        private readonly record struct MessageDefinition(string Name, string Signature, nint[] Types);
    }
}

internal static unsafe class WaylandListeners
{
    public static readonly nint Registry = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, byte*, uint, void>)&WaylandGameWindow.RegistryGlobal,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.RegistryGlobalRemove,
    ]);

    public static readonly nint WmBase = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.WmBasePing,
    ]);

    public static readonly nint XdgSurface = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.XdgSurfaceConfigure,
    ]);

    public static readonly nint Toplevel = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, int, WaylandNative.WlArray*, void>)&WaylandGameWindow.ToplevelConfigure,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&WaylandGameWindow.ToplevelClose,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, int, void>)&WaylandGameWindow.ToplevelConfigureBounds,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, WaylandNative.WlArray*, void>)&WaylandGameWindow.ToplevelWmCapabilities,
    ]);

    public static readonly nint Decoration = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.DecorationConfigure,
    ]);

    public static readonly nint Seat = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.SeatCapabilities,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte*, void>)&WaylandGameWindow.SeatName,
    ]);

    public static readonly nint Pointer = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, int, int, void>)&WaylandGameWindow.PointerEnter,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, void>)&WaylandGameWindow.PointerLeave,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int, int, void>)&WaylandGameWindow.PointerMotion,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint, uint, void>)&WaylandGameWindow.PointerButton,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, int, void>)&WaylandGameWindow.PointerAxis,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&WaylandGameWindow.PointerFrame,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, void>)&WaylandGameWindow.PointerAxisSource,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, void>)&WaylandGameWindow.PointerAxisStop,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int, void>)&WaylandGameWindow.PointerAxisDiscrete,
    ]);

    public static readonly nint Keyboard = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int, uint, void>)&WaylandGameWindow.KeyboardKeymap,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, WaylandNative.WlArray*, void>)&WaylandGameWindow.KeyboardEnter,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, nint, void>)&WaylandGameWindow.KeyboardLeave,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint, uint, void>)&WaylandGameWindow.KeyboardKey,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, uint, uint, uint, uint, void>)&WaylandGameWindow.KeyboardModifiers,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, int, void>)&WaylandGameWindow.KeyboardRepeatInfo,
    ]);

    public static readonly nint Surface = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&WaylandGameWindow.SurfaceEnter,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&WaylandGameWindow.SurfaceLeave,
    ]);

    public static readonly nint Output = Allocate([
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, int, int, int, int, byte*, byte*, int, void>)&WaylandGameWindow.OutputGeometry,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint, int, int, int, void>)&WaylandGameWindow.OutputMode,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&WaylandGameWindow.OutputDone,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, int, void>)&WaylandGameWindow.OutputScale,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte*, void>)&WaylandGameWindow.OutputName,
        (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte*, void>)&WaylandGameWindow.OutputDescription,
    ]);

    public static nint Allocate(ReadOnlySpan<nint> callbacks)
    {
        var memory = (nint*)NativeMemory.Alloc(
            checked((nuint)(callbacks.Length * sizeof(nint))));
        callbacks.CopyTo(new Span<nint>(memory, callbacks.Length));
        return (nint)memory;
    }
}
