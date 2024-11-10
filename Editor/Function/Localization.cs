using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dev.kesera2.transition_helper
{
    public class Localization : ScriptableSingleton<Localization>
    {
        public static LanguageHash Lang { get; private set; }

        // ローカライズ
        private const string LangAssetFolderPath = "Language/";
        private const string JpAssetName = "JP";
        private const string EnAssetName = "EN";
        private const string KrAssetName = "KR";
        private const string CnAssetName = "CN";

        public void OnEnable()
        {
            Debug.Log(Application.systemLanguage);
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + JpAssetName);
            }
            else
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + EnAssetName);
            }

        }

        public static void Localize()
        {
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + JpAssetName);
            }
            else if (Application.systemLanguage == SystemLanguage.Korean)
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + KrAssetName);
            }
            else if (Application.systemLanguage == SystemLanguage.Chinese)
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + CnAssetName);
            }
            else
            {
                Lang = Resources.Load<LanguageHash>(LangAssetFolderPath + EnAssetName);
            }
        }

    }
}