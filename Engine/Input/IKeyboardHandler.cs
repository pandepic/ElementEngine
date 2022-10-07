using Veldrid;

namespace ElementEngine
{
    public interface IKeyboardHandler
    {
        public int KeyboardPriority { get; set; }

        public void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public void HandleTextInput(char key, GameTimer gameTimer) { }
    }
}
