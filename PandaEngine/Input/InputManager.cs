using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace PandaEngine
{
    public class InputManager
    {
        public InputSnapshot InputSnapshot { get; set; }

        public void Update(InputSnapshot snapshot, GameTimer gameTimer)
        {
            InputSnapshot = snapshot;
        }
    }
}
