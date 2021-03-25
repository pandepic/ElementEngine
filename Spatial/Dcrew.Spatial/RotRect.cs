//https://github.com/DeanReynolds/Dcrew.Spatial

using ElementEngine;
using System;
using System.Numerics;

namespace Dcrew.Spatial
{
    /// <summary>A rotated rectangle.</summary>
    public struct RotRect
    {
        /// <summary>Coordinates of this <see cref="RotRect"/>.</summary>
        public Vector2 XY;
        /// <summary>Size of bounds.</summary>
        public Vector2 Size;
        /// <summary>Rotation (in radians) of this <see cref="RotRect"/>.</summary>
        public float Rotation;
        /// <summary>Center of rotation of this <see cref="RotRect"/>.</summary>
        public Vector2 Origin;

        /// <summary>X coordinate of this <see cref="RotRect"/>.</summary>
        public float X
        {
            get => XY.X;
            set => XY.X = value;
        }
        /// <summary>Y coordinate of this <see cref="RotRect"/>.</summary>
        public float Y
        {
            get => XY.Y;
            set => XY.Y = value;
        }
        /// <summary>X size of this <see cref="RotRect"/>.</summary>
        public float Width
        {
            get => Size.X;
            set => Size.X = value;
        }
        /// <summary>Y size coordinate of this <see cref="RotRect"/>.</summary>
        public float Height
        {
            get => Size.Y;
            set => Size.Y = value;
        }

        /// <summary>A <see cref="Vector2"/> located in the center of this <see cref="RotRect"/>.</summary>
        public Vector2 Center
        {
            get
            {
                float cos = MathF.Cos(Rotation),
                    sin = MathF.Sin(Rotation),
                    x = -Origin.X,
                    y = -Origin.Y,
                    w = Size.X + x,
                    h = Size.Y + y,
                    xcos = x * cos,
                    ycos = y * cos,
                    xsin = x * sin,
                    ysin = y * sin,
                    wcos = w * cos,
                    wsin = w * sin,
                    hcos = h * cos,
                    hsin = h * sin;
                return new Vector2((((xcos - ysin) + (wcos - ysin) + (wcos - hsin) + (xcos - hsin)) / 4) + XY.X, (((xsin + ycos) + (wsin + ycos) + (wsin + hcos) + (xsin + hcos)) / 4) + XY.Y);
            }
        }
        /// <summary>A <see cref="Rectangle"/> covering the min/max coordinates (bounds) of this <see cref="RotRect"/>.</summary>
        public RectangleL AABB
        {
            get
            {
                float cos = MathF.Cos(Rotation),
                    sin = MathF.Sin(Rotation),
                    x = -Origin.X,
                    y = -Origin.Y,
                    w = Size.X + x,
                    h = Size.Y + y,
                    xcos = x * cos,
                    ycos = y * cos,
                    xsin = x * sin,
                    ysin = y * sin,
                    wcos = w * cos,
                    wsin = w * sin,
                    hcos = h * cos,
                    hsin = h * sin,
                    tlx = xcos - ysin,
                    tly = xsin + ycos,
                    trx = wcos - ysin,
                    tr_y = wsin + ycos,
                    brx = wcos - hsin,
                    bry = wsin + hcos,
                    blx = xcos - hsin,
                    bly = xsin + hcos,
                    minx = tlx,
                    miny = tly,
                    maxx = minx,
                    maxy = miny;
                if (trx < minx)
                    minx = trx;
                if (brx < minx)
                    minx = brx;
                if (blx < minx)
                    minx = blx;
                if (tr_y < miny)
                    miny = tr_y;
                if (bry < miny)
                    miny = bry;
                if (bly < miny)
                    miny = bly;
                if (trx > maxx)
                    maxx = trx;
                if (brx > maxx)
                    maxx = brx;
                if (blx > maxx)
                    maxx = blx;
                if (tr_y > maxy)
                    maxy = tr_y;
                if (bry > maxy)
                    maxy = bry;
                if (bly > maxy)
                    maxy = bly;
                var r = new RectangleL((long)minx, (long)miny, (long)MathF.Ceiling(maxx - minx), (long)MathF.Ceiling(maxy - miny));
                r.Location += new Vector2L(XY);
                return r;
            }
        }

        /// <summary>Creates a new instance of <see cref="Rectangle"/> struct, with the specified position, width, height, angle, and origin.</summary>
        /// <param name="x">X coordinate of the created <see cref="RotRect"/>.</param>
        /// <param name="y">Y coordinate of the created <see cref="RotRect"/>.</param>
        /// <param name="width">X size of the created <see cref="RotRect"/>.</param>
        /// <param name="height">Y size of the created <see cref="RotRect"/>.</param>
        /// <param name="rotation">Rotation (in radians) of the created <see cref="RotRect"/>.</param>
        /// <param name="origin">Center of rotation of the created <see cref="RotRect"/>.</param>
        public RotRect(float x, float y, float width, float height, float rotation = default, Vector2 origin = default)
        {
            XY = new Vector2(x, y);
            Size = new Vector2(width, height);
            Rotation = rotation;
            Origin = origin;
        }
        /// <summary>Creates a new instance of <see cref="Rectangle"/> struct, with the specified position, width, height, angle, and origin.</summary>
        /// <param name="x">X coordinate of the created <see cref="RotRect"/>.</param>
        /// <param name="y">Y coordinate of the created <see cref="RotRect"/>.</param>
        /// <param name="size">Size of the created <see cref="RotRect"/>.</param>
        /// <param name="rotation">Rotation (in radians) of the created <see cref="RotRect"/>.</param>
        /// <param name="origin">Center of rotation of the created <see cref="RotRect"/>.</param>
        public RotRect(float x, float y, Vector2 size, float rotation = default, Vector2 origin = default) : this(x, y, size.X, size.Y, rotation, origin) { }
        /// <summary>Creates a new instance of <see cref="Rectangle"/> struct, with the specified position, width, height, angle, and origin.</summary>
        /// <param name="xy">Coordinates of the created <see cref="RotRect"/>.</param>
        /// <param name="size">Size of the created <see cref="RotRect"/>.</param>
        /// <param name="rotation">Rotation (in radians) of the created <see cref="RotRect"/>.</param>
        /// <param name="origin">Center of rotation of the created <see cref="RotRect"/>.</param>
        public RotRect(Vector2 xy, Vector2 size, float rotation = default, Vector2 origin = default) : this(xy.X, xy.Y, size.X, size.Y, rotation, origin) { }
        /// <summary>Creates a new instance of <see cref="Rectangle"/> struct, with the specified position, width, height, angle, and origin.</summary>
        /// <param name="rectangle">Area of the created <see cref="RotRect"/>.</param>
        /// <param name="rotation">Rotation (in radians) of the created <see cref="RotRect"/>.</param>
        /// <param name="origin">Center of rotation of the created <see cref="RotRect"/>.</param>
        public RotRect(RectangleL rectangle, float rotation = default, Vector2 origin = default) : this(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, rotation, origin) { }

        /// <summary>Gets whether or not the other <see cref="Rectangle"/> intersects with this rectangle.</summary>
        /// <param name="value">The other rectangle for testing.</param>
        /// <returns><c>true</c> if other <see cref="Rectangle"/> intersects with this rectangle; <c>false</c> otherwise.</returns>
        public bool Intersects(RectangleL value) => Intersects(new RotRect(value.Location.ToVector2(), value.Size.ToVector2(), 0, Vector2.Zero));
        /// <summary>Gets whether or not the other <see cref="RotRect"/> intersects with this rectangle.</summary>
        /// <param name="value">The other rectangle for testing.</param>
        /// <returns><c>true</c> if other <see cref="RotRect"/> intersects with this rectangle; <c>false</c> otherwise.</returns>
        public bool Intersects(RotRect value) => IntersectsAnyEdge(value) || value.IntersectsAnyEdge(this);

        /// <summary>Gets whether or not the provided <see cref="Vector2"/> lies within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="value">The coordinates to check for inclusion in this <see cref="RotRect"/>.</param>
        /// <returns><c>true</c> if the provided <see cref="Vector2"/> lies inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(Vector2 value)
        {
            static float IsLeft(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
            float cos = MathF.Cos(Rotation),
                sin = MathF.Sin(Rotation),
                x = -Origin.X,
                y = -Origin.Y,
                w = Size.X + x,
                h = Size.Y + y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin,
                wcos = w * cos,
                wsin = w * sin,
                hcos = h * cos,
                hsin = h * sin;
            Vector2 tl = new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y),
                tr = new Vector2(wcos - ysin + XY.X, wsin + ycos + XY.Y),
                br = new Vector2(wcos - hsin + XY.X, wsin + hcos + XY.Y),
                bl = new Vector2(xcos - hsin + XY.X, xsin + hcos + XY.Y);
            return IsLeft(tl, tr, value) > 0 && IsLeft(tr, br, value) > 0 && IsLeft(br, bl, value) > 0 && IsLeft(bl, tl, value) > 0;
        }
        /// <summary>Gets whether or not the provided <see cref="Point"/> lies within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="value">The coordinates to check for inclusion in this <see cref="RotRect"/>.</param>
        /// <returns><c>true</c> if the provided <see cref="Point"/> lies inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(Vector2I value) => Contains(value.ToVector2());
        /// <summary>Gets whether or not the provided coordinates lie within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="x">The x coordinate of the point to check for containment.</param>
        /// <param name="y">The y coordinate of the point to check for containment.</param>
        /// <returns><c>true</c> if the provided coordinates lie inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(int x, int y) => Contains(new Vector2(x, y));
        /// <summary>Gets whether or not the provided coordinates lie within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="x">The x coordinate of the point to check for containment.</param>
        /// <param name="y">The y coordinate of the point to check for containment.</param>
        /// <returns><c>true</c> if the provided coordinates lie inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(float x, float y) => Contains(new Vector2(x, y));
        /// <summary>Gets whether or not the provided <see cref="Rectangle"/> lies within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="value">The <see cref="Rectangle"/> to check for inclusion in this <see cref="RotRect"/>.</param>
        /// <returns><c>true</c> if the provided <see cref="Rectangle"/>'s bounds lie entirely inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(RectangleL value) => Contains(new RotRect(value.Location.ToVector2(), value.Size.ToVector2(), 0, Vector2.Zero));
        /// <summary>Gets whether or not the provided <see cref="RotRect"/> lies within the bounds of this <see cref="RotRect"/>.</summary>
        /// <param name="value">The <see cref="RotRect"/> to check for inclusion in this <see cref="RotRect"/>.</param>
        /// <returns><c>true</c> if the provided <see cref="RotRect"/>'s bounds lie entirely inside this <see cref="RotRect"/>; <c>false</c> otherwise.</returns>
        public bool Contains(RotRect value)
        {
            static float IsLeft(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
            static bool PointInRectangle(Vector2 x, Vector2 y, Vector2 z, Vector2 w, Vector2 p) => IsLeft(x, y, p) > 0 && IsLeft(y, z, p) > 0 && IsLeft(z, w, p) > 0 && IsLeft(w, x, p) > 0;
            float cos = MathF.Cos(Rotation),
             sin = MathF.Sin(Rotation),
             x = -Origin.X,
             y = -Origin.Y,
             w = Size.X + x,
             h = Size.Y + y,
             xcos = x * cos,
             ycos = y * cos,
             xsin = x * sin,
             ysin = y * sin,
             wcos = w * cos,
             wsin = w * sin,
             hcos = h * cos,
             hsin = h * sin;
            Vector2 tl2 = new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y),
             tr2 = new Vector2(wcos - ysin + XY.X, wsin + ycos + XY.Y),
             br2 = new Vector2(wcos - hsin + XY.X, wsin + hcos + XY.Y),
             bl2 = new Vector2(xcos - hsin + XY.X, xsin + hcos + XY.Y);
            cos = MathF.Cos(value.Rotation);
            sin = MathF.Sin(value.Rotation);
            x = -value.Origin.X;
            y = -value.Origin.Y;
            w = value.Size.X + x;
            h = value.Size.Y + y;
            xcos = x * cos;
            ycos = y * cos;
            xsin = x * sin;
            ysin = y * sin;
            wcos = w * cos;
            wsin = w * sin;
            hcos = h * cos;
            hsin = h * sin;
            Vector2 tl = new Vector2(xcos - ysin + value.XY.X, xsin + ycos + value.XY.Y),
             tr = new Vector2(wcos - ysin + value.XY.X, wsin + ycos + value.XY.Y),
             br = new Vector2(wcos - hsin + value.XY.X, wsin + hcos + value.XY.Y),
             bl = new Vector2(xcos - hsin + value.XY.X, xsin + hcos + value.XY.Y);
            return PointInRectangle(tl2, tr2, br2, bl2, tl) && PointInRectangle(tl2, tr2, br2, bl2, tr) && PointInRectangle(tl2, tr2, br2, bl2, br) && PointInRectangle(tl2, tr2, br2, bl2, bl);
        }

        /// <summary>Adjusts the edges of this <see cref="RotRect"/> by specified horizontal and vertical amounts.</summary>
        /// <param name="horizontal">Value to adjust the left and right edges.</param>
        /// <param name="vertical">Value to adjust the top and bottom edges.</param>
        public void Inflate(float horizontal, float vertical)
        {
            Size = new Vector2(horizontal * 2 + Size.X, vertical * 2 + Size.Y);
            Origin = new Vector2(horizontal + Origin.X, vertical + Origin.Y);
        }
        /// <summary>Adjusts the edges of this <see cref="RotRect"/> by specified horizontal and vertical amounts.</summary>
        /// <param name="value">Value to adjust all edges.</param>
        public void Inflate(Vector2 value) => Inflate(value.X, value.Y);
        /// <summary>Adjusts the edges of this <see cref="RotRect"/> by specified horizontal and vertical amounts.</summary>
        /// <param name="value">Value to adjust all edges.</param>
        public void Inflate(Vector2I value) => Inflate(value.X, value.Y);

        /// <summary>Changes the <see cref="XY"/> of this <see cref="RotRect"/>.</summary>
        /// <param name="offsetX">The x coordinate to add to this <see cref="RotRect"/>.</param>
        /// <param name="offsetY">The y coordinate to add to this <see cref="RotRect"/>.</param>
        public void Offset(float offsetX, float offsetY) => XY = new Vector2(XY.X + offsetX, XY.Y + offsetY);
        /// <summary>Changes the <see cref="XY"/> of this <see cref="RotRect"/>.</summary>
        /// <param name="amount">The x and y components to add to this <see cref="RotRect"/>.</param>
        public void Offset(Vector2 amount) => Offset(amount.X, amount.Y);
        /// <summary>Changes the <see cref="XY"/> of this <see cref="RotRect"/>.</summary>
        /// <param name="amount">The x and y components to add to this <see cref="RotRect"/>.</param>
        public void Offset(Vector2I amount) => Offset(amount.X, amount.Y);

        /// <summary>Find the closest point to the given position from within this <see cref="RotRect"/>.</summary>
        /// <param name="xy">Position.</param>
        /// <returns><see cref="Vector2"/> closest to <paramref name="xy"/> that lies within this <see cref="RotRect"/>.</returns>
        public Vector2 ClosestPoint(Vector2 xy)
        {
            float cos = MathF.Cos(-Rotation),
                sin = MathF.Sin(-Rotation),
                x = xy.X - XY.X,
                y = xy.Y - XY.Y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin;
            var p = new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y);
            p.X = MathHelper.Clamp(p.X, XY.X - Origin.X, XY.X + Size.X - Origin.X);
            p.Y = MathHelper.Clamp(p.Y, XY.Y - Origin.Y, XY.Y + Size.Y - Origin.Y);
            cos = MathF.Cos(Rotation);
            sin = MathF.Sin(Rotation);
            x = p.X - XY.X;
            y = p.Y - XY.Y;
            xcos = x * cos;
            ycos = y * cos;
            xsin = x * sin;
            ysin = y * sin;
            return new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y);
        }
        /// <summary>Find the closest corner point of this <see cref="RotRect"/> to the given position.</summary>
        /// <param name="xy">Position.</param>
        /// <returns><see cref="Vector2"/> closest corner of this <see cref="RotRect"/> to <paramref name="xy"/>.</returns>
        public Vector2 ClosestCornerPoint(Vector2 xy)
        {
            float cos = MathF.Cos(-Rotation),
                sin = MathF.Sin(-Rotation),
                x = xy.X - XY.X,
                y = xy.Y - XY.Y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin;
            var p = new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y);
            float cXMin = XY.X - Origin.X,
                cXMax = XY.X + Size.X - Origin.X,
                cYMin = XY.Y - Origin.Y,
                cYMax = XY.Y + Size.Y - Origin.Y;
            if (p.X - cXMin < cXMax - p.X)
                p.X = cXMin;
            else
                p.X = cXMax;
            if (p.Y - cYMin < cYMax - p.Y)
                p.Y = cYMin;
            else
                p.Y = cYMax;
            cos = MathF.Cos(Rotation);
            sin = MathF.Sin(Rotation);
            x = p.X - XY.X;
            y = p.Y - XY.Y;
            xcos = x * cos;
            ycos = y * cos;
            xsin = x * sin;
            ysin = y * sin;
            return new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y);
        }

        bool IntersectsAnyEdge(RotRect value)
        {
            static float IsLeft(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (p.X - a.X) * (b.Y - a.Y);
            static bool PointInRectangle(Vector2 x, Vector2 y, Vector2 z, Vector2 w, Vector2 p) => IsLeft(x, y, p) > 0 && IsLeft(y, z, p) > 0 && IsLeft(z, w, p) > 0 && IsLeft(w, x, p) > 0;
            float cos = MathF.Cos(Rotation),
                sin = MathF.Sin(Rotation),
                x = -Origin.X,
                y = -Origin.Y,
                w = Size.X + x,
                h = Size.Y + y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin,
                wcos = w * cos,
                wsin = w * sin,
                hcos = h * cos,
                hsin = h * sin;
            Vector2 tl = new Vector2(xcos - ysin + XY.X, xsin + ycos + XY.Y),
               tr = new Vector2(wcos - ysin + XY.X, wsin + ycos + XY.Y),
               br = new Vector2(wcos - hsin + XY.X, wsin + hcos + XY.Y),
               bl = new Vector2(xcos - hsin + XY.X, xsin + hcos + XY.Y),
               center = new Vector2((((xcos - ysin) + (wcos - ysin) + (wcos - hsin) + (xcos - hsin)) / 4) + XY.X, (((xsin + ycos) + (wsin + ycos) + (wsin + hcos) + (xsin + hcos)) / 4) + XY.Y);
            float rvCos = MathF.Cos(-value.Rotation),
                rvSin = MathF.Sin(-value.Rotation),
                vCos = MathF.Cos(value.Rotation),
                vSin = MathF.Sin(value.Rotation),
                vX = value.X,
                vY = value.Y,
                vMinX = value.X - value.Origin.X,
                vMinY = value.Y - value.Origin.Y,
                vMaxX = value.X + value.Width - value.Origin.X,
                vMaxY = value.Y + value.Height - value.Origin.Y;
            Vector2 ClosestPoint(Vector2 xy)
            {
                float x = xy.X - vX,
                    y = xy.Y - vY,
                    xcos = x * rvCos,
                    ycos = y * rvCos,
                    xsin = x * rvSin,
                    ysin = y * rvSin;
                var p = new Vector2(xcos - ysin + vX, xsin + ycos + vY);
                p.X = MathHelper.Clamp(p.X, vMinX, vMaxX);
                p.Y = MathHelper.Clamp(p.Y, vMinY, vMaxY);
                x = p.X - vX;
                y = p.Y - vY;
                xcos = x * vCos;
                ycos = y * vCos;
                xsin = x * vSin;
                ysin = y * vSin;
                return new Vector2(xcos - ysin + vX, xsin + ycos + vY);
            }
            return PointInRectangle(tl, tr, br, bl, ClosestPoint(center)) || PointInRectangle(tl, tr, br, bl, ClosestPoint(tl)) || PointInRectangle(tl, tr, br, bl, ClosestPoint(tr)) || PointInRectangle(tl, tr, br, bl, ClosestPoint(br)) || PointInRectangle(tl, tr, br, bl, ClosestPoint(bl));
        }
    }
}