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
            public string Name;
            public Dictionary<string, string> Strings = new();
        }

        public static Dictionary<string, Language> Languages = new();
        public static Language DefaultLanguage;
        public static Language CurrentLanguage;

#if DEBUG
        public static HashSet<string> MissingKeys = new();
#endif

        public static void SetDefaultLanguage(string languageName)
        {
            if (!Languages.TryGetValue(languageName, out var language))
                throw new ArgumentException($"Language not loaded {languageName}", nameof(languageName));

            DefaultLanguage = language;
        }

        public static void SetLanguage(string languageName)
        {
            if (!Languages.TryGetValue(languageName, out var language))
                throw new ArgumentException($"Language not loaded {languageName}", nameof(languageName));

            CurrentLanguage = Languages[languageName];

#if DEBUG
            MissingKeys.Clear();
#endif
        }

        public static Language LoadLanguage(string languageName, string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new ArgumentException($"Invalid language asset {assetName}", nameof(assetName));

            if (!Languages.TryGetValue(languageName, out var language))
            {
                language = new Language()
                {
                    Name = languageName,
                    Strings = new(),
                };

                Languages.Add(languageName, language);
            }

            var strings = AssetManager.Instance.LoadJSON<Dictionary<string, string>>(assetName);

            foreach (var str in strings)
            {
                if (!language.Strings.ContainsKey(str.Key))
                    language.Strings.Add(str.Key, str.Value);
            }

            return language;
        }

        public static string GetString(string key)
        {
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
                        return strBase;
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
                return strCurrent;
            }

            return null;
        }

        public static string GetString(string key, params (string, string)[] variables)
        {
            var str = GetString(key);

            if (variables != null)
            {
                foreach (var (name, value) in variables)
                    str = str.Replace("{" + name + "}", value);
            }

            return str;
        }

    } // LocalisationManager
}
