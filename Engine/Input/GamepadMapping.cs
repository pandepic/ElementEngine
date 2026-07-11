using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ElementEngine;

internal readonly record struct GamepadDeviceIdentifier(
    ushort Bus,
    ushort Vendor,
    ushort Product,
    ushort Version)
{
    public string MappingIdentifier
    {
        get
        {
            Span<byte> bytes = stackalloc byte[16];
            bytes.Clear();
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, Bus);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes[4..], Vendor);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes[8..], Product);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes[12..], Version);
            return Convert.ToHexStringLower(bytes);
        }
    }

    public string GetNameMappingIdentifier(string name)
    {
        Span<byte> bytes = stackalloc byte[16];
        bytes.Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, Bus);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 12)).CopyTo(bytes[4..]);
        return Convert.ToHexStringLower(bytes);
    }
}

internal readonly record struct RawGamepadAxis(float SignedValue, float UnsignedValue);

internal readonly record struct RawGamepadState(
    IReadOnlyList<RawGamepadAxis> Axes,
    IReadOnlySet<int> PressedButtons,
    IReadOnlyList<int> Hats);

internal sealed class GamepadMapping
{
    private const float DigitalAxisThreshold = 0.5f;

    private readonly IReadOnlyList<GamepadMappingBinding> _bindings;

    public string Identifier { get; }
    public string Name { get; }

    private GamepadMapping(string identifier, string name, IReadOnlyList<GamepadMappingBinding> bindings)
    {
        Identifier = identifier;
        Name = name;
        _bindings = bindings;
    }

    public static bool TryParse(string value, out GamepadMapping mapping)
    {
        mapping = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var fields = value.Split(',', StringSplitOptions.TrimEntries);
        if (fields.Length < 2 || string.IsNullOrWhiteSpace(fields[0]))
            return false;

        var bindings = new List<GamepadMappingBinding>(fields.Length - 2);
        for (var index = 2; index < fields.Length; index++)
        {
            var separator = fields[index].IndexOf(':');
            if (separator <= 0 || separator == fields[index].Length - 1)
                continue;

            var targetValue = fields[index][..separator];
            var sourceValue = fields[index][(separator + 1)..];
            if (TryParseTarget(targetValue, out var target) &&
                TryParseSource(sourceValue, out var source))
            {
                bindings.Add(new GamepadMappingBinding(target, source));
            }
        }

        mapping = new GamepadMapping(fields[0], fields[1], bindings);
        return true;
    }

    public NativeGamepadState Apply(RawGamepadState state)
    {
        var axes = new Dictionary<GamepadAxis, float>();
        var buttons = new HashSet<GamepadButtonType>();

        foreach (var binding in _bindings)
        {
            if (binding.Target.Axis.HasValue)
            {
                var axis = binding.Target.Axis.Value;
                var value = ReadAnalogValue(binding.Source, state, axis);
                value *= binding.Target.Direction == 0 ? 1 : binding.Target.Direction;

                axes.TryGetValue(axis, out var currentValue);
                var minimum = axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger ? 0f : -1f;
                axes[axis] = Math.Clamp(currentValue + value, minimum, 1f);
            }
            else if (binding.Target.Button.HasValue && IsPressed(binding.Source, state))
            {
                buttons.Add(binding.Target.Button.Value);
            }
        }

        return new NativeGamepadState(axes, buttons);
    }

    private static float ReadAnalogValue(
        GamepadMappingSource source,
        RawGamepadState state,
        GamepadAxis targetAxis)
    {
        if (source.Kind == GamepadMappingSourceKind.Button)
            return state.PressedButtons.Contains(source.Index) ? 1f : 0f;

        if (source.Kind == GamepadMappingSourceKind.Hat)
            return IsHatPressed(source, state) ? 1f : 0f;

        if (source.Index >= state.Axes.Count)
            return 0f;

        var axis = state.Axes[source.Index];
        var signedValue = source.Inverted ? -axis.SignedValue : axis.SignedValue;
        if (source.Direction > 0)
            return MathF.Max(signedValue, 0f);
        if (source.Direction < 0)
            return MathF.Max(-signedValue, 0f);

        if (targetAxis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger)
            return source.Inverted ? 1f - axis.UnsignedValue : axis.UnsignedValue;

        return signedValue;
    }

    private static bool IsPressed(GamepadMappingSource source, RawGamepadState state)
    {
        if (source.Kind == GamepadMappingSourceKind.Button)
            return state.PressedButtons.Contains(source.Index);
        if (source.Kind == GamepadMappingSourceKind.Hat)
            return IsHatPressed(source, state);
        if (source.Index >= state.Axes.Count)
            return false;

        var value = state.Axes[source.Index].SignedValue;
        if (source.Inverted)
            value = -value;

        return source.Direction switch
        {
            > 0 => value >= DigitalAxisThreshold,
            < 0 => value <= -DigitalAxisThreshold,
            _ => MathF.Abs(value) >= DigitalAxisThreshold,
        };
    }

    private static bool IsHatPressed(GamepadMappingSource source, RawGamepadState state)
        => source.Index < state.Hats.Count &&
           (state.Hats[source.Index] & source.HatMask) == source.HatMask;

    private static bool TryParseTarget(string value, out GamepadMappingTarget target)
    {
        var direction = ParseDirection(ref value);
        var axis = value switch
        {
            "leftx" => GamepadAxis.LeftX,
            "lefty" => GamepadAxis.LeftY,
            "rightx" => GamepadAxis.RightX,
            "righty" => GamepadAxis.RightY,
            "lefttrigger" => GamepadAxis.LeftTrigger,
            "righttrigger" => GamepadAxis.RightTrigger,
            _ => (GamepadAxis?)null,
        };

        if (axis.HasValue)
        {
            target = new GamepadMappingTarget(axis, null, direction);
            return true;
        }

        var button = value switch
        {
            "a" => GamepadButtonType.A,
            "b" => GamepadButtonType.B,
            "x" => GamepadButtonType.X,
            "y" => GamepadButtonType.Y,
            "back" => GamepadButtonType.Back,
            "guide" => GamepadButtonType.Guide,
            "start" => GamepadButtonType.Start,
            "leftstick" => GamepadButtonType.LeftStick,
            "rightstick" => GamepadButtonType.RightStick,
            "leftshoulder" => GamepadButtonType.LeftShoulder,
            "rightshoulder" => GamepadButtonType.RightShoulder,
            "dpup" => GamepadButtonType.DPadUp,
            "dpdown" => GamepadButtonType.DPadDown,
            "dpleft" => GamepadButtonType.DPadLeft,
            "dpright" => GamepadButtonType.DPadRight,
            _ => (GamepadButtonType?)null,
        };

        target = new GamepadMappingTarget(null, button, direction);
        return button.HasValue;
    }

    private static bool TryParseSource(string value, out GamepadMappingSource source)
    {
        var inverted = value.EndsWith('~');
        if (inverted)
            value = value[..^1];

        var direction = ParseDirection(ref value);
        if (value.Length < 2)
        {
            source = default;
            return false;
        }

        if (value[0] is 'a' or 'b' &&
            int.TryParse(value.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
            index >= 0)
        {
            var kind = value[0] == 'a'
                ? GamepadMappingSourceKind.Axis
                : GamepadMappingSourceKind.Button;
            source = new GamepadMappingSource(kind, index, direction, 0, inverted);
            return true;
        }

        if (value[0] == 'h')
        {
            var separator = value.IndexOf('.');
            if (separator > 1 &&
                int.TryParse(value.AsSpan(1, separator - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var hat) &&
                int.TryParse(value.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var mask) &&
                hat >= 0 && mask > 0)
            {
                source = new GamepadMappingSource(GamepadMappingSourceKind.Hat, hat, direction, mask, inverted);
                return true;
            }
        }

        source = default;
        return false;
    }

    private static int ParseDirection(ref string value)
    {
        if (value.StartsWith('+'))
        {
            value = value[1..];
            return 1;
        }

        if (value.StartsWith('-'))
        {
            value = value[1..];
            return -1;
        }

        return 0;
    }

    private readonly record struct GamepadMappingBinding(
        GamepadMappingTarget Target,
        GamepadMappingSource Source);

    private readonly record struct GamepadMappingTarget(
        GamepadAxis? Axis,
        GamepadButtonType? Button,
        int Direction);

    private readonly record struct GamepadMappingSource(
        GamepadMappingSourceKind Kind,
        int Index,
        int Direction,
        int HatMask,
        bool Inverted);

    private enum GamepadMappingSourceKind
    {
        Axis,
        Button,
        Hat,
    }
}

internal static class GamepadMappingDatabase
{
    private const string LinuxMappingsResource = "ElementEngine.GamepadMappings.Linux.txt";

    private static readonly Lazy<MappingTables> LinuxMappings = new(LoadLinuxMappings);

    private sealed record MappingTables(
        IReadOnlyDictionary<string, GamepadMapping> Exact,
        IReadOnlyDictionary<string, GamepadMapping> VersionAgnostic);

    public static bool TryGetLinuxMapping(
        GamepadDeviceIdentifier identifier,
        string deviceName,
        out GamepadMapping mapping)
    {
        var tables = LinuxMappings.Value;

        if (tables.Exact.TryGetValue(identifier.MappingIdentifier, out mapping))
            return true;

        // Firmware revisions change the version field, so retry ignoring it like SDL does.
        if (tables.VersionAgnostic.TryGetValue((identifier with { Version = 0 }).MappingIdentifier, out mapping))
            return true;

        return identifier.Vendor == 0 &&
               identifier.Product == 0 &&
               tables.Exact.TryGetValue(identifier.GetNameMappingIdentifier(deviceName), out mapping);
    }

    private static MappingTables LoadLinuxMappings()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LinuxMappingsResource)
            ?? throw new InvalidOperationException($"Missing embedded gamepad mapping database '{LinuxMappingsResource}'.");
        using var reader = new StreamReader(stream);

        var exact = new Dictionary<string, GamepadMapping>(StringComparer.OrdinalIgnoreCase);
        var versionAgnostic = new Dictionary<string, GamepadMapping>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (!GamepadMapping.TryParse(line, out var mapping))
                throw new InvalidDataException($"Invalid gamepad mapping at line {lineNumber}.");

            exact[mapping.Identifier] = mapping;
            versionAgnostic.TryAdd(ZeroVersion(mapping.Identifier), mapping);
        }

        return new MappingTables(exact, versionAgnostic);
    }

    private static string ZeroVersion(string identifier)
        => identifier.Length == 32
            ? string.Concat(identifier.AsSpan(0, 24), "0000", identifier.AsSpan(28))
            : identifier;
}
