//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Xml.Linq;

//namespace PandaEngine
//{
//    public class PUIWBasicButton : PUIWidget
//    {
//        protected AnimatedSprite _buttonSprite = null;
//        protected AnimatedSprite _buttonPressedSprite = null;
//        protected AnimatedSprite _buttonHoverSprite = null;
//        protected AnimatedSprite _buttonDisabledSprite = null;

//        public DynamicSpriteFont Font { get; set; } = null;
//        public int FontSize { get; set; } = 0;
//        public string ButtonText { get; set; }
//        public Vector2 TextPosition { get; set; }
//        public Color ButtonTextColor { get; set; }

//        protected string _clickSound = "";

//        protected bool _buttonPressed = false;
//        protected bool _buttonHover = false;

//        public bool Disabled = false;

//        protected string _onClick = null;

//        public PUIWBasicButton() { }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            Init(parent, el);

//            bool preMultiplyAlpha = false;

//            var elAlpha = GetXMLElement("PreMultiplyAlpha");
//            if (elAlpha != null)
//                preMultiplyAlpha = bool.Parse(elAlpha.Value);

//            Texture2D buttonimage = null;
//            Texture2D buttonimage_pressed = null;
//            Texture2D buttonimage_hover = null;
//            Texture2D buttonimage_disabled = null;

//            var elImage = GetXMLElement("AssetName");
//            var elImagePressed = GetXMLElement("AssetNamePressed");
//            var elImageHover = GetXMLElement("AssetNameHover");
//            var elImageDisabled = GetXMLElement("AssetNameDisabled");

//            if (elImage != null && string.IsNullOrWhiteSpace(elImage.Value) == false)
//                buttonimage = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, elImage.Value, preMultiplyAlpha);
//            if (elImagePressed != null && string.IsNullOrWhiteSpace(elImagePressed.Value) == false)
//                buttonimage_pressed = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, elImagePressed.Value, preMultiplyAlpha);
//            if (elImageHover != null && string.IsNullOrWhiteSpace(elImageHover.Value) == false)
//                buttonimage_hover = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, elImageHover.Value, preMultiplyAlpha);
//            if (elImageDisabled != null && string.IsNullOrWhiteSpace(elImageDisabled.Value) == false)
//                buttonimage_disabled = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, elImageDisabled.Value, preMultiplyAlpha);

//            XElement buttonLabelPosition = GetXMLElement("Label", "Position");
//            XElement buttonLabelColor = GetXMLElement("Label", "Color");

//            Font = parent.CommonWidgetResources.Fonts[GetXMLElement("Label", "FontName").Value];
//            FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);

//            Font.Size = FontSize;
//            var labelSize = Font.MeasureString(GetXMLElement("Label", "Text").Value);

//            int textX = (buttonLabelPosition.Element("X").Value.ToUpper() != "CENTER"
//                ? int.Parse(buttonLabelPosition.Element("X").Value)
//                : (int)((buttonimage.Width / 2) - (labelSize.X / 2)));

//            int textY = (buttonLabelPosition.Element("Y").Value.ToUpper() != "CENTER"
//                ? int.Parse(buttonLabelPosition.Element("Y").Value)
//                : (int)((buttonimage.Height / 2) - (labelSize.Y / 2)));

//            var elOnClick = GetXMLElement("OnClick");

//            _buttonSprite = buttonimage == null ? null : new AnimatedSprite(buttonimage, buttonimage.Width, buttonimage.Height);
//            _buttonPressedSprite = buttonimage_pressed == null ? null : new AnimatedSprite(buttonimage_pressed, buttonimage_pressed.Width, buttonimage_pressed.Height);
//            _buttonHoverSprite = buttonimage_hover == null ? null : new AnimatedSprite(buttonimage_hover, buttonimage_hover.Width, buttonimage_hover.Height);
//            _buttonDisabledSprite = buttonimage_disabled == null ? null : new AnimatedSprite(buttonimage_disabled, buttonimage_disabled.Width, buttonimage_disabled.Height);
//            Width = buttonimage.Width;
//            Height = buttonimage.Height;
//            _onClick = elOnClick == null ? null : elOnClick.Value;
//            ButtonText = GetXMLElement("Label", "Text").Value;
//            TextPosition = new Vector2() { X = textX, Y = textY };
//            ButtonTextColor = PUIColorConversion.Instance.ToColor(buttonLabelColor.Value);

//            var clickSoundElement = GetXMLElement("ClickSound");
//            if (clickSoundElement != null)
//                _clickSound = clickSoundElement.Value;

//            UpdateRect();
//        }

//        protected void UpdateRect()
//        {
//            if (_buttonSprite != null)
//            {
//                Width = _buttonSprite.FrameWidth;
//                Height = _buttonSprite.FrameHeight;
//            }
//        }

//        public override void OnMouseDown(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            if (Disabled)
//                return;
//            if (button != MouseButtonID.Left)
//                return;

//            if (PointInsideWidget(mousePosition))
//            {
//                _buttonPressed = true;
//            }
//        }

//        public override void OnMouseClicked(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            if (Disabled)
//                return;
//            if (button != MouseButtonID.Left)
//                return;

//            if (_buttonPressed == true)
//            {
//                // button was clicked
//                HandleEvents?.Invoke(_onClick);

//                if (!string.IsNullOrWhiteSpace(_clickSound))
//                    ModManager.Instance.SoundManager.PlaySound(_clickSound, PandaMonogameConfig.UISoundType, false);

//                _buttonPressed = false;
//            }
//        }

//        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTime gameTime)
//        {
//            if (Disabled)
//                return;

//            if (PointInsideWidget(currentPosition))
//            {
//                _buttonHover = true;
//            }
//            else
//            {
//                _buttonHover = false;
//            }

//            if (_buttonPressed)
//            {
//                if (PointInsideWidget(currentPosition) == false)
//                {
//                    _buttonPressed = false;
//                }
//                else
//                {
//                    _buttonPressed = true;
//                }
//            }
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            if (Disabled)
//            {
//                if (_buttonDisabledSprite != null)
//                    _buttonDisabledSprite.Draw(spriteBatch, Position + Parent.Position);
//                else
//                {
//                    if (_buttonSprite != null)
//                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
//                }

//                return;
//            }

//            if (_buttonPressed == false)
//            {
//                if (_buttonHover)
//                {
//                    if (_buttonHoverSprite != null)
//                        _buttonHoverSprite.Draw(spriteBatch, Position + Parent.Position);
//                    else
//                    {
//                        if (_buttonSprite != null)
//                            _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
//                    }
//                }
//                else
//                {
//                    if (_buttonSprite != null)
//                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
//                }
//            }
//            else
//            {
//                if (_buttonPressedSprite != null)
//                    _buttonPressedSprite.Draw(spriteBatch, Position + Parent.Position);
//                else
//                {
//                    if (_buttonSprite != null)
//                        _buttonSprite.Draw(spriteBatch, Position + Parent.Position);
//                }
//            }

//            if (Font != null && ButtonText.Length > 0)
//            {
//                Font.Size = FontSize;
//                spriteBatch.DrawString(Font, ButtonText, TextPosition + Position + Parent.Position, ButtonTextColor);
//            }

//        } // draw
//    }
//}
