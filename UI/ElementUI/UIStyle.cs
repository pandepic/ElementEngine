using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ElementEngine.ElementUI
{
    public class UIStyle
    {
        public UIStyle(string assetName)
        {
            using var fs = AssetManager.GetAssetStream(assetName);
            var doc = XDocument.Load(fs);
            Load(doc);
        }

        public UIStyle(FileStream fs)
        {
            var doc = XDocument.Load(fs);
            Load(doc);
        }

        public UIStyle(XDocument doc)
        {
            Load(doc);
        }

        protected void Load(XDocument doc)
        {
        }
    } // UIStyle
}
