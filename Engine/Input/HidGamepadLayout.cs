using System;
using System.Collections.Generic;

namespace ElementEngine;

[Flags]
internal enum HidHatDirection
{
    None = 0,
    Up = 1,
    Right = 2,
    Down = 4,
    Left = 8,
}

internal static class HidGamepadLayout
{
    public const ushort GenericDesktopUsagePage = 0x01;
    public const ushort ButtonUsagePage = 0x09;
    public const ushort JoystickUsage = 0x04;
    public const ushort GamepadUsage = 0x05;
    public const ushort AxisUsageX = 0x30;
    public const ushort AxisUsageY = 0x31;
    public const ushort AxisUsageZ = 0x32;
    public const ushort AxisUsageRX = 0x33;
    public const ushort AxisUsageRY = 0x34;
    public const ushort AxisUsageRZ = 0x35;
    public const ushort HatSwitchUsage = 0x39;

    private static readonly GamepadButtonType[] ButtonOrder =
    {
        GamepadButtonType.A,
        GamepadButtonType.B,
        GamepadButtonType.X,
        GamepadButtonType.Y,
        GamepadButtonType.LeftShoulder,
        GamepadButtonType.RightShoulder,
        GamepadButtonType.Back,
        GamepadButtonType.Start,
        GamepadButtonType.LeftStick,
        GamepadButtonType.RightStick,
        GamepadButtonType.Guide,
    };

    public static GamepadButtonType GetButtonForIndex(int buttonIndex)
        => buttonIndex >= 0 && buttonIndex < ButtonOrder.Length
            ? ButtonOrder[buttonIndex]
            : GamepadButtonType.Invalid;

    // Default layout for pads without a mapping database entry: X/Y drive the left
    // stick and the right stick prefers Z/Rz (the common DirectInput arrangement).
    // Leftover value axes only become triggers when a second stick pair exists,
    // because a lone Z on a single-stick device is usually a throttle.
    public static IReadOnlyDictionary<ushort, GamepadAxis> CreateAxisLayout(IReadOnlySet<ushort> axisUsages)
    {
        var layout = new Dictionary<ushort, GamepadAxis>();

        if (axisUsages.Contains(AxisUsageX))
            layout[AxisUsageX] = GamepadAxis.LeftX;
        if (axisUsages.Contains(AxisUsageY))
            layout[AxisUsageY] = GamepadAxis.LeftY;

        if (axisUsages.Contains(AxisUsageZ) && axisUsages.Contains(AxisUsageRZ))
        {
            layout[AxisUsageZ] = GamepadAxis.RightX;
            layout[AxisUsageRZ] = GamepadAxis.RightY;

            if (axisUsages.Contains(AxisUsageRX))
                layout[AxisUsageRX] = GamepadAxis.LeftTrigger;
            if (axisUsages.Contains(AxisUsageRY))
                layout[AxisUsageRY] = GamepadAxis.RightTrigger;
        }
        else if (axisUsages.Contains(AxisUsageRX) && axisUsages.Contains(AxisUsageRY))
        {
            layout[AxisUsageRX] = GamepadAxis.RightX;
            layout[AxisUsageRY] = GamepadAxis.RightY;

            if (axisUsages.Contains(AxisUsageZ))
                layout[AxisUsageZ] = GamepadAxis.LeftTrigger;
            if (axisUsages.Contains(AxisUsageRZ))
                layout[AxisUsageRZ] = GamepadAxis.RightTrigger;
        }

        return layout;
    }

    public static float ScaleAxisValue(GamepadAxis axis, long value, long logicalMin, long logicalMax)
        => axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger
            ? ScaleToUnsignedRange(value, logicalMin, logicalMax)
            : ScaleToSignedRange(value, logicalMin, logicalMax);

    public static float ScaleToSignedRange(long value, long logicalMin, long logicalMax)
    {
        var range = logicalMax - logicalMin;
        if (range <= 0)
            return 0f;

        return Math.Clamp((value - logicalMin) / (float)range, 0f, 1f) * 2f - 1f;
    }

    public static float ScaleToUnsignedRange(long value, long logicalMin, long logicalMax)
    {
        var range = logicalMax - logicalMin;
        if (range <= 0)
            return 0f;

        return Math.Clamp((value - logicalMin) / (float)range, 0f, 1f);
    }

    // Values outside the logical range are the hat's null (centered) state.
    public static HidHatDirection DecodeHatSwitch(long value, long logicalMin, long logicalMax)
    {
        if (value < logicalMin || value > logicalMax)
            return HidHatDirection.None;

        return (value - logicalMin) switch
        {
            0 => HidHatDirection.Up,
            1 => HidHatDirection.Up | HidHatDirection.Right,
            2 => HidHatDirection.Right,
            3 => HidHatDirection.Down | HidHatDirection.Right,
            4 => HidHatDirection.Down,
            5 => HidHatDirection.Down | HidHatDirection.Left,
            6 => HidHatDirection.Left,
            7 => HidHatDirection.Up | HidHatDirection.Left,
            _ => HidHatDirection.None,
        };
    }

    public static void AddHatButtons(HashSet<GamepadButtonType> pressedButtons, HidHatDirection direction)
    {
        if ((direction & HidHatDirection.Up) != 0)
            pressedButtons.Add(GamepadButtonType.DPadUp);
        if ((direction & HidHatDirection.Right) != 0)
            pressedButtons.Add(GamepadButtonType.DPadRight);
        if ((direction & HidHatDirection.Down) != 0)
            pressedButtons.Add(GamepadButtonType.DPadDown);
        if ((direction & HidHatDirection.Left) != 0)
            pressedButtons.Add(GamepadButtonType.DPadLeft);
    }

    // HID report values arrive as raw unsigned bit fields; controls with a negative
    // logical minimum encode negative positions in two's complement at BitSize width.
    public static int SignExtend(uint rawValue, int bitSize)
    {
        if (bitSize <= 0 || bitSize >= 32)
            return unchecked((int)rawValue);

        var shift = 32 - bitSize;
        return unchecked((int)(rawValue << shift)) >> shift;
    }
}
