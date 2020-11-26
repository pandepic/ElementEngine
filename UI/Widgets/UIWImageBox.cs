using System;
using System.Xml.Linq;

namespace ElementEngine
{
    public class UIWImageBox : UIWidget, IDisposable
    {
        protected UISprite _image = null;

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _image?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIWImageBox() { }

        ~UIWImageBox()
        {
            Dispose(false);
        }

        public override void Load(UIFrame parent, XElement el)
        {
            Init(parent, el);

            _image = UISprite.CreateUISprite(this, "Image");
            
            Width = _image.Width;
            Height = _image.Height;
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            _image?.Draw(spriteBatch, Position + Parent.Position);
        }
    } // UIWImageBox
}
