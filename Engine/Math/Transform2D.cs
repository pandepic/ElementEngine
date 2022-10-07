using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine
{
    //public class Transform2D
    //{
    //    public Transform2D Parent;
    //    public float Rotation;

    //    protected Vector2 _position;
    //    public Vector2 Position
    //    {
    //        get
    //        {
    //            if (Parent == null)
    //                return _position;

    //            var transformMatrix =
    //                    Matrix3x2.CreateRotation(Parent.Rotation) *
    //                    Matrix3x2.CreateTranslation(Parent.Position);

    //            return Vector2.Transform(Position, transformMatrix);
    //        }

    //        set
    //        {
    //            _position = value;
    //        }
    //    }

    //    public Transform2D(Vector2 position, float rotation = 0f, Transform2D parent = null)
    //    {
    //        _position = position;
    //        Rotation = rotation;
    //        Parent = parent;
    //    }
    //} // Transform2D
}
