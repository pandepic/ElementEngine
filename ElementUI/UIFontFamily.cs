using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UIFontFamily
    {
        public readonly string Name;
        public readonly Dictionary<(UIFontStyle, UIFontWeight), SpriteFont> SpriteFonts = new Dictionary<(UIFontStyle, UIFontWeight), SpriteFont>();

        public UIFontFamily(string name)
        {
            Name = name;
        }

        public void AddFont(UIFontStyle style, UIFontWeight weight, string assetName)
        {
            AddFont(style, weight, AssetManager.LoadSpriteFont(assetName));
        }

        public void AddFont(UIFontStyle style, UIFontWeight weight, SpriteFont font)
        {
            if (SpriteFonts.ContainsKey((style, weight)))
                return;

            SpriteFonts.Add((style, weight), font);
        }

        public SpriteFont GetFont(UIFontStyle style, UIFontWeight weight)
        {
            return SpriteFonts[(style, weight)];
        }

    } // UIFontFamily
}
