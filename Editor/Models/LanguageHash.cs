using UnityEngine;

namespace dev.kesera2.transition_helper{
    [CreateAssetMenu(menuName = "TransitionHelper/LaguageData")]
    public class LanguageHash : ScriptableObject
    {
        [Header("Tabs")]
        public string layerSpecificationMode;
        public string transitionSpecificationMode;

        [Header("Lable")]
        public string settingsLabelText;
        public string ignoreNoConditionText;
        public string writeDefaultsOffText;
        public string keepWriteDefaultsOfBlendTree;
        public string selectedLayer;
        public string selectedTransitionsCount;

        [Header("Button")]
        public string setupButtonText;
        public string toggleAll;
        public string toggleNone;
        public string selectAllTransitionsButton;
        public string unselectTransitionsButton;

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
        public string errorNeedsToSelectTransition;
        public string warnStateMachineTransitionSelected;
        public string errorNeedsToSelectStateTransition;

        [Header("Log")]
        public string logMessage;
    }
}