//using System;
//using System.Xml.Linq;

//namespace PandaEngine
//{
//    public class PUIWHScrollBar : PUIWidget
//    {
//        protected AnimatedSprite _background = null;
//        protected AnimatedSprite _slider = null;
//        protected AnimatedSprite _sliderHover = null;

//        protected int _maxValue = 100;
//        protected int _minValue = 1;
//        protected int _increment = 1;

//        protected int _previousValue = -1;
//        protected int _currentValue = 1;
//        protected int _sliderIndex = 1;
//        protected int _totalNotches = 1;

//        public int Value
//        {
//            get => _currentValue;
//            set
//            {
//                UpdateCurrentValue(value);
//                UpdateSliderPosition();
//            }
//        }

//        public float FValue
//        {
//            get => (((float)_currentValue - (float)_minValue) / ((float)_maxValue - (float)_minValue));
//            set
//            {
//                var newValue = (int)(((float)_maxValue - (float)_minValue) * value + (float)_minValue);
//                UpdateCurrentValue(newValue);
//                UpdateSliderPosition();
//            }
//        }

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

//        protected int _sliderOffsetX = 0;
//        protected int _sliderOffsetY = 0;

//        protected float _sliderIncrementX = 0f;

//        protected string _onValueChanged = null;

//        protected Vector2 _bgPosition = Vector2.Zero;
//        protected Vector2 _sliderPosition = Vector2.Zero;

//        public PUIWHScrollBar() { }

//        ~PUIWHScrollBar()
//        {
//            if (_background != null)
//                _background.Texture?.Dispose();

//            if (_slider != null)
//                _slider.Texture?.Dispose();

//            if (_sliderHover != null)
//                _sliderHover.Texture?.Dispose();
//        }

//        public override void Load(PUIFrame parent, XElement el)
//        {
//            Init(parent, el);

//            bool preMultiplyAlpha = false;

//            var elAlpha = GetXMLElement("PreMultiplyAlpha");
//            if (elAlpha != null)
//                preMultiplyAlpha = bool.Parse(elAlpha.Value);

//            var sliderTexture = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, GetXMLElement("AssetNameSlider").Value, preMultiplyAlpha);
//            var sliderTextureHover = ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, GetXMLElement("AssetNameSliderHover").Value, preMultiplyAlpha);
//            _slider = new AnimatedSprite(sliderTexture, sliderTexture.Width, sliderTexture.Height);
//            _sliderHover = new AnimatedSprite(sliderTextureHover, sliderTextureHover.Width, sliderTextureHover.Height);

//            var backgroundElLeft = GetXMLElement("Background", "Left");
//            var backgroundElRight = GetXMLElement("Background", "Right");
//            var backgroundElCenter = GetXMLElement("Background", "Center");

//            var textureLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElLeft.Value, preMultiplyAlpha));
//            var textureRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElRight.Value, preMultiplyAlpha));
//            var textureCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : ModManager.Instance.AssetManager.LoadTexture2D(parent.CommonWidgetResources.Graphics, backgroundElCenter.Value, preMultiplyAlpha));

//            var bgWidth = int.Parse(GetXMLElement("Background", "Width").Value);
//            var backgroundTexture = new RenderTarget2D(parent.CommonWidgetResources.Graphics, bgWidth, textureCenter.Height);

//            parent.CommonWidgetResources.Graphics.SetRenderTarget(backgroundTexture);

//            using (SpriteBatch spriteBatch = new SpriteBatch(parent.CommonWidgetResources.Graphics))
//            {
//                parent.CommonWidgetResources.Graphics.Clear(Color.Transparent);
//                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

//                var currentX = 0;
//                var endX = bgWidth;

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

//            parent.CommonWidgetResources.Graphics.SetRenderTarget(GraphicsGlobals.DefaultRenderTarget);

//            var atStartValue = GetXMLAttribute("StartValue");
//            var atMinValue = GetXMLAttribute("MinValue");
//            var atMaxValue = GetXMLAttribute("MaxValue");
//            var atIncrement = GetXMLAttribute("Increment");
//            var elSliderOffsetX = GetXMLElement("SliderOffsetX");
//            var elOnValueChanged = GetXMLElement("OnValueChanged");

//            _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Width, backgroundTexture.Height);

//            if (atStartValue != null)
//                _currentValue = int.Parse(atStartValue.Value);
//            if (atMinValue != null)
//                _minValue = int.Parse(atMinValue.Value);
//            if (atMaxValue != null)
//                _maxValue = int.Parse(atMaxValue.Value);
//            if (atIncrement != null)
//                _increment = int.Parse(atIncrement.Value);
//            if (elSliderOffsetX != null)
//                _sliderOffsetX = int.Parse(elSliderOffsetX.Value);
//            if (elOnValueChanged != null)
//                _onValueChanged = elOnValueChanged.Value;

//            var sliderWidthOffset = _slider.Width - (_sliderOffsetX * 2);
//            Width = _background.Width + (sliderWidthOffset <= 0 ? 0 : sliderWidthOffset);
//            Height = _background.Height;
//            if (_slider.Height > Height)
//                Height = _slider.Height;

//            // center the background texture
//            _bgPosition.X = (Width - _background.Width) / 2;
//            _bgPosition.Y = (Height - _background.Height) / 2;

//            _totalNotches = ((_maxValue - _minValue) / _increment) + 1;

//            _sliderIncrementX = (Width - _slider.Width - (_sliderOffsetX * 2)) / ((_maxValue - _minValue) / _increment);

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

//            UpdateCurrentValue(_currentValue);
//            _previousValue = _currentValue;
//            UpdateSliderPosition();
//        }

//        protected void SetSliderPosition(Vector2 mousePosition)
//        {
//            var relativePosition = mousePosition - Position;
//            UpdateCurrentValue((int)((relativePosition.X - _sliderOffsetX) / _sliderIncrementX) * _increment);
//            UpdateSliderPosition();

//            if (_previousValue != _currentValue)
//            {
//                HandleEvents?.Invoke((_onValueChanged == null ? null
//                    : _onValueChanged.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00")))
//                );

//                _previousValue = _currentValue;
//            }
//        }

//        protected void UpdateSliderPosition()
//        {
//            _sliderPosition.X = _sliderOffsetX + (_sliderIncrementX * (_sliderIndex - 1));
//        }

//        protected void UpdateCurrentValue(int value)
//        {
//            _currentValue = value;

//            if (_minValue > _currentValue)
//                _currentValue = _minValue;
//            if (_currentValue > _maxValue)
//                _currentValue = _maxValue;

//            _sliderIndex = (_currentValue / _increment) + 1;

//            if (Font != null)
//            {
//                LabelText = LabelTemplate.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00"));

//                Font.Size = FontSize;
//                var labelSize = Font.MeasureString(LabelText);

//                int textX = (_labelCenterX == false ? _labelOffsetX : (int)((Width / 2) - (labelSize.X / 2)));
//                int textY = (_labelCenterY == false ? _labelOffsetY : (int)((Height / 2) - (labelSize.Y / 2)));

//                TextPosition = new Vector2(textX, textY);
//            }
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            _background.Draw(spriteBatch, Position + _bgPosition + Parent.Position);

//            if (Focused)
//                _sliderHover.Draw(spriteBatch, Position + _sliderPosition + Parent.Position);
//            else
//                _slider.Draw(spriteBatch, Position + _sliderPosition + Parent.Position);

//            if (Font != null && LabelText.Length > 0)
//            {
//                Font.Size = FontSize;
//                spriteBatch.DrawString(Font, LabelText, TextPosition + Position + Parent.Position, LabelTextColor);
//            }
//        }

//        public override void Update(GameTime gameTime)
//        {

//        }

//        public override void OnMouseDown(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            if (!Focused && PointInsideWidget(mousePosition))
//            {
//                GrabFocus();
//                SetSliderPosition(mousePosition);
//            }
//        }

//        public override void OnMouseClicked(MouseButtonID button, Vector2 mousePosition, GameTime gameTime)
//        {
//            if (!Focused)
//                return;

//            DropFocus();
//        }

//        public override void OnMouseMoved(Vector2 originalPosition, Vector2 currentPosition, GameTime gameTime)
//        {
//            if (!Focused)
//                return;

//            SetSliderPosition(currentPosition);
//        }
//    } // PUIWHScrollBar
//}
