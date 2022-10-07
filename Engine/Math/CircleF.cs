using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public struct CircleF
    {
        public Vector2 Center;
        public float Radius;

        public CircleF(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
