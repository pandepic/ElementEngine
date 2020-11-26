using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public enum UILayoutGroupDirection
    {
        Vertical,
        Horizontal,
    }

    public class UILayoutGroup
    {
        public string Name { get; set; }
        public UILayoutGroupDirection Direction { get; set; }
        public int Spacing { get; set; }
        public UIFrame ParentFrame { get; set; }
        public List<UIWidget> Widgets { get; set; } = new List<UIWidget>();
        public WidgetPositionFlags PositionFlags { get; set; } = new WidgetPositionFlags();

        public UILayoutGroup(XElement el, UIFrame parentFrame)
        {
            ParentFrame = parentFrame;
            Name = el.Attribute("Name").Value;
            Direction = el.Attribute("Direction").Value.ToEnum<UILayoutGroupDirection>();
            Spacing = 0;

            var attSpacing = el.Attribute("Spacing");
            if (attSpacing != null)
                Spacing = int.Parse(attSpacing.Value);

            var valueX = el.Element("StartPosition").Attribute("X").Value;

            if (valueX.ToUpper() == "CENTER")
                PositionFlags.CenterX = true;
            else if (valueX.ToUpper() == "LEFT")
                PositionFlags.AnchorLeft = true;
            else if (valueX.ToUpper() == "RIGHT")
                PositionFlags.AnchorRight = true;
            else
                PositionFlags.SetX = int.Parse(valueX);

            var valueY = el.Element("StartPosition").Attribute("Y").Value;

            if (valueY.ToUpper() == "CENTER")
                PositionFlags.CenterY = true;
            else if (valueY.ToUpper() == "TOP")
                PositionFlags.AnchorTop = true;
            else if (valueY.ToUpper() == "BOTTOM")
                PositionFlags.AnchorBottom = true;
            else
                PositionFlags.SetY = int.Parse(valueY);
        }

        public void UpdateWidgetPositions()
        {
            var width = 0;
            var height = 0;

            foreach (var widget in Widgets)
            {
                if (widget.Width > width)
                    width = widget.Width;
                if (widget.Height > height)
                    height = widget.Height;
            }

            var startPosition = Vector2I.Zero;

            if (PositionFlags.CenterX)
                startPosition.X = ParentFrame.Width / 2;
            else if (PositionFlags.AnchorLeft)
                startPosition.X = 0;
            else if (PositionFlags.AnchorRight)
                startPosition.X = ParentFrame.Width;
            else if (PositionFlags.SetX.HasValue)
                startPosition.X = PositionFlags.SetX.Value;

            if (PositionFlags.CenterY)
                startPosition.Y = ParentFrame.Height / 2;
            else if (PositionFlags.AnchorTop)
                startPosition.Y = 0;
            else if (PositionFlags.AnchorBottom)
                startPosition.Y = ParentFrame.Height;
            else if (PositionFlags.SetY.HasValue)
                startPosition.Y = PositionFlags.SetY.Value;

            var currentPosition = startPosition;

            foreach (var widget in Widgets)
            {
                var offset = widget.Offset.ToVector2I();

                if (Direction == UILayoutGroupDirection.Vertical)
                {
                    if (widget.PositionFlags.CenterX)
                        currentPosition.X = startPosition.X - (width / 2);
                    else if (widget.PositionFlags.AnchorLeft)
                        currentPosition.X = startPosition.X;
                    else if (widget.PositionFlags.AnchorRight)
                        currentPosition.X = startPosition.X - widget.Width;
                    else if (widget.PositionFlags.SetX.HasValue)
                        currentPosition.X = startPosition.X + widget.PositionFlags.SetX.Value;

                    widget.X = currentPosition.X;
                    widget.Y = currentPosition.Y;

                    currentPosition.Y += widget.Height + Spacing + offset.Y;
                }
                else if (Direction == UILayoutGroupDirection.Horizontal)
                {
                    if (widget.PositionFlags.CenterY)
                        currentPosition.Y = startPosition.Y - (height / 2);
                    else if (widget.PositionFlags.AnchorTop)
                        currentPosition.Y = startPosition.Y;
                    else if (widget.PositionFlags.AnchorBottom)
                        currentPosition.Y = startPosition.Y - widget.Height;
                    else if (widget.PositionFlags.SetY.HasValue)
                        currentPosition.Y = startPosition.Y + widget.PositionFlags.SetY.Value;

                    widget.X = currentPosition.X;
                    widget.Y = currentPosition.Y;

                    currentPosition.X += widget.Width + Spacing + offset.X;
                }
            }

        } // UpdateWidgetPositions
    } // UILayoutGroup

    public class CommonWidgetResources
    {
        public Dictionary<string, XElement> Templates { get; set; } = null;
        public Dictionary<string, UILayoutGroup> LayoutGroups { get; set; } = new Dictionary<string, UILayoutGroup>();
    }

    public class UIFrame : IDisposable
    {
        protected UIMenu _parent = null;
        protected Rectangle _frameRect = Rectangle.Empty;

        public string Name { get; set; } = "";
        public UISprite FrameSprite { get; set; } = null;
        public UIWidgetList Widgets { get; set; } = new UIWidgetList();
        public Dictionary<string, XElement> Templates { get; set; } = null;

        public int DrawOrder { get; set; } = 0;

        public bool Visible { get; set; } = true;
        public bool Active { get; set; } = true;
        public bool Draggable { get; set; } = false;
        public bool Focused { get; private set; } = false;

        public bool AutoWidth { get; set; } = false;
        public bool AutoHeight { get; set; } = false;
        public bool CenterX { get; set; } = false;
        public bool CenterY { get; set; } = false;
        public bool AnchorTop { get; set; } = false;
        public bool AnchorBottom { get; set; } = false;
        public bool AnchorLeft { get; set; } = false;
        public bool AnchorRight { get; set; } = false;

        protected bool _dragging = false;
        protected Vector2 _dragMousePosition = Vector2.Zero;
        protected Rectangle _draggableRect = Rectangle.Empty;

        internal CommonWidgetResources CommonWidgetResources { get; set; } = new CommonWidgetResources();

        public Vector2 Position
        {
            get
            {
                return new Vector2(_frameRect.X, _frameRect.Y);
            }

            set
            {
                _frameRect.X = (int)value.X;
                _frameRect.Y = (int)value.Y;
            }
        }

        public int X
        {
            get
            {
                return _frameRect.X;
            }
            set
            {
                _frameRect.X = value;
            }
        }

        public int Y
        {
            get
            {
                return _frameRect.Y;
            }
            set
            {
                _frameRect.Y = value;
            }
        }

        public int Width
        {
            get
            {
                return _frameRect.Width;
            }
            set
            {
                _frameRect.Width = value;
            }
        }

        public int Height
        {
            get
            {
                return _frameRect.Height;
            }
            set
            {
                _frameRect.Height = value;
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
                    Widgets?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public UIFrame(UIMenu parent, XElement el, Dictionary<string, XElement> templates)
        {
            var screenWidth = ElementGlobals.TargetResolutionWidth;
            var screenHeight = ElementGlobals.TargetResolutionHeight;

            _parent = parent;
            Templates = templates;

            var drawOrderAttribute = el.Attribute("DrawOrder");
            if (drawOrderAttribute != null)
                DrawOrder = int.Parse(drawOrderAttribute.Value);

            var elBackground = el.Element("Background");

            if (elBackground != null)
            {
                var bgWidget = new UIWidget();
                bgWidget.Init(this, el);
                FrameSprite = UISprite.CreateUISprite(bgWidget, "Background");
            }

            var framePosition = el.Element("Position");
            var frameSize = el.Element("Size");

            var frameSizeW = frameSize.Attribute("Width").Value;
            var frameSizeH = frameSize.Attribute("Height").Value;

            if (frameSizeW.ToUpper() == "FILL")
                Width = screenWidth;
            else if (frameSizeW.ToUpper() == "AUTO")
                AutoWidth = true;
            else if (frameSizeW.ToUpper() == "BG")
                Width = FrameSprite.Width;
            else
                Width = int.Parse(frameSizeW);

            if (frameSizeH.ToUpper() == "FILL")
                Height = screenHeight;
            else if (frameSizeH.ToUpper() == "AUTO")
                AutoHeight = true;
            else if (frameSizeH.ToUpper() == "BG")
                Height = FrameSprite.Height;
            else
                Height = int.Parse(frameSizeH);

            var frameX = framePosition.Attribute("X").Value.ToUpper();
            var frameY = framePosition.Attribute("Y").Value.ToUpper();

            X = (frameX != "CENTER"
                ? int.Parse(frameX)
                : (int)((screenWidth / 2) - (Width / 2))
                );

            Y = (frameY != "CENTER"
                ? int.Parse(frameY)
                : (int)((screenHeight / 2) - (Height / 2))
                );

            if (frameX == "CENTER")
                CenterX = true;
            else if (frameX == "LEFT")
                AnchorLeft = true;
            else if (frameX == "RIGHT")
                AnchorRight = true;

            if (frameY == "CENTER")
                CenterY = true;
            if (frameY == "TOP")
                AnchorTop = true;
            if (frameY == "BOTTOM")
                AnchorBottom = true;

            var visibleAttribute = el.Attribute("Visible");
            var activeAttribute = el.Attribute("Active");
            var draggableAttribute = el.Attribute("Draggable");

            if (visibleAttribute != null)
                Visible = bool.Parse(visibleAttribute.Value);
            if (activeAttribute != null)
                Active = bool.Parse(activeAttribute.Value);
            if (draggableAttribute != null)
                Draggable = bool.Parse(draggableAttribute.Value);

            Name = el.Attribute("Name").Value;

            XElement widgetsRoot = el.Element("Widgets");

            var currentWidth = 0;
            var currentHeight = 0;

            CommonWidgetResources.Templates = templates;

            var tempWidgets = new List<UIWidget>();

            foreach (var kvp in ElementGlobals.UIWidgetTypes)
            {
                var type = kvp.Key;
                var xmlElements = widgetsRoot.Elements(kvp.Value).ToList();

                foreach (var xmlElement in xmlElements)
                {
                    UIWidget widget = (UIWidget)Activator.CreateInstance(type);
                    widget.Load(this, xmlElement);
                    tempWidgets.Add(widget);
                }
            }

            foreach (var widget in tempWidgets)
            {
                if (AutoWidth)
                    if ((widget.X + widget.Width) > currentWidth)
                        currentWidth = widget.X + widget.Width;

                if (AutoHeight)
                    if ((widget.Y + widget.Height) > currentHeight)
                        currentHeight = widget.Y + widget.Height;
            }

            if (AutoWidth)
                Width = currentWidth;
            if (AutoHeight)
                Height = currentHeight;

            if (AutoWidth && CenterX)
                X = (int)((screenWidth / 2) - (Width / 2));

            if (AutoHeight && CenterY)
                Y = (int)((screenHeight / 2) - (Height / 2));

            if (AnchorLeft)
                X = 0;
            if (AnchorRight)
                X = screenWidth - Width;
            if (AnchorTop)
                Y = 0;
            if (AnchorBottom)
                Y = screenHeight - Height;

            var draggableRectAttribute = el.Attribute("DraggableRect");
            if (draggableRectAttribute != null)
            {
                var rectParts = draggableRectAttribute.Value.Split(',');
                _draggableRect.X = int.Parse(rectParts[0]);
                _draggableRect.Y = int.Parse(rectParts[1]);

                var widthPart = rectParts[2];
                var heightPart = rectParts[3];

                if (widthPart.ToUpper() == "FILL")
                    _draggableRect.Width = Width;
                else
                    _draggableRect.Width = int.Parse(widthPart);

                if (heightPart.ToUpper() == "FILL")
                    _draggableRect.Height = Height;
                else
                    _draggableRect.Height = int.Parse(heightPart);
            }

            foreach (var elLayoutGroup in el.Elements("LayoutGroup"))
            {
                var layoutGroup = new UILayoutGroup(elLayoutGroup, this);
                CommonWidgetResources.LayoutGroups.Add(layoutGroup.Name, layoutGroup);
            }

            foreach (var widget in tempWidgets)
            {
                widget.LoadStandardXML();

                var attLayoutGroup = widget.GetXMLAttribute("LayoutGroup");
                if (attLayoutGroup != null && CommonWidgetResources.LayoutGroups.TryGetValue(attLayoutGroup.Value, out var layoutGroup))
                {
                    widget.LayoutGroup = layoutGroup;
                    layoutGroup.Widgets.Add(widget);
                }

                AddWidget(widget, false);
            }

            foreach (var kvp in CommonWidgetResources.LayoutGroups)
                kvp.Value.UpdateWidgetPositions();

            Widgets.OrderByDrawOrder();

        } // UIFrame

        ~UIFrame()
        {
            Dispose(false);
        }

        internal void TriggerUIEvent(UIEventType type, UIWidget widget)
        {
            _parent.TriggerUIEvent(type, widget);
        }

        internal void GrabFocus(UIWidget widget)
        {
            _parent.GrabFocus(this);
            Widgets.UnFocusAllExcept(widget.Name);
            widget.Focus();
            Focused = true;
        }

        internal void DropFocus(UIWidget widget)
        {
            _parent.DropFocus(this);
            widget.UnFocus();
            Focused = false;
        }

        public void UnFocus()
        {
            Widgets.UnFocusAll();
            Focused = false;
            _parent.DropFocus(this);
        }

        public void Open()
        {
            Visible = true;
            Active = true;
        }

        public void Close()
        {
            Visible = false;
            Active = false;
            UnFocus();
        }

        public void Toggle()
        {
            if (Visible)
                Close();
            else
                Open();
        }

        public void AddWidget(UIWidget widget, bool order = true)
        {
            Widgets.Add(widget);

            if (order)
                Widgets.OrderByDrawOrder();
        }

        public dynamic GetWidget(string name)
        {
            return Widgets[name];
        }

        public T GetWidget<T>(string name) where T : UIWidget
        {
            return (T)GetWidget(name);
        }

        public virtual void Update(GameTimer gameTimer)
        {
            FrameSprite?.Update(gameTimer);
            Widgets.Update(gameTimer);
        }

        public virtual void Draw(SpriteBatch2D spriteBatch)
        {
            FrameSprite?.Draw(spriteBatch, Position);
            Widgets.Draw(spriteBatch);
        }

        public virtual bool OnMouseMoved(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var pointInFrame = PointInsideFrame(mousePosition);

            if (_dragging)
            {
                Position += mousePosition - _dragMousePosition;
                _dragMousePosition = mousePosition;
            }

            Widgets.OnMouseMoved(mousePosition, prevMousePosition, gameTimer, Position);

            return pointInFrame;
        }

        public virtual bool OnMouseDown(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var pointInFrame = PointInsideFrame(mousePosition);

            if (Focused && !pointInFrame)
                UnFocus();

            if (Draggable && (_draggableRect.IsEmpty ? pointInFrame : PointInsideDraggableRect(mousePosition)) && !_dragging)
            {
                _dragging = true;
                _dragMousePosition = mousePosition;
            }

            Widgets.OnMouseDown(button, mousePosition, gameTimer, Position);

            return pointInFrame;
        }

        public virtual bool OnMouseClicked(MouseButton button, Vector2 mousePosition, GameTimer gameTimer)
        {
            var pointInFrame = PointInsideFrame(mousePosition);

            if (Focused && !pointInFrame)
                UnFocus();

            if (_dragging)
                _dragging = false;

            Widgets.OnMouseClicked(button, mousePosition, gameTimer, Position);

            return pointInFrame;
        }

        public virtual bool OnMouseScroll(MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            return Widgets.OnMouseScroll(type, mouseWheelDelta, gameTimer);
        }

        public virtual void OnKeyPressed(Key key, GameTimer gameTimer)
        {
            Widgets.OnKeyPressed(key, gameTimer);
        }

        public virtual void OnKeyReleased(Key key, GameTimer gameTimer)
        {
            Widgets.OnKeyReleased(key, gameTimer);
        }

        public virtual void OnKeyDown(Key key, GameTimer gameTimer)
        {
            Widgets.OnKeyDown(key, gameTimer);
        }

        public void OnTextInput(char key, GameTimer gameTimer)
        {
            Widgets.OnTextInput(key, gameTimer);
        }

        public bool PointInsideDraggableRect(Vector2 point)
        {
            if (_draggableRect.IsEmpty)
                return false;

            if (_draggableRect.Width == 0 && _draggableRect.Height == 0)
                return false;

            if (point.X < (_frameRect.X + _draggableRect.X))
                return false;
            if (point.Y < (_frameRect.Y + _draggableRect.Y))
                return false;
            if (point.X > ((_frameRect.X + _draggableRect.X) + _draggableRect.Width))
                return false;
            if (point.Y > ((_frameRect.Y + _draggableRect.Y) + _draggableRect.Height))
                return false;

            return true;
        }

        public bool PointInsideFrame(Vector2 point)
        {
            if (_frameRect.Width == 0 && _frameRect.Height == 0)
                return false;

            if (point.X < _frameRect.X)
                return false;
            if (point.Y < _frameRect.Y)
                return false;
            if (point.X > (_frameRect.X + _frameRect.Width))
                return false;
            if (point.Y > (_frameRect.Y + _frameRect.Height))
                return false;

            return true;
        }

    }
}
