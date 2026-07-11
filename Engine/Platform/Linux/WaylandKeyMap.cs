namespace ElementEngine;

internal static class WaylandKeyMap
{
    public static Key Translate(uint keyCode)
    {
        if (keyCode is >= 2 and <= 10)
            return (Key)((int)Key.Number1 + keyCode - 2);

        if (keyCode == 11)
            return Key.Number0;

        if (keyCode is >= 59 and <= 68)
            return (Key)((int)Key.F1 + keyCode - 59);

        if (keyCode is >= 71 and <= 73)
            return (Key)((int)Key.Keypad7 + keyCode - 71);

        if (keyCode is >= 75 and <= 77)
            return (Key)((int)Key.Keypad4 + keyCode - 75);

        if (keyCode is >= 79 and <= 81)
            return (Key)((int)Key.Keypad1 + keyCode - 79);

        if (keyCode is >= 183 and <= 194)
            return (Key)((int)Key.F13 + keyCode - 183);

        return keyCode switch
        {
            1 => Key.Escape,
            12 => Key.Minus,
            13 => Key.Plus,
            14 => Key.BackSpace,
            15 => Key.Tab,
            16 => Key.Q,
            17 => Key.W,
            18 => Key.E,
            19 => Key.R,
            20 => Key.T,
            21 => Key.Y,
            22 => Key.U,
            23 => Key.I,
            24 => Key.O,
            25 => Key.P,
            26 => Key.BracketLeft,
            27 => Key.BracketRight,
            28 => Key.Enter,
            29 => Key.ControlLeft,
            30 => Key.A,
            31 => Key.S,
            32 => Key.D,
            33 => Key.F,
            34 => Key.G,
            35 => Key.H,
            36 => Key.J,
            37 => Key.K,
            38 => Key.L,
            39 => Key.Semicolon,
            40 => Key.Quote,
            41 => Key.Tilde,
            42 => Key.ShiftLeft,
            43 => Key.BackSlash,
            44 => Key.Z,
            45 => Key.X,
            46 => Key.C,
            47 => Key.V,
            48 => Key.B,
            49 => Key.N,
            50 => Key.M,
            51 => Key.Comma,
            52 => Key.Period,
            53 => Key.Slash,
            54 => Key.ShiftRight,
            55 => Key.KeypadMultiply,
            56 => Key.AltLeft,
            57 => Key.Space,
            58 => Key.CapsLock,
            69 => Key.NumLock,
            70 => Key.ScrollLock,
            74 => Key.KeypadSubtract,
            78 => Key.KeypadAdd,
            82 => Key.Keypad0,
            83 => Key.KeypadDecimal,
            86 => Key.NonUSBackSlash,
            87 => Key.F11,
            88 => Key.F12,
            96 => Key.KeypadEnter,
            97 => Key.ControlRight,
            98 => Key.KeypadDivide,
            99 => Key.PrintScreen,
            100 => Key.AltRight,
            102 => Key.Home,
            103 => Key.Up,
            104 => Key.PageUp,
            105 => Key.Left,
            106 => Key.Right,
            107 => Key.End,
            108 => Key.Down,
            109 => Key.PageDown,
            110 => Key.Insert,
            111 => Key.Delete,
            119 => Key.Pause,
            125 => Key.WinLeft,
            126 => Key.WinRight,
            127 => Key.Menu,
            142 => Key.Sleep,
            _ => Key.Unknown,
        };
    }
}
