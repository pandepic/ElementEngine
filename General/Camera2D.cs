using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine
{
    public class Camera2D
    {
        public float Rotation { get; set; } = 0f;
        public float Zoom { get; set; } = 1f;

        protected Rectangle _boundingBox = Rectangle.Empty;
        public Rectangle BoundingBox
        {
            get => _boundingBox;
            set
            {
                _boundingBox = value;
                CheckBoundingBox();
            }
        }
        
        protected Rectangle _view = Rectangle.Empty;
        public Rectangle View
        {
            get
            {
                _view.X = (int)_position.X;
                _view.Y = (int)_position.Y;
                return _view;
            }
        }

        public Rectangle ScaledView => View / Zoom;

        protected Vector2 _position = Vector2.Zero;
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                CheckBoundingBox();
            }
        }

        public float X
        {
            get => _position.X;
            set
            {
                _position.X = value;
                CheckBoundingBox();
            }
        }

        public float Y
        {
            get => _position.Y;
            set
            {
                _position.Y = value;
                CheckBoundingBox();
            }
        }

        public Vector2 Origin = Vector2.Zero;
        public Vector2 Velocity = Vector2.Zero;
        public Vector2 ScaledOrigin => Origin / Zoom;

        public Camera2D() { }

        public Camera2D(Rectangle view, Rectangle? boundingBox = null)
        {
            _view = view;
            Origin = new Vector2(view.Width / 2f, view.Height / 2f);
            _position.X = _view.X;
            _position.Y = _view.Y;

            if (boundingBox.HasValue)
                _boundingBox = boundingBox.Value;
            else
                _boundingBox = Rectangle.Empty;

            CheckBoundingBox();
        }

        public void Update(GameTimer gameTimer)
        {
            if (Velocity != Vector2.Zero)
            {
                _position += Velocity * gameTimer.DeltaS;
                CheckBoundingBox();
            }

            _view.X = (int)_position.X;
            _view.Y = (int)_position.Y;
        }

        public Matrix4x4 GetViewMatrix(float z = 0f)
        {
            return Matrix4x4.CreateTranslation(new Vector3(-_position, z)) *
                    Matrix4x4.CreateTranslation(new Vector3(-Origin, z)) *
                    Matrix4x4.CreateScale(Zoom, Zoom, 1) *
                    Matrix4x4.CreateRotationZ(Rotation) *
                    Matrix4x4.CreateTranslation(new Vector3(Origin, z));
        }

        public Matrix4x4 GetViewMatrixI(float z = 0f)
        {
            return Matrix4x4.CreateTranslation(new Vector3((-_position).ToVector2I(), z)) *
                    Matrix4x4.CreateTranslation(new Vector3((-Origin).ToVector2I(), z)) *
                    Matrix4x4.CreateScale(Zoom, Zoom, 1) *
                    Matrix4x4.CreateRotationZ(Rotation) *
                    Matrix4x4.CreateTranslation(new Vector3(Origin.ToVector2I(), z));
        }

        public void Center(Vector2I position)
        {
            Center(position.ToVector2());
        }

        public void Center(Vector2 position)
        {
            _position.X = position.X - _view.Width / 2;
            _position.Y = position.Y - _view.Height / 2;
            CheckBoundingBox();
        }

        public void CheckBoundingBox()
        {
            if (_boundingBox.IsEmpty)
                return;

            var worldCameraXY = ScreenToWorld(Vector2.Zero);
            var worldCameraWH = ScreenToWorld(new Vector2(BoundingBox.X * Zoom + _view.Width, BoundingBox.Y * Zoom + _view.Height));

            if (_boundingBox.Width * Zoom > _view.Width)
            {
                if (worldCameraXY.X < BoundingBox.X)
                    _position.X += (worldCameraXY.X * -1.0f) + BoundingBox.X;
                if (worldCameraWH.X > BoundingBox.Right)
                    _position.X -= (worldCameraWH.X - BoundingBox.Right);
            }
            if (_boundingBox.Height * Zoom > _view.Height)
            {
                if (worldCameraXY.Y < BoundingBox.Y)
                    _position.Y += (worldCameraXY.Y * -1.0f) + BoundingBox.Y;
                if (worldCameraWH.Y > BoundingBox.Bottom)
                    _position.Y -= (worldCameraWH.Y - BoundingBox.Bottom);
            }

            _view.X = (int)_position.X;
            _view.Y = (int)_position.Y;

        } // CheckBoundingBox

        public Vector2 ScreenToWorld(Vector2 position)
        {
            Matrix4x4.Invert(GetViewMatrix(), out var inverted);
            return Vector2.Transform(position, inverted);
        }

        public Rectangle ScreenToWorld(Rectangle rect)
        {
            Matrix4x4.Invert(GetViewMatrix(), out var inverted);
            return new Rectangle(Vector2.Transform(rect.LocationF, inverted), rect.SizeF);
        }

        public Vector2 WorldToScreen(Vector2 position)
        {
            return Vector2.Transform(position, GetViewMatrix());
        }

        public Rectangle WorldToScreen(Rectangle rect)
        {
            return new Rectangle(Vector2.Transform(rect.LocationF, GetViewMatrix()), rect.SizeF);
        }

        public override string ToString()
        {
            return _view.ToString();
        }

    } // Camera2D
}
