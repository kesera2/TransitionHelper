using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.PackageManager.UI;
using UnityEngine;

// Copyright (c) 2023 kesera2
namespace TransitionHelper
{
    public class TransitionHelperView : EditorWindow
    {
        private static Texture2D logo;
        // スクロール位置
        private Vector2 _scrollPosition = Vector2.zero;
        // アニメーターコントローラー指定時のAnimatorController
        private AnimatorController animatorController;
        // 有効状態のレイヤー
        private bool[] layerEnabled = { };
        // サブステートマシンのチェックボックスの有効状態
        bool includeSubStateMachine = true;
        // Conditionsの指定のないHasExitTimeの設定を無視する
        bool ignoreNoCondition = true;
        // 設定
        bool writeDefaultsOff = true;
        bool showSettings = false;
        bool hasExitTime = false;
        float exitTime = 0;
        bool fixedDuration = true;
        int transitionDuration = 0;
        int transitionOffset = 0;
        bool keepWriteDefaultsOfBlendTree = true;
        // 設定のラベルとコンテンツの間の空欄の幅
        private const float SETTINGS_LABEL_WIDTH_OFFSET = 10f;
        // Tabの表示名
        private readonly string[] _tabToggles = { "Controller指定", "Transition指定" };
        // 選択中のTab
        private int _tabIndex;
        // 選択中のトランジション
        private int _transitionCount = 0;

        [MenuItem("Tools/もちもちまーと/Transition Helper")]
        public static void OpenWindow()
        {
            TransitionHelperView window = GetWindow<TransitionHelperView>();
            window.titleContent = new GUIContent("Transition Helper");
            window.Show();
        }

        private void OnGUI()
        {
            // 描画範囲が足りなければスクロール出来るように
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            Localization.Localize();
            DrawLogo();
            DrawInfomation();
            DrawTabs();
            DrawAnimatorController();
            if (_tabIndex == 0)
            {
                DrawToggleButtons();
                DrawMainOptions();
            }
            test();
            DrawErrorBox();
            DrawSettingsFoldOut();
            DrawExecuteButton();
            //スクロール箇所終了
            EditorGUILayout.EndScrollView();
        }
        void test() {
            AnimatorStateTransition[] x = Selection.objects.Select(x => x as AnimatorStateTransition).ToArray();
            foreach(AnimatorStateTransition a  in x) {
                Debug.Log(a.name);
            }
        }


        // ロゴの描画
        private static void DrawLogo()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            logo = Resources.Load<Texture2D>("Icon/Logo");
            EditorGUILayout.LabelField(new GUIContent(logo), GUILayout.Height(100), GUILayout.Width(400));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Tabの描画
        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(_tabIndex, _tabToggles, new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);
            }
            _transitionCount = Selection.objects.Select(x => x as AnimatorStateTransition).Where(y => y != null).ToArray().Length;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Transitions");

            }
        }

        private void sample() {
            AnimatorController animatorController = null;
            AnimatorStateMachine stateMachine = null;
            AnimatorStateTransition transition = null;

            // Animatorウィンドウで選択されたトランジションを取得
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Animator")
            {
                EditorWindow animatorWindow = EditorWindow.focusedWindow;
                System.Type animatorWindowType = animatorWindow.GetType();
                var controllerProperty = animatorWindowType.GetProperty("controller");
                Debug.Log("controllerProperty is null?" + controllerProperty == null);
                animatorController = controllerProperty.GetValue(animatorWindow, null) as AnimatorController;

                var stateMachineProperty = animatorWindowType.GetProperty("stateMachine");
                stateMachine = stateMachineProperty.GetValue(animatorWindow, null) as AnimatorStateMachine;

                var transitionProperty = animatorWindowType.GetProperty("transition");
                transition = transitionProperty.GetValue(animatorWindow, null) as AnimatorStateTransition;
            }

            if (animatorController != null && stateMachine != null && transition != null)
            {
                string sourceStateName = GetStateName(animatorController, transition.destinationState);
                string destinationStateName = GetStateName(animatorController, transition.destinationState);

                EditorGUILayout.LabelField("Selected Transition Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Source State:", sourceStateName);
                EditorGUILayout.LabelField("Destination State:", destinationStateName);
            }
        }

        private string GetStateName(AnimatorController animatorController, AnimatorState state)
        {
            for (int i = 0; i < animatorController.layers.Length; i++)
            {
                AnimatorStateMachine stateMachine = animatorController.layers[i].stateMachine;
                for (int j = 0; j < stateMachine.states.Length; j++)
                {
                    if (stateMachine.states[j].state == state)
                    {
                        return stateMachine.states[j].state.name;
                    }
                }
            }

            return "Unknown";
        }

        // Animator Controller関連の描画
        private void DrawAnimatorController()
        {
            animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
            using (new EditorGUI.IndentLevelScope())
            {
                if (animatorController == null)
                {
                    return;
                }
                // AnimatorControllerに設定されているレイヤーの名前をチェックボックスとして表示する
                if (layerEnabled == null || layerEnabled.Length != animatorController.layers.Length)
                {
                    layerEnabled = new bool[animatorController.layers.Length];
                    for (int i = 0; i < animatorController.layers.Length; i++)
                    {
                        layerEnabled[i] = false; // デフォルトはアンチェック
                    }
                }
                for (int i = 0; i < animatorController.layers.Length; i++)
                {
                    layerEnabled[i] = EditorGUILayout.ToggleLeft("　" + animatorController.layers[i].name, layerEnabled[i]);
                }
            }
        }

        // トグルボタンの描画
        private void DrawToggleButtons()
        {
            if (animatorController == null)
            {
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                // すべてのチェックボックスをONにするボタン
                if (GUILayout.Button(Localization.lang.toggleAll))
                {
                    for (int i = 0; i < layerEnabled.Length; i++)
                    {
                        layerEnabled[i] = true;
                    }
                }

                // すべてのチェックボックスをOFFにするボタン
                if (GUILayout.Button(Localization.lang.toggleNone))
                {
                    for (int i = 0; i < layerEnabled.Length; i++)
                    {
                        layerEnabled[i] = false;
                    }
                }
            }
        }

        // 説明の描画
        private void DrawInfomation()
        {
            EditorGUILayout.HelpBox(Localization.lang.infoExplainMessage, MessageType.Info);
        }

        // 確認ダイアログの表示
        private bool DisplayConfirmDialog()
        {
            return EditorUtility.DisplayDialog(
            Localization.lang.confirmTitle,
            Localization.lang.confirmContent,
            Localization.lang.answerYes,
            Localization.lang.answerNo);
        }

        // 実行ボタンの描画
        private void DrawExecuteButton()
        {
            // アニメーターが選択されていないまたはレイヤーが1つも選択されていない場合、実行ボタンをDisable
            EditorGUI.BeginDisabledGroup(!(isAnimatorControlerEmpty() || isLayerSelectedAtLeastOne()));
            if (GUILayout.Button(Localization.lang.setupButtonText, GUILayout.Height(40)))
            {
                if (!DisplayConfirmDialog())
                {
                    return;
                }
                List<AnimatorStateTransition> transitions = null;

                // Animator Controllerの指定がある場合
                List<AnimatorControllerLayer> layers = GetTargetLayer(animatorController.layers.ToList());
                List<AnimatorState> states = null;
                if (includeSubStateMachine)
                {
                    transitions = GetSubStateMachineTransitions(layers);
                    states = GetSubStateMachineStates(layers);
                }
                else
                {
                    transitions = layers.Select(c => c.stateMachine).SelectMany(c => c.states).SelectMany(c => c.state.transitions).ToList();
                }

                // 取得したtransition全てに適用させる
                //foreach (AnimatorStateTransition transition in transitions)
                //{
                //    SetTransitionValue(transition);
                //}

                AnimatorStateTransition[] selectedTransitions = Selection.objects.Select(x => x as AnimatorStateTransition).Where(y => y != null).ToArray();
                foreach (var selectedTransition in selectedTransitions)
                {
                    SetTransitionValue(selectedTransition);
                }

                // Write Defaultsの設定
                foreach (var state in states)
                {
                    if (Utility.IsBlendTreeState(state) && keepWriteDefaultsOfBlendTree)
                    {
                        continue;
                    }
                    if (writeDefaultsOff)
                    {
                        state.writeDefaultValues = false;
                    }
                }

                // 変更を保存する
                SaveChanges();
            }
            EditorGUI.EndDisabledGroup();
        }

        // SubStateMachineを含むチェックボックスを描画
        private void DrawMainOptions()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    includeSubStateMachine = EditorGUILayout.ToggleLeft(Localization.lang.includeSubStateMachineText, includeSubStateMachine);
                    writeDefaultsOff = EditorGUILayout.ToggleLeft(Localization.lang.writeDefaultsOffText, writeDefaultsOff);
                }
            }
        }

        // エラーメッセージの描画
        private void DrawErrorBox()
        {
            List<string> messages = GetErrorMessages();
            if (messages.Count > 0)
            {
                foreach (string message in messages)
                {
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                }
            }
        }

        // 設定の描画
        private void DrawSettingsFoldOut()
        {
            float LabelWidth = Utility.GetNomalFontStyle().CalcSize(new GUIContent(Localization.lang.keepWriteDefaultsOfBlendTree)).x + SETTINGS_LABEL_WIDTH_OFFSET;
            showSettings = EditorGUILayout.Foldout(showSettings, Localization.lang.settingsLabelText);
            if (showSettings)
            {
                using (new LabelWidthScope(LabelWidth))
                {
                    hasExitTime = EditorGUILayout.Toggle("Has Exit Time", hasExitTime);
                }
                if (!hasExitTime)
                {

                    using (new EditorGUI.IndentLevelScope())
                    {
                        ignoreNoCondition = EditorGUILayout.ToggleLeft(Localization.lang.ignoreNoConditionText, ignoreNoCondition);
                        if (!ignoreNoCondition)
                        {
                            EditorGUILayout.HelpBox(Localization.lang.warnNeedsConditionOrExitTime, MessageType.Warning);
                        }
                    }
                }
                using (new LabelWidthScope(LabelWidth))
                {
                    exitTime = EditorGUILayout.FloatField("Exit Time", exitTime);
                    fixedDuration = EditorGUILayout.Toggle("Fixed Duration", fixedDuration);
                    transitionDuration = EditorGUILayout.IntField("Transition Duration", transitionDuration);
                    transitionOffset = EditorGUILayout.IntField("Transition Offset", transitionOffset);
                    keepWriteDefaultsOfBlendTree = EditorGUILayout.Toggle(Localization.lang.keepWriteDefaultsOfBlendTree, keepWriteDefaultsOfBlendTree);
                }
            }
        }

        // レイヤーの取得
        private List<AnimatorControllerLayer> GetTargetLayer(List<AnimatorControllerLayer> layers)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (!layerEnabled[i])
                {
                    layers.RemoveAt(i);
                }
            }

            return layers;
        }

        // トランジションの設定
        private void SetTransitionValue(AnimatorStateTransition transition)
        {
            if (!(transition.conditions.Length == 0 && ignoreNoCondition))
            {
                transition.hasExitTime = hasExitTime;
            }
            transition.exitTime = exitTime;
            transition.hasFixedDuration = fixedDuration;
            transition.duration = transitionDuration;
            transition.offset = transitionOffset;
        }

        // トランジションの全取得
        private List<AnimatorStateTransition> GetSubStateMachineTransitions(List<AnimatorControllerLayer> layers)
        {
            List<AnimatorStateTransition> result = new List<AnimatorStateTransition>();
            List<AnimatorStateTransition[]> AllTransitionsList = new List<AnimatorStateTransition[]>();
            foreach (AnimatorControllerLayer layer in layers)
            {
                Utility.GetAllStatesTransitions(layer.stateMachine, null, AllTransitionsList);
                foreach (AnimatorStateTransition[] transitions in AllTransitionsList)
                {
                    foreach (AnimatorStateTransition transition in transitions)
                    {
                        result.Add(transition);
                    }
                }
            }
            return result;
        }

        // 再帰処理: ステートの取得
        private List<AnimatorState> GetSubStateMachineStates(List<AnimatorControllerLayer> layers)
        {
            List<AnimatorState> result = new List<AnimatorState>();
            foreach (AnimatorControllerLayer layer in layers)
            {
                Utility.GetAllStates(layer.stateMachine, null, result);
            }
            return result;
        }

        // エラーメッセージ取得
        private List<string> GetErrorMessages()
        {
            List<string> messages = new List<string>();
            if (isAnimatorControlerEmpty())
            {
                messages.Add(Localization.lang.errorMessage);
            }
            else if (!isLayerSelectedAtLeastOne())
            {
                messages.Add(Localization.lang.errorNeedsToSelectLayer);
            }
            return messages;
        }

        // Animator Controllerが空の状態か
        private bool isAnimatorControlerEmpty()
        {
            return animatorController == null;
        }

        // レイヤーが一つ以上選択されているか
        private bool isLayerSelectedAtLeastOne()
        {
            foreach (bool isEnabled in layerEnabled)
            {
                if (isEnabled)
                {
                    return true;
                }
            }
            return false;
        }

        // 設定の保存
        private void SaveChanges()
        {
            Debug.Log(Localization.lang.logMessage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
