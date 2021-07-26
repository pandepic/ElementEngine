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
        public UIPosition Position;

        public virtual void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2 position, Vector2? size = null, float rotation = 0f) { }

        public virtual Vector2 GetDrawPosition(UIObject parent, Vector2 parentPosition, Vector2 size)
        {
            var position = parentPosition + (Position.Position ?? Vector2.Zero);

            if (Position.CenterX)
                position.X += (parent.Size.X / 2) - (size.X / 2);
            if (Position.CenterY)
                position.X += (parent.Size.Y / 2) - (size.Y / 2);

            return position;
        }

    } // UISprite
}
