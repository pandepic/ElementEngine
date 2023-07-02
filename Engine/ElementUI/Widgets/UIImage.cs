namespace ElementEngine.ElementUI
{
    public class UIImage : UIObject
    {
        public new UIImageStyle Style => (UIImageStyle)_style;

        public UIScaleType ScaleType = UIScaleType.Scale;
        public int? CropWidth;
        public int? CropHeight;

        public UIImage(string name, UIImageStyle style) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.Sprite);

            CanFocus = false;
            ScaleType = Style.ScaleType ?? ScaleType;
        }

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            Style.Sprite.Update(gameTimer);
        }

        protected override void InnerDraw(SpriteBatch2D spriteBatch)
        {
            if (CropWidth.HasValue || CropHeight.HasValue)
            {
                var scissorRect = new Rectangle(DrawPosition, Vector2I.Zero);
                scissorRect.Width = CropWidth ?? Width;
                scissorRect.Height = CropHeight ?? Height;
                spriteBatch.PushScissorRect(0, scissorRect, true);
            }

            Style.Sprite.Draw(this, spriteBatch, DrawPosition, _size);

            if (CropWidth.HasValue || CropHeight.HasValue)
                spriteBatch.PopScissorRect(0);
        }
    }
}
