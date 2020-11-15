using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace ElementEngine
{
    public class LayeredSprite : AnimatedSprite
    {
        protected static Dictionary<string, Texture2D> _layeredCache = new Dictionary<string, Texture2D>();

        protected List<string> _layers = new List<string>();

        public LayeredSprite(List<string> layers, Vector2I? frameSize = null, int defaultFrame = 1, bool centerOrigin = false)
        {
            SetLayers(layers);
            BuildSprite();

            if (!frameSize.HasValue)
                frameSize = Texture.Size;

            InitSprite(Texture, centerOrigin);
            InitAnimatedSprite(Texture, frameSize, defaultFrame, centerOrigin);
        }

        ~LayeredSprite()
        {

        }

        public static void Cleanup()
        {
            foreach (var kvp in _layeredCache)
            {
                kvp.Value?.Dispose();
            }
        }

        public void SetLayers(List<string> _layers)
        {
            this._layers.Clear();

            foreach (var l in _layers)
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    this._layers.Add(l);
                }
            }
        }

        public void BuildSprite()
        {
            var cacheName = "";

            foreach (var l in _layers)
            {
                cacheName += l;
            }

            if (_layeredCache.ContainsKey(cacheName))
            {
                Texture = _layeredCache[cacheName];
                return;
            }

            var layerTextures = new List<Texture2D>();

            foreach (var l in _layers)
            {
                layerTextures.Add(AssetManager.LoadTexture2D(l));
            }

            if (layerTextures.Count <= 0)
                return;

            Texture = new Texture2D(layerTextures[0].Width, layerTextures[0].Height);

            Texture.BeginRenderTarget();
            Texture.RenderTargetClear(RgbaFloat.Clear);

            var spriteBatch = Texture.GetRenderTargetSpriteBatch2D();
            spriteBatch.Begin(SamplerType.Point);

            foreach (var t in layerTextures)
                spriteBatch.DrawTexture2D(t, Vector2.Zero);

            spriteBatch.End();
            Texture.EndRenderTarget();

            _layeredCache.Add(cacheName, Texture);
        }
    }
}
