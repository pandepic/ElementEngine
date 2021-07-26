using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using TextCopy;
using Veldrid;

namespace ElementEngine.UI
{
    public class UIWTextBox : UIWidget, IDisposable
    {
        protected string _text = "";

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _cursorIndex = _text.Length;
                TriggerUIEvent(UIEventType.OnValueChanged);
            }
        }

        protected Vector2 _textPosition = Vector2.Zero;
        protected bool _centerTextX = false;
        protected bool _centerTextY = false;
        protected Rectangle _textRect = Rectangle.Empty;
        protected SpriteFont _font = null;
        protected Clipboard _clipboard = new Clipboard();

        public int FontSize { get; set; } = 0;
        public int OutlineSize { get; set; } = 0;

        protected UISprite _background = null;
        protected Sprite _cursor = null;

        protected int _cursorPadding = 0;
        protected int _cursorIndex = 0;
        protected Vector2 _cursorPosition = Vector2.Zero;

        protected List<char> _recentlyAdded = new List<char>();

        public RgbaByte Colour { get; set; } = RgbaByte.White;

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
                    _background?.Dispose();
                    _cursor?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIWTextBox() { }

        ~UIWTextBox()
        {
            Dispose(false);
        }

        public override void Load(UIFrame parent, XElement el)
        {
            _background = UISprite.CreateUISprite(this, "Background");

            Height = _background.Height;
            Width = _background.Width;

            var atTextPosX = GetXMLAttribute("TextPosition", "X");
            var atTextPosY = GetXMLAttribute("TextPosition", "Y");

            if (atTextPosX != null)
            {
                switch (atTextPosX.Value.ToUpper())
                {
                    case "CENTER":
                        _centerTextX = true;
                        break;

                    default:
                        _textPosition.X = int.Parse(atTextPosX.Value);
                        break;
                }
            }

            if (atTextPosY != null)
            {
                switch (atTextPosY.Value.ToUpper())
                {
                    case "CENTER":
                        _centerTextY = true;
                        break;

                    default:
                        _textPosition.Y = int.Parse(atTextPosY.Value);
                        break;
                }
            }

            _font = AssetManager.LoadSpriteFont(GetXMLElement("FontName").Value);
            FontSize = int.Parse(GetXMLElement("FontSize").Value);
            _text = GetXMLElement("Text").Value;
            Colour = new RgbaByte().FromHex(GetXMLElement("Color").Value);
            _cursorPadding = (GetXMLAttribute("CursorPadding") == null ? 0 : int.Parse(GetXMLAttribute("CursorPadding").Value));
            _cursorIndex = _text.Length;
            
            var cursorTexture = new Texture2D(2, (uint)(_background.Height - (_cursorPadding * 2)), Colour);
            _cursor = new Sprite(cursorTexture);
            
            _textRect.X = 0;
            _textRect.Y = 0;
            _textRect.Width = (int)(Width - (_textPosition.X * 2));
            _textRect.Height = (int)(Height - (_textPosition.Y * 2));
        }

        public override void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
        }

        public override void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            if (PointInsideWidget(mousePosition))
            {
                if (!Focused)
                    GrabFocus();

                if (Focused)
                {
                    if (_text.Length > 0)
                    {
                        var relativeX = mousePosition.X - X - _textPosition.X;
                        var textSize = 0;
                        var found = false;

                        for (var i = 0; i <= _text.Length; i++)
                        {
                            if (i > 0)
                                textSize = (int)_font.MeasureText(_text.Substring(0, i), FontSize, OutlineSize).X;

                            if (textSize - _textRect.X > relativeX)
                            {
                                _cursorIndex = i - 1;
                                found = true;
                                break;
                            }
                        }

                        if (!found && textSize - _textRect.X < relativeX)
                            _cursorIndex = _text.Length;

                        _cursorIndex = Math.Clamp(_cursorIndex, 0, _text.Length);
                    }
                }
            }
            else if (Focused)
            {
                DropFocus();
            }
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
                            TriggerUIEvent(UIEventType.OnValueChanged);
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

                                TriggerUIEvent(UIEventType.OnValueChanged);
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
                            TriggerUIEvent(UIEventType.OnValueChanged);
                        }
                    }
                    break;

                case Key.End:
                    {
                        _cursorIndex = _text.Length;
                    }
                    break;

                case Key.Home:
                    {
                        _cursorIndex = 0;
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
            TriggerUIEvent(UIEventType.OnValueChanged);
        }

        private void AddCharacter(char c)
        {
            var str = c.ToString();

            // try catch is to filter out unsupported characters
            try
            {
                _font.MeasureText(str, FontSize);
            }
            catch
            {
                return;
            }

            _text = _text.Insert(_cursorIndex, str);
            _cursorIndex += 1;
            _recentlyAdded.Add(c);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            _background.Draw(spriteBatch, Position + ParentPosition);

            if (_font != null && _text.Length > 0)
            {
                try
                {
                    var offsetPosition = Vector2.Zero;
                    var textSize = _font.MeasureText(_text.Length > 0 ? _text : " ", FontSize);

                    if (_centerTextX)
                        offsetPosition.X = (Width / 2) - textSize.X / 2;
                    if (_centerTextY)
                        offsetPosition.Y = (Height / 2) - textSize.Y / 2;

                    var cursorOffset = new Vector2(_textRect.X * -1f, 0f);
                    var drawPosition = offsetPosition + _textPosition + Position + ParentPosition;

                    spriteBatch.SetScissorRect(new Rectangle(drawPosition, _textRect.SizeF));
                    spriteBatch.DrawText(_font, _text, drawPosition + cursorOffset, Colour, FontSize);
                    spriteBatch.ResetScissorRect();
                }
                catch (ArgumentException)
                {
                    // remove recently added characters if they're not supported by the spritefont
                    foreach (var c in _recentlyAdded)
                    {
                        _text = _text.Replace(c.ToString(), "");
                        _cursorIndex -= 1;
                    }

                    TriggerUIEvent(UIEventType.OnValueChanged);
                }

                _recentlyAdded.Clear();
            }

            if (Focused)
            {
                CalculateCursorPosition();

                if (_text.Length > 0)
                {
                    if (_cursorPosition.X > (Width - _textPosition.X))
                        _textRect.X += (int)(_cursorPosition.X - (Width - _textPosition.X));
                    else if (_cursorPosition.X < (int)_textPosition.X)
                        _textRect.X -= (int)(_textPosition.X - _cursorPosition.X);
                }

                _cursor.Draw(spriteBatch, Position + ParentPosition + _cursorPosition);
            }
        }

        protected void CalculateCursorPosition()
        {
            _cursorPosition = new Vector2(_textPosition.X, _cursorPadding);

            if (_text.Length > 0)
            {
                var textSize = _font.MeasureText(_text.Substring(0, _cursorIndex), FontSize);
                _cursorPosition.X = _textPosition.X + textSize.X - _textRect.X;
            }
        }

        public override void Update(GameTimer gameTimer)
        {
            _background.Update(gameTimer);
        }

    } // UIWTextBox
}
