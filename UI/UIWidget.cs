using System;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine.UI
{
    public class WidgetPositionFlags
    {
        public bool CenterX;
        public bool CenterY;
        public bool AnchorLeft;
        public bool AnchorRight;
        public bool AnchorTop;
        public bool AnchorBottom;
        public int? SetX = null;
        public int? SetY = null;
    }

    public class UIWidget
    {
        protected XElement _template = null;
        protected Rectangle _widgetRect = new Rectangle(0, 0, 0, 0);

        protected Vector2 _offset = Vector2.Zero;
        public Vector2 Offset => _offset;

        public XElement XMLElement { get; set; } = null;
        public UIFrame Parent { get; set; }

        public string Name { get; set; }
        public bool Focused { get; private set; } = false;
        public int DrawOrder { get; set; }
        public bool Visible { get; set; }
        public bool Active { get; set; }
        public UILayoutGroup LayoutGroup { get; set; }
        public UIPanel Panel { get; set; }
        public bool IgnorePanelScissorRect { get; set; } = false;
        public string BindingID { get; set; } = null;

        protected WidgetPositionFlags _positionFlags = new WidgetPositionFlags();
        public WidgetPositionFlags PositionFlags => _positionFlags;

        public Vector2 ParentPosition => Panel != null ? Parent.Position + Panel.Position + (IgnorePanelScissorRect ? Vector2.Zero : Panel.ScrollOffset) : Parent.Position;

        public Rectangle Bounds => new Rectangle(Position + ParentPosition, _widgetRect.Size.ToVector2());

        public Vector2 Position
        {
            get
            {
                return new Vector2(_widgetRect.X + _offset.X, _widgetRect.Y + _offset.Y);
            }
        }

        public int X
        {
            get
            {
                return _widgetRect.X + (int)_offset.X;
            }
            set
            {
                _widgetRect.X = value;
            }
        }

        public int Y
        {
            get
            {
                return _widgetRect.Y + (int)_offset.Y;
            }
            set
            {
                _widgetRect.Y = value;
            }
        }

        public int Width
        {
            get
            {
                return _widgetRect.Width;
            }
            set
            {
                _widgetRect.Width = value;
            }
        }

        public int Height
        {
            get
            {
                return _widgetRect.Height;
            }
            set
            {
                _widgetRect.Height = value;
            }
        }

        public UIWidget() { DrawOrder = 0; Visible = true; Active = true; }
        public UIWidget(int x, int y)
        {
            _widgetRect.X = x; _widgetRect.Y = y;
            DrawOrder = 0;
            Visible = true;
            Active = true;
        }
        public UIWidget(int x, int y, int width, int height)
        {
            _widgetRect.X = x; _widgetRect.Y = y; _widgetRect.Height = height; _widgetRect.Width = width;
            DrawOrder = 0;
            Visible = true;
            Active = true;
        }

        public virtual void Load(UIFrame parent, XElement el) { }

        public virtual void Update(GameTimer gameTimer) { }
        public virtual void OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public virtual void OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer) { }
        public virtual void OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer) { }
        public virtual bool OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { return false; }
        public virtual void OnKeyPressed(Key key, GameTimer gameTimer) { }
        public virtual void OnKeyReleased(Key key, GameTimer gameTimer) { }
        public virtual void OnKeyDown(Key key, GameTimer gameTimer) { }
        public virtual void OnTextInput(char key, GameTimer gameTimer) { }

        public virtual void Draw(SpriteBatch2D spriteBatch) { }

        public void Init(UIFrame parent, XElement el)
        {
            Parent = parent;
            XMLElement = el;
            LoadTemplate();
        } // init

        public void Init(UIFrame parent)
        {
            Parent = parent;
        }

        public void Show()
        {
            Visible = true;
            Active = true;
        }

        public void Hide()
        {
            Visible = false;
            Active = false;
        }

        public void ToggleVisible()
        {
            Visible = !Visible;
            Active = Visible;
        }

        public void Enable() { Active = true; }
        public void Disable() { Active = false; }
        public void ToggleActive() { Active = !Active; }

        protected virtual void GrabFocus()
        {
            Parent.GrabFocus(this);
        }

        protected virtual void DropFocus()
        {
            Parent.DropFocus(this);
        }

        internal virtual void Focus()
        {
            Focused = true;
        }

        internal virtual void UnFocus()
        {
            Focused = false;
        }

        #region Templates
        public void LoadTemplate()
        {
            if (Parent == null || Parent.Templates == null || Parent.Templates.Count == 0)
                return;

            var templateAttribute = XMLElement.Attribute("Template");
            if (templateAttribute == null)
                return;

            var templateName = templateAttribute.Value;
            if (string.IsNullOrWhiteSpace(templateName))
                return;

            if (!Parent.Templates.ContainsKey(templateName))
                return;

            _template = Parent.Templates[templateName];
        } // LoadTemplate

        public XElement GetXMLElement(string name)
        {
            XElement el = null;

            if (_template != null)
                el = _template.Element(name);

            if (el == null)
                el = XMLElement.Element(name);

            return el;
        } // GetXMLElement

        public XElement GetXMLElement(string parent, string name)
        {
            XElement el = null;

            if (_template != null)
            {
                var p = _template.Element(parent);
                if (p != null)
                    el = p.Element(name);
            }

            if (el == null)
            {
                var p = XMLElement.Element(parent);
                if (p != null)
                    el = p.Element(name);
            }

            return el;
        } // GetXMLElement

        public XAttribute GetXMLAttribute(string name)
        {
            XAttribute att = null;

            if (_template != null)
                att = _template.Attribute(name);

            if (att == null)
                att = XMLElement.Attribute(name);

            return att;
        } // GetXMLAttribute

        public XAttribute GetXMLAttribute(string parent, string name)
        {
            XAttribute att = null;

            var p = XMLElement.Element(parent);
            if (p != null)
                att = p.Attribute(name);

            if (_template != null && att == null)
            {
                p = _template.Element(parent);
                if (p != null)
                    att = p.Attribute(name);
            }

            return att;
        } // GetXMLAttribute

        public T GetXMLAttribute<T>(string name)
        {
            return GetXMLAttribute(name).Value.ConvertTo<T>();
        }

        public T GetXMLAttribute<T>(string parent, string name)
        {
            return GetXMLAttribute(parent, name).Value.ConvertTo<T>();
        }

        public T GetXMLElement<T>(string name)
        {
            return GetXMLElement(name).Value.ConvertTo<T>();
        }

        public T GetXMLElement<T>(string parent, string name)
        {
            return GetXMLElement(parent, name).Value.ConvertTo<T>();
        }
        #endregion

        public void LoadStandardXML()
        {
            Name = GetXMLAttribute("Name").Value;

            var valueX = GetXMLAttribute("Position", "X").Value;

            if (valueX.ToUpper() == "CENTER")
                _positionFlags.CenterX = true;
            else if (valueX.ToUpper() == "LEFT")
                _positionFlags.AnchorLeft = true;
            else if (valueX.ToUpper() == "RIGHT")
                _positionFlags.AnchorRight = true;
            else
                _positionFlags.SetX = int.Parse(valueX);

            var valueY = GetXMLAttribute("Position", "Y").Value;

            if (valueY.ToUpper() == "CENTER")
                _positionFlags.CenterY = true;
            else if (valueY.ToUpper() == "TOP")
                _positionFlags.AnchorTop = true;
            else if (valueY.ToUpper() == "BOTTOM")
                _positionFlags.AnchorBottom = true;
            else
                _positionFlags.SetY = int.Parse(valueY);

            UpdatePositionFromFlags();

            var offsetElement = GetXMLElement("Offset");
            if (offsetElement != null)
            {
                _offset.X = int.Parse(GetXMLAttribute("Offset", "X").Value);
                _offset.Y = int.Parse(GetXMLAttribute("Offset", "Y").Value);
            }

            var sizeElement = GetXMLElement("Size");
            if (sizeElement != null)
            {
                Width = int.Parse(GetXMLAttribute("Size", "Width").Value);
                Height = int.Parse(GetXMLAttribute("Size", "Height").Value);
            }

            DrawOrder = GetXMLAttribute("DrawOrder") == null ? 0 : int.Parse(GetXMLAttribute("DrawOrder").Value);
            Visible = (GetXMLAttribute("Visible") != null ? Convert.ToBoolean(GetXMLAttribute("Visible").Value) : true);
            Active = (GetXMLAttribute("Active") != null ? Convert.ToBoolean(GetXMLAttribute("Active").Value) : true);
            BindingID = (GetXMLAttribute("BindingID") != null ? GetXMLAttribute("BindingID").Value : null);

        } // LoadStandardXML

        public void UpdatePositionFromFlags()
        {
            if (LayoutGroup != null)
            {
                LayoutGroup.UpdateWidgetPositions();
                return;
            }

            if (_positionFlags.CenterX)
                CenterX();
            else if (_positionFlags.AnchorLeft)
                AnchorLeft();
            else if (_positionFlags.AnchorRight)
                AnchorRight();
            else if (_positionFlags.SetX.HasValue)
                X = _positionFlags.SetX.Value;

            if (_positionFlags.CenterY)
                CenterY();
            else if (_positionFlags.AnchorTop)
                AnchorTop();
            else if (_positionFlags.AnchorBottom)
                AnchorBottom();
            else if (_positionFlags.SetY.HasValue)
                Y = _positionFlags.SetY.Value;

        } // UpdatePositionFromFlags

        public void AnchorTop() { Y = 0; }
        public void AnchorBottom() { Y = (Panel != null ? Panel.Height : Parent.Height) - Height; }
        public void AnchorLeft() { X = 0; }
        public void AnchorRight() { X = (Panel != null ? Panel.Width : Parent.Width) - Width; }
        public void CenterX() { X = ((Panel != null ? Panel.Width : Parent.Width) / 2) - (Width / 2); }
        public void CenterY() { Y = ((Panel != null ? Panel.Height : Parent.Height) / 2) - (Height / 2); }
        
        internal void TriggerUIEvent(UIEventType type)
        {
            Parent.TriggerUIEvent(type, this);
        }

        public bool PointInsideWidget(Vector2 point)
        {
            if (_widgetRect.Width == 0 && _widgetRect.Height == 0)
                return false;

            if (Panel != null && !IgnorePanelScissorRect)
            {
                var scissorRect = Panel.ScissorRect - Panel.ScrollOffset;
                scissorRect -= Panel.Parent.Position;
                scissorRect.X -= Panel.Parent.PaddingLeft;
                scissorRect.Y -= Panel.Parent.PaddingTop;

                if (!scissorRect.Contains(point))
                    return false;
            }

            return _widgetRect.Contains(point);

        } // PointInsideWidget

    } // UIWidget
}
