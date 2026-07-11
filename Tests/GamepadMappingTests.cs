using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class GamepadMappingTests
{
    [Fact]
    public void LinuxDeviceIdentifierUsesZeroPaddedLittleEndianFields()
    {
        var identifier = new GamepadDeviceIdentifier(
            Bus: 0x0003,
            Vendor: 0x045e,
            Product: 0x028e,
            Version: 0x0120);

        Assert.Equal("030000005e0400008e02000020010000", identifier.MappingIdentifier);
    }

    [Fact]
    public void BundledDatabaseFindsExactLinuxDeviceIdentifier()
    {
        var identifier = new GamepadDeviceIdentifier(
            Bus: 0x0003,
            Vendor: 0x045e,
            Product: 0x028e,
            Version: 0x0120);

        Assert.True(GamepadMappingDatabase.TryGetLinuxMapping(identifier, "Ignored Name", out var mapping));
        Assert.Equal("8BitDo Adapter", mapping.Name);
    }

    [Fact]
    public void BundledDatabaseFallsBackToNameForDevicesWithoutHardwareIds()
    {
        var identifier = new GamepadDeviceIdentifier(
            Bus: 0x0005,
            Vendor: 0,
            Product: 0,
            Version: 0);

        Assert.Equal(
            "0500000047532047616d657061640000",
            identifier.GetNameMappingIdentifier("GS Gamepad"));
        Assert.True(GamepadMappingDatabase.TryGetLinuxMapping(identifier, "GS Gamepad", out var mapping));
        Assert.Equal("GameStop Gamepad", mapping.Name);
    }

    [Fact]
    public void MappingAppliesAxesButtonsHatsAndInversion()
    {
        const string value =
            "guid,Test Controller,a:b1,b:b0,dpup:h0.1,dpright:h0.2," +
            "leftx:a0~,lefty:a1,lefttrigger:a2,righttrigger:b2,+rightx:b3,-rightx:b4";
        Assert.True(GamepadMapping.TryParse(value, out var mapping));

        var state = mapping.Apply(new RawGamepadState(
            new[]
            {
                new RawGamepadAxis(0.75f, 0.875f),
                new RawGamepadAxis(-0.5f, 0.25f),
                new RawGamepadAxis(-0.5f, 0.25f),
            },
            new HashSet<int> { 1, 2, 3 },
            new[] { 3 }));

        Assert.Equal(-0.75f, state.Axes[GamepadAxis.LeftX]);
        Assert.Equal(-0.5f, state.Axes[GamepadAxis.LeftY]);
        Assert.Equal(0.25f, state.Axes[GamepadAxis.LeftTrigger]);
        Assert.Equal(1f, state.Axes[GamepadAxis.RightTrigger]);
        Assert.Equal(1f, state.Axes[GamepadAxis.RightX]);
        Assert.Contains(GamepadButtonType.A, state.PressedButtons);
        Assert.DoesNotContain(GamepadButtonType.B, state.PressedButtons);
        Assert.Contains(GamepadButtonType.DPadUp, state.PressedButtons);
        Assert.Contains(GamepadButtonType.DPadRight, state.PressedButtons);
    }

    [Fact]
    public void MappingConvertsDirectionalAxesToButtons()
    {
        Assert.True(GamepadMapping.TryParse(
            "guid,Test Controller,dpup:-a0,dpdown:+a0",
            out var mapping));

        var state = mapping.Apply(new RawGamepadState(
            new[] { new RawGamepadAxis(-0.75f, 0.125f) },
            new HashSet<int>(),
            Array.Empty<int>()));

        Assert.Contains(GamepadButtonType.DPadUp, state.PressedButtons);
        Assert.DoesNotContain(GamepadButtonType.DPadDown, state.PressedButtons);
    }

    [Fact]
    public void NonGuidDatabaseEntryCanBeParsedButNotProducedByDeviceIdentifier()
    {
        Assert.True(GamepadMapping.TryParse(
            "xinput,XInput Controller,a:b0",
            out var mapping));

        Assert.Equal("xinput", mapping.Identifier);
        Assert.Equal(32, new GamepadDeviceIdentifier().MappingIdentifier.Length);
    }
}
