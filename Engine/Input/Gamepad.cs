using System;
using System.Collections.Generic;

namespace ElementEngine
{
    public enum GamepadAxis
    {
        LeftX,
        LeftY,
        RightX,
        RightY,
        LeftTrigger,
        RightTrigger,
    }

    internal readonly record struct AxisMotionEvent(
        GamepadInputType InputType,
        GamepadAxisMotionType MotionType,
        float Value);

    internal readonly record struct GamepadButtonEvent(
        GamepadButtonType ButtonType,
        bool IsPressed,
        bool PrevIsPressed);

    internal readonly record struct NativeGamepadState(
        IReadOnlyDictionary<GamepadAxis, float> Axes,
        IReadOnlySet<GamepadButtonType> PressedButtons);

    internal interface INativeGamepad : IDisposable
    {
        int DeviceId { get; }
        string Name { get; }
        bool TryPoll(out NativeGamepadState state);
    }

    public sealed class Gamepad : IDisposable
    {
        private readonly INativeGamepad _device;
        private bool _disposed;

        public int ControllerIndex => _device.DeviceId;
        public string ControllerName => _device.Name;
        public float Deadzone { get; set; }

        internal readonly Dictionary<GamepadAxis, float> AxisValues = new();
        internal readonly Dictionary<GamepadButtonType, bool> ButtonsPressed = new();
        internal readonly List<GamepadButtonEvent> ButtonEvents = new();
        internal readonly List<AxisMotionEvent> AxisMotionEvents = new();

        internal Gamepad(INativeGamepad device, float deadzone)
        {
            _device = device;
            Deadzone = deadzone;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _device.Dispose();
            _disposed = true;
        }

        public float GetAxis(GamepadAxis axis)
        {
            AxisValues.TryGetValue(axis, out var value);
            return value;
        }

        public bool IsButtonPressed(GamepadButtonType button)
        {
            ButtonsPressed.TryGetValue(button, out var pressed);
            return pressed;
        }

        internal bool Poll()
        {
            if (!_device.TryPoll(out var state))
                return false;

            foreach (var button in Enum.GetValues<GamepadButtonType>())
            {
                if (button is GamepadButtonType.Invalid
                    or GamepadButtonType.LeftTrigger
                    or GamepadButtonType.RightTrigger
                    or GamepadButtonType.Max)
                    continue;

                var wasPressed = IsButtonPressed(button);
                var isPressed = state.PressedButtons.Contains(button);
                ButtonsPressed[button] = isPressed;

                if (wasPressed != isPressed || isPressed)
                    ButtonEvents.Add(new GamepadButtonEvent(button, isPressed, wasPressed));
            }

            UpdateAxis(state, GamepadAxis.LeftX, GamepadInputType.LeftAxis, GamepadAxisMotionType.X);
            UpdateAxis(state, GamepadAxis.LeftY, GamepadInputType.LeftAxis, GamepadAxisMotionType.Y);
            UpdateAxis(state, GamepadAxis.RightX, GamepadInputType.RightAxis, GamepadAxisMotionType.X);
            UpdateAxis(state, GamepadAxis.RightY, GamepadInputType.RightAxis, GamepadAxisMotionType.Y);
            UpdateTrigger(state, GamepadAxis.LeftTrigger, GamepadButtonType.LeftTrigger);
            UpdateTrigger(state, GamepadAxis.RightTrigger, GamepadButtonType.RightTrigger);
            return true;
        }

        private void UpdateAxis(
            NativeGamepadState state,
            GamepadAxis axis,
            GamepadInputType inputType,
            GamepadAxisMotionType motionType)
        {
            state.Axes.TryGetValue(axis, out var value);
            AxisValues.TryGetValue(axis, out var previousValue);
            AxisValues[axis] = value;

            var filteredValue = MathF.Abs(value) < Deadzone ? 0f : value;
            var filteredPreviousValue = MathF.Abs(previousValue) < Deadzone ? 0f : previousValue;

            if (filteredValue != filteredPreviousValue)
                AxisMotionEvents.Add(new AxisMotionEvent(inputType, motionType, filteredValue));
        }

        private void UpdateTrigger(
            NativeGamepadState state,
            GamepadAxis axis,
            GamepadButtonType button)
        {
            state.Axes.TryGetValue(axis, out var value);
            AxisValues[axis] = value;
            var wasPressed = IsButtonPressed(button);
            var isPressed = value > Deadzone;
            ButtonsPressed[button] = isPressed;

            if (wasPressed != isPressed || isPressed)
                ButtonEvents.Add(new GamepadButtonEvent(button, isPressed, wasPressed));
        }
    }
}
