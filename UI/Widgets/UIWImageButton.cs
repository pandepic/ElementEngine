using System;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class UIWImageButton : UIWidget, IDisposable
    {
        public Vector2 ImagePosition { get; set; }
        public bool Disabled = false;

        protected UISprite _buttonSprite = null;
        protected UISprite _buttonPressedSprite = null;
        protected UISprite _buttonHoverSprite = null;
        protected UISprite _buttonDisabledSprite = null;
        protected UISprite _imageSprite = null;

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
                    _imageSprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIWImageButton() { }

        ~UIWImageButton()
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

            _imageSprite = UISprite.CreateUISprite(this, "Image");

            XElement buttonImagePosition = GetXMLElement("ImagePosition");

            int imageX = (buttonImagePosition.Attribute("X").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonImagePosition.Attribute("X").Value)
                : (int)((_buttonSprite.Width / 2) - (_imageSprite.Size.X / 2)));

            int imageY = (buttonImagePosition.Attribute("Y").Value.ToUpper() != "CENTER"
                ? int.Parse(buttonImagePosition.Attribute("Y").Value)
                : (int)((_buttonSprite.Height / 2) - (_imageSprite.Size.Y / 2)));

            var imagePosition = new Vector2() { X = imageX, Y = imageY };

            string clickSound = null;
            var clickSoundElement = GetXMLElement("ClickSound");
            if (clickSoundElement != null)
                clickSound = clickSoundElement.Value;

            Load(parent,
                _buttonSprite,
                _buttonPressedSprite,
                _buttonHoverSprite,
                _buttonDisabledSprite,
                _imageSprite,
                imagePosition,
                clickSound);
        }

        public void Load(UIFrame parent,
            UISprite buttonSprite,
            UISprite buttonSpritePressed,
            UISprite buttonSpriteHover,
            UISprite buttonSpriteDisabled,
            UISprite imageSprite,
            Vector2 imagePosition,
            string clickSound = null)
        {
            Init(parent);

            _buttonSprite = buttonSprite;
            _buttonPressedSprite = buttonSpritePressed;
            _buttonHoverSprite = buttonSpriteHover;
            _buttonDisabledSprite = buttonSpriteDisabled;
            _imageSprite = imageSprite;

            Width = _buttonSprite.Width;
            Height = _buttonSprite.Height;
            ImagePosition = imagePosition;
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

                TriggerUIEvent(UIEventType.OnMouseClicked);
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

        public override void Update(GameTimer gameTimer)
        {
            _buttonSprite?.Update(gameTimer);
            _buttonPressedSprite?.Update(gameTimer);
            _buttonHoverSprite?.Update(gameTimer);
            _buttonDisabledSprite?.Update(gameTimer);
            _imageSprite?.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (Disabled)
            {
                if (_buttonDisabledSprite != null)
                    _buttonDisabledSprite.Draw(spriteBatch, Position + ParentPosition);
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + ParentPosition);
                }

                return;
            }

            if (_buttonPressed == false)
            {
                if (_buttonHover)
                {
                    if (_buttonHoverSprite != null)
                        _buttonHoverSprite.Draw(spriteBatch, Position + ParentPosition);
                    else
                    {
                        if (_buttonSprite != null)
                            _buttonSprite.Draw(spriteBatch, Position + ParentPosition);
                    }
                }
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + ParentPosition);
                }
            }
            else
            {
                if (_buttonPressedSprite != null)
                    _buttonPressedSprite.Draw(spriteBatch, Position + ParentPosition);
                else
                {
                    if (_buttonSprite != null)
                        _buttonSprite.Draw(spriteBatch, Position + ParentPosition);
                }
            }

            if (_imageSprite != null)
                _imageSprite.Draw(spriteBatch, ImagePosition + Position + ParentPosition);

        } // Draw

    } // UIImageButton
}
