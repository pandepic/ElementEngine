//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;

//namespace PandaEngine
//{
//    public class PUIWTextBox : PUIWidget
//    {
//        protected GraphicsDevice _graphics = null;

//        protected string _text = "";

//        public string Text
//        {
//            get => _text;
//            set
//            {
//                _text = value;
//                _cursorIndex = _text.Length;
//                UpdateTextTexture();
//            }
//        }

//        protected Vector2 _textPosition = Vector2.Zero;
//        protected Rectangle _textRect = Rectangle.Empty;
//        protected DynamicSpriteFont _font = null;
//        public int FontSize { get; set; } = 0;

//        protected AnimatedSprite _background = null;
//        protected Sprite _cursor = null;
//        protected Texture2D _textTexture = null;

//        protected int _cursorPadding = 0;
//        protected int _cursorIndex = 0;

//        protected List<char> _recentlyAdded = new List<char>();

//        public Color Colour { get; set; } = new Color(0, 0, 0, 255);

//        public PUIWTextBox() { }

//        ~PUIWTextBox()
//        {
//            if (_textTexture != null)
//                _textTexture.Dispose();

//            if (_background != null)
//                _background.Texture?.Dispose();
//        }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            _graphics = parent.CommonWidgetResources.Graphics;
//            Init(parent, el);

//            var backgroundElLeft = GetXMLElement("Background", "Left");
//            var backgroundElRight = GetXMLElement("Background", "Right");
//            var backgroundElCenter = GetXMLElement("Background", "Center");

//            var textureLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(_graphics, backgroundElLeft.Value));
//            var textureRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(_graphics, backgroundElRight.Value));
//            var textureCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(_graphics, backgroundElCenter.Value));

//            var bgWidth = int.Parse(GetXMLElement("Background", "Width").Value);
//            var backgroundTexture = new RenderTarget2D(_graphics, bgWidth, textureCenter.Height);

//            _graphics.SetRenderTarget(backgroundTexture);

//            using (var spriteBatch = new SpriteBatch(_graphics))
//            {
//                _graphics.Clear(Color.Transparent);
//                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

//                var currentX = 0;
//                var endX = 0;

//                if (textureLeft != null)
//                {
//                    currentX += textureLeft.Width;
//                    spriteBatch.Draw(textureLeft, new Vector2(0, 0), Color.White);
//                }

//                if (textureRight != null)
//                {
//                    endX = bgWidth - textureRight.Width;
//                    spriteBatch.Draw(textureRight, new Vector2(endX, 0), Color.White);
//                }

//                while (currentX < endX)
//                {
//                    var drawWidth = textureCenter.Width;

//                    if ((currentX + drawWidth) > endX)
//                        drawWidth = endX - currentX;

//                    spriteBatch.Draw(textureCenter, new Rectangle(currentX, 0, drawWidth, textureCenter.Height), Color.White);

//                    currentX += textureCenter.Width;
//                }

//                spriteBatch.End();
//            }

//            _graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);

//            _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Width, backgroundTexture.Height);

//            Height = _background.Height;
//            Width = _background.Width;

//            _textPosition = new Vector2(int.Parse(GetXMLElement("TextX").Value), int.Parse(GetXMLElement("TextY").Value));
//            _font = parent.CommonWidgetResources.Fonts[GetXMLElement("FontName").Value];
//            FontSize = int.Parse(GetXMLElement("FontSize").Value);
//            _text = GetXMLElement("Text").Value;
//            Colour = PUIColorConversion.Instance.ToColor(GetXMLElement("Color").Value);
//            _cursorPadding = (GetXMLAttribute("CursorPadding") == null ? 0 : int.Parse(GetXMLAttribute("CursorPadding").Value));
//            _cursorIndex = _text.Length;

//            var cursorTexture = new RenderTarget2D(_graphics, 2, textureCenter.Height - (_cursorPadding * 2));
//            _graphics.SetRenderTarget(cursorTexture);
//            _graphics.Clear(Colour);
//            _graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);

//            _cursor = new Sprite(cursorTexture)
//            {
//                Center = Vector2.Zero
//            };

//            _textRect.X = 0;
//            _textRect.Y = 0;
//            _textRect.Width = Width - ((int)_textPosition.X * 2);

//            UpdateTextTexture();

//            _textRect.Height = _textTexture.Height;
//        }

//        public void UpdateTextTexture()
//        {
//            if (_textTexture != null)
//                _textTexture.Dispose();

//            _font.Size = FontSize;
//            var tSize = _font.MeasureString(_text.Length > 0 ? _text : " ");

//            var temp = new RenderTarget2D(_graphics, (int)tSize.X, (int)tSize.Y);

//            _graphics.SetRenderTarget(temp);

//            using (var spriteBatch = new SpriteBatch(_graphics))
//            {
//                _graphics.Clear(Color.Transparent);
//                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

//                spriteBatch.DrawString(_font, _text, Vector2.Zero, Colour);

//                spriteBatch.End();
//                _graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);
//            }

//            _textTexture = (Texture2D)temp;

//            _textRect.Width = _textTexture.Width;

//            if (_textRect.Width > (Width - ((int)_textPosition.X * 2)))
//                _textRect.Width = (Width - ((int)_textPosition.X * 2));

//            if ((_textRect.Width + _textRect.X) > _textTexture.Width)
//                _textRect.Width = _textTexture.Width - _textRect.X;
//        }

//        public override void OnMouseDown(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//        }

//        public override void OnMouseClicked(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            if (!Focused && PointInsideWidget(mousePosition))
//                GrabFocus();
//            else if (Focused)
//                DropFocus();
//        }

//        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTime gameTime)
//        {
//        }

//        public override void OnKeyReleased(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Focused)
//                return;

//            switch (key)
//            {
//                case Keys.Left:
//                    {
//                        if (_cursorIndex > 0)
//                            _cursorIndex -= 1;
//                    }
//                    break;

//                case Keys.Right:
//                    {
//                        if (_cursorIndex < _text.Length)
//                            _cursorIndex += 1;
//                    }
//                    break;

//                case Keys.Delete:
//                    {
//                        if (_text.Length > 0 && _cursorIndex < _text.Length)
//                        {
//                            _text = _text.Remove(_cursorIndex, 1);
//                            UpdateTextTexture();
//                        }
//                    }
//                    break;

//                case Keys.V:
//                    {
//                        if (currentKeyState.IsKeyHeld(Keys.LeftControl) || currentKeyState.IsKeyHeld(Keys.RightControl))
//                        {
//                            var clipboard = TextCopy.Clipboard.GetText();

//                            if (clipboard != null && clipboard.Length > 0)
//                            {
//                                for (int i = 0; i < clipboard.Length; i++)
//                                {
//                                    var c = clipboard[i];
//                                    AddCharacter(c);
//                                }
//                                UpdateTextTexture();
//                            }
//                        }
//                    }
//                    break;
//            }
//        }

//        public override void OnKeyDown(Keys key, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//        }

//        public override void OnTextInput(TextInputEventArgs e, GameTime gameTime, CurrentKeyState currentKeyState)
//        {
//            if (!Focused)
//                return;

//            if (e.Key == Keys.Back)
//            {
//                if (_text.Length > 0 && _cursorIndex > 0)
//                {
//                    _text = _text.Remove(_cursorIndex - 1, 1);
//                    _cursorIndex -= 1;
//                    UpdateTextTexture();
//                }
//            }
//            else
//            {
//                AddCharacter(e.Character);
//                UpdateTextTexture();
//            }
//        }

//        private void AddCharacter(char c)
//        {
//            try
//            {
//                _font.Size = FontSize;
//                _font.MeasureString(c.ToString());
//            }
//            catch
//            {
//                return;
//            }

//            _text = _text.Insert(_cursorIndex, c.ToString());
//            _cursorIndex += 1;
//            _recentlyAdded.Add(c);
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            _background.Draw(spriteBatch, Position + Parent.Position);

//            if (_font != null && _text.Length > 0)
//            {
//                try
//                {
//                    //spriteBatch.DrawString(font, text, textPosition + position + parent.position, color);
//                    spriteBatch.Draw(_textTexture, _textPosition + Position + Parent.Position, _textRect, Colour);
//                }
//                catch (ArgumentException)
//                {
//                    // remove recently added characters if they're not supported by the spritefont
//                    foreach (var c in _recentlyAdded)
//                    {
//                        _text = _text.Replace(c.ToString(), "");
//                        _cursorIndex -= 1;
//                    }

//                    UpdateTextTexture();
//                }

//                _recentlyAdded.Clear();
//            }

//            if (Focused)
//            {
//                var cursorPosition = new Vector2(_textPosition.X, _cursorPadding);

//                if (_text.Length > 0)
//                {
//                    _font.Size = FontSize;
//                    var tSize = _font.MeasureString(_text.Substring(0, _cursorIndex));
//                    cursorPosition.X = _textPosition.X + tSize.X - _textRect.X;

//                    if (cursorPosition.X > (Width - ((int)_textPosition.X * 2)))
//                        _textRect.X += ((int)cursorPosition.X - (Width - ((int)_textPosition.X * 2)));
//                    else if (cursorPosition.X < (int)_textPosition.X)
//                        _textRect.X -= (int)_textPosition.X - (int)cursorPosition.X;
//                }

//                _cursor.Draw(spriteBatch, Position + Parent.Position + cursorPosition);
//            }
//        }

//        public override void Update(GameTime gameTime)
//        {

//        }

//    }
//}
