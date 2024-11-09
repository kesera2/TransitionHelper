using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dev.kesera2.transition_helper
{
    public class Localization : ScriptableSingleton<Localization>
    {
        public LanguageHash Lang { get; private set; }
        public string[] selectedMode;
        public static LanguageEnum SelectedLanguage;

        // ローカライズ
        private const string LangAssetFolderPath = "Language/";
        private const string JpAssetName = "JP";
        private const string EnAssetName = "EN";
        private const string KrAssetName = "KR";
        private const string CnAssetName = "CN";

        public enum LanguageEnum
        {
            日本語,
            English,
            한국어,
            汉语
        }

        public void OnEnable()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + JpAssetName);
                    SelectedLanguage = LanguageEnum.日本語;
                    break;
                case SystemLanguage.Korean:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + KrAssetName);
                    SelectedLanguage = LanguageEnum.한국어;
                    break;
                case SystemLanguage.Chinese:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + CnAssetName);
                    SelectedLanguage = LanguageEnum.汉语;
                    break;
                default:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + EnAssetName);
                    SelectedLanguage = LanguageEnum.English;
                    break;
            }
        }

        public void Localize()
        {
            switch (SelectedLanguage)
            {
                case LanguageEnum.日本語:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + JpAssetName);
                    break;
                case LanguageEnum.한국어:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + KrAssetName);
                    break;
                case LanguageEnum.汉语:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + CnAssetName);
                    break;
                default:
                    Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + EnAssetName);
                    break;
            }
        }
    }
}