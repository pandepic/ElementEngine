using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ElementEngine;

[SupportedOSPlatform("windows")]
internal static class WindowsGamepadProvider
{
    public static IReadOnlyList<INativeGamepad> Enumerate()
    {
        var devices = new List<INativeGamepad>(4);

        for (var index = 0; index < 4; index++)
        {
            var device = new XInputGamepad(index);
            if (device.TryPoll(out _))
                devices.Add(device);
            else
                device.Dispose();
        }

        try
        {
            devices.AddRange(WindowsHidGamepadProvider.Enumerate());
        }
        catch (Exception ex)
        {
            // A HID enumeration failure must never break the XInput pads.
            Logging.Warning("HID gamepad enumeration failed: {message}", ex.Message);
        }

        return devices;
    }

    private sealed class XInputGamepad : INativeGamepad
    {
        public int DeviceId { get; }
        public string Name => $"XInput Controller {DeviceId + 1}";

        public XInputGamepad(int deviceId)
        {
            DeviceId = deviceId;
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            if (GetState((uint)DeviceId, out var nativeState) != 0)
            {
                state = default;
                return false;
            }

            var gamepad = nativeState.Gamepad;
            var axes = new Dictionary<GamepadAxis, float>(6)
            {
                [GamepadAxis.LeftX] = NormalizeAxis(gamepad.ThumbLX),
                [GamepadAxis.LeftY] = -NormalizeAxis(gamepad.ThumbLY),
                [GamepadAxis.RightX] = NormalizeAxis(gamepad.ThumbRX),
                [GamepadAxis.RightY] = -NormalizeAxis(gamepad.ThumbRY),
                [GamepadAxis.LeftTrigger] = gamepad.LeftTrigger / 255f,
                [GamepadAxis.RightTrigger] = gamepad.RightTrigger / 255f,
            };

            var buttons = new HashSet<GamepadButtonType>();
            AddButton(buttons, gamepad.Buttons, 0x1000, GamepadButtonType.A);
            AddButton(buttons, gamepad.Buttons, 0x2000, GamepadButtonType.B);
            AddButton(buttons, gamepad.Buttons, 0x4000, GamepadButtonType.X);
            AddButton(buttons, gamepad.Buttons, 0x8000, GamepadButtonType.Y);
            AddButton(buttons, gamepad.Buttons, 0x0020, GamepadButtonType.Back);
            AddButton(buttons, gamepad.Buttons, 0x0010, GamepadButtonType.Start);
            AddButton(buttons, gamepad.Buttons, 0x0040, GamepadButtonType.LeftStick);
            AddButton(buttons, gamepad.Buttons, 0x0080, GamepadButtonType.RightStick);
            AddButton(buttons, gamepad.Buttons, 0x0100, GamepadButtonType.LeftShoulder);
            AddButton(buttons, gamepad.Buttons, 0x0200, GamepadButtonType.RightShoulder);
            AddButton(buttons, gamepad.Buttons, 0x0001, GamepadButtonType.DPadUp);
            AddButton(buttons, gamepad.Buttons, 0x0002, GamepadButtonType.DPadDown);
            AddButton(buttons, gamepad.Buttons, 0x0004, GamepadButtonType.DPadLeft);
            AddButton(buttons, gamepad.Buttons, 0x0008, GamepadButtonType.DPadRight);

            state = new NativeGamepadState(axes, buttons);
            return true;
        }

        public void Dispose()
        {
        }

        private static float NormalizeAxis(short value)
            => value < 0 ? -(value / (float)short.MinValue) : value / (float)short.MaxValue;

        private static void AddButton(
            HashSet<GamepadButtonType> pressedButtons,
            ushort nativeButtons,
            ushort mask,
            GamepadButtonType button)
        {
            if ((nativeButtons & mask) != 0)
                pressedButtons.Add(button);
        }

        private static uint GetState(uint userIndex, out XInputState state)
        {
            try
            {
                return XInputGetState14(userIndex, out state);
            }
            catch (DllNotFoundException)
            {
                return GetLegacyState(userIndex, out state);
            }
            catch (EntryPointNotFoundException)
            {
                return GetLegacyState(userIndex, out state);
            }
        }

        private static uint GetLegacyState(uint userIndex, out XInputState state)
        {
            try
            {
                return XInputGetState910(userIndex, out state);
            }
            catch (DllNotFoundException)
            {
                state = default;
                return 1167;
            }
            catch (EntryPointNotFoundException)
            {
                state = default;
                return 1167;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepadState Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepadState
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [DllImport("xinput1_4", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.Winapi)]
    private static extern uint XInputGetState14(uint userIndex, out XInputState state);

    [DllImport("xinput9_1_0", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.Winapi)]
    private static extern uint XInputGetState910(uint userIndex, out XInputState state);
}
