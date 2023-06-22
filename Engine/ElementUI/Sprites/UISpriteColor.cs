using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UISpriteColor : UISprite
    {
        public static Texture2D Texture;

        static UISpriteColor()
        {
            Texture = new Texture2D(1, 1, RgbaByte.White);
        }

        public UISpriteColor(RgbaByte color, Vector2I? size = null)
        {
            Color = color;
            Size = size ?? Vector2I.One;
        }

        public override void Draw(UIObject parent, SpriteBatch2D spriteBatch, Vector2I position, Vector2I? size = null, float rotation = 0)
        {
            spriteBatch.DrawTexture2D(
                texture: Texture,
                position: GetDrawPosition(parent, position, size ?? Size).ToVector2(),
                scale: (size ?? Size).ToVector2(),
                rotation: rotation,
                color: Color ?? RgbaByte.White);
        }
    }
}
