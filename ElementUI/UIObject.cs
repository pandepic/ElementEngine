using System;
using System.Collections.Generic;
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

        public int Width => (int)_size.X;
        public int Height => (int)_size.Y;

        public Vector2 Position
        {
            get => _position;
            set
            {
                _uiPosition.Position = value;
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

        public float SizeX
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

        public float SizeY
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

        public Rectangle Bounds
        {
            get => new Rectangle(_position, _size);
        }

        public Rectangle PaddingBounds
        {
            get => new Rectangle(_position + _padding.TopLeftF, _size - _padding.TopLeftF - _padding.BottomRightF);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value)
                    Disable();
                else
                    Enable();
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
        #endregion

        public UIObject(string name)
        {
            Name = name;
        }

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

        public List<T> FindChildrenByName<T>(string name, bool recursive, bool clearList = true) where T : UIObject
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
            if (!_uiSize.IsAutoSizedX)
                SizeX = size.X;
            if (!_uiSize.IsAutoSizedY)
                SizeY = size.Y;
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

        #region Positioning Properties

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

        public void Center()
        {
            _uiPosition.CenterX = true;
            _uiPosition.CenterY = true;
            _layoutDirty = true;
        }

        internal virtual void CheckLayout()
        {
            if (_layoutDirty)
            {
                UpdateLayout();
                _layoutDirty = false;
            }

            foreach (var child in Children)
                child.CheckLayout();
        }

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
            CheckLayout();

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

        internal virtual void UpdateLayout()
        {
            _size = _uiSize.GetSize(this);
            _position = _uiPosition.GetPosition(this);
            _childOrigin = _position + _padding.TopLeftF;

            foreach (var child in Children)
                child.UpdateLayout();
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

    } // UIObject
}
