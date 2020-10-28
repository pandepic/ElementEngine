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
            _logo = AssetManager.LoadTexture2D("logo.png");
        }

        public override void Update(GameTimer gameTimer)
        {
        }

        public override void Draw(GameTimer gameTimer)
        {
            _spriteBatch.Begin(eSamplerType.Point);
            _spriteBatch.Draw(_logo, new Vector2(50, 50));
            _spriteBatch.End();
        }
    } // Game
}
