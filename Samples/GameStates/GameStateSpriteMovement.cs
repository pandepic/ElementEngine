using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ElementEngine;
using ElementEngine.ECS;
using ImGuiNET;
using Veldrid;

namespace Samples
{
    internal class GameStateSpriteMovement : GameState
    {
        public SpriteBatch2D SpriteBatch;
        public Registry Registry;
        public SystemManager UpdateSystems;
        public SystemManager DrawSystems;
        public Entity BallEntity;
        public int MoveSpeed = 75;

        internal GameStateSpriteMovement()
        {
            SpriteBatch = new();
        }

        public override void Load()
        {
            Registry = new();
            UpdateSystems = new();
            DrawSystems = new();

            UpdateSystems.AddSystem(new MoveToSystem(Registry));
            DrawSystems.AddSystem(new SpriteSystem(Registry, SpriteBatch));

            var ballTexture = AssetManager.Instance.LoadTexture2D("Textures/Ball.png");

            BallEntity = Registry.CreateEntity();
            BallEntity.TryAddComponent(new Sprite()
            {
                Texture = ballTexture,
                Origin = ballTexture.SizeF / 2f,
            });
            BallEntity.TryAddComponent(new Transform()
            {
                Position = new Vector2(250, 250),
            });
        }

        public override void Unload()
        {
        }

        public override void Update(GameTimer gameTimer)
        {
            UpdateSystems.Run(gameTimer);
            Registry.SystemsFinished();
        }

        public override void Draw(GameTimer gameTimer)
        {
            DrawSystems.Run(gameTimer);

            SpriteBatch.Begin(SamplerType.Point);
            SpriteBatch.DrawText(Globals.DebugFont, LocalisationManager.GetString("SpriteMovementInstructions"), new Vector2(50, 50), RgbaByte.White, 32, 1);
            SpriteBatch.End();

            ref var ballTransform = ref BallEntity.GetComponent<Transform>();
            
            ImGui.Begin("Ball Debug", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.InputFloat("X", ref ballTransform.Position.X);
            ImGui.InputFloat("Y", ref ballTransform.Position.Y);
            ImGui.InputInt("Move Speed", ref MoveSpeed);
            ImGui.End();
        }

        public override void HandleGameControl(string controlName, GameControlState state, GameTimer gameTimer)
        {
            switch (controlName)
            {
                case "MouseInteract":
                    {
                        BallEntity.TryAddComponent(new MoveTo()
                        {
                            Target = InputManager.MousePosition,
                            Velocity = new Vector2(MoveSpeed),
                        });
                    }
                    break;
            }
        }
    }
}
