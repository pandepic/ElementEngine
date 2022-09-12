using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace ElementEngine
{
    public enum GameControllerCategory
    {
        Unknown,
        Xbox,
        Playstation,
        Switch,
    }

    public class GameController
    {
        public readonly SDL_GameController Controller;

        public int ControllerIndex;
        public string ControllerName;
        public float Deadzone;

        internal readonly Dictionary<SDL_GameControllerAxis, float> _axisValues = new();
        internal readonly Dictionary<SDL_GameControllerButton, bool> _buttonsPressed = new();

        public unsafe GameController(int controllerIndex, float deadzone)
        {
            ControllerIndex = controllerIndex;
            Controller = Sdl2Native.SDL_GameControllerOpen(controllerIndex);
            ControllerName = Marshal.PtrToStringUTF8((IntPtr)Sdl2Native.SDL_GameControllerName(Controller));
            Deadzone = deadzone;
        }

        public float GetAxis(SDL_GameControllerAxis axis)
        {
            return Sdl2Native.SDL_GameControllerGetAxis(Controller, axis);
        }

        public bool IsPressed(SDL_GameControllerButton button)
        {
            _buttonsPressed.TryGetValue(button, out bool pressed);
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
