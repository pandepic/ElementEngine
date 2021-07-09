using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.Localisation
{
    public class Language
    {
        public string AssetName;
        public Dictionary<string, string> Strings = new Dictionary<string, string>();
    }

    public static class LocalisationManager
    {
        public static Dictionary<string, Language> Languages = new Dictionary<string, Language>();
        public static Language DefaultLanguage;
        public static Language CurrentLanguage;

        public static void SetDefaultLanguage(string assetName)
        {
            DefaultLanguage = GetLanguage(assetName);
        }

        public static void SetLanguage(string assetName)
        {
            CurrentLanguage = GetLanguage(assetName);
        }

        public static Language GetLanguage(string assetName)
        {
            if (Languages.TryGetValue(assetName, out var language))
                return language;
            
            var strings = AssetManager.LoadJSON<Dictionary<string, string>>(assetName);

            language = new Language()
            {
                AssetName = assetName,
                Strings = strings,
            };

            Languages.Add(assetName, language);

            return language;
        }

        public static string GetString(string key, params (string, string)[] variables)
        {
            var str = "";

            if (!CurrentLanguage.Strings.TryGetValue(key, out var strCurrent))
            {
                if (!DefaultLanguage.Strings.TryGetValue(key, out var strBase))
                    return null;
                else
                    str = strBase;
            }
            else
            {
                str = strCurrent;
            }

            foreach (var (name, value) in variables)
                str = str.Replace("{" + name + "}", value);

            return str;
        }

    } // LocalisationManager
}
