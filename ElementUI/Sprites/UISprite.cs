﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UISprite
    {
        public Vector2I Size;
        public UIPosition Position;

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0f) { }

        public virtual Vector2I GetDrawPosition(UIObject parent, Vector2I parentPosition, Vector2I size)
        {
            var position = parentPosition + (Position.Position ?? Vector2I.Zero);

            if (Position.CenterX)
                position.X += (parent.Size.X / 2) - (size.X / 2);
            if (Position.CenterY)
                position.X += (parent.Size.Y / 2) - (size.Y / 2);

            return position;
        }

    } // UISprite
}