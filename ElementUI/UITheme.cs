using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.ElementUI
{
    public class UITheme
    {
        //public readonly Dictionary<string, UIStyle> Styles = new Dictionary<string, UIStyle>();

        //public UITheme() { }

        //public UITheme(string assetName)
        //{
        //    Styles = AssetManager.LoadJSON<Dictionary<string, UIStyle>>(assetName);
        //}

        //public UITheme(FileStream fs)
        //{
        //    using var streamReader = new StreamReader(fs);
        //    using var jsonTextReader = new JsonTextReader(streamReader);

        //    var serializer = new JsonSerializer();
        //    Styles = serializer.Deserialize<Dictionary<string, UIStyle>>(jsonTextReader);
        //}

        //public T GetStyle<T>(string name) where T : UIStyle
        //{
        //    return (T)Styles[name];
        //}
    } // UITheme
}
