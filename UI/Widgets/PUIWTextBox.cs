using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using TextCopy;
using Veldrid;

namespace ElementEngine
{
    public class PUIWTextBox : PUIWidget
    {
        protected string _text = "";

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _cursorIndex = _text.Length;
                UpdateTextTexture();
            }
        }

        protected Vector2 _textPosition = Vector2.Zero;
        protected Rectangle _textRect = Rectangle.Empty;
        protected SpriteFont _font = null;
        protected Clipboard _clipboard = new Clipboard();

        public int FontSize { get; set; } = 0;

        protected AnimatedSprite _background = null;
        protected Sprite _cursor = null;
        protected Texture2D _textTexture = null;

        protected int _cursorPadding = 0;
        protected int _cursorIndex = 0;

        protected List<char> _recentlyAdded = new List<char>();

        public RgbaByte Colour { get; set; } = RgbaByte.White;

        public PUIWTextBox() { }

        ~PUIWTextBox()
        {
            if (_textTexture != null)
                _textTexture.Dispose();

            if (_background != null)
                _background.Texture?.Dispose();
        }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None;

            var elAlpha = GetXMLElement("PreMultiplyAlpha");
            if (elAlpha != null)
                preMultiplyAlpha = elAlpha.Value.ToEnum<TexturePremultiplyType>();

            var backgroundElLeft = GetXMLElement("Background", "Left");
            var backgroundElRight = GetXMLElement("Background", "Right");
            var backgroundElCenter = GetXMLElement("Background", "Center");

            var textureLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : AssetManager.LoadTexture2D(backgroundElLeft.Value, preMultiplyAlpha));
            var textureRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : AssetManager.LoadTexture2D(backgroundElRight.Value, preMultiplyAlpha));
            var textureCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : AssetManager.LoadTexture2D(backgroundElCenter.Value, preMultiplyAlpha));

            var bgWidth = int.Parse(GetXMLElement("Background", "Width").Value);
            var backgroundTexture = new Texture2D(bgWidth, textureCenter.Height);
            backgroundTexture.BeginRenderTarget();
            backgroundTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = backgroundTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            var currentX = 0;
            var endX = 0;

            if (textureLeft != null)
            {
                currentX += textureLeft.Width;
                spriteBatch.DrawTexture2D(textureLeft, new Vector2(0, 0));
            }

            if (textureRight != null)
            {
                endX = bgWidth - textureRight.Width;
                spriteBatch.DrawTexture2D(textureRight, new Vector2(endX, 0));
            }

            while (currentX < endX)
            {
                var drawWidth = textureCenter.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(textureCenter, new Rectangle(currentX, 0, drawWidth, textureCenter.Height));
                currentX += textureCenter.Width;
            }

            spriteBatch.End();
            backgroundTexture.EndRenderTarget();

            _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Size);

            Height = _background.Height;
            Width = _background.Width;

            _textPosition = new Vector2(int.Parse(GetXMLElement("TextX").Value), int.Parse(GetXMLElement("TextY").Value));
            _font = AssetManager.LoadSpriteFont(GetXMLElement("FontName").Value);
            FontSize = int.Parse(GetXMLElement("FontSize").Value);
            _text = GetXMLElement("Text").Value;
            Colour = new RgbaByte().FromHex(GetXMLElement("Color").Value);
            _cursorPadding = (GetXMLAttribute("CursorPadding") == null ? 0 : int.Parse(GetXMLAttribute("CursorPadding").Value));
            _cursorIndex = _text.Length;

            var cursorTexture = new Texture2D(2, (uint)(textureCenter.Height - (_cursorPadding * 2)), Colour);
            _cursor = new Sprite(cursorTexture);

            _textRect.X = 0;
            _textRect.Y = 0;
            _textRect.Width = Width - ((int)_textPosition.X * 2);

            UpdateTextTexture();

            _textRect.Height = _textTexture.Height;
        }

        public void UpdateTextTexture()
        {
            if (_textTexture != null)
                _textTexture.Dispose();

            var tSize = _font.MeasureText(_text.Length > 0 ? _text : " ", FontSize);

            _textTexture = new Texture2D((int)tSize.X, (int)tSize.Y);
            _textTexture.BeginRenderTarget();
            _textTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = _textTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);
            spriteBatch.DrawText(_font, _text, Vector2.Zero, Colour, FontSize);
            spriteBatch.End();
            _textTexture.EndRenderTarget();

            _textRect.Width = _textTexture.Width;
            _textRect.Height = _textTexture.Height;

            if (_textRect.Width > (Width - ((int)_textPosition.X * 2)))
                _textRect.Width = (Width - ((int)_textPosition.X * 2));

            if ((_textRect.Width + _textRect.X) > _textTexture.Width)
                _textRect.Width = _textTexture.Width - _textRect.X;
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
        }

        public override void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (!Focused && PointInsideWidget(mousePosition))
                GrabFocus();
            else if (Focused)
                DropFocus();
        }

        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTimer gameTimer)
        {
        }

        public override void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            if (!Focused)
                return;

            switch (key)
            {
                case Key.Left:
                    {
                        if (_cursorIndex > 0)
                            _cursorIndex -= 1;
                    }
                    break;

                case Key.Right:
                    {
                        if (_cursorIndex < _text.Length)
                            _cursorIndex += 1;
                    }
                    break;

                case Key.Delete:
                    {
                        if (_text.Length > 0 && _cursorIndex < _text.Length)
                        {
                            _text = _text.Remove(_cursorIndex, 1);
                            UpdateTextTexture();
                        }
                    }
                    break;

                case Key.V:
                    {
                        if (InputManager.IsKeyDown(Key.ControlLeft) || InputManager.IsKeyDown(Key.ControlRight))
                        {
                            var clipboard = _clipboard.GetText();

                            if (clipboard != null && clipboard.Length > 0)
                            {
                                for (int i = 0; i < clipboard.Length; i++)
                                {
                                    var c = clipboard[i];
                                    AddCharacter(c);
                                }
                                UpdateTextTexture();
                            }
                        }
                    }
                    break;

                case Key.BackSpace:
                    {
                        if (_text.Length > 0 && _cursorIndex > 0)
                        {
                            _text = _text.Remove(_cursorIndex - 1, 1);
                            _cursorIndex -= 1;
                            UpdateTextTexture();
                        }
                    }
                    break;
            }
        }

        public override void OnKeyDown(Key key, GameTimer gameTimer)
        {
        }

        public override void OnTextInput(char key, GameTimer gameTimer)
        {
            if (!Focused)
                return;

            AddCharacter(key);
            UpdateTextTexture();
        }

        private void AddCharacter(char c)
        {
            try
            {
                _font.MeasureText(c.ToString(), FontSize);
            }
            catch
            {
                return;
            }

            _text = _text.Insert(_cursorIndex, c.ToString());
            _cursorIndex += 1;
            _recentlyAdded.Add(c);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            _background.Draw(spriteBatch, Position + Parent.Position);

            if (_font != null && _text.Length > 0)
            {
                try
                {
                    spriteBatch.DrawTexture2D(_textTexture, _textPosition + Position + Parent.Position, _textRect, null, null, 0f, Colour.ToRgbaFloat());
                }
                catch (ArgumentException)
                {
                    // remove recently added characters if they're not supported by the spritefont
                    foreach (var c in _recentlyAdded)
                    {
                        _text = _text.Replace(c.ToString(), "");
                        _cursorIndex -= 1;
                    }

                    UpdateTextTexture();
                }

                _recentlyAdded.Clear();
            }

            if (Focused)
            {
                var cursorPosition = new Vector2(_textPosition.X, _cursorPadding);

                if (_text.Length > 0)
                {
                    var tSize = _font.MeasureText(_text.Substring(0, _cursorIndex), FontSize);
                    cursorPosition.X = _textPosition.X + tSize.X - _textRect.X;

                    if (cursorPosition.X > (Width - ((int)_textPosition.X * 2)))
                        _textRect.X += ((int)cursorPosition.X - (Width - ((int)_textPosition.X * 2)));
                    else if (cursorPosition.X < (int)_textPosition.X)
                        _textRect.X -= (int)_textPosition.X - (int)cursorPosition.X;
                }

                _cursor.Draw(spriteBatch, Position + Parent.Position + cursorPosition);
            }
        }

        public override void Update(GameTimer gameTimer)
        {
        }

    } // PUIWTextBox
}
