//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml.Linq;

//namespace ElementEngine
//{
//    public class PUIWLabel : PUIWidget
//    {
//        public string UpdateArg = "";
//        public DynamicSpriteFont font = null;
//        public int fontSize = 0;

//        protected string _text = "";
//        public string Text
//        {
//            get => _text;
//            set
//            {
//                UpdateText(value);
//            }
//        }

//        public Color color = new Color(0, 0, 0, 255);

//        public BASICEVENT_FUNC onUpdate = null;

//        public PUIWLabel() { }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            Init(parent, el);

//            font = parent.CommonWidgetResources.Fonts[GetXMLElement("FontName").Value];
//            fontSize = int.Parse(GetXMLElement("FontSize").Value);
//            color = PUIColorConversion.Instance.ToColor(GetXMLElement("Color").Value);
//            onUpdate = parent.CommonWidgetResources.HandleEvents;
//            UpdateArg = (GetXMLElement("UpdateMethod") != null ? GetXMLElement("UpdateMethod").Value : "");

//            UpdateText(GetXMLElement("Text").Value);
//        }

//        protected void UpdateText(string text)
//        {
//            _text = text;

//            font.Size = fontSize;
//            var tSize = font.MeasureString(_text);
//            Width = (int)tSize.X;
//            Height = (int)tSize.Y;

//            UpdatePositionFromFlags();
//        }

//        public override void Update(GameTime gameTime)
//        {
//            onUpdate?.Invoke(UpdateArg);
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            font.Size = fontSize;
//            spriteBatch.DrawString(font, _text, Position + Parent.Position, color);
//        }

//    }
//}
