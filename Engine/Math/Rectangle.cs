﻿using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ElementEngine
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        [JsonIgnore]
        public bool IsZero => X == 0 && Y == 0 && Width == 0 && Height == 0;

        [JsonIgnore]
        public static Rectangle Empty = new Rectangle(0, 0, 0, 0);

        [JsonIgnore]
        public bool IsEmpty => this == Empty;

        [JsonIgnore]
        public Vector2I Location
        {
            get => new Vector2I(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        [JsonIgnore]
        public Vector2 LocationF
        {
            get => new Vector2(X, Y);
            set
            {
                X = (int)value.X;
                Y = (int)value.Y;
            }
        }

        [JsonIgnore]
        public Vector2I TopRight
        {
            get => new Vector2I(Right, Top);
            set
            {
                Right = value.X;
                Y = value.Y;
            }
        }

        [JsonIgnore]
        public Vector2 TopRightF
        {
            get => new Vector2(Right, Top);
            set
            {
                Right = (int)value.X;
                Y = (int)value.Y;
            }
        }

        [JsonIgnore]
        public Vector2I BottomLeft
        {
            get => new Vector2I(Left, Bottom);
            set
            {
                Bottom = value.Y;
                X = value.X;
            }
        }

        [JsonIgnore]
        public Vector2 BottomLeftF
        {
            get => new Vector2(Left, Bottom);
            set
            {
                Bottom = (int)value.Y;
                X = (int)value.X;
            }
        }

        [JsonIgnore]
        public Vector2I BottomRight
        {
            get => new Vector2I(Right, Bottom);
            set
            {
                Right = value.X;
                Bottom = value.Y;
            }
        }

        [JsonIgnore]
        public Vector2 BottomRightF
        {
            get => new Vector2(Right, Bottom);
            set
            {
                Right = (int)value.X;
                Bottom = (int)value.Y;
            }
        }

        [JsonIgnore]
        public Vector2I BottomCenter
        {
            get => new Vector2I(Center.X, Bottom);
            set
            {
                Bottom = value.Y;
                X = value.X - (Width / 2);
            }
        }

        [JsonIgnore]
        public Vector2 BottomCenterF
        {
            get => new Vector2(CenterF.X, BottomF);
            set
            {
                Bottom = (int)value.Y;
                X = (int)value.X - (Width / 2);
            }
        }

        [JsonIgnore]
        public Vector2I Size
        {
            get => new Vector2I(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        [JsonIgnore]
        public Vector2 SizeF
        {
            get => new Vector2(Width, Height);
            set
            {
                Width = (int)value.X;
                Height = (int)value.Y;
            }
        }

        [JsonIgnore]
        public System.Drawing.Rectangle DrawingRectangle => new System.Drawing.Rectangle(X, Y, Width, Height);

        [JsonIgnore]
        public int Left
        {
            get => X;
            set
            {
                X = value;
            }
        }

        [JsonIgnore]
        public int Right
        {
            get => X + Width;
            set
            {
                Width = value - X;
            }
        }

        [JsonIgnore]
        public int Top
        {
            get => Y;
            set
            {
                Y = value;
            }
        }

        [JsonIgnore]
        public int Bottom
        {
            get => Y + Height;
            set
            {
                Height = value - Y;
            }
        }

        [JsonIgnore]
        public float BottomF
        {
            get => Y + Height;
            set
            {
                Height = (int)(value - Y);
            }
        }

        [JsonIgnore]
        public Vector2I Center => new Vector2I(X + Width / 2, Y + Height / 2);

        [JsonIgnore]
        public Vector2 CenterF => new Vector2(X + Width / 2f, Y + Height / 2f);

        public Rectangle(Vector2I location, Vector2I size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.X;
            Height = size.Y;
        }

        public Rectangle(Vector2 location, Vector2 size)
        {
            X = (int)location.X;
            Y = (int)location.Y;
            Width = (int)size.X;
            Height = (int)size.Y;
        }

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rectangle(float x, float y, float width, float height)
        {
            X = (int)x;
            Y = (int)y;
            Width = (int)width;
            Height = (int)height;
        }

        public bool Contains(Vector2I vec) => (X <= vec.X) && (vec.X < (X + Width)) && (Y <= vec.Y) && (vec.Y < (Y + Height));
        public bool Contains(Vector2 vec) => (X <= vec.X) && (vec.X < (X + Width)) && (Y <= vec.Y) && (vec.Y < (Y + Height));
        public bool Contains(Rectangle rect) => (X <= rect.X) && ((rect.X + rect.Width) <= (X + Width)) && (Y <= rect.Y) && ((rect.Y + rect.Height) <= (Y + Height));

        public bool Intersects(Rectangle rect)
        {
            return rect.Left < Right &&
                   Left < rect.Right &&
                   rect.Top < Bottom &&
                   Top < rect.Bottom;
        }

        public Rectangle Intersect(Rectangle rect)
        {
            if (!Intersects(rect))
                return Empty;

            int rightSide = Math.Min(X + Width, rect.X + rect.Width);
            int leftSide = Math.Max(X, rect.X);
            int topSide = Math.Max(Y, rect.Y);
            int bottomSide = Math.Min(Y + Height, rect.Y + rect.Height);

            return new Rectangle(leftSide, topSide, rightSide - leftSide, bottomSide - topSide);
        }

        public bool Intersect(CircleI circle)
        {
            return Intersect(new CircleF(circle.Center.ToVector2(), circle.Radius));
        }

        public bool Intersect(CircleF circle)
        {
            float halfRectWidth = Width / 2.0f;
            float halfRectHeight = Height / 2.0f;

            float distX = MathF.Abs(circle.Center.X - (X + halfRectWidth));
            float distY = MathF.Abs(circle.Center.Y - (Y + halfRectHeight));

            if (distX >= circle.Radius + halfRectWidth || distY >= circle.Radius + halfRectHeight)
                return false;

            if (distX < halfRectWidth || distY < halfRectHeight)
                return true;

            distX -= halfRectWidth;
            distY -= halfRectHeight;

            if (distX * distX + distY * distY < circle.Radius * circle.Radius)
                return true;

            return false;
        }

        public static bool Intersects(Rectangle rect1, Rectangle rect2) => rect1.Intersects(rect2);
        public static Rectangle Intersect(Rectangle rect1, Rectangle rect2) => rect1.Intersect(rect2);

        public static bool Intersect(Rectangle rect, CircleI circle) => rect.Intersect(circle);
        public static bool Intersect(Rectangle rect, CircleF circle) => rect.Intersect(circle);

        public void ExpandToContain(Rectangle rect)
        {
            var newRect = ExpandToContain(this, rect);
            X = newRect.X;
            Y = newRect.Y;
            Width = newRect.Width;
            Height = newRect.Height;
        }

        public static Rectangle ExpandToContain(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.Contains(rect2))
                return rect1;

            if (rect1.X > rect2.X)
                rect1.X = rect2.X;
            if (rect1.Right < rect2.Right)
                rect1.Right = rect2.Right;
            if (rect1.Y > rect2.Y)
                rect1.Y = rect2.Y;
            if (rect1.Bottom < rect2.Bottom)
                rect1.Bottom = rect2.Bottom;

            return rect1;
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}", X, Y, Width, Height, Right, Bottom);
        }

        public override bool Equals(object obj)
        {
            if (obj is Rectangle rect)
                return rect == this;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(Rectangle rect1, Rectangle rect2) => rect1.X == rect2.X && rect1.Y == rect2.Y && rect1.Width == rect2.Width && rect1.Height == rect2.Height;
        public static bool operator !=(Rectangle rect1, Rectangle vec) => !(rect1 == vec);

        public static bool operator ==(Rectangle rect1, Vector2 vec) => rect1.X == vec.X && rect1.Y == vec.Y;
        public static bool operator !=(Rectangle rect1, Vector2 vec) => !(rect1 == vec);

        public static bool operator ==(Rectangle rect1, Vector2I vec) => rect1.X == vec.X && rect1.Y == vec.Y;
        public static bool operator !=(Rectangle rect1, Vector2I vec) => !(rect1 == vec);

        public static Rectangle operator -(Rectangle rect, Vector2 vec) => new Rectangle(rect.X - vec.X, rect.Y - vec.Y, rect.Width, rect.Height);
        public static Rectangle operator +(Rectangle rect, Vector2 vec) => new Rectangle(rect.X + vec.X, rect.Y + vec.Y, rect.Width, rect.Height);
        public static Rectangle operator *(Rectangle rect, Vector2 vec) => new Rectangle(rect.X * vec.X, rect.Y * vec.Y, rect.Width, rect.Height);
        public static Rectangle operator /(Rectangle rect, Vector2 vec) => new Rectangle(rect.X / vec.X, rect.Y / vec.Y, rect.Width, rect.Height);

        public static Rectangle operator -(Rectangle rect, Vector2I vec) => new Rectangle(rect.X - vec.X, rect.Y - vec.Y, rect.Width, rect.Height);
        public static Rectangle operator +(Rectangle rect, Vector2I vec) => new Rectangle(rect.X + vec.X, rect.Y + vec.Y, rect.Width, rect.Height);
        public static Rectangle operator *(Rectangle rect, Vector2I vec) => new Rectangle(rect.X * vec.X, rect.Y * vec.Y, rect.Width, rect.Height);
        public static Rectangle operator /(Rectangle rect, Vector2I vec) => new Rectangle(rect.X / vec.X, rect.Y / vec.Y, rect.Width, rect.Height);

        public static Rectangle operator -(Rectangle rect1, Rectangle rect2) => new Rectangle(rect1.X - rect2.X, rect1.Y - rect2.Y, rect1.Width - rect2.Width, rect1.Height - rect2.Height);
        public static Rectangle operator +(Rectangle rect1, Rectangle rect2) => new Rectangle(rect1.X + rect2.X, rect1.Y + rect2.Y, rect1.Width + rect2.Width, rect1.Height + rect2.Height);
        public static Rectangle operator *(Rectangle rect1, Rectangle rect2) => new Rectangle(rect1.X * rect2.X, rect1.Y * rect2.Y, rect1.Width * rect2.Width, rect1.Height * rect2.Height);
        public static Rectangle operator /(Rectangle rect1, Rectangle rect2) => new Rectangle(rect1.X / rect2.X, rect1.Y / rect2.Y, rect1.Width / rect2.Width, rect1.Height / rect2.Height);

        public static Rectangle operator -(Rectangle rect, float val) => new Rectangle(rect.X - val, rect.Y - val, rect.Width - val, rect.Height - val);
        public static Rectangle operator +(Rectangle rect, float val) => new Rectangle(rect.X + val, rect.Y + val, rect.Width + val, rect.Height + val);
        public static Rectangle operator *(Rectangle rect, float val) => new Rectangle(rect.X * val, rect.Y * val, rect.Width * val, rect.Height * val);
        public static Rectangle operator /(Rectangle rect, float val) => new Rectangle(rect.X / val, rect.Y / val, rect.Width / val, rect.Height / val);

    } // Rectangle
}
