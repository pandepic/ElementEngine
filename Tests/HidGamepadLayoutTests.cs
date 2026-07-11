using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class HidGamepadLayoutTests
{
    [Theory]
    [InlineData(0, (int)HidHatDirection.Up)]
    [InlineData(1, (int)(HidHatDirection.Up | HidHatDirection.Right))]
    [InlineData(2, (int)HidHatDirection.Right)]
    [InlineData(3, (int)(HidHatDirection.Down | HidHatDirection.Right))]
    [InlineData(4, (int)HidHatDirection.Down)]
    [InlineData(5, (int)(HidHatDirection.Down | HidHatDirection.Left))]
    [InlineData(6, (int)HidHatDirection.Left)]
    [InlineData(7, (int)(HidHatDirection.Up | HidHatDirection.Left))]
    public void DecodeHatSwitchMapsAllEightDirections(long value, int expected)
    {
        Assert.Equal((HidHatDirection)expected, HidGamepadLayout.DecodeHatSwitch(value, 0, 7));
    }

    [Fact]
    public void DecodeHatSwitchHonorsOneBasedLogicalRange()
    {
        Assert.Equal(HidHatDirection.Up, HidGamepadLayout.DecodeHatSwitch(1, 1, 8));
        Assert.Equal(HidHatDirection.Up | HidHatDirection.Left, HidGamepadLayout.DecodeHatSwitch(8, 1, 8));
    }

    [Theory]
    [InlineData(8, 0, 7)]
    [InlineData(15, 0, 7)]
    [InlineData(-1, 0, 7)]
    [InlineData(0, 1, 8)]
    public void DecodeHatSwitchTreatsValuesOutsideLogicalRangeAsCentered(long value, long min, long max)
    {
        Assert.Equal(HidHatDirection.None, HidGamepadLayout.DecodeHatSwitch(value, min, max));
    }

    [Fact]
    public void AddHatButtonsAddsDiagonalDirectionsAsTwoButtons()
    {
        var buttons = new HashSet<GamepadButtonType>();

        HidGamepadLayout.AddHatButtons(buttons, HidHatDirection.Down | HidHatDirection.Left);

        Assert.Equal(
            new HashSet<GamepadButtonType> { GamepadButtonType.DPadDown, GamepadButtonType.DPadLeft },
            buttons);
    }

    [Theory]
    [InlineData(0, 0, 255, -1f)]
    [InlineData(255, 0, 255, 1f)]
    [InlineData(-32768, -32768, 32767, -1f)]
    [InlineData(32767, -32768, 32767, 1f)]
    [InlineData(0, -32768, 32767, 0f)]
    public void ScaleToSignedRangeMapsLogicalRangeToMinusOneToOne(long value, long min, long max, float expected)
    {
        Assert.Equal(expected, HidGamepadLayout.ScaleToSignedRange(value, min, max), 3);
    }

    [Fact]
    public void ScaleToSignedRangeClampsValuesOutsideLogicalRange()
    {
        Assert.Equal(-1f, HidGamepadLayout.ScaleToSignedRange(-10, 0, 255));
        Assert.Equal(1f, HidGamepadLayout.ScaleToSignedRange(300, 0, 255));
    }

    [Theory]
    [InlineData(0, 0, 255, 0f)]
    [InlineData(255, 0, 255, 1f)]
    [InlineData(-128, -128, 127, 0f)]
    [InlineData(127, -128, 127, 1f)]
    public void ScaleToUnsignedRangeMapsLogicalRangeToZeroToOne(long value, long min, long max, float expected)
    {
        Assert.Equal(expected, HidGamepadLayout.ScaleToUnsignedRange(value, min, max), 3);
    }

    [Fact]
    public void DegenerateLogicalRangeScalesToZero()
    {
        Assert.Equal(0f, HidGamepadLayout.ScaleToSignedRange(5, 5, 5));
        Assert.Equal(0f, HidGamepadLayout.ScaleToUnsignedRange(5, 5, 5));
        Assert.Equal(0f, HidGamepadLayout.ScaleToSignedRange(5, 7, 3));
    }

    [Fact]
    public void ScaleAxisValueUsesUnsignedRangeOnlyForTriggers()
    {
        Assert.Equal(0f, HidGamepadLayout.ScaleAxisValue(GamepadAxis.LeftTrigger, 0, 0, 255));
        Assert.Equal(-1f, HidGamepadLayout.ScaleAxisValue(GamepadAxis.LeftX, 0, 0, 255));
    }

    [Fact]
    public void ButtonOrderFollowsDefaultHidLayout()
    {
        var expected = new[]
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

        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(expected[index], HidGamepadLayout.GetButtonForIndex(index));
    }

    [Fact]
    public void ButtonIndicesBeyondLayoutAreInvalid()
    {
        Assert.Equal(GamepadButtonType.Invalid, HidGamepadLayout.GetButtonForIndex(11));
        Assert.Equal(GamepadButtonType.Invalid, HidGamepadLayout.GetButtonForIndex(-1));
    }

    [Fact]
    public void AxisLayoutPrefersZRzForTheRightStickAndRxRyForTriggers()
    {
        var layout = HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>
        {
            HidGamepadLayout.AxisUsageX,
            HidGamepadLayout.AxisUsageY,
            HidGamepadLayout.AxisUsageZ,
            HidGamepadLayout.AxisUsageRX,
            HidGamepadLayout.AxisUsageRY,
            HidGamepadLayout.AxisUsageRZ,
        });

        Assert.Equal(GamepadAxis.LeftX, layout[HidGamepadLayout.AxisUsageX]);
        Assert.Equal(GamepadAxis.LeftY, layout[HidGamepadLayout.AxisUsageY]);
        Assert.Equal(GamepadAxis.RightX, layout[HidGamepadLayout.AxisUsageZ]);
        Assert.Equal(GamepadAxis.RightY, layout[HidGamepadLayout.AxisUsageRZ]);
        Assert.Equal(GamepadAxis.LeftTrigger, layout[HidGamepadLayout.AxisUsageRX]);
        Assert.Equal(GamepadAxis.RightTrigger, layout[HidGamepadLayout.AxisUsageRY]);
    }

    [Fact]
    public void AxisLayoutFallsBackToRxRyForTheRightStick()
    {
        var layout = HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>
        {
            HidGamepadLayout.AxisUsageX,
            HidGamepadLayout.AxisUsageY,
            HidGamepadLayout.AxisUsageZ,
            HidGamepadLayout.AxisUsageRX,
            HidGamepadLayout.AxisUsageRY,
        });

        Assert.Equal(GamepadAxis.RightX, layout[HidGamepadLayout.AxisUsageRX]);
        Assert.Equal(GamepadAxis.RightY, layout[HidGamepadLayout.AxisUsageRY]);
        Assert.Equal(GamepadAxis.LeftTrigger, layout[HidGamepadLayout.AxisUsageZ]);
    }

    [Fact]
    public void AxisLayoutWithoutSecondStickPairAssignsNoTriggers()
    {
        var layout = HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>
        {
            HidGamepadLayout.AxisUsageX,
            HidGamepadLayout.AxisUsageY,
            HidGamepadLayout.AxisUsageZ,
        });

        Assert.Equal(2, layout.Count);
        Assert.Equal(GamepadAxis.LeftX, layout[HidGamepadLayout.AxisUsageX]);
        Assert.Equal(GamepadAxis.LeftY, layout[HidGamepadLayout.AxisUsageY]);
        Assert.False(layout.ContainsKey(HidGamepadLayout.AxisUsageZ));
    }

    [Fact]
    public void AxisLayoutOfEmptyUsageSetIsEmpty()
    {
        Assert.Empty(HidGamepadLayout.CreateAxisLayout(new HashSet<ushort>()));
    }

    [Theory]
    [InlineData(0xFFu, 8, -1)]
    [InlineData(0x80u, 8, -128)]
    [InlineData(0x7Fu, 8, 127)]
    [InlineData(0xFFFFu, 16, -1)]
    [InlineData(0x7FFFu, 16, 32767)]
    [InlineData(0xFFFFFFFFu, 32, -1)]
    [InlineData(5u, 0, 5)]
    public void SignExtendInterpretsRawBitFieldsAtTheirReportedWidth(uint rawValue, int bitSize, int expected)
    {
        Assert.Equal(expected, HidGamepadLayout.SignExtend(rawValue, bitSize));
    }
}
