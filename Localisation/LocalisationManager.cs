using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public static class LocalisationManager
    {
        public class Language
        {
            public string AssetName;
            public Dictionary<string, string> Strings = new();
        }

        public static Dictionary<string, Language> Languages = new();
        public static Language DefaultLanguage;
        public static Language CurrentLanguage;

#if DEBUG
        public static HashSet<string> MissingKeys = new();
#endif

        public static void SetDefaultLanguage(string assetName)
        {
            DefaultLanguage = GetLanguage(assetName);
        }

        public static void SetLanguage(string assetName)
        {
            CurrentLanguage = GetLanguage(assetName);

#if DEBUG
            MissingKeys.Clear();
#endif
        }

        public static Language GetLanguage(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException($"Invalid language asset {assetName}", nameof(assetName));

            if (Languages.TryGetValue(assetName, out var language))
                return language;
            
            var strings = AssetManager.Instance.LoadJSON<Dictionary<string, string>>(assetName);

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
            string str = "";

            if (!CurrentLanguage.Strings.TryGetValue(key, out var strCurrent))
            {
                var keyMissing = false;

                if (DefaultLanguage == null)
                    keyMissing = true;
                else
                {
                    if (!DefaultLanguage.Strings.TryGetValue(key, out var strBase))
                        keyMissing = true;
                    else
                        str = strBase;
                }

                if (keyMissing)
                {
#if DEBUG
                    if (MissingKeys.Add(key))
                        Logging.Debug($"MISSING KEY: {key}");
#endif
                    return $"MISSING KEY: {key}";
                }
            }
            else
            {
                str = strCurrent;
            }

            if (variables != null)
            {
                foreach (var (name, value) in variables)
                    str = str.Replace("{" + name + "}", value);
            }

            return str;
        }

    } // LocalisationManager
}
