using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISprite
    {
        public Vector2I Size;
        public UIPosition Position;
        public UISpacing Margin;
        public bool IgnoreSize;
        public RgbaByte? Color;

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0f) { }

        public virtual Vector2I GetDrawPosition(UIObject parent, Vector2I parentPosition, Vector2I size)
        {
            var position = parentPosition + (Position.Position ?? Vector2I.Zero);

            if (Position.CenterX)
                position.X += (parent.Size.X / 2) - (size.X / 2);
            if (Position.CenterY)
                position.Y += (parent.Size.Y / 2) - (size.Y / 2);

            if (Position.AnchorRight)
                position.X += parent.PaddingLeft + (parent.PaddingBounds.Width - size.X) - Margin.Right;
            if (Position.AnchorBottom)
                position.Y += parent.PaddingTop + (parent.PaddingBounds.Height - size.Y) - Margin.Bottom;

            position += Margin.TopLeft;
            return position;
        }
    }
}
