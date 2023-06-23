namespace ElementEngine
{
    public interface IGamepadHandler
    {
        public int GamepadPriority { get; set; }

        public void HandleGamepadButtonPressed(Gamepad controller, GamepadButtonType button, GameTimer gameTimer) { }
        public void HandleGamepadButtonReleased(Gamepad controller, GamepadButtonType button, GameTimer gameTimer) { }
        public void HandleGamepadButtonDown(Gamepad controller, GamepadButtonType button, GameTimer gameTimer) { }
        public void HandleGamepadAxisMotion(Gamepad controller, GamepadInputType inputType, GamepadAxisMotionType motionType, float value, GameTimer gameTimer) { }
    }
}
