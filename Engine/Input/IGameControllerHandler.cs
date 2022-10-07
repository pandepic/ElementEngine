using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace ElementEngine
{
    public interface IGameControllerHandler
    {
        public int GameControllerPriority { get; set; }

        public void HandleControllerButtonPressed(GameController controller, GameControllerButtonType button, GameTimer gameTimer) { }
        public void HandleControllerButtonReleased(GameController controller, GameControllerButtonType button, GameTimer gameTimer) { }
        public void HandleControllerButtonDown(GameController controller, GameControllerButtonType button, GameTimer gameTimer) { }
        public void HandleControllerAxisMotion(GameController controller, GameControllerInputType inputType, GameControllerAxisMotionType motionType, float value, GameTimer gameTimer) { }
    }
}
