using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class PUIWBasicButton : PUIWidget
    {
        protected AnimatedSprite _buttonSprite = null;
        protected AnimatedSprite _buttonPressedSprite = null;
        protected AnimatedSprite _buttonHoverSprite = null;
        protected AnimatedSprite _buttonDisabledSprite = null;

        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public string ButtonText { get; set; }
        public Vector2 TextPosition { get; set; }
        public RgbaByte ButtonTextColor { get; set; }

        protected string _clickSound = "";

        protected bool _buttonPressed = false;
        protected bool _buttonHover = false;

        public bool Disabled = false;

        public PUIWBasicButton() { }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None;

            var elAlpha = GetXMLAttribute("PremultiplyAlpha");
            if (elAlpha != null)
                preMultiplyAlpha = elAlpha.Value.ToEnum<TexturePremultiplyType>();

            Texture2D buttonImage = null;
            Texture2D buttonImagePressed = null;
            Texture2D buttonImageHover = null;
            Texture2D buttonImageDisabled = null;

            var elImage = GetXMLElement("AssetName");
            var elImagePressed = GetXMLElement("AssetNamePressed");
            var elImageHover = GetXMLElement("AssetNameHover");
            var elImageDisabled = GetXMLElement("AssetNameDisabled");

            if (elImage != null && string.IsNullOrWhiteSpace(elImage.Value) == false)
                buttonImage = AssetManager.LoadTexture2D(elImage.Value, preMultiplyAlpha);
            if (elImagePressed != null && string.IsNullOrWhiteSpace(elImagePressed.Value) == false)
                buttonImagePressed = AssetManager.LoadTexture2D(elImagePressed.Value, preMultiplyAlpha);
            if (elImageHover != null && string.IsNullOrWhiteSpace(elImageHover.Value) == false)
                buttonImageHover = AssetManager.LoadTexture2D(elImageHover.Value, preMultiplyAlpha);
            if (elImageDisabled != null && string.IsNullOrWhiteSpace(elImageDisabled.Value) == false)
                buttonImageDisabled = AssetManager.LoadTexture2D(elImageDisabled.Value, preMultiplyAlpha);

            XElement buttonLabelPosition = GetXMLElement("Label", "Position");
            XElement buttonLabelColor = GetXMLElement("Label", "Color");

            Font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
            FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);

            ButtonText = GetXMLAttribute("Label", "Text").Value;
            var labelSize = Font.MeasureText(ButtonText, FontSize);

            int textX = (buttonLabelPosition.Attribute("X").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("X").Value)
                : (int)((buttonImage.Width / 2) - (labelSize.X / 2)));

            int textY = (buttonLabelPosition.Attribute("Y").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("Y").Value)
                : (int)((buttonImage.Height / 2) - (labelSize.Y / 2)));

            _buttonSprite = buttonImage == null ? null : new AnimatedSprite(buttonImage, buttonImage.Size);
            _buttonPressedSprite = buttonImagePressed == null ? null : new AnimatedSprite(buttonImagePressed, buttonImagePressed.Size);
            _buttonHoverSprite = buttonImageHover == null ? null : new AnimatedSprite(buttonImageHover, buttonImageHover.Size);
            _buttonDisabledSprite = buttonImageDisabled == null ? null : new AnimatedSprite(buttonImageDisabled, buttonImageDisabled.Size);

            Width = buttonImage.Width;
            Height = buttonImage.Height;

            TextPosition = new Vector2() { X = textX, Y = textY };
            ButtonTextColor = new RgbaByte().FromHex(buttonLabelColor.Value);

            var clickSoundElement = GetXMLElement("ClickSound");
            if (clickSoundElement != null)
                _clickSound = clickSoundElement.Value;

            UpdateRect();
        }

        protected void UpdateRect()
        {
            if (_buttonSprite != null)
            {
                Width = _buttonSprite.FrameSize.X;
                Height = _buttonSprite.FrameSize.Y;
            }
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;
            if (button != MouseButton.Left)
                return;

            if (PointInsideWidget(mousePosition))
            {
                _buttonPressed = true;
            }
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

                TriggerPUIEvent(PUIEventType.ButtonClick);
                _buttonPressed = false;
            }
        }

        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTimer gameTimer)
        {
            if (Disabled)
                return;

            if (PointInsideWidget(currentPosition))
            {
                _buttonHover = true;
            }
            else
            {
                _buttonHover = false;
            }

            if (_buttonPressed)
            {
                if (PointInsideWidget(currentPosition) == false)
                {
                    _buttonPressed = false;
                }
                else
                {
                    _buttonPressed = true;
                }
            }
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
    } // PUIBasicButton
}
