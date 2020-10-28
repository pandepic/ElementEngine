using PandaEngine;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace SwordsAndSorceryGame
{
    public class Game : BaseGame
    {
        protected SpriteBatch2D _spriteBatch;
        protected Texture2D _logo;
        protected Vector2 _camera = Vector2.Zero;
        protected Vector2 _pos = Vector2.Zero;

        public override void Load()
        {
            var windowRect = new PandaEngine.Rectangle()
            {
                X = 100,
                Y = 100,
                Width = 1600,
                Height = 900
            };

            SetupWindow(windowRect, "Swords and Sorcery", GraphicsBackend.Direct3D11);
            SetupAssets();
            ClearColour = RgbaFloat.CornflowerBlue;

            Window.Resizable = false;

            _spriteBatch = new SpriteBatch2D();
            _logo = AssetManager.LoadTexture2D("Logo");
            _pos = new Vector2(100, 100f);
        }

        public override void Update(GameTimer gameTimer)
        {
            //_camera.Y += 0.00001f;
            //_pos.X -= 0.01f;
            //_pos.Y -= 0.01f;
            if (PandaGlobals.InputManager.InputSnapshot == null)
                return;

            foreach (var e in PandaGlobals.InputManager.InputSnapshot.KeyEvents)
            {
                if (e.Down && e.Key == Key.Up)
                    _pos.Y -= 10f;
                if (e.Down && e.Key == Key.Down)
                    _pos.Y += 10f;

                if (e.Down && e.Key == Key.Left)
                    _pos.X -= 10f;
                if (e.Down && e.Key == Key.Right)
                    _pos.X += 10f;
            }
        }

        public override void Draw(GameTimer gameTimer)
        {
            _spriteBatch.Begin(eSamplerType.Point);//, Matrix4x4.CreateTranslation(new Vector3(_camera, 0f)));
            _spriteBatch.Draw(_logo, _pos);
            _spriteBatch.End();
        }
    } // Game
}
