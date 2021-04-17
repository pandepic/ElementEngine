using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ECS.CommonComponents
{
    public struct TransformComponent
    {
        public Entity Parent;
        public float Rotation;

        private Vector2 _position;
        public Vector2 Position
        {
            get
            {
                if (!Parent.IsAlive)
                    return _position;
                else
                {
                    ref var parentTransform = ref Parent.GetComponent<TransformComponent>();
                    var transformMatrix =
                        Matrix3x2.CreateRotation(parentTransform.Rotation) *
                        Matrix3x2.CreateTranslation(parentTransform.Position);

                    return Vector2.Transform(_position, transformMatrix);
                }
            }

            set
            {
                _position = value;
            }
        }
    } // TransformComponent
}
