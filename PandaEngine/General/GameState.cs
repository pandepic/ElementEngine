using System;
using System.Collections.Generic;
using System.Text;

namespace PandaEngine
{
    public class GameState
    {
        public virtual void Load(AssetManager assetManager)
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void Update(GameTimer gameTimer)
        {

        }

        public virtual void Draw(GameTimer gameTimer)
        {

        }
    }
}
