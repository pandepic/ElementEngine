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
        public Vector2 Position;
        public Vector2 SectorPosition;

        public Vector2 TransformedPosition
        {
            get
            {
                if (!Parent.IsAlive)
                    return Position;
                else
                {
                    ref var parentTransform = ref Parent.GetComponent<TransformComponent>();
                    var transformMatrix =
                        Matrix3x2.CreateRotation(parentTransform.Rotation) *
                        Matrix3x2.CreateTranslation(parentTransform.TransformedPosition);

                    return Vector2.Transform(Position, transformMatrix);
                }
            }
        }

        public Vector2 TransformedSectorPosition
        {
            get
            {
                if (!Parent.IsAlive)
                    return SectorPosition;
                else
                {
                    ref var parentTransform = ref Parent.GetComponent<TransformComponent>();
                    return parentTransform.TransformedSectorPosition;
                }
            }
        }
    } // TransformComponent
}
