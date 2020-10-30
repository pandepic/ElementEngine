//using System.Xml.Linq;

//namespace PandaEngine
//{
//    public class PUIWHProgressBar : PUIWidget
//    {
//        protected AnimatedSprite _background = null;
//        protected AnimatedSprite _fill = null;

//        public DynamicSpriteFont Font { get; set; } = null;
//        public int FontSize { get; set; } = 0;
//        public string LabelText { get; set; }
//        public string LabelTemplate { get; set; }
//        public Color LabelTextColor { get; set; }

//        public Vector2 TextPosition { get; set; }
//        protected bool _labelCenterX = false;
//        protected bool _labelCenterY = false;
//        protected int _labelOffsetX = 0;
//        protected int _labelOffsetY = 0;

//        protected int _fillOffsetX = 0;
//        protected int _fillOffsetY = 0;
//        protected int _fillOffsetRight = 0;
//        protected Vector2 _bgPosition = Vector2.Zero;
//        protected Vector2 _fillPosition = Vector2.Zero;

//        protected int _maxValue = 100;
//        protected int _minValue = 0;
//        protected int _currentValue = 0;

//        protected string _onValueChanged = null;

//        protected Texture2D _fillLeft;
//        protected Texture2D _fillRight;
//        protected Texture2D _fillCenter;

//        protected int _backgroundWidth;
//        protected int _maxFillWidth;
//        protected int _fillWidth;

//        protected bool _isDirty = false;

//        public int Value
//        {
//            get => _currentValue;
//            set
//            {
//                _currentValue = value;
//                _isDirty = true;
//            }
//        }

//        public float FValue
//        {
//            get => (((float)_currentValue - (float)_minValue) / ((float)_maxValue - (float)_minValue));
//            set
//            {
//                _currentValue = (int)(((float)_maxValue - (float)_minValue) * value + (float)_minValue);
//                _isDirty = true;
//            }
//        }

//        public PUIWHProgressBar() { }

//        ~PUIWHProgressBar()
//        {
//            if (_background != null)
//                _background.Texture?.Dispose();

//            if (_fill != null)
//                _fill.Texture?.Dispose();
//        }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            Init(parent, el);

//            bool preMultiplyAlpha = false;

//            var elAlpha = GetXMLElement("PreMultiplyAlpha");
//            if (elAlpha != null)
//                preMultiplyAlpha = bool.Parse(elAlpha.Value);

//            var atStartValue = GetXMLAttribute("StartValue");
//            var atMinValue = GetXMLAttribute("MinValue");
//            var atMaxValue = GetXMLAttribute("MaxValue");
//            var elFillOffsetX = GetXMLElement("Fill", "OffsetX");
//            var elFillOffsetY = GetXMLElement("Fill", "OffsetY");
//            var elFillOffsetRight = GetXMLElement("Fill", "OffsetRight");

//            if (atStartValue != null)
//                _currentValue = int.Parse(atStartValue.Value);
//            if (atMinValue != null)
//                _minValue = int.Parse(atMinValue.Value);
//            if (atMaxValue != null)
//                _maxValue = int.Parse(atMaxValue.Value);
//            if (elFillOffsetX != null)
//                _fillOffsetX = int.Parse(elFillOffsetX.Value);
//            if (elFillOffsetY != null)
//                _fillOffsetY = int.Parse(elFillOffsetY.Value);

//            if (elFillOffsetRight != null)
//                _fillOffsetRight = int.Parse(elFillOffsetRight.Value);
//            else
//                _fillOffsetRight = _fillOffsetX;

//            _fillPosition = new Vector2(_fillOffsetX, _fillOffsetY);

//            var backgroundElLeft = GetXMLElement("Background", "Left");
//            var backgroundElRight = GetXMLElement("Background", "Right");
//            var backgroundElCenter = GetXMLElement("Background", "Center");

//            var bgLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElLeft.Value, preMultiplyAlpha));
//            var bgRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElRight.Value, preMultiplyAlpha));
//            var bgCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElCenter.Value, preMultiplyAlpha));

//            var fillElLeft = GetXMLElement("Fill", "Left");
//            var fillElRight = GetXMLElement("Fill", "Right");
//            var fillElCenter = GetXMLElement("Fill", "Center");

//            _fillLeft = fillElLeft == null ? null : (string.IsNullOrWhiteSpace(fillElLeft.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, fillElLeft.Value, preMultiplyAlpha));
//            _fillRight = fillElRight == null ? null : (string.IsNullOrWhiteSpace(fillElRight.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, fillElRight.Value, preMultiplyAlpha));
//            _fillCenter = fillElCenter == null ? null : (string.IsNullOrWhiteSpace(fillElCenter.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, fillElCenter.Value, preMultiplyAlpha));

//            _backgroundWidth = int.Parse(GetXMLElement("Background", "Width").Value);
//            _maxFillWidth = _backgroundWidth - (_fillOffsetX + _fillOffsetRight);

//            var backgroundTexture = new RenderTarget2D(parent.CommonWidgetResources.Graphics, _backgroundWidth, bgCenter.Height);

//            parent.CommonWidgetResources.Graphics.SetRenderTarget(backgroundTexture);

//            using (SpriteBatch spriteBatch = new SpriteBatch(parent.CommonWidgetResources.Graphics))
//            {
//                parent.CommonWidgetResources.Graphics.Clear(Color.Transparent);
//                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

//                var currentX = 0;
//                var endX = _backgroundWidth;

//                if (bgLeft != null)
//                {
//                    currentX += bgLeft.Width;
//                    spriteBatch.Draw(bgLeft, new Vector2(0, 0), Color.White);
//                }

//                if (bgRight != null)
//                {
//                    endX = _backgroundWidth - bgRight.Width;
//                    spriteBatch.Draw(bgRight, new Vector2(endX, 0), Color.White);
//                }

//                while (currentX < endX)
//                {
//                    var drawWidth = bgCenter.Width;

//                    if ((currentX + drawWidth) > endX)
//                        drawWidth = endX - currentX;

//                    spriteBatch.Draw(bgCenter, new Rectangle(currentX, 0, drawWidth, bgCenter.Height), Color.White);

//                    currentX += bgCenter.Width;
//                }

//                spriteBatch.End();
//            }

//            parent.CommonWidgetResources.Graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);

//            _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Width, backgroundTexture.Height);

//            Width = backgroundTexture.Width;
//            Height = backgroundTexture.Height;

//            var elLabel = GetXMLElement("Label");

//            if (elLabel != null)
//            {
//                XElement labelPosition = GetXMLElement("Label", "Position");
//                XElement labelColor = GetXMLElement("Label", "Color");

//                Font = parent.CommonWidgetResources.Fonts[GetXMLElement("Label", "FontName").Value];
//                FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);
//                LabelTextColor = PUIColorConversion.Instance.ToColor(labelColor.Value);
//                LabelTemplate = GetXMLElement("Label", "Template").Value;

//                var labelX = labelPosition.Element("X").Value;
//                var labelY = labelPosition.Element("Y").Value;

//                if (labelX.ToUpper() == "CENTER")
//                    _labelCenterX = true;
//                else
//                    _labelOffsetX = int.Parse(labelX);

//                if (labelY.ToUpper() == "CENTER")
//                    _labelCenterY = true;
//                else
//                    _labelOffsetY = int.Parse(labelY);
//            }

//            UpdateFillTexture();
//        }

//        protected void UpdateFillTexture()
//        {
//            _isDirty = false;

//            if (_currentValue < _minValue)
//                _currentValue = _minValue;
//            if (_currentValue > _maxValue)
//                _currentValue = _maxValue;

//            UpdateText();

//            if (_fill != null)
//            {
//                _fill.Texture?.Dispose();
//                _fill.Texture = null;
//            }

//            if (FValue <= 0)
//                return;

//            _fillWidth = (int)(_maxFillWidth * FValue);

//            var fillTexture = new RenderTarget2D(Parent.CommonWidgetResources.Graphics, _fillWidth, _fillCenter.Height);

//            Parent.CommonWidgetResources.Graphics.SetRenderTarget(fillTexture);

//            using (SpriteBatch spriteBatch = new SpriteBatch(Parent.CommonWidgetResources.Graphics))
//            {
//                Parent.CommonWidgetResources.Graphics.Clear(Color.Transparent);
//                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

//                var currentX = 0;
//                var endX = _maxFillWidth;

//                if (_fillLeft != null)
//                {
//                    currentX += _fillLeft.Width;
//                    spriteBatch.Draw(_fillLeft, new Vector2(0, 0), Color.White);
//                }

//                if (_fillRight != null)
//                {
//                    endX = _maxFillWidth - _fillRight.Width;
//                    spriteBatch.Draw(_fillRight, new Vector2(endX, 0), Color.White);
//                }

//                while (currentX < endX)
//                {
//                    var drawWidth = _fillCenter.Width;

//                    if ((currentX + drawWidth) > endX)
//                        drawWidth = endX - currentX;

//                    spriteBatch.Draw(_fillCenter, new Rectangle(currentX, 0, drawWidth, _fillCenter.Height), Color.White);

//                    currentX += _fillCenter.Width;
//                }

//                spriteBatch.End();
//            }

//            Parent.CommonWidgetResources.Graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);

//            _fill = new AnimatedSprite(fillTexture, fillTexture.Width, fillTexture.Height);

//        } // UpdateFillTexture

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            if (_isDirty)
//                UpdateFillTexture();

//            _background?.Draw(spriteBatch, Position + _bgPosition + Parent.Position);
//            _fill?.Draw(spriteBatch, Position + _fillPosition + Parent.Position);

//            if (Font != null && LabelText != null && LabelText.Length > 0)
//            {
//                Font.Size = FontSize;
//                spriteBatch.DrawString(Font, LabelText, TextPosition + Position + Parent.Position, LabelTextColor);
//            }
//        }

//        protected void UpdateText()
//        {
//            if (Font != null)
//            {
//                LabelText = LabelTemplate.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00"));

//                Font.Size = FontSize;
//                var labelSize = Font.MeasureString(LabelText);

//                int textX = (_labelCenterX == false ? _labelOffsetX : (int)((Width / 2) - (labelSize.X / 2)));
//                int textY = (_labelCenterY == false ? _labelOffsetY : (int)((Height / 2) - (labelSize.Y / 2)));

//                TextPosition = new Vector2(textX, textY);
//            }
//        } // UpdateText

//        public void UpdateTextFormat(string format)
//        {
//            LabelTemplate = format;

//            UpdateText();
//        }

//        public override void Update(GameTime gameTime)
//        {
//            _background?.Update(gameTime);
//            _fill?.Update(gameTime);
//        }
//    } // PUIWHProgressBar
//}
