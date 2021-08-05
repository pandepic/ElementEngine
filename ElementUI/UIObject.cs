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
        internal static int _nextObjectID = 0;
        internal const int NO_DRAW_ORDER = -1;

        public int ObjectID = _nextObjectID++;
        public UIObject Parent;
        public UIScreen ParentScreen => this is UIScreen thisScreen ? thisScreen : (Parent == null ? null : (Parent is UIScreen screen ? screen : Parent.ParentScreen));

        public UIStyle Style => _style;
        public readonly List<UIObject> Children = new List<UIObject>();
        public readonly List<UIObject> ReverseChildren = new List<UIObject>();
        public string Name;
        public int ScrollSpeed = 10;

        public bool IsFocused => ParentScreen.FocusedObject == this;
        public bool CanFocus = true;
        public bool IgnoreOverflow = false;

        internal int _drawOrder = NO_DRAW_ORDER;
        public int DrawOrder
        {
            get => _drawOrder;
            set
            {
                _drawOrder = value;

                if (Parent != null)
                    Parent.SetLayoutDirty();
            }
        }

        #region Position, Size & Bounds
        internal bool _ignoreParentPadding;
        public bool IgnoreParentPadding
        {
            get => _ignoreParentPadding;
            set
            {
                _ignoreParentPadding = value;
                SetLayoutDirty();
            }
        }

        public bool HasMargin => !_margins.IsZero;
        public bool HasPadding => !_padding.IsZero;

        public Vector2I DrawPosition
        {
            get => _position + _parentOffset;
        }

        public Vector2I Position
        {
            get => _position;
        }

        public UISizeFillType? FillType
        {
            get => _uiSize.FillType;
            set
            {
                _uiSize.FillType = value;
                SetLayoutDirty();
            }
        }

        public void SetPosition(Vector2 position)
        {
            SetPosition(position.ToVector2I());
        }

        public void SetPosition(Vector2I position)
        {
            _uiPosition.Position = position;
            InternalOnPositionChanged();
            SetLayoutDirty();
        }

        public void OffsetPosition(Vector2I offset)
        {
            if (!_uiPosition.Position.HasValue)
                _uiPosition.Position = new Vector2I();

            _uiPosition.Position += offset;
            InternalOnPositionChanged();
            SetLayoutDirty();
        }

        public int X
        {
            get => _position.X;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;
                current.X = value;
                _uiPosition.Position = current;
                InternalOnPositionChanged();
                SetLayoutDirty();
            }
        }

        public int Y
        {
            get => _position.Y;
            set
            {
                var current = _uiPosition.Position ?? Vector2I.Zero;
                current.Y = value;
                _uiPosition.Position = current;
                InternalOnPositionChanged();
                SetLayoutDirty();
            }
        }

        public Vector2I Size
        {
            get => _size;
            set
            {
                _uiSize.Size = value;
                InternalOnSizeChanged();
                SetLayoutDirty();
            }
        }

        public int Width
        {
            get => _size.X;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;
                current.X = value;
                _uiSize.Size = current;
                SetLayoutDirty();
            }
        }

        public int Height
        {
            get => _size.Y;
            set
            {
                var current = _uiSize.Size ?? Vector2I.Zero;
                current.Y = value;
                _uiSize.Size = current;
                SetLayoutDirty();
            }
        }

        public bool AutoWidth
        {
            get => _uiSize.AutoWidth;
            set
            {
                _uiSize.AutoWidth = value;
                SetLayoutDirty();
            }
        }

        public bool AutoHeight
        {
            get => _uiSize.AutoHeight;
            set
            {
                _uiSize.AutoHeight = value;
                SetLayoutDirty();
            }
        }

        public bool ParentWidth
        {
            get => _uiSize.ParentWidth;
            set
            {
                _uiSize.ParentWidth = value;
                SetLayoutDirty();
            }
        }

        public bool ParentHeight
        {
            get => _uiSize.ParentHeight;
            set
            {
                _uiSize.ParentHeight = value;
                SetLayoutDirty();
            }
        }

        public float? ParentWidthRatio
        {
            get => _uiSize.ParentWidthRatio;
            set
            {
                _uiSize.ParentWidthRatio = value;
                SetLayoutDirty();
            }
        }

        public float? ParentHeightRatio
        {
            get => _uiSize.ParentHeightRatio;
            set
            {
                _uiSize.ParentHeightRatio = value;
                SetLayoutDirty();
            }
        }

        public int? MinWidth
        {
            get => _uiSize.MinWidth;
            set
            {
                _uiSize.MinWidth = value;
                SetLayoutDirty();
            }
        }

        public int? MaxWidth
        {
            get => _uiSize.MaxWidth;
            set
            {
                _uiSize.MaxWidth = value;
                SetLayoutDirty();
            }
        }

        public int? MinHeight
        {
            get => _uiSize.MinHeight;
            set
            {
                _uiSize.MinHeight = value;
                SetLayoutDirty();
            }
        }

        public int? MaxHeight
        {
            get => _uiSize.MaxHeight;
            set
            {
                _uiSize.MaxHeight = value;
                SetLayoutDirty();
            }
        }

        public Rectangle Bounds
        {
            get => new Rectangle(DrawPosition, _size);
        }

        public Rectangle MarginBounds
        {
            get => new Rectangle(_position - _margins.TopLeft, _size + _margins.TopLeft + _margins.BottomRight);
        }

        public Rectangle PaddingBounds
        {
            get => new Rectangle(_position + _padding.TopLeft, _size - _padding.TopLeft - _padding.BottomRight);
        }

        internal virtual void InternalOnPositionChanged() { }
        internal virtual void InternalOnSizeChanged() { }
        #endregion

        #region Positioning

        public bool CenterX
        {
            get => _uiPosition.CenterX;
            set
            {
                _uiPosition.CenterX = value;
                SetLayoutDirty();
            }
        }

        public bool CenterY
        {
            get => _uiPosition.CenterY;
            set
            {
                _uiPosition.CenterY = value;
                SetLayoutDirty();
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

                _uiPosition.Position = new Vector2I(_uiPosition.Position.Value.X, 0);
                SetLayoutDirty();
            }
        }

        public bool AnchorBottom
        {
            get => _uiPosition.AnchorBottom;
            set
            {
                _uiPosition.AnchorBottom = value;
                SetLayoutDirty();
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

                _uiPosition.Position = new Vector2I(0, _uiPosition.Position.Value.Y);
                SetLayoutDirty();
            }
        }

        public bool AnchorRight
        {
            get => _uiPosition.AnchorRight;
            set
            {
                _uiPosition.AnchorRight = value;
                SetLayoutDirty();
            }
        }

        public void Center()
        {
            _uiPosition.CenterX = true;
            _uiPosition.CenterY = true;
            SetLayoutDirty();
        }
        #endregion

        #region Margins
        public int MarginLeft
        {
            get => _margins.Left;
            set
            {
                _margins.Left = value;
                SetLayoutDirty();
            }
        }

        public int MarginRight
        {
            get => _margins.Right;
            set
            {
                _margins.Right = value;
                SetLayoutDirty();
            }
        }

        public int MarginTop
        {
            get => _margins.Top;
            set
            {
                _margins.Top = value;
                SetLayoutDirty();
            }
        }

        public int MarginBottom
        {
            get => _margins.Bottom;
            set
            {
                _margins.Bottom = value;
                SetLayoutDirty();
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
            SetLayoutDirty();
        }
        #endregion

        #region Padding
        public int PaddingLeft
        {
            get => _padding.Left;
            set
            {
                _padding.Left = value;
                SetLayoutDirty();
            }
        }

        public int PaddingRight
        {
            get => _padding.Right;
            set
            {
                _padding.Right = value;
                SetLayoutDirty();
            }
        }

        public int PaddingTop
        {
            get => _padding.Top;
            set
            {
                _padding.Top = value;
                SetLayoutDirty();
            }
        }

        public int PaddingBottom
        {
            get => _padding.Bottom;
            set
            {
                _padding.Bottom = value;
                SetLayoutDirty();
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
            SetLayoutDirty();
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

        public T FindChildByType<T>(bool recursive) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t)
                    return t;

                if (recursive)
                {
                    t = child.FindChildByType<T>(recursive);
                    if (t != null)
                        return t;
                }
            }

            return null;
        }

        public List<T> FindChildrenByName<T>(string name, bool recursive) where T : UIObject
        {
            var list = new List<T>();
            FindChildrenByName(name, recursive, list);
            return list;
        }

        internal void FindChildrenByName<T>(string name, bool recursive, List<T> list) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t && child.Name == name)
                    list.Add(t);

                if (recursive)
                    child.FindChildrenByName<T>(name, recursive, list);
            }
        }

        public List<T> FindChildrenByType<T>(bool recursive) where T : UIObject
        {
            var list = new List<T>();
            FindChildrenByType(recursive, list);
            return list;
        }

        internal void FindChildrenByType<T>(bool recursive, List<T> list) where T : UIObject
        {
            foreach (var child in Children)
            {
                if (child is T t)
                    list.Add(t);

                if (recursive)
                    child.FindChildrenByType<T>(recursive, list);
            }
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

        internal int _childIndex;

        internal bool _isActive = true;
        internal bool _isVisible = true;
        internal bool _useScissorRect => _style == null ? false : (_style.OverflowType == OverflowType.Hide || _style.OverflowType == OverflowType.Scroll);
        internal bool _isScrollable => _style == null ? false : _style.OverflowType == OverflowType.Scroll;

        internal UIStyle _style;
        internal UIPosition _uiPosition;
        internal UISize _uiSize;
        internal Vector2I _position;
        internal Vector2I _childOrigin;
        internal Vector2I _childOffset;
        internal Vector2I _size;
        internal UISpacing _margins;
        internal UISpacing _padding;

        internal Vector2I _parentOffset => Parent == null ? Vector2I.Zero : IgnoreOverflow ? Parent._parentOffset : Parent._childOffset + Parent._parentOffset;
        internal bool _layoutDirty { get; set; } = false;

        public UIObject(string name)
        {
            Name = name;
        }

        public void ApplyStyle(UIStyle style)
        {
            if (style == null)
                return;

            _style = style;
            _uiPosition = style.UIPosition ?? new UIPosition();
            _uiSize = style.UISize ?? new UISize();
            _margins = style.Margins ?? new UISpacing();
            _padding = style.Padding ?? new UISpacing();
            ScrollSpeed = style.ScrollSpeed ?? ScrollSpeed;
            IgnoreOverflow = style.IgnoreOverflow ?? IgnoreOverflow;
        }

        public void ApplyDefaultSize(UISprite sprite)
        {
            ApplyDefaultSize(sprite.Size);
        }

        public void ApplyDefaultSize(UIObject obj)
        {
            ApplyDefaultSize(obj.Size);
        }

        public void ApplyDefaultSize(Vector2I size)
        {
            if (_uiSize.Size.HasValue)
                return;

            Width = size.X;
            Height = size.Y;
        }

        public void OverrideDefaultSize(Vector2I size)
        {
            Width = size.X;
            Height = size.Y;
        }

        #region Children
        public bool AddChild(UIObject child)
        {
            if (Children.AddIfNotContains(child))
            {
                child.Parent = this;
                SetLayoutDirty();

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveChild(string name)
        {
            var child = Children.Find((obj) => { return obj.Name == name; });

            if (child != null)
            {
                Children.Remove(child);
                SetLayoutDirty();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveChild<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];

                if (child is T)
                {
                    Children.RemoveAt(i);
                    SetLayoutDirty();
                    return true;
                }
            }

            return false;
        }

        public void RemoveChildren<T>() where T : UIObject
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];

                if (child is T)
                {
                    Children.RemoveAt(i);
                    SetLayoutDirty();
                }
            }
        }

        public void ClearChildren()
        {
            Children.Clear();
            SetLayoutDirty();
        }

        public void BringToFront(UIObject child)
        {
            Children.Remove(child);
            Children.Add(child);

            for (var i = 0; i < Children.Count; i++)
                Children[i].DrawOrder = i;

            SortChildren();
            SetLayoutDirty();
        }

        public bool HasFocusedChild(bool recursive)
        {
            foreach (var child in Children)
            {
                if (child.IsFocused)
                    return true;

                if (recursive && child.HasFocusedChild(recursive))
                    return true;
            }

            return false;
        }

        internal void SortChildren()
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i]._childIndex = i;

            Children.Sort((c1, c2) =>
            {
                if (c1.DrawOrder == c2.DrawOrder)
                    return c1._childIndex.CompareTo(c2._childIndex);

                return c1.DrawOrder.CompareTo(c2.DrawOrder);
            });

            for (var i = 0; i < Children.Count; i++)
                Children[i].DrawOrder = i;

            ReverseChildren.Clear();
            ReverseChildren.AddRange(Children);
            ReverseChildren.Sort((c1, c2) => { return c2.DrawOrder.CompareTo(c1.DrawOrder); });
        }
        #endregion

        #region Scrolling
        internal virtual void InternalOnScrollX() { }
        internal virtual void InternalOnScrollY() { }

        internal void ScrollLeft(int? amount = null)
        {
            _childOffset.X += amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollX();
        }

        internal void ScrollRight(int? amount = null)
        {
            _childOffset.X -= amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollX();
        }

        internal void ScrollUp(int? amount = null)
        {
            _childOffset.Y += amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollY();
        }

        internal void ScrollDown(int? amount = null)
        {
            _childOffset.Y -= amount ?? ScrollSpeed;
            ClampScroll();
            InternalOnScrollY();
        }

        internal void ClampScroll()
        {
            if (_uiSize._fullChildBounds.IsZero)
            {
                _childOffset = Vector2I.Zero;
                return;
            }

            var prevOffset = _childOffset;

            var minX = (_uiSize._fullChildBounds.Right - PaddingBounds.Width) * -1;
            var maxX = _uiSize._fullChildBounds.Left * -1;
            var minY = (_uiSize._fullChildBounds.Bottom - PaddingBounds.Height) * -1;
            var maxY = _uiSize._fullChildBounds.Top * -1;

            if (_uiSize._fullChildBounds.Width <= PaddingBounds.Width)
                _childOffset.X = _uiSize._fullChildBounds.Left * -1;
            else if (_uiSize._fullChildBounds.Right > PaddingBounds.Width)
                _childOffset.X = Math.Clamp(_childOffset.X, minX, maxX);

            if (_uiSize._fullChildBounds.Height <= PaddingBounds.Height)
                _childOffset.Y = _uiSize._fullChildBounds.Top * -1;
            else if (_uiSize._fullChildBounds.Bottom > PaddingBounds.Height)
                _childOffset.Y = Math.Clamp(_childOffset.Y, minY, maxY);
        }
        #endregion

        internal virtual void SetLayoutDirty()
        {
            //_layoutDirty = true;
            //Parent?.SetLayoutDirty();

            if (ParentScreen != null)
                ParentScreen._layoutDirty = true;
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

            SortChildren();
            HandleMargins();
            ClampScroll();
            _layoutDirty = false;
        }

        internal void UpdateSize()
        {
            _size = _uiSize.GetSize(this);
        }

        internal void UpdatePosition()
        {
            _position = _uiPosition.GetPosition(this);
            _childOrigin = _position + _padding.TopLeft;
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

                var sortedChildren = GlobalObjectPool<List<UIObject>>.Rent();
                sortedChildren.Clear();
                sortedChildren.AddRange(Children);
                sortedChildren.Sort((obj1, obj2) => { return obj1.Position.Y.CompareTo(obj2.Position.Y); });

                foreach (var sibling in sortedChildren)
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
                            var offset = new Vector2I(0, sibling.MarginBounds.Bottom - child.MarginBounds.Top);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }

                GlobalObjectPool<List<UIObject>>.Return(sortedChildren);
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

                var sortedChildren = GlobalObjectPool<List<UIObject>>.Rent();
                sortedChildren.Clear();
                sortedChildren.AddRange(Children);
                sortedChildren.Sort((obj1, obj2) => { return obj1.Position.X.CompareTo(obj2.Position.X); });

                foreach (var sibling in sortedChildren)
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
                            var offset = new Vector2I(sibling.MarginBounds.Right - child.MarginBounds.Left, 0);
                            child._uiPosition.Position += offset;
                            child.UpdateLayout();
                        }
                    }
                }

                GlobalObjectPool<List<UIObject>>.Return(sortedChildren);
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

        internal virtual void PreDraw(SpriteBatch2D spriteBatch)
        {
            if (IgnoreOverflow)
                spriteBatch.PushScissorRect(0, null);
        }

        internal virtual void PostDraw(SpriteBatch2D spriteBatch)
        {
            if (IgnoreOverflow)
                spriteBatch.PopScissorRect(0);
        }

        protected virtual void InnerPreDraw(SpriteBatch2D spriteBatch) { }
        protected virtual void InnerPostDraw(SpriteBatch2D spriteBatch) { }

        public virtual void Draw(SpriteBatch2D spriteBatch)
        {
            if (_useScissorRect)
                spriteBatch.PushScissorRect(0, PaddingBounds, true);

            InnerPreDraw(spriteBatch);

            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    child.PreDraw(spriteBatch);
                    child.Draw(spriteBatch);
                    child.PostDraw(spriteBatch);
                }
            }

            InnerPostDraw(spriteBatch);

            if (_useScissorRect)
                spriteBatch.PopScissorRect(0);
        }

        #region Input Handling (Interface Passthrough)
        public void HandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        public void HandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);
        }

        public void HandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);
        }

        public void HandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            InternalHandleMouseButtonDown(mousePosition, button, gameTimer);
        }

        public void HandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);
        }

        public virtual void HandleKeyPressed(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyPressed(key, gameTimer);
        }

        public virtual void HandleKeyReleased(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyReleased(key, gameTimer);
        }

        public virtual void HandleKeyDown(Key key, GameTimer gameTimer)
        {
            InternalHandleKeyDown(key, gameTimer);
        }

        public virtual void HandleTextInput(char key, GameTimer gameTimer)
        {
            InternalHandleTextInput(key, gameTimer);
        }
        #endregion

        #region Input Handling
        internal UIObject GetFirstChildContainsMouse(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildContainsMouseCanFocus(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;
                if (!child.CanFocus)
                    continue;

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildScrollableContainsMouse(Vector2 mousePosition)
        {
            if (_useScissorRect && !PaddingBounds.Contains(mousePosition))
                return null;

            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;
                if (!child._isScrollable)
                    continue;

                if (child.Bounds.Contains(mousePosition))
                    return child;
            }

            return null;
        }

        internal UIObject GetFirstChildFocused()
        {
            foreach (var child in ReverseChildren)
            {
                if (!child.IsVisible)
                    continue;
                if (!child.IsActive)
                    continue;
                if (!child.CanFocus)
                    return child;
                
                if (child.IsFocused)
                    return child;
            }

            return null;
        }

        internal virtual bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer);

            foreach (var childNoMotion in Children)
            {
                if (childNoMotion == child)
                    continue;

                childNoMotion?.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
            }

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;
            else
                return false;
        }

        internal virtual void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            foreach (var childNoMotion in Children)
                childNoMotion?.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal virtual bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouseCanFocus(mousePosition);
            var childCaptured = child?.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;

            if (child == null && CanFocus)
                ParentScreen.FocusedObject = this;

            return false;
        }

        internal virtual bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            var childCaptured = child?.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer);

            if (childCaptured.HasValue && childCaptured.Value == true)
                return true;
            else
                return false;
        }

        internal virtual bool InternalHandleMouseButtonDown(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            var child = GetFirstChildContainsMouse(mousePosition);
            child?.InternalHandleMouseButtonDown(mousePosition, button, gameTimer);

            return child != null;
        }

        internal virtual bool InternalHandleMouseWheel(Vector2 mousePosition, MouseWheelChangeType type, float mouseWheelDelta, GameTimer gameTimer)
        {
            var child = GetFirstChildScrollableContainsMouse(mousePosition);
            child?.InternalHandleMouseWheel(mousePosition, type, mouseWheelDelta, gameTimer);

            if (child == null && _isScrollable)
            {
                if (mouseWheelDelta > 0)
                    ScrollUp();
                else if (mouseWheelDelta < 0)
                    ScrollDown();
            }

            return child != null;
        }

        public virtual bool InternalHandleKeyPressed(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyPressed(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleKeyReleased(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyReleased(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleKeyDown(Key key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleKeyDown(key, gameTimer);

            return false;
        }

        public virtual bool InternalHandleTextInput(char key, GameTimer gameTimer)
        {
            if (this is UIScreen screen && screen.FocusedObject != null)
                return ParentScreen.FocusedObject.InternalHandleTextInput(key, gameTimer);

            return false;
        }
        #endregion

        public override string ToString()
        {
            return $"{GetType().Name} - {Name} [{Bounds}]";
        }

    } // UIObject
}
