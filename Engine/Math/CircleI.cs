using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public struct CircleI
    {
        public Vector2I Center;
        public int Radius;

        public CircleI(Vector2I center, int radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
