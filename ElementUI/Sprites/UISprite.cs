using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISprite
    {
        public Vector2 Size;

        public virtual void Draw(SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0f) { }
    }
}
