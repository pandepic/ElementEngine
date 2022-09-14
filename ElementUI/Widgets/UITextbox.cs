using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TextCopy;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UITextbox : UIObject
    {
        protected static readonly Texture2D _cursorTexture;
        protected static readonly Texture2D _selectionTexture;
        protected static Clipboard _clipboard = new Clipboard();

        public new UITextboxStyle Style => (UITextboxStyle)_style;
        public event Action<UIOnValueChangedArgs<string>> OnValueChanged;

        public readonly UILabel Label;
        public readonly UILabel DummyLabel;

        public bool IsPressed { get; protected set; }

        protected int _cursorIndex;
        public int CursorIndex
        {
            get => _cursorIndex;
            protected set
            {
                _cursorIndex = Math.Clamp(value, 0, Text.Length);
            }
        }

        protected int _selectionIndexStart = -1;

        public string Text
        {
            get => Label.Text;
            set
            {
                var prev = Label.Text;
                Label.Text = value;

                if (prev != Label.Text)
                    OnValueChanged?.Invoke(new UIOnValueChangedArgs<string>(this, prev, Label.Text));
            }
        }

        static UITextbox()
        {
            if (_cursorTexture == null)
                _cursorTexture = new Texture2D(1, 1, RgbaByte.White);
            if (_selectionTexture == null)
                _selectionTexture = new Texture2D(1, 1, RgbaByte.White);
        }

        public UITextbox(string name, UITextboxStyle style, string text) : base(name)
        {
            ApplyStyle(style);
            ApplyDefaultSize(Style.BackgroundNormal);

            Label = new UILabel("", style.TextStyle, text);
            DummyLabel = new UILabel("", style.TextStyle, " ");
            CursorIndex = text.Length;

            AddChild(Label);
            AddChild(DummyLabel);

            DummyLabel.IsVisible = false;
        }

        protected void CheckCursorPosition()
        {
            if (Text.Length == 0)
                return;

            var cursorPosition = GetCursorPosition();

            if (cursorPosition.X > PaddingBounds.Right)
            {
                Label._uiPosition._internalOffset.X -= cursorPosition.X - PaddingBounds.Right + Style.CursorWidth;
                Label.SetLayoutDirty();
            }
            else if (cursorPosition.X < PaddingBounds.Left)
            {
                Label._uiPosition._internalOffset.X += PaddingBounds.Left + Style.CursorWidth - cursorPosition.X;
                Label.SetLayoutDirty();
            }
        }

        protected Vector2I GetCursorPosition()
        {
            var measureLabel = Text.Length > 0 ? Label : DummyLabel;
            _cursorIndex = Math.Clamp(_cursorIndex, 0, Text.Length);

            return new Vector2I(
                Text.Length == 0 ? 0 : (Label.CurrentFont.MeasureText(Label.DisplayText.Substring(0, CursorIndex), Label.Style.FontSize, Label.Style.Outline).ToVector2I().X),
                0) + measureLabel.Position;
        }

        protected override void InternalUpdate(GameTimer gameTimer)
        {
            Style.BackgroundNormal?.Update(gameTimer);
            Style.BackgroundDisabled?.Update(gameTimer);
        }

        protected override void InnerDraw(SpriteBatch2D spriteBatch)
        {
            var sprite = IsActive ? Style.BackgroundNormal : Style.BackgroundDisabled;
            sprite?.Draw(this, spriteBatch, DrawPosition, _size);
        }

        protected int GetBeginSelectionIndex() => Math.Min(CursorIndex, _selectionIndexStart);
        protected int GetEndSelectionIndex() => Math.Max(CursorIndex, _selectionIndexStart);
        public string GetSelectedText() => Text.Substring(GetBeginSelectionIndex(), GetEndSelectionIndex() - GetBeginSelectionIndex());

        protected override void InnerPostDraw(SpriteBatch2D spriteBatch)
        {
            if (IsFocused)
            {
                CheckCursorPosition();

                var cursorPosition = GetCursorPosition();
                var measureLabel = Text.Length > 0 ? Label : DummyLabel;
                var cursorHeight = Style.CursorHeight ?? measureLabel.TextSize.Y;

                if (Text.Length > 0 && _selectionIndexStart != CursorIndex && _selectionIndexStart != -1)
                {
                    var beginSelectionIndex = GetBeginSelectionIndex();
                    var endSelectionIndex = GetEndSelectionIndex();

                    var selectedText = Text.Substring(beginSelectionIndex, endSelectionIndex - beginSelectionIndex);
                    var selectedTextSize = Label.CurrentFont.MeasureText(selectedText, Label.Style.FontSize, Label.Style.Outline).ToVector2I();

                    var selectionStartX = CursorIndex > _selectionIndexStart ? cursorPosition.X - selectedTextSize.X : cursorPosition.X;
                    var selectionEndX = CursorIndex > _selectionIndexStart ? cursorPosition.X : cursorPosition.X + selectedTextSize.X;

                    spriteBatch.DrawTexture2D(_selectionTexture, new Rectangle(0, 0, selectionEndX - selectionStartX, cursorHeight) + new Vector2I(selectionStartX, cursorPosition.Y), color: Style.SelectionColor.ToRgbaFloat());
                }

                spriteBatch.DrawTexture2D(_cursorTexture, new Rectangle(0, 0, Style.CursorWidth, cursorHeight) + cursorPosition, color: Style.CursorColor.ToRgbaFloat());
            }
        }

        protected bool TryRemoveSelection()
        {
            if (_selectionIndexStart != -1 && _selectionIndexStart != CursorIndex)
            {
                var begin = GetBeginSelectionIndex();
                var end = GetEndSelectionIndex();

                Text = Text.Remove(begin, end - begin);

                if (CursorIndex == end)
                    CursorIndex = begin;

                _selectionIndexStart = -1;
                return true;
            }

            return false;
        }

        public override bool InternalHandleKeyDown(Key key, GameTimer gameTimer)
        {
            switch (key)
            {
                case Key.Left:
                    {
                        if (!InputManager.IsKeyDown(Key.ShiftLeft) && !InputManager.IsKeyDown(Key.ShiftRight))
                            _selectionIndexStart = -1;
                        else if (_selectionIndexStart == -1)
                            _selectionIndexStart = CursorIndex;

                        CursorIndex -= 1;
                    }
                    break;

                case Key.Right:
                    {
                        if (!InputManager.IsKeyDown(Key.ShiftLeft) && !InputManager.IsKeyDown(Key.ShiftRight))
                            _selectionIndexStart = -1;
                        else if (_selectionIndexStart == -1)
                            _selectionIndexStart = CursorIndex;

                        CursorIndex += 1;
                    }
                    break;

                case Key.BackSpace:
                    {
                        if (!TryRemoveSelection() && CursorIndex > 0)
                        {
                            Text = Text.Remove(CursorIndex - 1, 1);
                            CursorIndex -= 1;
                            _selectionIndexStart = -1;
                        }
                    }
                    break;

                case Key.Delete:
                    {
                        if (!TryRemoveSelection() && CursorIndex < Text.Length)
                        {
                            Text = Text.Remove(CursorIndex, 1);
                            _selectionIndexStart = -1;
                        }
                    }
                    break;

                case Key.Home:
                    {
                        if (!InputManager.IsKeyDown(Key.ShiftLeft) && !InputManager.IsKeyDown(Key.ShiftRight))
                            _selectionIndexStart = -1;
                        else if (_selectionIndexStart == -1)
                            _selectionIndexStart = CursorIndex;

                        CursorIndex = 0;
                    }
                    break;

                case Key.End:
                    {
                        if (!InputManager.IsKeyDown(Key.ShiftLeft) && !InputManager.IsKeyDown(Key.ShiftRight))
                            _selectionIndexStart = -1;
                        else if (_selectionIndexStart == -1)
                            _selectionIndexStart = CursorIndex;

                        CursorIndex = Text.Length;
                    }
                    break;

                case Key.C:
                    {
                        if (InputManager.IsKeyDown(Key.ControlLeft) || InputManager.IsKeyDown(Key.ControlRight))
                        {
                            _clipboard.SetText(GetSelectedText());
                        }
                    }
                    break;

                case Key.V:
                    {
                        if (InputManager.IsKeyDown(Key.ControlLeft) || InputManager.IsKeyDown(Key.ControlRight))
                        {
                            var clipboard = _clipboard.GetText();

                            if (clipboard.Length > 0)
                            {
                                TryRemoveSelection();
                                Text = Text.Insert(CursorIndex, clipboard);
                                CursorIndex += clipboard.Length;
                                _selectionIndexStart = -1;
                            }
                        }
                    }
                    break;

                case Key.A:
                    {
                        if (InputManager.IsKeyDown(Key.ControlLeft) || InputManager.IsKeyDown(Key.ControlRight))
                        {
                            CursorIndex = 0;
                            _selectionIndexStart = Text.Length;
                        }
                    }
                    break;
            }

            return true;
        }

        public override bool InternalHandleTextInput(char key, GameTimer gameTimer)
        {
            TryRemoveSelection();

            Text = Text.Insert(CursorIndex, key.ToString());
            CursorIndex += 1;
            _selectionIndexStart = -1;
            return true;
        }

        protected bool UpdateCursorIndexFromMouse(Vector2 mousePosition)
        {
            var cursorPosition = GetCursorPosition();
            if (mousePosition.X == cursorPosition.X)
                return true;

            var searchDirection = mousePosition.X < cursorPosition.X ? -1 : 1;

            if (searchDirection == -1 && CursorIndex == 0)
                return true;
            if (searchDirection == 1 && CursorIndex == Text.Length)
                return true;

            while (true)
            {
                if (searchDirection == -1 && CursorIndex == 0)
                    break;
                if (searchDirection == 1 && CursorIndex == Text.Length)
                    break;

                CursorIndex += searchDirection;
                var checkCursorPosition = GetCursorPosition();

                if (searchDirection == 1 && checkCursorPosition.X > mousePosition.X)
                {
                    CursorIndex -= 1;
                    break;
                }

                if (searchDirection == -1 && checkCursorPosition.X < mousePosition.X)
                    break;
            }

            return true;
        }

        internal override bool InternalHandleMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseMotion(mousePosition, prevMousePosition, gameTimer))
                return true;

            if (IsPressed)
                return UpdateCursorIndexFromMouse(mousePosition);

            return false;
        }

        internal override void InternalHandleNoMouseMotion(Vector2 mousePosition, Vector2 prevMousePosition, GameTimer gameTimer)
        {
            IsPressed = false;

            base.InternalHandleNoMouseMotion(mousePosition, prevMousePosition, gameTimer);
        }

        internal override bool InternalHandleMouseButtonPressed(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonPressed(mousePosition, button, gameTimer))
                return true;

            UpdateCursorIndexFromMouse(mousePosition);

            IsPressed = true;
            _selectionIndexStart = CursorIndex;

            return true;
        }

        internal override bool InternalHandleMouseButtonReleased(Vector2 mousePosition, MouseButton button, GameTimer gameTimer)
        {
            if (base.InternalHandleMouseButtonReleased(mousePosition, button, gameTimer))
                return true;

            if (IsPressed)
            {
                IsPressed = false;
                return true;
            }

            return false;
        }

    } // UITextbox
}
