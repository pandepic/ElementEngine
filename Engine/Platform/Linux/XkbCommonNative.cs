using System;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal static unsafe class XkbCommonNative
{
    public const int KeymapFormatTextV1 = 1;
    public const int ComposeNothing = 0;
    public const int ComposeComposing = 1;
    public const int ComposeComposed = 2;
    public const int ComposeCancelled = 3;

    private const string Library = "libxkbcommon.so.0";

    [DllImport(Library, EntryPoint = "xkb_context_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ContextNew(int flags);

    [DllImport(Library, EntryPoint = "xkb_context_unref", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextUnref(nint context);

    [DllImport(Library, EntryPoint = "xkb_keymap_new_from_string", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint KeymapNewFromString(nint context, byte* text, int format, int flags);

    [DllImport(Library, EntryPoint = "xkb_keymap_unref", CallingConvention = CallingConvention.Cdecl)]
    public static extern void KeymapUnref(nint keymap);

    [DllImport(Library, EntryPoint = "xkb_keymap_key_repeats", CallingConvention = CallingConvention.Cdecl)]
    public static extern int KeymapKeyRepeats(nint keymap, uint key);

    [DllImport(Library, EntryPoint = "xkb_state_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint StateNew(nint keymap);

    [DllImport(Library, EntryPoint = "xkb_state_unref", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StateUnref(nint state);

    [DllImport(Library, EntryPoint = "xkb_state_update_mask", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint StateUpdateMask(
        nint state,
        uint depressedModifiers,
        uint latchedModifiers,
        uint lockedModifiers,
        uint depressedLayout,
        uint latchedLayout,
        uint lockedLayout);

    [DllImport(Library, EntryPoint = "xkb_state_key_get_one_sym", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint StateKeyGetOneSym(nint state, uint key);

    [DllImport(Library, EntryPoint = "xkb_state_key_get_utf8", CallingConvention = CallingConvention.Cdecl)]
    public static extern int StateKeyGetUtf8(nint state, uint key, byte* buffer, nuint size);

    [DllImport(Library, EntryPoint = "xkb_compose_table_new_from_locale", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ComposeTableNewFromLocale(nint context, byte* locale, int flags);

    [DllImport(Library, EntryPoint = "xkb_compose_table_unref", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ComposeTableUnref(nint table);

    [DllImport(Library, EntryPoint = "xkb_compose_state_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint ComposeStateNew(nint table, int flags);

    [DllImport(Library, EntryPoint = "xkb_compose_state_unref", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ComposeStateUnref(nint state);

    [DllImport(Library, EntryPoint = "xkb_compose_state_feed", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComposeStateFeed(nint state, uint keySym);

    [DllImport(Library, EntryPoint = "xkb_compose_state_get_status", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComposeStateGetStatus(nint state);

    [DllImport(Library, EntryPoint = "xkb_compose_state_get_utf8", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComposeStateGetUtf8(nint state, byte* buffer, nuint size);

    [DllImport(Library, EntryPoint = "xkb_compose_state_reset", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ComposeStateReset(nint state);
}
