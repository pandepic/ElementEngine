using System;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine.UI
{
    public class UIWHProgressBar : UIWidget, IDisposable
    {
        protected UISprite _background = null;
        protected UISprite _fill = null;

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
                    _fill?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIWHProgressBar() { }

        ~UIWHProgressBar()
        {
            Dispose(false);
        }

        public override void Load(UIFrame parent, XElement el)
        {
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

            _background = UISprite.CreateUISprite(this, "Background");
            _fill = UISprite.CreateUISprite(this, "Fill");

            _fillPosition = new Vector2(_fillOffsetX, _fillOffsetY);
            _maxFillWidth = _background.Width - (_fillOffsetX + _fillOffsetRight);

            Width = _background.Width;
            Height = _background.Height;

            var elLabel = GetXMLElement("Label");

            if (elLabel != null)
            {
                XElement labelPosition = GetXMLElement("Label", "Position");
                XElement labelColor = GetXMLElement("Label", "Color");

                Font = AssetManager.Instance.LoadSpriteFont(GetXMLElement("Label", "FontName").Value);
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

            if (FValue <= 0)
                return;

            if (_fill != null && _fill.Texture != null)
                _fill.Texture.Dispose();

            _fillWidth = (int)(_maxFillWidth * FValue);
            _fill.SetWidth(_fillWidth);

        } // UpdateFillTexture

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (_isDirty)
            {
                UpdateFillTexture();
                TriggerUIEvent(UIEventType.OnValueChanged);
            }

            _background?.Draw(spriteBatch, Position + _bgPosition + ParentPosition);
            _fill?.Draw(spriteBatch, Position + _fillPosition + ParentPosition);

            if (Font != null && LabelText != null && LabelText.Length > 0)
                spriteBatch.DrawText(Font, LabelText, TextPosition + Position + ParentPosition, LabelTextColor, FontSize);
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
    } // UIWHProgressBar
}
