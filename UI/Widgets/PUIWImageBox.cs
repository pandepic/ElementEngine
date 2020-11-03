using System.Xml.Linq;

namespace ElementEngine
{
    public class PUIWImageBox : PUIWidget
    {
        protected AnimatedSprite _image = null;

        public PUIWImageBox() { }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None;

            var elAlpha = GetXMLElement("PreMultiplyAlpha");
            if (elAlpha != null)
                preMultiplyAlpha = elAlpha.Value.ToEnum<TexturePremultiplyType>();

            Texture2D texture = AssetManager.LoadTexture2D(GetXMLElement("AssetName").Value, preMultiplyAlpha);

            Width = texture.Width;
            Height = texture.Height;

            _image = new AnimatedSprite(texture, texture.Size);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (_image != null)
                _image.Draw(spriteBatch, Position + Parent.Position);
        }
    } // PUIWImageBox
}
