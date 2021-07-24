using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public class CommonWidgetResources
    {
        public Dictionary<string, XElement> Templates { get; set; } = null;
        public Dictionary<string, UILayoutGroup> LayoutGroups { get; set; } = new Dictionary<string, UILayoutGroup>();
    }

    public class UIFrame : IDisposable
    {
        internal UIMenu Parent = null;
        internal Rectangle FrameRect = Rectangle.Empty;

        public string Name { get; set; } = "";
        public XElement XMLElement { get; set; } = null;
        public UISprite FrameSprite { get; set; } = null;
        public UIWidgetList Widgets { get; } = new UIWidgetList();
        public List<UIPanel> Panels { get; } = new List<UIPanel>();
        public Dictionary<string, XElement> Templates { get; set; } = null;

        public int PaddingTop { get; set; } = 0;
        public int PaddingBottom { get; set; } = 0;
        public int PaddingLeft { get; set; } = 0;
        public int PaddingRight { get; set; } = 0;

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
                return new Vector2(FrameRect.X, FrameRect.Y);
            }

            set
            {
                FrameRect.X = (int)value.X;
                FrameRect.Y = (int)value.Y;
            }
        }

        public int X
        {
            get
            {
                return FrameRect.X;
            }
            set
            {
                FrameRect.X = value;
            }
        }

        public int Y
        {
            get
            {
                return FrameRect.Y;
            }
            set
            {
                FrameRect.Y = value;
            }
        }

        public int Width
        {
            get
            {
                return FrameRect.Width;
            }
            set
            {
                FrameRect.Width = value;
            }
        }

        public int Height
        {
            get
            {
                return FrameRect.Height;
            }
            set
            {
                FrameRect.Height = value;
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
            Parent = parent;
            Templates = templates;
            CommonWidgetResources.Templates = templates;
            XMLElement = el;

            var screenWidth = ElementGlobals.TargetResolutionWidth;
            var screenHeight = ElementGlobals.TargetResolutionHeight;

            var drawOrderAttribute = XMLElement.Attribute("DrawOrder");
            if (drawOrderAttribute != null)
                DrawOrder = int.Parse(drawOrderAttribute.Value);

            var framePosition = XMLElement.Element("Position");
            var frameSize = XMLElement.Element("Size");

            var frameSizeW = frameSize.Attribute("Width").Value;
            var frameSizeH = frameSize.Attribute("Height").Value;

            if (frameSizeW.ToUpper() == "FILL")
                Width = screenWidth;
            else if (frameSizeW.ToUpper() == "AUTO")
                AutoWidth = true;
            else if (frameSizeW.ToUpper() == "BG") { }
            else
                Width = int.Parse(frameSizeW);

            if (frameSizeH.ToUpper() == "FILL")
                Height = screenHeight;
            else if (frameSizeH.ToUpper() == "AUTO")
                AutoHeight = true;
            else if (frameSizeH.ToUpper() == "BG") { }
            else
                Height = int.Parse(frameSizeH);

            var visibleAttribute = XMLElement.Attribute("Visible");
            var activeAttribute = XMLElement.Attribute("Active");
            var draggableAttribute = XMLElement.Attribute("Draggable");

            if (visibleAttribute != null)
                Visible = bool.Parse(visibleAttribute.Value);
            if (activeAttribute != null)
                Active = bool.Parse(activeAttribute.Value);
            if (draggableAttribute != null)
                Draggable = bool.Parse(draggableAttribute.Value);

            Name = XMLElement.Attribute("Name").Value;

            ReloadWidgets();

            var elBackground = XMLElement.Element("Background");

            if (elBackground != null)
            {
                var bgWidget = new UIWidget();
                bgWidget.Init(this, XMLElement);
                bgWidget.Width = Width;
                bgWidget.Height = Height;
                FrameSprite = UISprite.CreateUISprite(bgWidget, "Background");
            }

            if (frameSizeW.ToUpper() == "BG")
                Width = FrameSprite.Width;

            if (frameSizeH.ToUpper() == "BG")
                Height = FrameSprite.Height;

            var draggableRectAttribute = XMLElement.Attribute("DraggableRect");
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

            foreach (var widget in Widgets.Widgets)
            {
                widget.UpdatePositionFromFlags();
            }

            var frameX = framePosition.Attribute("X").Value.ToUpper();
            var frameY = framePosition.Attribute("Y").Value.ToUpper();

            if (frameX == "CENTER")
                CenterX = true;
            else if (frameX == "LEFT")
                AnchorLeft = true;
            else if (frameX == "RIGHT")
                AnchorRight = true;
            else
                X = int.Parse(frameX);

            if (frameY == "CENTER")
                CenterY = true;
            else if (frameY == "TOP")
                AnchorTop = true;
            else if (frameY == "BOTTOM")
                AnchorBottom = true;
            else
                Y = int.Parse(frameY);

            UpdatePosition();

        } // UIFrame

        ~UIFrame()
        {
            Dispose(false);
        }

        public void UpdatePosition()
        {
            var screenWidth = ElementGlobals.TargetResolutionWidth;
            var screenHeight = ElementGlobals.TargetResolutionHeight;

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
        }

        public void ReloadWidgets()
        {
            Widgets.Clear();
            CommonWidgetResources.LayoutGroups.Clear();

            XElement widgetsRoot = XMLElement.Element("Widgets");

            var currentWidth = 0;
            var currentHeight = 0;

            var tempWidgets = new List<UIWidget>();

            foreach (var elWidget in widgetsRoot.Elements())
            {
                if (elWidget.Name.ToString().ToUpper() == "PANEL")
                    AddPanel(elWidget, tempWidgets);
                else
                    AddWidget(elWidget, tempWidgets);
            }

            foreach (var elLayoutGroup in XMLElement.Elements("LayoutGroup"))
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

            var attPadding = XMLElement.Attribute("Padding");
            if (attPadding != null)
            {
                var paddingSplit = attPadding.Value.Split(",");

                switch (paddingSplit.Length)
                {
                    case 1:
                        {
                            var all = int.Parse(paddingSplit[0]);
                            PaddingTop = all;
                            PaddingBottom = all;
                            PaddingLeft = all;
                            PaddingRight = all;
                        }
                        break;

                    case 2:
                        {
                            var horizontal = int.Parse(paddingSplit[0]);
                            var vertical = int.Parse(paddingSplit[1]);
                            PaddingTop = vertical;
                            PaddingBottom = vertical;
                            PaddingLeft = horizontal;
                            PaddingRight = horizontal;
                        }
                        break;

                    case 4:
                        {
                            PaddingLeft = int.Parse(paddingSplit[0]);
                            PaddingRight = int.Parse(paddingSplit[1]);
                            PaddingTop = int.Parse(paddingSplit[2]);
                            PaddingBottom = int.Parse(paddingSplit[3]);
                        }
                        break;
                }
            }

            foreach (var widget in tempWidgets)
            {
                widget.X += PaddingLeft;
                widget.Y += PaddingTop;
            }

            foreach (var panel in Panels)
            {
                panel.X += PaddingLeft;
                panel.Y += PaddingTop;
            }

            foreach (var widget in tempWidgets)
            {
                if (widget.Panel != null)
                    continue;

                if (AutoWidth)
                    if ((widget.X + widget.Width) > currentWidth)
                        currentWidth = widget.X + widget.Width;

                if (AutoHeight)
                    if ((widget.Y + widget.Height) > currentHeight)
                        currentHeight = widget.Y + widget.Height;
            }

            foreach (var panel in Panels)
            {
                if (AutoWidth)
                    if ((panel.X + panel.Width) > currentWidth)
                        currentWidth = panel.X + panel.Width;

                if (AutoHeight)
                    if ((panel.Y + panel.Height) > currentHeight)
                        currentHeight = panel.Y + panel.Height;
            }

            if (AutoWidth)
                Width = currentWidth;
            if (AutoHeight)
                Height = currentHeight;

            Width += PaddingRight;
            Height += PaddingBottom;

            UpdatePosition();
            Widgets.OrderByDrawOrder();

        } // ReloadWidgets

        internal void AddPanel(XElement elPanel, List<UIWidget> widgets)
        {
            var panel = new UIPanel(this, elPanel);
            var widgetsRoot = elPanel.Element("Widgets");

            foreach (var elWidget in widgetsRoot.Elements())
            {
                var widget = AddWidget(elWidget, widgets, panel);
                if (widget != null)
                    panel.AddWidget(widget);
            }

            if (panel.HScrollBar != null)
                widgets.Add(panel.HScrollBar);
            if (panel.VScrollBar != null)
                widgets.Add(panel.VScrollBar);

            Panels.Add(panel);
        }

        internal UIWidget AddWidget(XElement elWidget, List<UIWidget> widgets, UIPanel panel = null, bool ignoreBindRepeater = false, bool ignoreBindObject = false)
        {
            var attRepeater = elWidget.Attribute("BindRepeater");
            var attObject = elWidget.Attribute("BindObject");

            if (attRepeater != null && !ignoreBindRepeater)
            {
                var repeaterName = attRepeater.Value;
                var repeaterElements = UIDataBinding.GetRepeaterOutput(Parent, repeaterName, elWidget);

                if (repeaterElements == null || repeaterElements.Count == 0)
                    return null;

                foreach (var el in repeaterElements)
                    AddWidget(el, widgets, panel, ignoreBindRepeater: true);

                Parent.RegisterBoundObject(this, repeaterName);
            }
            else if (attObject != null && !ignoreBindObject)
            {
                var objectName = attObject.Value;

                AddWidget(UIDataBinding.GetElement(Parent, objectName, elWidget), widgets, panel, ignoreBindObject: true);
                Parent.RegisterBoundObject(this, objectName);
            }
            else
            {
                foreach (var (typeName, type) in ElementGlobals.UIWidgetTypes)
                {
                    if (typeName.ToUpper() == elWidget.Name.ToString().ToUpper())
                    {
                        UIWidget widget = (UIWidget)Activator.CreateInstance(type);
                        widget.Panel = panel;
                        widget.Init(this, elWidget);
                        widget.Load(this, elWidget);
                        widgets.Add(widget);
                        return widget;
                    }
                }
            }

            return null;
        } // AddWidget

        internal void TriggerUIEvent(UIEventType type, UIWidget widget)
        {
            Parent.TriggerUIEvent(type, widget);
        }

        internal void GrabFocus(UIWidget widget)
        {
            Parent.GrabFocus(this);
            Widgets.UnFocusAllExcept(widget.Name);
            widget.Focus();
            Focused = true;
        }

        internal void DropFocus(UIWidget widget)
        {
            Parent.DropFocus(this);
            widget.UnFocus();
            Focused = false;
        }

        public void UnFocus()
        {
            Widgets.UnFocusAll();
            Focused = false;
            Parent.DropFocus(this);
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

            foreach (var panel in Panels)
                panel.Update(gameTimer);
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

            if (point.X < (FrameRect.X + _draggableRect.X))
                return false;
            if (point.Y < (FrameRect.Y + _draggableRect.Y))
                return false;
            if (point.X > ((FrameRect.X + _draggableRect.X) + _draggableRect.Width))
                return false;
            if (point.Y > ((FrameRect.Y + _draggableRect.Y) + _draggableRect.Height))
                return false;

            return true;
        }

        public bool PointInsideFrame(Vector2 point)
        {
            if (FrameRect.Width == 0 && FrameRect.Height == 0)
                return false;

            if (point.X < FrameRect.X)
                return false;
            if (point.Y < FrameRect.Y)
                return false;
            if (point.X > (FrameRect.X + FrameRect.Width))
                return false;
            if (point.Y > (FrameRect.Y + FrameRect.Height))
                return false;

            return true;
        }

    }
}
