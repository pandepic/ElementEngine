using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace ElementEngine
{
    internal struct AxisMotionEvent
    {
        internal GameControllerInputType InputType;
        internal GameControllerAxisMotionType MotionType;
        internal float Value;

        public AxisMotionEvent(GameControllerInputType inputType, GameControllerAxisMotionType motionType, float value)
        {
            InputType = inputType;
            MotionType = motionType;
            Value = value;
        }
    }

    internal struct GameControllerButtonEvent
    {
        internal GameControllerButtonType ButtonType;
        internal bool IsPressed;
        internal bool PrevIsPressed;

        public GameControllerButtonEvent(GameControllerButtonType buttonType, bool isPressed, bool prevIsPressed)
        {
            ButtonType = buttonType;
            IsPressed = isPressed;
            PrevIsPressed = prevIsPressed;
        }
    }

    public class GameController : IDisposable
    {
        public readonly SDL_GameController Controller;

        public int ControllerIndex;
        public string ControllerName;
        public float Deadzone;

        internal readonly Dictionary<SDL_GameControllerAxis, float> AxisValues = new();
        internal readonly Dictionary<GameControllerButtonType, bool> ButtonsPressed = new();
        internal readonly List<GameControllerButtonEvent> ButtonEvents = new();
        internal readonly List<AxisMotionEvent> AxisMotionEvents = new();

        public void Dispose()
        {
            Sdl2Native.SDL_GameControllerClose(Controller);
        }

        public unsafe GameController(int controllerIndex, float deadzone)
        {
            ControllerIndex = controllerIndex;
            Controller = Sdl2Native.SDL_GameControllerOpen(controllerIndex);
            ControllerName = Marshal.PtrToStringUTF8((IntPtr)Sdl2Native.SDL_GameControllerName(Controller));
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

        public bool IsButtonPressed(GameControllerButtonType button)
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
