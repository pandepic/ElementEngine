using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PandaEngine
{
    public class Camera2D
    {
        public float Rotation { get; set; } = 0f;
        public float Zoom { get; set; } = 1f;

        protected Rectangle _boundingBox = Rectangle.Empty;
        public Rectangle BoundingBox { get => _boundingBox; }
        
        protected Rectangle _view = Rectangle.Empty;
        public Rectangle View { get => _view; }

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

        protected Vector2 _origin = Vector2.Zero;
        public Vector2 Velocity = Vector2.Zero;

        public Camera2D() { }

        public Camera2D(Rectangle view, Rectangle? boundingBox = null)
        {
            _view = view;
            _origin = new Vector2(view.Width / 2f, view.Height / 2f);
            _position.X = _view.X;
            _position.Y = _view.Y;

            if (boundingBox.HasValue)
                _boundingBox = boundingBox.Value;
            else
                _boundingBox = Rectangle.Empty;
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
                    Matrix4x4.CreateTranslation(new Vector3(-_origin, z)) *
                    Matrix4x4.CreateScale(Zoom, Zoom, 1) *
                    Matrix4x4.CreateRotationZ(Rotation) *
                    Matrix4x4.CreateTranslation(new Vector3(_origin, z));
        }

        public void CheckBoundingBox()
        {
            if (_boundingBox.IsEmpty || _boundingBox == null)
                return;

            _view.X = (int)_position.X;
            _view.Y = (int)_position.Y;

            if (_view.X < BoundingBox.X)
                _position.X = BoundingBox.X;
            if (_view.X + _view.Width > BoundingBox.X + BoundingBox.Width)
                _position.X = (BoundingBox.X + BoundingBox.Width) - _view.Width;
            if (_view.Y < BoundingBox.Y)
                _position.Y = BoundingBox.Y;
            if ((_view.Y + _view.Height) > (BoundingBox.Y + BoundingBox.Height))
                _position.Y = (BoundingBox.Y + BoundingBox.Height) - _view.Height;
        } // CheckBoundingBox

    } // Camera2D
}
