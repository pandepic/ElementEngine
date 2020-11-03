using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class PUIWLabel : PUIWidget
    {
        public string UpdateArg = "";
        public SpriteFont Font = null;
        public int FontSize = 0;
        public RgbaByte Color = RgbaByte.White;

        protected string _text = "";
        public string Text
        {
            get => _text;
            set
            {
                UpdateText(value);
            }
        }

        public PUIWLabel() { }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            Font = AssetManager.LoadSpriteFont(GetXMLElement("FontName").Value);
            FontSize = int.Parse(GetXMLElement("FontSize").Value);
            Color = new RgbaByte().FromHex(GetXMLElement("Color").Value);

            UpdateText(GetXMLElement("Text").Value);
        }

        protected void UpdateText(string text)
        {
            _text = text;

            var tSize = Font.MeasureText(_text, FontSize);
            Width = (int)tSize.X;
            Height = (int)tSize.Y;

            UpdatePositionFromFlags();
        }

        public override void Update(GameTimer gameTimer)
        {
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(Font, _text, Position + Parent.Position, Color, FontSize);
        }

    } // PUIWLabel
}
