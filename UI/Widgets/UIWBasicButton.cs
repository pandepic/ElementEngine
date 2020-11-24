using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class UIWBasicButton : UIWidget
    {
        protected Sprite _buttonSprite = null;
        protected Sprite _buttonPressedSprite = null;
        protected Sprite _buttonHoverSprite = null;
        protected Sprite _buttonDisabledSprite = null;

        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public string ButtonText { get; set; }
        public Vector2 TextPosition { get; set; }
        public RgbaByte ButtonTextColor { get; set; }

        protected string _clickSound = "";

        protected bool _buttonPressed = false;
        protected bool _buttonHover = false;

        public bool Disabled = false;

        public UIWBasicButton() { }

        public override void Load(UIFrame parent, XElement el)
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

            var font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
            var fontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);

            var buttonText = GetXMLAttribute("Label", "Text").Value;
            var labelSize = font.MeasureText(buttonText, fontSize);

            int textX = (buttonLabelPosition.Attribute("X").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("X").Value)
                : (int)((buttonImage.Width / 2) - (labelSize.X / 2)));

            int textY = (buttonLabelPosition.Attribute("Y").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonLabelPosition.Attribute("Y").Value)
                : (int)((buttonImage.Height / 2) - (labelSize.Y / 2)));

            var textPosition = new Vector2() { X = textX, Y = textY };
            var buttonTextColor = new RgbaByte().FromHex(buttonLabelColor.Value);

            string clickSound = null;
            var clickSoundElement = GetXMLElement("ClickSound");
            if (clickSoundElement != null)
                clickSound = clickSoundElement.Value;

            Load(parent,
                buttonImage == null ? null : new AnimatedSprite(buttonImage, buttonImage.Size),
                buttonImagePressed == null ? null : new AnimatedSprite(buttonImagePressed, buttonImagePressed.Size),
                buttonImageHover == null ? null : new AnimatedSprite(buttonImageHover, buttonImageHover.Size),
                buttonImageDisabled == null ? null : new AnimatedSprite(buttonImageDisabled, buttonImageDisabled.Size),
                buttonText, font, fontSize, 0, textPosition, buttonTextColor, clickSound, preMultiplyAlpha);
        }

        public void Load(UIFrame parent,
            Sprite buttonSprite,
            Sprite buttonSpritePressed,
            Sprite buttonSpriteHover,
            Sprite buttonSpriteDisabled,
            string buttonText,
            SpriteFont font,
            int fontSize,
            int fontOutline,
            Vector2 textPosition,
            RgbaByte buttonTextColor,
            string clickSound = null,
            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None)
        {
            Init(parent);

            Font = font;
            FontSize = fontSize;
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

                TriggerUIEvent(UIEventType.ButtonClick);
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
    } // UIBasicButton
}
