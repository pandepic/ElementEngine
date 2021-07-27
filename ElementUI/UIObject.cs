using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid;

using Rectangle = ElementEngine.Rectangle;

namespace ElementEngine.ElementUI
{
    public class UIObject : IMouseHandler, IKeyboardHandler
    {
        public UIObject Parent;
        public UIStyle Style => _style;
        public List<UIObject> Children = new List<UIObject>();
        public string Name;

        #region Position, Size & Bounds
        public bool HasMargin => !_margins.IsZero;
        public bool HasPadding => !_padding.IsZero;

        public Vector2 Position
        {
            get => _position;
            set
            {
                _uiPosition.Position = value;
                _layoutDirty = true;
            }
        }

        public float X
        {
            get => _position.X;
            set
            {
                var current = _uiPosition.Position ?? Vector2.Zero;
                current.X = value;
                _uiPosition.Position = current;
                _layoutDirty = true;
            }
        }

        public float Y
        {
            get => _position.Y;
            set
            {
                var current = _uiPosition.Position ?? Vector2.Zero;
                current.Y = value;
                _uiPosition.Position = current;
                _layoutDirty = true;
            }
        }

        public Vector2 Size
        {
            get => _size;
            set
            {
                _uiSize.Size = value;
                _layoutDirty = true;
            }
        }

        public float Width
        {
            get => _size.X;
            set
            {
                var current = _uiSize.Size ?? Vector2.Zero;
                current.X = value;
                _uiSize.Size = current;
                _layoutDirty = true;
            }
        }

        public float Height
        {
            get => _size.Y;
            set
            {
                var current = _uiSize.Size ?? Vector2.Zero;
                current.Y = value;
                _uiSize.Size = current;
                _layoutDirty = true;
            }
        }

        public bool AutoWidth
        {
            get => _uiSize.AutoWidth;
            set
            {
                _uiSize.AutoWidth = value;
                _layoutDirty = true;
            }
        }

        public bool AutoHeight
        {
            get => _uiSize.AutoHeight;
            set
            {
                _uiSize.AutoHeight = value;
                _layoutDirty = true;
            }
        }

        public bool ParentWidth
        {
            get => _uiSize.ParentWidth;
            set
            {
                _uiSize.ParentWidth = value;
                _layoutDirty = true;
            }
        }

        public bool ParentHeight
        {
            get => _uiSize.ParentHeight;
            set
            {
                _uiSize.ParentHeight = value;
                _layoutDirty = true;
            }
        }

        public float? ParentWidthRatio
        {
            get => _uiSize.ParentWidthRatio;
            set
            {
                _uiSize.ParentWidthRatio = value;
                _layoutDirty = true;
            }
        }

        public float? ParentHeightRatio
        {
            get => _uiSize.ParentHeightRatio;
            set
            {
                _uiSize.ParentHeightRatio = value;
                _layoutDirty = true;
            }
        }

        public Rectangle Bounds
        {
            get => new Rectangle(_position, _size);
        }

        public Rectangle MarginBounds
        {
            get => new Rectangle(_position - _margins.TopLeftF, _size + _margins.TopLeftF + _margins.BottomRightF);
        }

        public Rectangle PaddingBounds
        {
            get => new Rectangle(_position + _padding.TopLeftF, _size - _padding.TopLeftF - _padding.BottomRightF);
        }
        #endregion

        #region Positioning

        public bool CenterX
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                _layoutDirty = true;
            }
        }

        public bool CenterY
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                _layoutDirty = true;
            }
        }

        public bool AnchorTop
        {
            get => _uiPosition.Position.HasValue ? _uiPosition.Position.Value.Y == 0 : false;
            set
            {
                if (!value)
                    return;
                if (!_uiPosition.Position.HasValue)
                    return;

                _uiPosition.Position = new Vector2(_uiPosition.Position.Value.X, 0);
                _layoutDirty = true;
            }
        }

        public bool AnchorBottom
        {
            get => _uiPosition.AnchorBottom;
            set
            {
                _uiPosition.AnchorBottom = value;
                _layoutDirty = true;
            }
        }

        public bool AnchorLeft
        {
            get => _uiPosition.Position.HasValue ? _uiPosition.Position.Value.X == 0 : false;
            set
            {
                if (!value)
                    return;
                if (!_uiPosition.Position.HasValue)
                    return;

                _uiPosition.Position = new Vector2(0, _uiPosition.Position.Value.Y);
                _layoutDirty = true;
            }
        }

        public bool AnchorRight
        {
            get => _uiPosition.AnchorRight;
            set
            {
                _uiPosition.AnchorRight = value;
                _layoutDirty = true;
            }
        }

        public void Center()
        {
            _uiPosition.CenterX = true;
            _uiPosition.CenterY = true;
            _layoutDirty = true;
        }
        #endregion

        #region Margins
        public int MarginLeft
        {
            get => _margins.Left;
            set
            {
                _margins.Left = value;
                _layoutDirty = true;
            }
        }

        public int MarginRight
        {
            get => _margins.Right;
            set
            {
                _margins.Right = value;
                _layoutDirty = true;
            }
        }

        public int MarginTop
        {
            get => _margins.Top;
            set
            {
                _margins.Top = value;
                _layoutDirty = true;
            }
        }

        public int MarginBottom
        {
            get => _margins.Bottom;
            set
            {
                _margins.Bottom = value;
                _layoutDirty = true;
            }
        }

        public void SetMargins(int margin)
        {
            SetMargins(margin, margin, margin, margin);
        }

        public void SetMargins(int horizontal, int vertical)
        {
            SetMargins(horizontal, horizontal, vertical, vertical);
        }

        public void SetMargins(int left, int right, int top, int bottom)
        {
            _margins.Left = left;
            _margins.Right = right;
            _margins.Top = top;
            _margins.Bottom = bottom;
            _layoutDirty = true;
        }
        #endregion

        #region Padding
        public int PaddingLeft
        {
            get => _padding.Left;
            set
            {
                _padding.Left = value;
                _layoutDirty = true;
            }
        }

        public int PaddingRight
        {
            get => _padding.Right;
            set
            {
                _padding.Right = value;
                _layoutDirty = true;
            }
        }

        public int PaddingTop
        {
            get => _padding.Top;
            set
            {
                _padding.Top = value;
                _layoutDirty = true;
            }
        }

        public int PaddingBottom
        {
            get => _padding.Bottom;
            set
            {
                _padding.Bottom = value;
                _layoutDirty = true;
            }
        }

        public void SetPadding(int padding)
        {
            SetPadding(padding, padding, padding, padding);
        }

        public void SetPadding(int horizontal, int vertical)
        {
            SetPadding(horizontal, horizontal, vertical, vertical);
        }

        public void SetPadding(int left, int right, int top, int bottom)
        {
            _padding.Left = left;
            _padding.Right = right;
            _padding.Top = top;
            _padding.Bottom = bottom;
            _layoutDirty = true;
        }
        #endregion

        #region Find Children
        public T FindChildByName<T>(string name, bool recursive) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t && child.Name == name)
                    return t;

                if (recursive)
                {
                    t = child.FindChildByName<T>(name, recursive);
                    if (t != null)
                        return t;
                }
            }

            return null;
        }

        public List<T> FindChildrenByName<T>(string name, bool recursive) where T : UIObject
        {
            return FindChildrenByName<T>(name, recursive, true);
        }

        internal List<T> FindChildrenByName<T>(string name, bool recursive, bool clearList) where T : UIObject
        {
            if (clearList)
                TempFindChildrenList<T>.List.Clear();

            foreach (var child in Children)
            {
                if (child is T t && child.Name == name)
                    TempFindChildrenList<T>.List.Add(t);

                if (recursive)
                    child.FindChildrenByName<T>(name, recursive, false);
            }

            return TempFindChildrenList<T>.List;
        }
        #endregion

        #region Visible & Active
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value)
                    Enable();
                else
                    Disable();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        public virtual void Show()
        {
            _isVisible = true;
        }

        public virtual void Hide()
        {
            _isVisible = false;
        }

        public void ToggleVisible()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public virtual void Enable()
        {
            _isActive = true;
        }

        public virtual void Disable()
        {
            _isActive = false;
        }

        public void ToggleActive()
        {
            if (_isActive)
                Disable();
            else
                Enable();
        }
        #endregion

        internal bool _isActive = true;
        internal bool _isVisible = true;
        internal bool _useScissorRect => _style == null ? false : _style.OverflowType == OverflowType.Hide;

        internal UIStyle _style;
        internal UIPosition _uiPosition;
        internal UISize _uiSize;
        internal Vector2 _position;
        internal Vector2 _childOrigin;
        internal Vector2 _size;
        internal UISpacing _margins;
        internal UISpacing _padding;

        internal bool _layoutDirty = false;

        #region Reusable Lists
        protected static class TempFindChildrenList<T> where T : UIObject
        {
            public static List<T> List = new List<T>();
        }

        internal List<UIObject> _tempChildrenList = new List<UIObject>();
        #endregion

        public UIObject(string name)
        {
            Name = name;
        }

        public void ApplyStyle(UIStyle style)
        {
            _style = style;
            _uiPosition = style.UIPosition ?? new UIPosition();
            _uiSize = style.UISize ?? new UISize();
            _margins = style.Margins ?? new UISpacing();
            _padding = style.Padding ?? new UISpacing();
        }

        public void ApplyDefaultSize(UISprite sprite)
        {
            ApplyDefaultSize(sprite.Size);
        }

        public void ApplyDefaultSize(UIObject obj)
        {
            ApplyDefaultSize(obj.Size);
        }

        public void ApplyDefaultSize(Vector2 size)
        {
            if (_uiSize.Size.HasValue)
                return;

            if (!_uiSize.IsAutoSizedX)
                Width = size.X;
            if (!_uiSize.IsAutoSizedY)
                Height = size.Y;
        }

        public bool AddChild(UIObject child)
        {
            if (Children.AddIfNotContains(child))
            {
                child.Parent = this;
                _layoutDirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal virtual void CheckLayout()
        {
            if (_layoutDirty)
                UpdateLayout();

            foreach (var child in Children)
                child.CheckLayout();
        }

        internal virtual void UpdateLayout()
        {
            foreach (var child in Children)
                child.UpdateLayout();

            UpdateSize();
            UpdatePosition();

            foreach (var child in Children)
                child.UpdateLayout();

            HandleMargins();
            _layoutDirty = false;
        }

        internal void UpdateSize()
        {
            _size = _uiSize.GetSize(this);
        }

        internal void UpdatePosition()
        {
            _position = _uiPosition.GetPosition(this);
            _childOrigin = _position + _padding.TopLeftF;
        }

        internal virtual void HandleMargins()
        {
            UIObject firstChildWithMargin = null;

            foreach (var child in Children)
            {
                if (child.HasMargin)
                {
                    firstChildWithMargin = child;
                    break;
                }
            }

            // vertical margin
            foreach (var child in Children)
            {
                if (child == firstChildWithMargin)
                    continue;
                if (!child._uiPosition.Position.HasValue)
                    continue;
                if (child is UIContainer)
                    continue;
                if (!child.HasMargin)
                    continue;

                foreach (var sibling in Children.OrderBy(c => c.Position.Y))
                {
                    if (child == sibling)
                        continue;
                    if (!sibling.HasMargin)
                        continue;
                    if (sibling is UIContainer)
                        continue;
                    if (!child.MarginBounds.Intersects(sibling.MarginBounds))
                        continue;

                    if ((child.MarginTop > 0 || sibling.MarginBottom > 0) && !child._uiPosition.IsAutoPositionY)
                    {
                        if (child.MarginBounds.Top < sibling.MarginBounds.Bottom && child.MarginBounds.Bottom > sibling.MarginBounds.Top)
                        {
                            var offset = new Vector2(0, sibling.MarginBounds.Bottom - child.MarginBounds.Top);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }
            }

            // horizontal margin
            foreach (var child in Children)
            {
                if (child == firstChildWithMargin)
                    continue;
                if (!child._uiPosition.Position.HasValue)
                    continue;
                if (child is UIContainer)
                    continue;
                if (!child.HasMargin)
                    continue;

                foreach (var sibling in Children.OrderBy(c => c.Position.Y))
                {
                    if (child == sibling)
                        continue;
                    if (!sibling.HasMargin)
                        continue;
                    if (sibling is UIContainer)
                        continue;
                    if (!child.MarginBounds.Intersects(sibling.MarginBounds))
                        continue;

                    if ((child.MarginLeft > 0 || sibling.MarginRight > 0) && !child._uiPosition.IsAutoPositionX)
                    {
                        if (child.MarginBounds.Left < sibling.MarginBounds.Right && child.MarginBounds.Right > sibling.MarginBounds.Left)
                        {
                            var offset = new Vector2(sibling.MarginBounds.Right - child.MarginBounds.Left, 0);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }
            }
        } // HandleMargins

        public virtual void Update(GameTimer gameTimer)
        {
            foreach (var child in Children)
            {
                if (child.IsActive)
                    child.Update(gameTimer);
            }
        }

        public virtual void Draw(SpriteBatch2D spriteBatch)
        {
            if (_useScissorRect)
                spriteBatch.SetScissorRect(PaddingBounds);

            foreach (var child in Children)
            {
                if (child.IsVisible)
                    child.Draw(spriteBatch);
            }

            if (_useScissorRect)
                spriteBatch.ResetScissorRect();
        }

        #region Input Handling
        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer) { }
        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer) { }
        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer) { }

        public void HandleKeyPressed(Key key, GameTimer gameTimer) { }
        public void HandleKeyReleased(Key key, GameTimer gameTimer) { }
        public void HandleKeyDown(Key key, GameTimer gameTimer) { }
        public void HandleTextInput(char key, GameTimer gameTimer) { }
        #endregion

        public override string ToString()
        {
            return $"{GetType().Name} - {Name} [{Bounds}]";
        }

    } // UIObject
}
