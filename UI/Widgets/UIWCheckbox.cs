using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class UIWCheckbox : UIWidget, IDisposable
    {
        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public int FontOutline { get; set; } = 0;
        public string Text { get; set; }
        public Vector2 TextPosition { get; set; }
        public Vector2 TextOffset { get; set; }
        public RgbaByte TextColor { get; set; }
        public bool Disabled { get; set; } = false;
        public bool Checked { get; set; } = false;

        protected UISprite _checkedSprite;
        protected UISprite _uncheckedSprite;
        protected UISprite _uncheckedHoverSprite;

        protected string _clickSound = null;
        protected bool _pressed = false;
        protected bool _hover = false;

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
                    _checkedSprite?.Dispose();
                    _uncheckedSprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public override void Load(UIFrame parent, XElement el)
        {
            _checkedSprite = UISprite.CreateUISprite(this, "Checked");
            _uncheckedSprite = UISprite.CreateUISprite(this, "Unchecked");

            if (GetXMLElement("UncheckedHover") != null)
                _uncheckedHoverSprite = UISprite.CreateUISprite(this, "UncheckedHover");

            XElement elLabelPosition = GetXMLElement("Label", "Position");
            XElement elLabelColor = GetXMLElement("Label", "Color");

            Font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
            FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);

            var elFontOutline = GetXMLElement("Label", "FontOutline");
            if (elFontOutline != null)
                FontOutline = int.Parse(elFontOutline.Value);

            var languageKeyAtt = GetXMLAttribute("Label", "LanguageKey");

            if (languageKeyAtt != null)
                Text = LocalisationManager.GetString(languageKeyAtt.Value);
            else
                Text = GetXMLAttribute("Label", "Text").Value;

            var labelSize = Font.MeasureText(Text, FontSize, FontOutline);
            var textX = int.Parse(elLabelPosition.Attribute("X").Value);

            var textY = (elLabelPosition.Attribute("Y").Value.ToUpper() != "CENTER"
                ? int.Parse(elLabelPosition.Attribute("Y").Value)
                : (int)((_checkedSprite.Height / 2) - (labelSize.Y / 2)));

            TextPosition = new Vector2() { X = textX, Y = textY };
            TextColor = new RgbaByte().FromHex(elLabelColor.Value);

            var elTextOffset = GetXMLElement("Label", "Offset");
            if (elTextOffset != null)
                TextOffset = new Vector2(int.Parse(elTextOffset.Attribute("X").Value), int.Parse(elTextOffset.Attribute("Y").Value));

            var clickSoundElement = GetXMLElement("ClickSound");
            if (clickSoundElement != null)
                _clickSound = clickSoundElement.Value;

            Width = (int)(_uncheckedSprite.Width + textX + labelSize.X);
            Height = _uncheckedSprite.Height;

            if (textY + labelSize.Y + TextOffset.Y > Height)
                Height = (int)(textY + labelSize.Y + TextOffset.Y);
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;
            if (button != MouseButton.Left)
                return;

            if (PointInsideWidget(mousePosition))
                _pressed = true;
        }

        public override void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;
            if (button != MouseButton.Left)
                return;

            if (_pressed == true)
            {
                if (!string.IsNullOrWhiteSpace(_clickSound))
                    SoundManager.Play(_clickSound, SoundManager.UISoundType);

                TriggerUIEvent(UIEventType.OnValueChanged);
                Checked = !Checked;
                _pressed = false;
            }
        }

        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;

            if (PointInsideWidget(currentPosition))
                _hover = true;
            else
                _hover = false;

            if (_pressed)
            {
                if (PointInsideWidget(currentPosition) == false)
                    _pressed = false;
                else
                    _pressed = true;
            }
        }

        public override void Update(GameTimer gameTimer)
        {
            _checkedSprite?.Update(gameTimer);
            _uncheckedSprite?.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = _checkedSprite;

            if (!Checked)
            {
                if (!_hover)
                    sprite = _uncheckedSprite;
                else
                    sprite = _uncheckedHoverSprite ?? _uncheckedSprite;
            }

            sprite.Draw(spriteBatch, Position + ParentPosition);
            spriteBatch.DrawText(Font, Text, Position + new Vector2(sprite.Width, 0) + ParentPosition + TextPosition + TextOffset, TextColor, FontSize, FontOutline);
        }

    } // UIWCheckbox
}
