using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace dev.kesera2.transition_helper
{
    [InitializeOnLoad]
    public static class Localization
    {
        public static Dictionary<string, string> localizedText;
        private const string localizationPathGuid = "4c9687c1caf8214458e952a01a88b560";
        private static string localizationPathRoot = AssetDatabase.GUIDToAssetPath(localizationPathGuid);
        private const string FallbackLanguage = "ja";
        public static int CurrentLanguage;

        private static ImmutableDictionary<string, string> SupportedLanguageDisplayNames
            = ImmutableDictionary<string, string>.Empty
                .Add("en", "English")
                .Add("ja", "日本語")
                .Add("zh-hans", "简体中文")
                .Add("ko", "한국어");

        public static readonly ImmutableList<string>
            SupportedLanguages = new string[] { "ja-JP", "en-US", "zh-Hans", "ko-KR" }.ToImmutableList();

        public static readonly string[] DisplayNames = SupportedLanguages.Select(l =>
        {
            return CollectionExtensions.GetValueOrDefault(SupportedLanguageDisplayNames, l, l);
        }).ToArray();

        static Localization()
        {
            var lastLanguage = EditorPrefs.GetString(localizationPathGuid);
            if (lastLanguage == string.Empty) Localize();
            CurrentLanguage = SupportedLanguages.IndexOf(lastLanguage);
            Localize(lastLanguage);
            Debug.Log(lastLanguage);
        }

        public static void Localize(string language = "ja-JP")
        {
            var path = Path.Combine(localizationPathRoot, language + ".json");
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            localizedText = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        public static string S(string key)
        {
            if (localizedText != null && localizedText.TryGetValue(key, out string value))
            {
                return value;
            }

            return string.Empty; // TODO: return default value
        }

        public static void SaveLanguage()
        {
            Debug.Log("Disabled on " + SupportedLanguages[CurrentLanguage]);
            EditorPrefs.SetString(localizationPathGuid, SupportedLanguages[CurrentLanguage]);
        }

        public static void LoadLanguage()
        {
            var lastLanguage = EditorPrefs.GetString(localizationPathGuid);
            Debug.Log("LoadLanguage : lastlanguage " + lastLanguage);
            if (lastLanguage == string.Empty) return;
            Debug.Log("Enabled " + SupportedLanguages.IndexOf(lastLanguage));
            CurrentLanguage = SupportedLanguages.IndexOf(lastLanguage);
        }
    }
}