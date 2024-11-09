using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dev.kesera2.transition_helper
{
    public class Localization : ScriptableSingleton<Localization>
    {
        public static LanguageHash lang { get; private set; }

        // ローカライズ
        private readonly static string LANG_ASSET_FOLDER_PATH = "Language/";
        private readonly static string JP_ASSET_NAME = "JP";
        private readonly static string EN_ASSET_NAME = "EN";
        private readonly static string KR_ASSET_NAME = "KR";
        private readonly static string CN_ASSET_NAME = "CN";

        public void OnEnable()
        {
            Debug.Log(Application.systemLanguage);
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + JP_ASSET_NAME);
            }
            else
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + EN_ASSET_NAME);
            }

        }

        public static void Localize()
        {
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + JP_ASSET_NAME);
            }
            else if (Application.systemLanguage == SystemLanguage.Korean)
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + KR_ASSET_NAME);
            }
            else if (Application.systemLanguage == SystemLanguage.Chinese)
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + CN_ASSET_NAME);
            }
            else
            {
                lang = Resources.Load<LanguageHash>(LANG_ASSET_FOLDER_PATH + EN_ASSET_NAME);
            }
        }

    }
}