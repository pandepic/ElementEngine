using System.Xml.Linq;

namespace ElementEngine
{
    public class UIWImageBox : UIWidget
    {
        protected UISprite _image = null;

        public UIWImageBox() { }

        public override void Load(UIFrame parent, XElement el)
        {
            Init(parent, el);

            _image = UISprite.CreateUISprite(GetXMLElement("Image"));
            
            Width = _image.Width;
            Height = _image.Height;
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            _image?.Draw(spriteBatch, Position + Parent.Position);
        }
    } // UIWImageBox
}
