using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class UIWLabel : UIWidget
    {
        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public int Outline { get; set; } = 0;
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

        public UIWLabel() { }

        public override void Load(UIFrame parent, XElement el)
        {
            Init(parent, el);

            Font = AssetManager.LoadSpriteFont(GetXMLElement("FontName").Value);
            FontSize = int.Parse(GetXMLElement("FontSize").Value);
            Color = new RgbaByte().FromHex(GetXMLElement("Color").Value);

            var attOutline = GetXMLAttribute("Outline");
            if (attOutline != null)
                Outline = int.Parse(attOutline.Value);

            UpdateText(GetXMLElement("Text").Value);
        }

        protected void UpdateText(string text)
        {
            _text = text;

            var tSize = Font.MeasureText(_text, FontSize, Outline);
            Width = (int)tSize.X;
            Height = (int)tSize.Y;

            UpdatePositionFromFlags();
        }

        public override void Update(GameTimer gameTimer)
        {
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            spriteBatch.DrawText(Font, _text, Position + Parent.Position, Color, FontSize, Outline);
        }

    } // UIWLabel
}
