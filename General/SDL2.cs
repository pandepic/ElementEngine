using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace ElementEngine
{
    internal unsafe static class SDL2
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct SDL_JoyAxisEvent
        {
            /// <summary>
            /// SDL_JOYAXISMOTION
            /// </summary>
            public uint type;

            /// <summary>
            /// timestamp of the event
            /// </summary>
            public uint timestamp;

            /// <summary>
            /// the instance id of the joystick that reported the event
            /// </summary>
            public int which;

            /// <summary>
            /// the index of the axis that changed
            /// </summary>
            public byte axis;

            private byte padding1;
            private byte padding2;
            private byte padding3;

            /// <summary>
            /// the current position of the axis (range: -32768 to 32767)
            /// </summary>
            public short value;

            private ushort padding4;
        }
        #endregion

        internal static byte* StringArgToBytes(string arg)
        {
            int maxBytes = Encoding.UTF8.GetMaxByteCount(arg.Length);
            byte* utf8Bytes = stackalloc byte[maxBytes + 1];
            fixed (char* textPtr = arg)
            {
                int encodedBytes = Encoding.UTF8.GetBytes(textPtr, arg.Length, utf8Bytes, maxBytes);
                utf8Bytes[encodedBytes] = 0;
            }

            return utf8Bytes;
        }

        #region SDL_GetPlatform
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte* SDL_GetPlatform_t();
        private static SDL_GetPlatform_t s_sdl_getPlatform = Sdl2Native.LoadFunction<SDL_GetPlatform_t>("SDL_GetPlatform");

        /// <summary>
        /// Get the name of the platform.
        /// </summary>
        /// <returns>Returns the name of the platform. If the correct platform name is not available, returns a string beginning with the text "Unknown".</returns>
        public static byte* SDL_GetPlatform() => s_sdl_getPlatform();
        #endregion

        #region SDL_GameControllerAddMapping
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SDL_GameControllerAddMapping_t(byte* text);
        private static SDL_GameControllerAddMapping_t s_sdl_gameControllerAddMapping = Sdl2Native.LoadFunction<SDL_GameControllerAddMapping_t>("SDL_GameControllerAddMapping");

        /// <summary>
        /// Add support for controllers that SDL is unaware of or to cause an existing controller to have a different binding.
        /// </summary>
        /// <param name="mappingText">See https://wiki.libsdl.org/SDL_GameControllerAddMapping for an example of mapping strings.</param>
        /// <returns>Returns 1 if a new mapping is added, 0 if an existing mapping is updated, -1 on error; call SDL_GetError() for more information.</returns>
        public static int SDL_GameControllerAddMapping(string mappingText) => s_sdl_gameControllerAddMapping(StringArgToBytes(mappingText));
        #endregion
    }
}
