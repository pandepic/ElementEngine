using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

        public override void Update(GameTimer gameTimer)
        {
            Style.Sprite.Update(gameTimer);
            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
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

            base.Draw(spriteBatch);
        }

    } // UIImage
}
