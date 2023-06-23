using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace ElementEngine
{
    internal struct AxisMotionEvent
    {
        internal GamepadInputType InputType;
        internal GamepadAxisMotionType MotionType;
        internal float Value;

        public AxisMotionEvent(GamepadInputType inputType, GamepadAxisMotionType motionType, float value)
        {
            InputType = inputType;
            MotionType = motionType;
            Value = value;
        }
    }

    internal struct GamepadButtonEvent
    {
        internal GamepadButtonType ButtonType;
        internal bool IsPressed;
        internal bool PrevIsPressed;

        public GamepadButtonEvent(GamepadButtonType buttonType, bool isPressed, bool prevIsPressed)
        {
            ButtonType = buttonType;
            IsPressed = isPressed;
            PrevIsPressed = prevIsPressed;
        }
    }

    public class Gamepad : IDisposable
    {
        public readonly SDL_GameController SDLController;

        public int ControllerIndex;
        public string ControllerName;
        public float Deadzone;

        internal readonly Dictionary<SDL_GameControllerAxis, float> AxisValues = new();
        internal readonly Dictionary<GamepadButtonType, bool> ButtonsPressed = new();
        internal readonly List<GamepadButtonEvent> ButtonEvents = new();
        internal readonly List<AxisMotionEvent> AxisMotionEvents = new();

        public void Dispose()
        {
            Sdl2Native.SDL_GameControllerClose(SDLController);
        }

        public unsafe Gamepad(int controllerIndex, float deadzone)
        {
            ControllerIndex = controllerIndex;
            SDLController = Sdl2Native.SDL_GameControllerOpen(controllerIndex);
            ControllerName = Marshal.PtrToStringUTF8((IntPtr)Sdl2Native.SDL_GameControllerName(SDLController));
            Deadzone = deadzone;
        }

        public float GetAxis(SDL_GameControllerAxis axis)
        {
            AxisValues.TryGetValue(axis, out var value);
            return value;
        }

        public bool IsSDLButtonPressed(SDL_GameControllerButton button)
        {
            ButtonsPressed.TryGetValue(InputManager.FromSDLControllerButton(button), out bool pressed);
            return pressed;
        }

        public bool IsButtonPressed(GamepadButtonType button)
        {
            ButtonsPressed.TryGetValue(button, out bool pressed);
            return pressed;
        }

        internal static float NormalizeAxis(short value)
        {
            return value < 0
                ? -(value / (float)short.MinValue)
                : (value / (float)short.MaxValue);
        }
    }
}
