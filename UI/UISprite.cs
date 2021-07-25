using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Veldrid;

namespace ElementEngine
{
    public enum UISpriteType
    {
        Color,
        Static,
        Animated,
        Auto3Slice,
        Auto9Slice,
    }

    public enum UISpriteFillType
    {
        None,
        Contain,
        Cover,
        Stretch,
    }

    public class UISprite : IDisposable
    {
        public static UISprite CreateUISprite(UIWidget widget, string elName)
        {
            var type = widget.GetXMLAttribute(elName, "UISpriteType").Value.ToEnum<UISpriteType>();

            return type switch
            {
                UISpriteType.Color => new UISpriteColor(widget, elName),
                UISpriteType.Static => new UISpriteStatic(widget, elName),
                UISpriteType.Animated => new UISpriteAnimated(widget, elName),
                UISpriteType.Auto3Slice => new UISpriteAuto3Slice(widget, elName),
                UISpriteType.Auto9Slice => new UISpriteAuto9Slice(widget, elName),
                _ => throw new Exception("Unsupported UISpriteType type: " + type.ToString()),
            };
        } // CreateUISprite

        public static UISprite CreateUISprite(UIWidget widget, XElement el)
        {
            var type = el.Attribute("UISpriteType").Value.ToEnum<UISpriteType>();

            return type switch
            {
                UISpriteType.Color => new UISpriteColor(widget, el),
                UISpriteType.Static => new UISpriteStatic(widget, el),
                UISpriteType.Animated => new UISpriteAnimated(widget, el),
                UISpriteType.Auto3Slice => new UISpriteAuto3Slice(widget, el),
                UISpriteType.Auto9Slice => new UISpriteAuto9Slice(widget, el),
                _ => throw new Exception("Unsupported UISpriteType type: " + type.ToString()),
            };
        } // CreateUISprite

        public readonly UIWidget Parent;
        public readonly XElement XMLElement;

        public TexturePremultiplyType PremultiplyType { get; protected set; } = TexturePremultiplyType.None;
        public Sprite Sprite { get; set; }

        public int Width => Sprite.Width;
        public int Height => Sprite.Height;
        public Vector2I Size => new Vector2I(Width, Height);
        public Vector2 SizeF => new Vector2(Width, Height);
        public Texture2D Texture => Sprite.Texture;
        public Rectangle SourceRect => Sprite.SourceRect;
        public Vector2 Position;

        protected void Preload(UIWidget widget)
        {
            var attPremultiplyAlpha = XMLElement.Attribute("PremultiplyAlpha");
            if (attPremultiplyAlpha != null)
                PremultiplyType = attPremultiplyAlpha.Value.ToEnum<TexturePremultiplyType>();
        }

        public UISprite(UIWidget widget, string elName)
        {
            Parent = widget;
            XMLElement = widget.GetXMLElement(elName);
            Preload(widget);
        }

        public UISprite(UIWidget widget, XElement el)
        {
            Parent = widget;
            XMLElement = el;
            Preload(widget);
        }

        #region IDisposable
        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Sprite?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        public virtual void Draw(SpriteBatch2D spriteBatch, Vector2 offset)
        {
            Sprite.Draw(spriteBatch, Position + offset);
        }

        public virtual void Update(GameTimer gameTimer)
        {
            Sprite.Update(gameTimer);
        }

        public virtual void SetWidth(int width) { }
        public virtual void SetHeight(int height) { }
        public virtual void SetSize(Vector2I size) { }

        public UISpriteFillType? GetFillType()
        {
            var attFillType = XMLElement.Attribute("UISpriteFillType");

            if (attFillType != null)
                return attFillType.Value.ToEnum<UISpriteFillType>();

            return null;
        }
    }

    public class UISpriteColor : UISprite
    {
        public RgbaByte Color;

        ~UISpriteColor()
        {
            Dispose(false);
        }

        public UISpriteColor(UIWidget widget, XElement el) : base(widget, el)
        {
            Init();
        }

        public UISpriteColor(UIWidget widget, string elName) : base(widget, elName)
        {
            Init();
        }

        protected void Init()
        {
            Color = new RgbaByte().FromHex(XMLElement.Attribute("Color").Value);

            var attWidth = XMLElement.Attribute("Width");
            var attHeight = XMLElement.Attribute("Height");

            var width = 1;
            var height = 1;

            if (attWidth != null)
                width = int.Parse(attWidth.Value);
            if (attHeight != null)
                height = int.Parse(attHeight.Value);

            Sprite = new Sprite(new Texture2D(width, height, Color));
        }

        public override void SetWidth(int width)
        {
            Sprite.Texture?.Dispose();
            Sprite.Texture = new Texture2D(width, Sprite.Height, Color);
        }

        public override void SetHeight(int height)
        {
            Sprite.Texture?.Dispose();
            Sprite.Texture = new Texture2D(Sprite.Width, height, Color);
        }
    } // UISpriteStatic

    public class UISpriteStatic : UISprite
    {
        ~UISpriteStatic()
        {
            Dispose(false);
        }

        public UISpriteStatic(UIWidget widget, XElement el) : base(widget, el)
        {
            Init();
        }

        public UISpriteStatic(UIWidget widget, string elName) : base(widget, elName)
        {
            Init();
        }

        protected void Init()
        {
            Sprite = new Sprite(AssetManager.LoadTexture2D(XMLElement.Value, PremultiplyType));

            var fillType = GetFillType();

            if (fillType.HasValue)
            {
                switch (fillType.Value)
                {
                    case UISpriteFillType.Contain:
                        {
                            var aspectRatio = (float)Width / Height;

                            if (Width < Height)
                            {
                                var targetHeight = (float)Parent.Height;
                                var targetWidth = targetHeight * aspectRatio;
                                Sprite.Scale = new Vector2(targetWidth / Width, targetHeight / Height);
                            }
                            else
                            {
                                var targetWidth = (float)Parent.Width;
                                var targetHeight = targetWidth / aspectRatio;
                                Sprite.Scale = new Vector2(targetWidth / Width, targetHeight / Height);
                            }
                        }
                        break;

                    case UISpriteFillType.Cover:
                        {
                            var aspectRatio = (float)Width / Height;

                            if (Width > Height)
                            {
                                var targetHeight = (float)Parent.Height;
                                var targetWidth = targetHeight * aspectRatio;
                                Sprite.Scale = new Vector2(targetWidth / Width, targetHeight / Height);
                            }
                            else
                            {
                                var targetWidth = (float)Parent.Width;
                                var targetHeight = targetWidth / aspectRatio;
                                Sprite.Scale = new Vector2(targetWidth / Width, targetHeight / Height);
                            }
                        }
                        break;

                    case UISpriteFillType.Stretch:
                        {
                            Sprite.Scale = new Vector2((float)Parent.Width / Width, (float)Parent.Height / Height);
                        }
                        break;
                }
            }
        }
    } // UISpriteStatic

    public class UISpriteAnimated : UISprite
    {
        public Animation Animation { get; set; }
        public AnimatedSprite AnimatedSprite => (Sprite as AnimatedSprite);

        ~UISpriteAnimated()
        {
            Dispose(false);
        }

        public UISpriteAnimated(UIWidget widget, XElement el) : base(widget, el)
        {
            Init();
        }

        public UISpriteAnimated(UIWidget widget, string elName) : base(widget, elName)
        {
            Init();
        }

        protected void Init()
        {
            Vector2I? frameSize = null;

            var attFrameSize = XMLElement.Attribute("FrameSize");
            if (attFrameSize != null)
            {
                var frameSizeSplit = attFrameSize.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                frameSize = new Vector2I(int.Parse(frameSizeSplit[0]), int.Parse(frameSizeSplit[1]));
            }

            Sprite = new AnimatedSprite(AssetManager.LoadTexture2D(XMLElement.Value, PremultiplyType), frameSize);

            var attAnimationFrames = XMLElement.Attribute("Frames");
            var attAnimationDuration = XMLElement.Attribute("DurationPerFrame");

            if (attAnimationFrames != null && attAnimationDuration != null)
            {
                Animation = new Animation();
                Animation.SetFramesFromString(attAnimationFrames.Value);
                Animation.DurationPerFrame = float.Parse(attAnimationDuration.Value);
                AnimatedSprite.PlayAnimation(Animation);
            }
        }
    } // UISpriteAnimated

    public class UISpriteAuto3Slice : UISprite
    {
        public enum UISprite3SliceDirection
        {
            Vertical,
            Horizontal,
        }

        public Texture2D TextureLeft { get; set; }
        public Texture2D TextureRight { get; set; }
        public Texture2D TextureTop { get; set; }
        public Texture2D TextureBottom { get; set; }
        public Texture2D TextureCenter { get; set; }
        public UISprite3SliceDirection Direction { get; set; }

        ~UISpriteAuto3Slice()
        {
            Dispose(false);
        }

        public UISpriteAuto3Slice(UIWidget widget, XElement el) : base(widget, el)
        {
            Init();
        }

        public UISpriteAuto3Slice(UIWidget widget, string elName) : base(widget, elName)
        {
            Init();
        }

        protected void Init()
        {
            var direction = XMLElement.Attribute("Direction").Value;
            Direction = direction.ToEnum<UISprite3SliceDirection>();

            if (Direction == UISprite3SliceDirection.Horizontal)
            {
                var width = Parent.Width;
                var attWidth = XMLElement.Attribute("Width");
                if (attWidth != null)
                    width = int.Parse(attWidth.Value);

                var assetLeft = XMLElement.Element("Left").Value;
                var assetCenter = XMLElement.Element("Center").Value;
                var assetRight = XMLElement.Element("Right").Value;

                TextureLeft = AssetManager.LoadTexture2D(assetLeft, PremultiplyType);
                TextureCenter = AssetManager.LoadTexture2D(assetCenter, PremultiplyType);
                TextureRight = AssetManager.LoadTexture2D(assetRight, PremultiplyType);

                SetWidth(width);
            }
            else if (Direction == UISprite3SliceDirection.Vertical)
            {
                var height = Parent.Height;
                var attHeight = XMLElement.Attribute("Height");
                if (attHeight != null)
                    height = int.Parse(attHeight.Value);

                var assetTop = XMLElement.Element("Top").Value;
                var assetCenter = XMLElement.Element("Center").Value;
                var assetBottom = XMLElement.Element("Bottom").Value;

                TextureTop = AssetManager.LoadTexture2D(assetTop, PremultiplyType);
                TextureCenter = AssetManager.LoadTexture2D(assetCenter, PremultiplyType);
                TextureBottom = AssetManager.LoadTexture2D(assetBottom, PremultiplyType);

                SetHeight(height);
            }
        }

        public override void SetSize(Vector2I size)
        {
            SetWidth(size.X);
        }

        public override void SetWidth(int width)
        {
            if (Direction != UISprite3SliceDirection.Horizontal)
                return;

            if (width <= 0)
                width = 1;

            var texture = GraphicsHelper.Create3SliceTextureHorizontal(width, TextureLeft, TextureCenter, TextureRight);

            Sprite?.Dispose();
            Sprite = new Sprite(texture);
        }

        public override void SetHeight(int height)
        {
            if (Direction != UISprite3SliceDirection.Vertical)
                return;

            if (height <= 0)
                height = 1;

            var texture = GraphicsHelper.Create3SliceTextureVertical(height, TextureTop, TextureCenter, TextureBottom);

            Sprite?.Dispose();
            Sprite = new Sprite(texture);
        }
    } // UISpriteAuto3Slice

    public class UISpriteAuto9Slice : UISprite
    {
        public Texture2D TopTextureLeft { get; set; }
        public Texture2D TopTextureCenter { get; set; }
        public Texture2D TopTextureRight { get; set; }
        public Texture2D MiddleTextureLeft { get; set; }
        public Texture2D MiddleTextureCenter { get; set; }
        public Texture2D MiddleTextureRight { get; set; }
        public Texture2D BottomTextureLeft { get; set; }
        public Texture2D BottomTextureCenter { get; set; }
        public Texture2D BottomTextureRight { get; set; }

        ~UISpriteAuto9Slice()
        {
            Dispose(false);
        }

        public UISpriteAuto9Slice(UIWidget widget, XElement el) : base(widget, el)
        {
            Init();
        }

        public UISpriteAuto9Slice(UIWidget widget, string elName) : base(widget, elName)
        {
            Init();   
        }

        protected void Init()
        {
            var width = Parent.Width;
            var attWidth = XMLElement.Attribute("Width");
            if (attWidth != null)
                width = int.Parse(attWidth.Value);

            var height = Parent.Height;
            var attHeight = XMLElement.Attribute("Height");
            if (attHeight != null)
                height = int.Parse(attHeight.Value);

            TopTextureLeft = AssetManager.LoadTexture2D(XMLElement.Element("TopLeft").Value, PremultiplyType);
            TopTextureCenter = AssetManager.LoadTexture2D(XMLElement.Element("TopCenter").Value, PremultiplyType);
            TopTextureRight = AssetManager.LoadTexture2D(XMLElement.Element("TopRight").Value, PremultiplyType);

            MiddleTextureLeft = AssetManager.LoadTexture2D(XMLElement.Element("MiddleLeft").Value, PremultiplyType);
            MiddleTextureCenter = AssetManager.LoadTexture2D(XMLElement.Element("MiddleCenter").Value, PremultiplyType);
            MiddleTextureRight = AssetManager.LoadTexture2D(XMLElement.Element("MiddleRight").Value, PremultiplyType);

            BottomTextureLeft = AssetManager.LoadTexture2D(XMLElement.Element("BottomLeft").Value, PremultiplyType);
            BottomTextureCenter = AssetManager.LoadTexture2D(XMLElement.Element("BottomCenter").Value, PremultiplyType);
            BottomTextureRight = AssetManager.LoadTexture2D(XMLElement.Element("BottomRight").Value, PremultiplyType);

            SetSize(width, height);
        }

        public override void SetWidth(int width)
        {
            SetSize(width, Height);
        }

        public override void SetHeight(int height)
        {
            SetSize(Width, height);
        }

        public override void SetSize(Vector2I size)
        {
            SetSize(size.X, size.Y);
        }

        public void SetSize(int width, int height)
        {
            if (width <= 0)
                width = 1;
            if (height <= 0)
                height = 1;

            var texture = GraphicsHelper.Create9SliceTexture(
                width, height,
                TopTextureLeft, TopTextureCenter, TopTextureRight,
                MiddleTextureLeft, MiddleTextureCenter, MiddleTextureRight,
                BottomTextureLeft, BottomTextureCenter, BottomTextureRight);

            Sprite?.Dispose();
            Sprite = new Sprite(texture);
        }
    } // UISpriteAuto9Slice
}
