namespace ElementEngine;

internal static class X11KeyMap
{
    private const nuint F1 = 0xFFBE;
    private const nuint F35 = 0xFFE0;

    public static Key Translate(nuint keySym)
    {
        if (keySym >= F1 && keySym <= F35)
            return (Key)((int)Key.F1 + (int)(keySym - F1));

        if (keySym is >= 0x0041 and <= 0x005A)
            return (Key)((int)Key.A + (int)(keySym - 0x0041));

        if (keySym is >= 0x0061 and <= 0x007A)
            return (Key)((int)Key.A + (int)(keySym - 0x0061));

        if (keySym is >= 0x0030 and <= 0x0039)
            return (Key)((int)Key.Number0 + (int)(keySym - 0x0030));

        if (keySym is >= 0xFFB0 and <= 0xFFB9)
            return (Key)((int)Key.Keypad0 + (int)(keySym - 0xFFB0));

        return keySym switch
        {
            0xFFE1 => Key.ShiftLeft,
            0xFFE2 => Key.ShiftRight,
            0xFFE3 => Key.ControlLeft,
            0xFFE4 => Key.ControlRight,
            0xFFE7 or 0xFFEB => Key.WinLeft,
            0xFFE8 or 0xFFEC => Key.WinRight,
            0xFFE9 => Key.AltLeft,
            0xFFEA or 0xFE03 or 0xFF7E => Key.AltRight,
            0xFF67 => Key.Menu,
            0xFF52 => Key.Up,
            0xFF54 => Key.Down,
            0xFF51 => Key.Left,
            0xFF53 => Key.Right,
            0xFF0D => Key.Enter,
            0xFF1B => Key.Escape,
            0x0020 or 0xFF80 => Key.Space,
            0xFF09 or 0xFF89 or 0xFE20 => Key.Tab,
            0xFF08 => Key.BackSpace,
            0xFF63 => Key.Insert,
            0xFFFF => Key.Delete,
            0xFF55 => Key.PageUp,
            0xFF56 => Key.PageDown,
            0xFF50 => Key.Home,
            0xFF57 => Key.End,
            0xFFE5 => Key.CapsLock,
            0xFF14 => Key.ScrollLock,
            0xFF61 => Key.PrintScreen,
            0xFF13 => Key.Pause,
            0xFF7F => Key.NumLock,
            0xFF0B => Key.Clear,
            0x1008FF2F => Key.Sleep,
            0xFF9E => Key.Keypad0,
            0xFF9C => Key.Keypad1,
            0xFF99 => Key.Keypad2,
            0xFF9B => Key.Keypad3,
            0xFF96 => Key.Keypad4,
            0xFF9D => Key.Keypad5,
            0xFF98 => Key.Keypad6,
            0xFF95 => Key.Keypad7,
            0xFF97 => Key.Keypad8,
            0xFF9A => Key.Keypad9,
            0xFFAF => Key.KeypadDivide,
            0xFFAA => Key.KeypadMultiply,
            0xFFAD => Key.KeypadSubtract,
            0xFFAB => Key.KeypadAdd,
            0xFFAE or 0xFF9F => Key.KeypadDecimal,
            0xFF8D => Key.KeypadEnter,
            0x0060 or 0x007E => Key.Tilde,
            0x002D or 0x005F => Key.Minus,
            0x003D or 0x002B or 0xFFBD => Key.Plus,
            0x005B or 0x007B => Key.BracketLeft,
            0x005D or 0x007D => Key.BracketRight,
            0x003B or 0x003A => Key.Semicolon,
            0x0027 or 0x0022 => Key.Quote,
            0x002C => Key.Comma,
            0x002E => Key.Period,
            0x002F or 0x003F => Key.Slash,
            0x005C or 0x007C => Key.BackSlash,
            0x003C or 0x003E => Key.NonUSBackSlash,
            _ => Key.Unknown,
        };
    }
}
