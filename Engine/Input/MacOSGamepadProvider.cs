using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal static class MacOSGamepadProvider
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string GameControllerFramework = "/System/Library/Frameworks/GameController.framework/GameController";

    private static readonly nint FrameworkHandle = NativeLibrary.Load(GameControllerFramework);
    private static readonly nint ControllerClass = objc_getClass("GCController");
    private static readonly nint ControllersSelector = sel_registerName("controllers");
    private static readonly nint CountSelector = sel_registerName("count");
    private static readonly nint ObjectAtIndexSelector = sel_registerName("objectAtIndex:");

    public static IReadOnlyList<INativeGamepad> Enumerate()
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            var controllerArray = SendObject(ControllerClass, ControllersSelector);
            var count = SendUnsigned(controllerArray, CountSelector);
            var devices = new List<INativeGamepad>(checked((int)count));

            for (nuint index = 0; index < count; index++)
            {
                var controller = SendObject(controllerArray, ObjectAtIndexSelector, index);
                if (controller != 0 && MacOSGamepad.Supports(controller))
                    devices.Add(new MacOSGamepad(controller));
            }

            try
            {
                devices.AddRange(MacOSHidGamepadProvider.Enumerate());
            }
            catch (Exception ex)
            {
                // An IOHID enumeration failure must never break the GameController pads.
                Logging.Warning("HID gamepad enumeration failed: {message}", ex.Message);
            }

            return devices;
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
            GC.KeepAlive(FrameworkHandle);
        }
    }

    private sealed class MacOSGamepad : INativeGamepad
    {
        private static readonly nint RespondsToSelector = sel_registerName("respondsToSelector:");
        private static readonly nint ContainsObjectSelector = sel_registerName("containsObject:");
        private static readonly nint ExtendedGamepadSelector = sel_registerName("extendedGamepad");
        private static readonly nint VendorNameSelector = sel_registerName("vendorName");
        private static readonly nint Utf8StringSelector = sel_registerName("UTF8String");
        private static readonly nint ValueSelector = sel_registerName("value");
        private static readonly nint PressedSelector = sel_registerName("isPressed");
        private static readonly nint LeftThumbstickSelector = sel_registerName("leftThumbstick");
        private static readonly nint RightThumbstickSelector = sel_registerName("rightThumbstick");
        private static readonly nint XAxisSelector = sel_registerName("xAxis");
        private static readonly nint YAxisSelector = sel_registerName("yAxis");
        private static readonly nint DPadSelector = sel_registerName("dpad");
        private static readonly nint UpSelector = sel_registerName("up");
        private static readonly nint DownSelector = sel_registerName("down");
        private static readonly nint LeftSelector = sel_registerName("left");
        private static readonly nint RightSelector = sel_registerName("right");
        private static readonly nint ButtonASelector = sel_registerName("buttonA");
        private static readonly nint ButtonBSelector = sel_registerName("buttonB");
        private static readonly nint ButtonXSelector = sel_registerName("buttonX");
        private static readonly nint ButtonYSelector = sel_registerName("buttonY");
        private static readonly nint LeftShoulderSelector = sel_registerName("leftShoulder");
        private static readonly nint RightShoulderSelector = sel_registerName("rightShoulder");
        private static readonly nint LeftTriggerSelector = sel_registerName("leftTrigger");
        private static readonly nint RightTriggerSelector = sel_registerName("rightTrigger");
        private static readonly nint ButtonMenuSelector = sel_registerName("buttonMenu");
        private static readonly nint ButtonOptionsSelector = sel_registerName("buttonOptions");
        private static readonly nint ButtonHomeSelector = sel_registerName("buttonHome");
        private static readonly nint LeftThumbstickButtonSelector = sel_registerName("leftThumbstickButton");
        private static readonly nint RightThumbstickButtonSelector = sel_registerName("rightThumbstickButton");

        private nint _controller;

        public int DeviceId { get; }
        public string Name { get; }

        public MacOSGamepad(nint controller)
        {
            _controller = objc_retain(controller);
            DeviceId = controller.GetHashCode();
            Name = GetName(controller);
        }

        public static bool Supports(nint controller) => SendObject(controller, ExtendedGamepadSelector) != 0;

        public bool TryPoll(out NativeGamepadState state)
        {
            var pool = objc_autoreleasePoolPush();

            try
            {
                if (_controller == 0 || !IsConnected(_controller))
                {
                    state = default;
                    return false;
                }

                var gamepad = SendObject(_controller, ExtendedGamepadSelector);
                if (gamepad == 0)
                {
                    state = default;
                    return false;
                }

                var leftStick = SendObject(gamepad, LeftThumbstickSelector);
                var rightStick = SendObject(gamepad, RightThumbstickSelector);
                var leftTrigger = SendOptionalObject(gamepad, LeftTriggerSelector);
                var rightTrigger = SendOptionalObject(gamepad, RightTriggerSelector);
                var axes = new Dictionary<GamepadAxis, float>(6)
                {
                    [GamepadAxis.LeftX] = AxisValue(leftStick, XAxisSelector),
                    [GamepadAxis.LeftY] = -AxisValue(leftStick, YAxisSelector),
                    [GamepadAxis.RightX] = AxisValue(rightStick, XAxisSelector),
                    [GamepadAxis.RightY] = -AxisValue(rightStick, YAxisSelector),
                    [GamepadAxis.LeftTrigger] = ButtonValue(leftTrigger),
                    [GamepadAxis.RightTrigger] = ButtonValue(rightTrigger),
                };

                var buttons = new HashSet<GamepadButtonType>();
                AddButton(buttons, gamepad, ButtonASelector, GamepadButtonType.A);
                AddButton(buttons, gamepad, ButtonBSelector, GamepadButtonType.B);
                AddButton(buttons, gamepad, ButtonXSelector, GamepadButtonType.X);
                AddButton(buttons, gamepad, ButtonYSelector, GamepadButtonType.Y);
                AddButton(buttons, gamepad, ButtonOptionsSelector, GamepadButtonType.Back);
                AddButton(buttons, gamepad, ButtonHomeSelector, GamepadButtonType.Guide);
                AddButton(buttons, gamepad, ButtonMenuSelector, GamepadButtonType.Start);
                AddButton(buttons, gamepad, LeftThumbstickButtonSelector, GamepadButtonType.LeftStick);
                AddButton(buttons, gamepad, RightThumbstickButtonSelector, GamepadButtonType.RightStick);
                AddButton(buttons, gamepad, LeftShoulderSelector, GamepadButtonType.LeftShoulder);
                AddButton(buttons, gamepad, RightShoulderSelector, GamepadButtonType.RightShoulder);

                var dPad = SendObject(gamepad, DPadSelector);
                AddButton(buttons, dPad, UpSelector, GamepadButtonType.DPadUp);
                AddButton(buttons, dPad, DownSelector, GamepadButtonType.DPadDown);
                AddButton(buttons, dPad, LeftSelector, GamepadButtonType.DPadLeft);
                AddButton(buttons, dPad, RightSelector, GamepadButtonType.DPadRight);

                state = new NativeGamepadState(axes, buttons);
                return true;
            }
            finally
            {
                objc_autoreleasePoolPop(pool);
            }
        }

        public void Dispose()
        {
            if (_controller == 0)
                return;

            objc_release(_controller);
            _controller = 0;
        }

        private static string GetName(nint controller)
        {
            var name = SendObject(controller, VendorNameSelector);
            var utf8 = name == 0 ? 0 : SendObject(name, Utf8StringSelector);
            return utf8 == 0 ? "Game Controller" : Marshal.PtrToStringUTF8(utf8) ?? "Game Controller";
        }

        private static float AxisValue(nint directionPad, nint selector)
        {
            var axis = SendObject(directionPad, selector);
            return axis == 0 ? 0f : SendFloat(axis, ValueSelector);
        }

        private static float ButtonValue(nint button) => button == 0 ? 0f : SendFloat(button, ValueSelector);

        private static void AddButton(
            HashSet<GamepadButtonType> buttons,
            nint gamepad,
            nint selector,
            GamepadButtonType button)
        {
            var input = SendOptionalObject(gamepad, selector);
            if (input != 0 && SendBoolean(input, PressedSelector))
                buttons.Add(button);
        }

        private static nint SendOptionalObject(nint target, nint selector)
            => target != 0 && SendBoolean(target, RespondsToSelector, selector)
                ? SendObject(target, selector)
                : 0;

        private static bool IsConnected(nint controller)
        {
            var controllers = SendObject(ControllerClass, ControllersSelector);
            return controllers != 0 && SendBoolean(controllers, ContainsObjectSelector, controller);
        }
    }

    private static nint SendObject(nint target, nint selector) => objc_msgSend(target, selector);
    private static nint SendObject(nint target, nint selector, nuint value) => objc_msgSend_unsigned(target, selector, value);
    private static nuint SendUnsigned(nint target, nint selector) => objc_msgSend_nuint(target, selector);
    private static float SendFloat(nint target, nint selector) => objc_msgSend_float(target, selector);
    private static bool SendBoolean(nint target, nint selector) => objc_msgSend_bool(target, selector);
    private static bool SendBoolean(nint target, nint selector, nint value) => objc_msgSend_bool_pointer(target, selector, value);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint objc_retain(nint value);

    [DllImport(ObjectiveCLibrary)]
    private static extern void objc_release(nint value);

    [DllImport(ObjectiveCLibrary)]
    private static extern nint objc_autoreleasePoolPush();

    [DllImport(ObjectiveCLibrary)]
    private static extern void objc_autoreleasePoolPop(nint pool);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend(nint target, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend_unsigned(nint target, nint selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nuint objc_msgSend_nuint(nint target, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern float objc_msgSend_float(nint target, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool(nint target, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_pointer(nint target, nint selector, nint value);
}
