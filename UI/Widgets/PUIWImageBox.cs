//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;

//namespace ElementEngine
//{
//    public class PUIWImageBox : PUIWidget
//    {
//        protected AnimatedSprite _image = null;

//        public PUIWImageBox() { }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            Init(parent, el);

//            bool preMultiplyAlpha = false;

//            var elAlpha = GetXMLElement("PreMultiplyAlpha");
//            if (elAlpha != null)
//                preMultiplyAlpha = bool.Parse(elAlpha.Value);

//            Texture2D texture = AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, GetXMLElement("AssetName").Value, preMultiplyAlpha);

//            Width = texture.Width;
//            Height = texture.Height;

//            _image = new AnimatedSprite(texture, texture.Width, texture.Height);
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            if (_image != null)
//                _image.Draw(spriteBatch, Position + Parent.Position);
//        }
//    }
//}
