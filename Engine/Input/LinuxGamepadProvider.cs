using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal static class LinuxGamepadProvider
{
    public static IReadOnlyList<INativeGamepad> Enumerate(bool applyMappings = false)
    {
        var devices = new List<INativeGamepad>();
        const string inputDirectory = "/dev/input";

        if (!Directory.Exists(inputDirectory))
            return devices;

        try
        {
            foreach (var path in Directory.EnumerateFiles(inputDirectory, "event*"))
            {
                if (EvdevGamepad.TryOpen(path, applyMappings, out var gamepad))
                    devices.Add(gamepad);
            }
        }
        catch
        {
            foreach (var device in devices)
                device.Dispose();

            throw;
        }

        return devices;
    }

    private sealed unsafe class EvdevGamepad : INativeGamepad
    {
        private const int OpenReadOnly = 0;
        private const int OpenNonBlocking = 0x800;
        private const int ErrorInterrupted = 4;
        private const int ErrorTryAgain = 11;
        private const int EventSynchronization = 0x00;
        private const int EventKey = 0x01;
        private const int EventAbsolute = 0x03;
        private const int MaximumAbsoluteAxis = 0x3f;
        private const int FirstJoystickButton = 0x120;
        private const int MaximumKey = 0x2ff;
        private const int SynchronizationReport = 0;
        private const int SynchronizationDropped = 3;
        private const int ButtonSouth = 304;
        private const int ButtonEast = 305;
        private const int ButtonNorth = 307;
        private const int ButtonWest = 308;
        private const int ButtonLeftShoulder = 310;
        private const int ButtonRightShoulder = 311;
        private const int ButtonSelect = 314;
        private const int ButtonStart = 315;
        private const int ButtonMode = 316;
        private const int ButtonLeftStick = 317;
        private const int ButtonRightStick = 318;
        private const int ButtonDPadUp = 544;
        private const int ButtonDPadDown = 545;
        private const int ButtonDPadLeft = 546;
        private const int ButtonDPadRight = 547;
        private const int AxisX = 0;
        private const int AxisY = 1;
        private const int AxisZ = 2;
        private const int AxisRX = 3;
        private const int AxisRY = 4;
        private const int AxisRZ = 5;
        private const int AxisHat0X = 16;
        private const int AxisHat0Y = 17;
        private const int AxisHat3Y = 23;

        private readonly int _fileDescriptor;
        private readonly GamepadMapping _mapping;
        private readonly List<int> _supportedAxes = new();
        private readonly Dictionary<int, int> _axisIndices = new();
        private readonly Dictionary<int, int> _buttonIndices = new();
        private readonly Dictionary<int, AbsoluteAxisInfo> _axisInfo = new();
        private readonly Dictionary<GamepadAxis, float> _axes = new();
        private readonly HashSet<GamepadButtonType> _buttons = new();
        private readonly HashSet<int> _rawButtons = new();
        private readonly HashSet<GamepadButtonType> _tappedButtons = new();
        private readonly HashSet<int> _tappedRawButtons = new();
        private RawGamepadAxis[] _rawAxes = Array.Empty<RawGamepadAxis>();
        private readonly int[] _hats = new int[4];
        private bool _disposed;
        private bool _synchronizationDropped;

        public int DeviceId { get; }
        public string Name { get; }

        private EvdevGamepad(int fileDescriptor, int deviceId, string name, GamepadMapping mapping)
        {
            _fileDescriptor = fileDescriptor;
            _mapping = mapping;
            DeviceId = deviceId;
            Name = name;

            ReadCapabilities();
            RefreshState();
        }

        public static bool TryOpen(string path, bool applyMappings, out INativeGamepad gamepad)
        {
            gamepad = null;
            var fileDescriptor = open(path, OpenReadOnly | OpenNonBlocking);
            if (fileDescriptor < 0)
                return false;

            try
            {
                var fileName = Path.GetFileName(path);
                var name = GetDeviceName(fileDescriptor) ?? fileName;
                GamepadMapping mapping = null;
                if (applyMappings && TryGetDeviceIdentifier(fileDescriptor, out var identifier))
                    GamepadMappingDatabase.TryGetLinuxMapping(identifier, name, out mapping);

                if (!SupportsGamepad(fileDescriptor, mapping != null))
                {
                    close(fileDescriptor);
                    return false;
                }

                var id = int.TryParse(fileName.AsSpan("event".Length), out var parsedId)
                    ? parsedId
                    : StringComparer.Ordinal.GetHashCode(path);

                gamepad = new EvdevGamepad(
                    fileDescriptor,
                    id,
                    name,
                    mapping);
                return true;
            }
            catch
            {
                close(fileDescriptor);
                throw;
            }
        }

        public bool TryPoll(out NativeGamepadState state)
        {
            if (_disposed)
            {
                state = default;
                return false;
            }

            while (true)
            {
                InputEvent inputEvent;
                var bytesRead = read(_fileDescriptor, &inputEvent, (nuint)sizeof(InputEvent));
                if (bytesRead == sizeof(InputEvent))
                {
                    if (inputEvent.Type == EventSynchronization)
                    {
                        if (inputEvent.Code == SynchronizationDropped)
                            _synchronizationDropped = true;
                        else if (inputEvent.Code == SynchronizationReport && _synchronizationDropped)
                        {
                            RefreshState();
                            _synchronizationDropped = false;
                        }

                        continue;
                    }

                    if (_synchronizationDropped)
                        continue;

                    if (inputEvent.Type == EventKey)
                        UpdateButton(inputEvent.Code, inputEvent.Value != 0);
                    else if (inputEvent.Type == EventAbsolute)
                        UpdateAxis(inputEvent.Code, inputEvent.Value);

                    continue;
                }

                if (bytesRead < 0)
                {
                    var error = Marshal.GetLastPInvokeError();
                    if (error == ErrorInterrupted)
                        continue;
                    if (error == ErrorTryAgain)
                        break;
                }

                state = default;
                return false;
            }

            // Report buttons tapped (pressed and released) entirely between two polls as
            // pressed for one poll, so sub-frame taps still produce press/release events.
            var reportRawButtons = _rawButtons;
            if (_tappedRawButtons.Count > 0)
            {
                if (!_tappedRawButtons.IsSubsetOf(_rawButtons))
                {
                    reportRawButtons = new HashSet<int>(_rawButtons);
                    reportRawButtons.UnionWith(_tappedRawButtons);
                }

                _tappedRawButtons.Clear();
            }

            var reportButtons = _buttons;
            if (_tappedButtons.Count > 0)
            {
                if (!_tappedButtons.IsSubsetOf(_buttons))
                {
                    reportButtons = new HashSet<GamepadButtonType>(_buttons);
                    reportButtons.UnionWith(_tappedButtons);
                }

                _tappedButtons.Clear();
            }

            state = _mapping?.Apply(new RawGamepadState(_rawAxes, reportRawButtons, _hats))
                ?? new NativeGamepadState(_axes, reportButtons);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            close(_fileDescriptor);
            _disposed = true;
        }

        private static bool SupportsGamepad(int fileDescriptor, bool hasMapping)
        {
            Span<byte> eventBits = stackalloc byte[8];
            Span<byte> keyBits = stackalloc byte[96];
            eventBits.Clear();
            keyBits.Clear();
            fixed (byte* eventBitsPointer = eventBits)
            fixed (byte* keyBitsPointer = keyBits)
            {
                if (ioctl(fileDescriptor, EventBitsRequest(0, eventBits.Length), eventBitsPointer) < 0)
                    return false;
                if (!IsBitSet(eventBits, EventKey) || !IsBitSet(eventBits, EventAbsolute))
                    return false;
                if (ioctl(fileDescriptor, EventBitsRequest(EventKey, keyBits.Length), keyBitsPointer) < 0)
                    return false;
            }

            return hasMapping || IsBitSet(keyBits, ButtonSouth);
        }

        private static string GetDeviceName(int fileDescriptor)
        {
            Span<byte> name = stackalloc byte[256];
            fixed (byte* pointer = name)
            {
                if (ioctl(fileDescriptor, ReadRequest(0x06, name.Length), pointer) < 0)
                    return null;
            }

            var terminator = name.IndexOf((byte)0);
            return System.Text.Encoding.UTF8.GetString(terminator < 0 ? name : name[..terminator]);
        }

        private static bool TryGetDeviceIdentifier(
            int fileDescriptor,
            out GamepadDeviceIdentifier identifier)
        {
            InputDeviceIdentifier nativeIdentifier;
            if (ioctl(fileDescriptor, ReadRequest(0x02, sizeof(InputDeviceIdentifier)), &nativeIdentifier) < 0)
            {
                identifier = default;
                return false;
            }

            identifier = new GamepadDeviceIdentifier(
                nativeIdentifier.Bus,
                nativeIdentifier.Vendor,
                nativeIdentifier.Product,
                nativeIdentifier.Version);
            return true;
        }

        private void ReadCapabilities()
        {
            Span<byte> axisBits = stackalloc byte[(MaximumAbsoluteAxis + 8) / 8];
            Span<byte> keyBits = stackalloc byte[(MaximumKey + 8) / 8];
            axisBits.Clear();
            keyBits.Clear();

            fixed (byte* axisBitsPointer = axisBits)
            fixed (byte* keyBitsPointer = keyBits)
            {
                if (ioctl(_fileDescriptor, EventBitsRequest(EventAbsolute, axisBits.Length), axisBitsPointer) < 0 ||
                    ioctl(_fileDescriptor, EventBitsRequest(EventKey, keyBits.Length), keyBitsPointer) < 0)
                {
                    return;
                }
            }

            for (var axis = 0; axis <= MaximumAbsoluteAxis; axis++)
            {
                if (!IsBitSet(axisBits, axis))
                    continue;

                _supportedAxes.Add(axis);
                if (axis is < AxisHat0X or > AxisHat3Y)
                    _axisIndices.Add(axis, _axisIndices.Count);
            }

            _rawAxes = new RawGamepadAxis[_axisIndices.Count];

            for (var button = FirstJoystickButton; button <= MaximumKey; button++)
            {
                if (IsBitSet(keyBits, button))
                    _buttonIndices.Add(button, _buttonIndices.Count);
            }
        }

        private void ReadAxisInfo(int axis)
        {
            AbsoluteAxisInfo info;
            if (ioctl(_fileDescriptor, ReadRequest(0x40 + axis, sizeof(AbsoluteAxisInfo)), &info) >= 0)
            {
                _axisInfo[axis] = info;
                UpdateAxis(axis, info.Value);
            }
        }

        private void RefreshState()
        {
            _axisInfo.Clear();
            _axes.Clear();
            _buttons.Clear();
            _rawButtons.Clear();
            _tappedButtons.Clear();
            _tappedRawButtons.Clear();
            Array.Clear(_rawAxes);
            Array.Clear(_hats);

            foreach (var axis in _supportedAxes)
                ReadAxisInfo(axis);

            ReadButtonState();
        }

        private void ReadButtonState()
        {
            Span<byte> keyBits = stackalloc byte[96];
            keyBits.Clear();

            fixed (byte* keyBitsPointer = keyBits)
            {
                if (ioctl(_fileDescriptor, ReadRequest(0x18, keyBits.Length), keyBitsPointer) < 0)
                    return;
            }

            for (var nativeButton = 0; nativeButton < keyBits.Length * 8; nativeButton++)
            {
                if (IsBitSet(keyBits, nativeButton))
                    UpdateButton(nativeButton, true);
            }
        }

        private void UpdateAxis(int nativeAxis, int value)
        {
            if (!_axisInfo.TryGetValue(nativeAxis, out var info))
                return;

            if (nativeAxis is >= AxisHat0X and <= AxisHat3Y)
            {
                UpdateHat(nativeAxis, value);
                return;
            }

            var range = info.Maximum - info.Minimum;
            if (range <= 0)
                return;

            var normalized = Math.Clamp((value - info.Minimum) / (float)range, 0f, 1f);
            if (_axisIndices.TryGetValue(nativeAxis, out var axisIndex))
                _rawAxes[axisIndex] = new RawGamepadAxis(normalized * 2f - 1f, normalized);

            var gamepadAxis = nativeAxis switch
            {
                AxisX => GamepadAxis.LeftX,
                AxisY => GamepadAxis.LeftY,
                AxisRX => GamepadAxis.RightX,
                AxisRY => GamepadAxis.RightY,
                AxisZ => GamepadAxis.LeftTrigger,
                AxisRZ => GamepadAxis.RightTrigger,
                _ => (GamepadAxis?)null,
            };

            if (!gamepadAxis.HasValue)
                return;

            if (gamepadAxis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger)
                _axes[gamepadAxis.Value] = normalized;
            else
                _axes[gamepadAxis.Value] = normalized * 2f - 1f;
        }

        private void UpdateHat(int nativeAxis, int value)
        {
            var relativeAxis = nativeAxis - AxisHat0X;
            var hatIndex = relativeAxis / 2;
            var isHorizontal = relativeAxis % 2 == 0;

            if (isHorizontal)
            {
                _hats[hatIndex] &= ~(2 | 8);
                if (value < 0)
                    _hats[hatIndex] |= 8;
                else if (value > 0)
                    _hats[hatIndex] |= 2;
            }
            else
            {
                _hats[hatIndex] &= ~(1 | 4);
                if (value < 0)
                    _hats[hatIndex] |= 1;
                else if (value > 0)
                    _hats[hatIndex] |= 4;
            }

            if (hatIndex != 0)
                return;

            SetButton(GamepadButtonType.DPadUp, (_hats[0] & 1) != 0);
            SetButton(GamepadButtonType.DPadRight, (_hats[0] & 2) != 0);
            SetButton(GamepadButtonType.DPadDown, (_hats[0] & 4) != 0);
            SetButton(GamepadButtonType.DPadLeft, (_hats[0] & 8) != 0);
        }

        private void UpdateButton(int nativeButton, bool pressed)
        {
            if (_buttonIndices.TryGetValue(nativeButton, out var buttonIndex))
            {
                if (pressed)
                {
                    _rawButtons.Add(buttonIndex);
                    _tappedRawButtons.Add(buttonIndex);
                }
                else
                {
                    _rawButtons.Remove(buttonIndex);
                }
            }

            var button = nativeButton switch
            {
                ButtonSouth => GamepadButtonType.A,
                ButtonEast => GamepadButtonType.B,
                ButtonWest => GamepadButtonType.X,
                ButtonNorth => GamepadButtonType.Y,
                ButtonSelect => GamepadButtonType.Back,
                ButtonMode => GamepadButtonType.Guide,
                ButtonStart => GamepadButtonType.Start,
                ButtonLeftStick => GamepadButtonType.LeftStick,
                ButtonRightStick => GamepadButtonType.RightStick,
                ButtonLeftShoulder => GamepadButtonType.LeftShoulder,
                ButtonRightShoulder => GamepadButtonType.RightShoulder,
                ButtonDPadUp => GamepadButtonType.DPadUp,
                ButtonDPadDown => GamepadButtonType.DPadDown,
                ButtonDPadLeft => GamepadButtonType.DPadLeft,
                ButtonDPadRight => GamepadButtonType.DPadRight,
                _ => GamepadButtonType.Invalid,
            };

            if (button != GamepadButtonType.Invalid)
                SetButton(button, pressed);
        }

        private void SetButton(GamepadButtonType button, bool pressed)
        {
            if (pressed)
            {
                _buttons.Add(button);
                _tappedButtons.Add(button);
            }
            else
            {
                _buttons.Remove(button);
            }
        }

        private static bool IsBitSet(ReadOnlySpan<byte> bits, int bit)
            => bit / 8 < bits.Length && (bits[bit / 8] & (1 << (bit % 8))) != 0;

        private static nuint EventBitsRequest(int eventType, int size) => ReadRequest(0x20 + eventType, size);

        private static nuint ReadRequest(int number, int size)
            => (nuint)((2u << 30) | ((uint)size << 16) | ((uint)'E' << 8) | (uint)number);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InputDeviceIdentifier
    {
        public ushort Bus;
        public ushort Vendor;
        public ushort Product;
        public ushort Version;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InputEvent
    {
        public nint Seconds;
        public nint Microseconds;
        public ushort Type;
        public ushort Code;
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AbsoluteAxisInfo
    {
        public int Value;
        public int Minimum;
        public int Maximum;
        public int Fuzz;
        public int Flat;
        public int Resolution;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern unsafe nint read(int fileDescriptor, void* buffer, nuint count);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fileDescriptor);

    [DllImport("libc", SetLastError = true)]
    private static extern unsafe int ioctl(int fileDescriptor, nuint request, void* argument);
}
