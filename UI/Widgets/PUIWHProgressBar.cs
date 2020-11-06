using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class PUIWHProgressBar : PUIWidget
    {
        protected AnimatedSprite _background = null;
        protected AnimatedSprite _fill = null;

        public SpriteFont Font { get; set; } = null;
        public int FontSize { get; set; } = 0;
        public string LabelText { get; set; }
        public string LabelTemplate { get; set; }
        public RgbaByte LabelTextColor { get; set; }

        public Vector2 TextPosition { get; set; }
        protected bool _labelCenterX = false;
        protected bool _labelCenterY = false;
        protected int _labelOffsetX = 0;
        protected int _labelOffsetY = 0;

        protected int _fillOffsetX = 0;
        protected int _fillOffsetY = 0;
        protected int _fillOffsetRight = 0;
        protected Vector2 _bgPosition = Vector2.Zero;
        protected Vector2 _fillPosition = Vector2.Zero;

        protected int _maxValue = 100;
        protected int _minValue = 0;
        protected int _currentValue = 0;

        protected string _onValueChanged = null;

        protected Texture2D _fillLeft;
        protected Texture2D _fillRight;
        protected Texture2D _fillCenter;

        protected int _backgroundWidth;
        protected int _maxFillWidth;
        protected int _fillWidth;

        protected bool _isDirty = false;

        public int Value
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                _isDirty = true;
            }
        }

        public float FValue
        {
            get => (((float)_currentValue - (float)_minValue) / ((float)_maxValue - (float)_minValue));
            set
            {
                _currentValue = (int)(((float)_maxValue - (float)_minValue) * value + (float)_minValue);
                _isDirty = true;
            }
        }

        public PUIWHProgressBar() { }

        ~PUIWHProgressBar()
        {
            if (_background != null)
                _background.Texture?.Dispose();

            if (_fill != null)
                _fill.Texture?.Dispose();
        }

        public override void Load(PUIFrame parent, XElement el)
        {
            Init(parent, el);

            TexturePremultiplyType preMultiplyAlpha = TexturePremultiplyType.None;

            var elAlpha = GetXMLElement("PreMultiplyAlpha");
            if (elAlpha != null)
                preMultiplyAlpha = elAlpha.Value.ToEnum<TexturePremultiplyType>();

            var atStartValue = GetXMLAttribute("StartValue");
            var atMinValue = GetXMLAttribute("MinValue");
            var atMaxValue = GetXMLAttribute("MaxValue");
            var elFillOffsetX = GetXMLElement("Fill", "OffsetX");
            var elFillOffsetY = GetXMLElement("Fill", "OffsetY");
            var elFillOffsetRight = GetXMLElement("Fill", "OffsetRight");

            if (atStartValue != null)
                _currentValue = int.Parse(atStartValue.Value);
            if (atMinValue != null)
                _minValue = int.Parse(atMinValue.Value);
            if (atMaxValue != null)
                _maxValue = int.Parse(atMaxValue.Value);
            if (elFillOffsetX != null)
                _fillOffsetX = int.Parse(elFillOffsetX.Value);
            if (elFillOffsetY != null)
                _fillOffsetY = int.Parse(elFillOffsetY.Value);

            if (elFillOffsetRight != null)
                _fillOffsetRight = int.Parse(elFillOffsetRight.Value);
            else
                _fillOffsetRight = _fillOffsetX;

            _fillPosition = new Vector2(_fillOffsetX, _fillOffsetY);

            var backgroundElLeft = GetXMLElement("Background", "Left");
            var backgroundElRight = GetXMLElement("Background", "Right");
            var backgroundElCenter = GetXMLElement("Background", "Center");

            var bgLeft = backgroundElLeft == null ? null : (string.IsNullOrWhiteSpace(backgroundElLeft.Value) ? null : AssetManager.LoadTexture2D(backgroundElLeft.Value, preMultiplyAlpha));
            var bgRight = backgroundElRight == null ? null : (string.IsNullOrWhiteSpace(backgroundElRight.Value) ? null : AssetManager.LoadTexture2D(backgroundElRight.Value, preMultiplyAlpha));
            var bgCenter = backgroundElCenter == null ? null : (string.IsNullOrWhiteSpace(backgroundElCenter.Value) ? null : AssetManager.LoadTexture2D(backgroundElCenter.Value, preMultiplyAlpha));

            var fillElLeft = GetXMLElement("Fill", "Left");
            var fillElRight = GetXMLElement("Fill", "Right");
            var fillElCenter = GetXMLElement("Fill", "Center");

            _fillLeft = fillElLeft == null ? null : (string.IsNullOrWhiteSpace(fillElLeft.Value) ? null : AssetManager.LoadTexture2D(fillElLeft.Value, preMultiplyAlpha));
            _fillRight = fillElRight == null ? null : (string.IsNullOrWhiteSpace(fillElRight.Value) ? null : AssetManager.LoadTexture2D(fillElRight.Value, preMultiplyAlpha));
            _fillCenter = fillElCenter == null ? null : (string.IsNullOrWhiteSpace(fillElCenter.Value) ? null : AssetManager.LoadTexture2D(fillElCenter.Value, preMultiplyAlpha));

            _backgroundWidth = int.Parse(GetXMLElement("Background", "Width").Value);
            _maxFillWidth = _backgroundWidth - (_fillOffsetX + _fillOffsetRight);

            if (_background == null)
            {
                var backgroundTexture = new Texture2D((uint)_backgroundWidth, (uint)bgCenter.Height);
                backgroundTexture.BeginRenderTarget();
                backgroundTexture.RenderTargetClear(RgbaFloat.Clear);

                var spriteBatch = backgroundTexture.GetRenderTargetSpriteBatch2D();
                spriteBatch.Begin(SamplerType.Point);

                var currentX = 0;
                var endX = _backgroundWidth;

                if (bgLeft != null)
                {
                    currentX += bgLeft.Width;
                    spriteBatch.DrawTexture2D(bgLeft, new Vector2(0, 0));
                }

                if (bgRight != null)
                {
                    endX = _backgroundWidth - bgRight.Width;
                    spriteBatch.DrawTexture2D(bgRight, new Vector2(endX, 0));
                }

                while (currentX < endX)
                {
                    var drawWidth = bgCenter.Width;

                    if ((currentX + drawWidth) > endX)
                        drawWidth = endX - currentX;

                    spriteBatch.DrawTexture2D(bgCenter, new Rectangle(currentX, 0, drawWidth, bgCenter.Height));
                    currentX += bgCenter.Width;
                }

                spriteBatch.End();

                backgroundTexture.EndRenderTarget();

                _background = new AnimatedSprite(backgroundTexture, backgroundTexture.Size);
            }

            Width = _background.Width;
            Height = _background.Height;

            var elLabel = GetXMLElement("Label");

            if (elLabel != null)
            {
                XElement labelPosition = GetXMLElement("Label", "Position");
                XElement labelColor = GetXMLElement("Label", "Color");

                Font = AssetManager.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
                FontSize = int.Parse(GetXMLElement("Label", "FontSize").Value);
                LabelTextColor = new RgbaByte().FromHex(labelColor.Value);
                LabelTemplate = GetXMLElement("Label", "Template").Value;

                var labelX = labelPosition.Attribute("X").Value;
                var labelY = labelPosition.Attribute("Y").Value;

                if (labelX.ToUpper() == "CENTER")
                    _labelCenterX = true;
                else
                    _labelOffsetX = int.Parse(labelX);

                if (labelY.ToUpper() == "CENTER")
                    _labelCenterY = true;
                else
                    _labelOffsetY = int.Parse(labelY);
            }

            UpdateFillTexture();
        }

        protected void UpdateFillTexture()
        {
            _isDirty = false;

            if (_currentValue < _minValue)
                _currentValue = _minValue;
            if (_currentValue > _maxValue)
                _currentValue = _maxValue;

            UpdateText();

            if (_fill != null)
            {
                _fill.Texture?.Dispose();
                _fill.Texture = null;
            }

            if (FValue <= 0)
                return;

            if (_fill != null && _fill.Texture != null)
                _fill.Texture.Dispose();

            _fillWidth = (int)(_maxFillWidth * FValue);

            var fillTexture = new Texture2D((uint)_fillWidth, (uint)_fillCenter.Height);
            fillTexture.BeginRenderTarget();
            fillTexture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = fillTexture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            var currentX = 0;
            var endX = _maxFillWidth;

            if (_fillLeft != null)
            {
                currentX += _fillLeft.Width;
                spriteBatch.DrawTexture2D(_fillLeft, new Vector2(0, 0));
            }

            if (_fillRight != null)
            {
                endX = _maxFillWidth - _fillRight.Width;
                spriteBatch.DrawTexture2D(_fillRight, new Vector2(endX, 0));
            }

            while (currentX < endX)
            {
                var drawWidth = _fillCenter.Width;

                if ((currentX + drawWidth) > endX)
                    drawWidth = endX - currentX;

                spriteBatch.DrawTexture2D(_fillCenter, new Rectangle(currentX, 0, drawWidth, _fillCenter.Height));
                currentX += _fillCenter.Width;
            }

            spriteBatch.End();
            fillTexture.EndRenderTarget();
            
            _fill = new AnimatedSprite(fillTexture, fillTexture.Size);

        } // UpdateFillTexture

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (_isDirty)
                UpdateFillTexture();

            _background?.Draw(spriteBatch, Position + _bgPosition + Parent.Position);
            _fill?.Draw(spriteBatch, Position + _fillPosition + Parent.Position);

            if (Font != null && LabelText != null && LabelText.Length > 0)
                spriteBatch.DrawText(Font, LabelText, TextPosition + Position + Parent.Position, LabelTextColor, FontSize);
        }

        protected void UpdateText()
        {
            if (Font != null)
            {
                LabelText = LabelTemplate.Replace("{value}", _currentValue.ToString()).Replace("{fvalue}", FValue.ToString("0.00"));
                
                var labelSize = Font.MeasureText(LabelText, FontSize);
                int textX = (_labelCenterX == false ? _labelOffsetX : (int)((Width / 2) - (labelSize.X / 2)));
                int textY = (_labelCenterY == false ? _labelOffsetY : (int)((Height / 2) - (labelSize.Y / 2)));

                TextPosition = new Vector2(textX, textY);
            }
        } // UpdateText

        public void UpdateTextFormat(string format)
        {
            LabelTemplate = format;

            UpdateText();
        }

        public override void Update(GameTimer gameTimer)
        {
            _background?.Update(gameTimer);
            _fill?.Update(gameTimer);
        }
    } // PUIWHProgressBar
}
