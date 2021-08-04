using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UITextbox : UIObject
    {
        protected static readonly Texture2D _cursorTexture;
        protected static Clipboard _clipboard = new Clipboard();

        public new UITextboxStyle Style => (UITextboxStyle)_style;
        public event Action<UIOnValueChangedArgs<string>> OnValueChanged;

        public readonly UILabel Label;
        public readonly UILabel DummyLabel;

        protected int _cursorIndex;
        public int CursorIndex
        {
            get => _cursorIndex;
            protected set
            {
                _cursorIndex = Math.Clamp(value, 0, Text.Length);
            }
        }

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

            return new Vector2I(
                Text.Length == 0 ? 0 : (Label.CurrentFont.MeasureText(Text.Substring(0, CursorIndex), Label.Style.FontSize, Label.Style.Outline).ToVector2I().X),
                0) + measureLabel.Position;
        }

        public override void Update(GameTimer gameTimer)
        {
            Style.BackgroundNormal?.Update(gameTimer);
            Style.BackgroundDisabled?.Update(gameTimer);

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            var sprite = IsActive ? Style.BackgroundNormal : Style.BackgroundDisabled;
            sprite?.Draw(this, spriteBatch, DrawPosition, _size);

            base.Draw(spriteBatch);
        }

        protected override void InnerPostDraw(SpriteBatch2D spriteBatch)
        {
            if (IsFocused)
            {
                CheckCursorPosition();

                var measureLabel = Text.Length > 0 ? Label : DummyLabel;
                var cursorHeight = Style.CursorHeight ?? measureLabel.TextSize.Y;

                if (!measureLabel._layoutDirty)
                    spriteBatch.DrawTexture2D(_cursorTexture, new Rectangle(0, 0, Style.CursorWidth, cursorHeight) + GetCursorPosition(), color: Style.CursorColor.ToRgbaFloat());
            }
        }

        public override bool InternalHandleKeyDown(Key key, GameTimer gameTimer)
        {
            switch (key)
            {
                case Key.Left:
                    CursorIndex -= 1;
                    break;

                case Key.Right:
                    CursorIndex += 1;
                    break;

                case Key.BackSpace:
                    {
                        if (CursorIndex > 0)
                        {
                            Text = Text.Remove(CursorIndex - 1, 1);
                            CursorIndex -= 1;
                        }
                    }
                    break;

                case Key.Delete:
                    {
                        if (CursorIndex < Text.Length)
                            Text = Text.Remove(CursorIndex, 1);
                    }
                    break;

                case Key.Home:
                    CursorIndex = 0;
                    break;

                case Key.End:
                    CursorIndex = Text.Length;
                    break;

                case Key.V:
                    {
                        if (InputManager.IsKeyDown(Key.ControlLeft) || InputManager.IsKeyDown(Key.ControlRight))
                        {
                            var clipboard = _clipboard.GetText();
                            Text = Text.Insert(CursorIndex, clipboard);
                            CursorIndex += clipboard.Length;
                        }
                    }
                    break;
            }

            return true;
        }

        public override bool InternalHandleTextInput(char key, GameTimer gameTimer)
        {
            Text = Text.Insert(CursorIndex, key.ToString());
            CursorIndex += 1;
            return true;
        }
    } // UITextbox
}
