﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ElementEngine
{
    public struct Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsZero => X == 0 && Y == 0 && Width == 0 && Height == 0;

        public static Rectangle Empty = new Rectangle(0, 0, 0, 0);
        public bool IsEmpty => this == Empty;

        public Vector2i Location
        {
            get => new Vector2i(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Vector2 LocationF
        {
            get => new Vector2(X, Y);
            set
            {
                X = (int)value.X;
                Y = (int)value.Y;
            }
        }

        public Vector2i Size
        {
            get => new Vector2i(Width, Height);
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        public Vector2 SizeF
        {
            get => new Vector2(Width, Height);
            set
            {
                Width = (int)value.X;
                Height = (int)value.Y;
            }
        }

        public int Left { get => X; }
        public int Right { get => X + Width; }
        public int Top { get => Y; }
        public int Bottom { get => Y + Height; }

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

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", X, Y, Width, Height);
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
            return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        }

        public static bool operator ==(Rectangle rect1, Rectangle rect2) => rect1.X == rect2.X && rect1.Y == rect2.Y && rect1.Width == rect2.Width && rect1.Height == rect2.Height;
        public static bool operator !=(Rectangle rect1, Rectangle rect2) => !(rect1 == rect2);
    }
}
