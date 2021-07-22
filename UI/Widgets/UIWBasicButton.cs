using System;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class UIWBasicButton : UIWidget, IDisposable
    {
        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public int FontOutline { get; set; } = 0;
        public string ButtonText { get; set; }
        public Vector2 TextPosition { get; set; }
        public RgbaByte ButtonTextColor { get; set; }
        public bool Disabled = false;

        protected UISprite _buttonSprite = null;
        protected UISprite _buttonPressedSprite = null;
        protected UISprite _buttonHoverSprite = null;
        protected UISprite _buttonDisabledSprite = null;

        protected string _clickSound = "";
        protected bool _buttonPressed = false;
        protected bool _buttonHover = false;

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
                    _buttonSprite?.Dispose();
                    _buttonPressedSprite?.Dispose();
                    _buttonHoverSprite?.Dispose();
                    _buttonDisabledSprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIWBasicButton() { }

        ~UIWBasicButton()
        {
            Dispose(false);
        }

        public override void Load(UIFrame parent, XElement el)
        {
            var elButton = GetXMLElement("Button");
            var elButtonPressed = GetXMLElement("ButtonPressed");
            var elButtonHover = GetXMLElement("ButtonHover");
            var elButtonDisabled = GetXMLElement("ButtonDisabled");

            if (elButton != null)
                _buttonSprite = UISprite.CreateUISprite(this, "Button");
            if (elButtonPressed != null)
                _buttonPressedSprite = UISprite.CreateUISprite(this, "ButtonPressed");
            if (elButtonHover != null)
                _buttonHoverSprite = UISprite.CreateUISprite(this, "ButtonHover");
            if (elButtonDisabled != null)
                _buttonDisabledSprite = UISprite.CreateUISprite(this, "ButtonDisabled");

            XElement buttonLabelPosition = GetXMLElement("Label", "Position");
            XElement buttonLabelColor = GetXMLElement("Label", "Color");

            var font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
            var fontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);

            var elFontOutline = GetXMLElement("Label", "FontOutline");

            if (elFontOutline != null)
                FontOutline = int.Parse(elFontOutline.Value);

            var languageKeyAtt = GetXMLAttribute("Label", "LanguageKey");
            string buttonText;

            if (languageKeyAtt != null)
                buttonText = LocalisationManager.GetString(languageKeyAtt.Value);
            else
                buttonText = GetXMLAttribute("Label", "Text").Value;

            var labelSize = font.MeasureText(buttonText, fontSize, FontOutline);

            int textX = (buttonLabelPosition.Attribute("X").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("X").Value)
                : (int)((_buttonSprite.Width / 2) - (labelSize.X / 2)));

            int textY = (buttonLabelPosition.Attribute("Y").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("Y").Value)
                : (int)((_buttonSprite.Height / 2) - (labelSize.Y / 2)));

            var textPosition = new Vector2() { X = textX, Y = textY };
            var buttonTextColor = new RgbaByte().FromHex(buttonLabelColor.Value);

            string clickSound = null;
            var clickSoundElement = GetXMLElement("ClickSound");
            if (clickSoundElement != null)
                clickSound = clickSoundElement.Value;

            Load(parent,
                _buttonSprite,
                _buttonPressedSprite,
                _buttonHoverSprite,
                _buttonDisabledSprite,
                buttonText, font, fontSize, FontOutline, textPosition, buttonTextColor, clickSound);
        }

        public void Load(UIFrame parent,
            UISprite buttonSprite,
            UISprite buttonSpritePressed,
            UISprite buttonSpriteHover,
            UISprite buttonSpriteDisabled,
            string buttonText,
            SpriteFont font,
            int fontSize,
            int fontOutline,
            Vector2 textPosition,
            RgbaByte buttonTextColor,
            string clickSound = null)
        {
            Init(parent);

            Font = font;
            FontSize = fontSize;
            FontOutline = fontOutline;
            ButtonText = buttonText;

            _buttonSprite = buttonSprite;
            _buttonPressedSprite = buttonSpritePressed;
            _buttonHoverSprite = buttonSpriteHover;
            _buttonDisabledSprite = buttonSpriteDisabled;

            Width = _buttonSprite.Width;
            Height = _buttonSprite.Height;
            TextPosition = textPosition;
            ButtonTextColor = buttonTextColor;
            _clickSound = clickSound;

            UpdateRect();
        }

        protected void UpdateRect()
        {
            if (_buttonSprite != null)
            {
                Width = _buttonSprite.Width;
                Height = _buttonSprite.Height;
            }
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;
            if (button != MouseButton.Left)
                return;

            if (PointInsideWidget(mousePosition))
                _buttonPressed = true;
        }

        public override void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;
            if (button != MouseButton.Left)
                return;

            if (_buttonPressed == true)
            {
                if (!string.IsNullOrWhiteSpace(_clickSound))
                    SoundManager.Play(_clickSound, SoundManager.UISoundType);

                TriggerUIEvent(UIEventType.OnMouseClicked);
                _buttonPressed = false;
            }
        }

        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;

            if (PointInsideWidget(currentPosition))
                _buttonHover = true;
            else
                _buttonHover = false;

            if (_buttonPressed)
            {
                if (PointInsideWidget(currentPosition) == false)
                    _buttonPressed = false;
                else
                    _buttonPressed = true;
            }
        }

        public override void Update(GameTimer gameTimer)
        {
            _buttonSprite?.Update(gameTimer);
            _buttonPressedSprite?.Update(gameTimer);
            _buttonHoverSprite?.Update(gameTimer);
            _buttonDisabledSprite?.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (Disabled)
            {
                if (_buttonDisabledSprite != null)
                    _buttonDisabledSprite.Draw(spriteBatch, Position + Parent.Position);
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
                }

                return;
            }

            if (_buttonPressed == false)
            {
                if (_buttonHover)
                {
                    if (_buttonHoverSprite != null)
                        _buttonHoverSprite.Draw(spriteBatch, Position + Parent.Position);
                    else
                    {
                        if (_buttonSprite != null)
                            _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
                    }
                }
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
                }
            }
            else
            {
                if (_buttonPressedSprite != null)
                    _buttonPressedSprite.Draw(spriteBatch, Position + Parent.Position);
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
                }
            }

            if (Font != null && ButtonText.Length > 0)
            {
                spriteBatch.DrawText(Font, ButtonText, TextPosition + Position + Parent.Position, ButtonTextColor, FontSize);
            }

        } // Draw
    } // UIBasicButton
}
