using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine.ElementUI
{
    public class UIScreen : UIObject
    {
        public UIScreen(Vector2? position = null, Vector2? size = null, string name = "Screen") : base(name)
        {
            _uiPosition.Position = position ?? Vector2.Zero;
            _uiSize.Size = size ?? new Vector2(ElementGlobals.TargetResolutionWidth, ElementGlobals.TargetResolutionHeight);

            UpdateLayout();
            Enable();
        }

        public override void Enable()
        {
            base.Enable();
            InputManager.AddKeyboardHandler(this);
            InputManager.AddMouseHandler(this);
        }

        public override void Disable()
        {
            base.Disable();
            InputManager.RemoveKeyboardHandler(this);
            InputManager.RemoveMouseHandler(this);
        }

        public override void Update(GameTimer gameTimer)
        {
            CheckLayout();

            if (!IsActive)
                return;

            base.Update(gameTimer);
        }

        public override void Draw(SpriteBatch2D spriteBatch)
        {
            if (!IsVisible)
                return;

            base.Draw(spriteBatch);
        }

    } // UIScreen
}
