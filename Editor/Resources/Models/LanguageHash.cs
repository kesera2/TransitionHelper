using UnityEngine;

namespace TransitionHelper
{
    [CreateAssetMenu(menuName = "TransitionHelper/LaguageData")]
    public class LanguageHash : ScriptableObject
    {
        [Header("Lable")]
        public string settingsLabelText;
        public string ignoreNoConditionText;
        public string writeDefaultsOffText;

        [Header("Button")]
        public string setupButtonText;
        public string toggleAll;
        public string toggleNone;

        [Header("CheckBox")]
        public string includeSubStateMachineText;

        [Header("Dialog")]
        public string confirmTitle;
        public string confirmContent;
        public string answerYes;
        public string answerNo;

        [Header("Help")]
        [Multiline] public string infoExplainMessage;
        public string warnNeedsConditionOrExitTime;
        public string infoSettingsMessage;
        public string errorMessage;
        public string errorNeedsToSelectLayer;

        [Header("Log")]
        public string logMessage;
    }
}