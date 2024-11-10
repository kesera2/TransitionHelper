using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Copyright (c) 2023 kesera2
namespace TransitionHelper
{
    /// <summary>
    /// TransitionHelperViewクラスは、UnityのEditorWindowを拡張してトランジションの操作を補助するためのウィンドウを提供します。
    /// </summary>
    public class TransitionHelperView : EditorWindow
    {
        private static Texture2D logo;                          // ロゴのテクスチャ
        private Vector2 _scrollPosition = Vector2.zero;         // スクロール位置
        private AnimatorController animatorController;          // アニメーターコントローラー指定時のAnimatorController
        private bool[] layerEnabled = { };                      // 有効状態のレイヤー
        bool includeSubStateMachine = true;                     // サブステートマシンのチェックボックスの有効状態
        bool ignoreNoCondition = true;                          // Conditionsの指定のないHasExitTimeの設定を無視する
        bool writeDefaultsOff = true;                           // 設定
        bool showSettings = false;                              // 設定を表示するかどうか
        bool hasExitTime = false;                               // トランジションに出口時間があるかどうか
        float exitTime = 0;                                     // 遷移終了時間（0から1の範囲）
        bool fixedDuration = true;                              // トランジションの固定時間を使用するかどうか
        int transitionDuration = 0;                             // トランジションの固定時間（ミリ秒）
        int transitionOffset = 0;                               // トランジションのオフセット時間（ミリ秒）
        bool keepWriteDefaultsOfBlendTree = true;               // Blend Treeのデフォルト値を保持するかどうか
        private const float SETTINGS_LABEL_WIDTH_OFFSET = 10f;  // 設定のラベルとコンテンツの間の空欄の幅
        private string[] _tabToggles;                           // Tabの表示名
        private int _tabIndex;                                  // 選択中のTab
        AnimatorStateTransition[] selectedStateTransitions;     // ステートからのトランジション
        AnimatorTransition[] selectedStateMachineTransitions;   // ステートマシンからのトランジション
        Dictionary<int, string> destSourceTransitionPairs;      // ステートのインスタンスIDとトランジションを紐付ける辞書
        private int selectedTransitionCount = 0;                // 選択中のトランジションの数
        private bool executeButtonDisabled;                     // 実行ボタンの非活性の有無
        bool showTransitions = true;                            // 選択中のトランジションを表示するかどうか(Foldに使用）

        [MenuItem("Tools/もちもちまーと/Transition Helper")]
        public static void OpenWindow()
        {
            TransitionHelperView window = GetWindow<TransitionHelperView>();
            window.titleContent = new GUIContent("Transition Helper");
            window.Show();
        }

        private void OnEnable()
        {
            Localization.Localize();
            _tabToggles = new string[] { Localization.lang.layerSpecificationMode, Localization.lang.transitionSpecificationMode };
        }

        void OnInspectorUpdate()
        {
            // レイヤー名の更新がマウスオーバー時になるのを防ぐ
            Repaint();
        }

        private void OnGUI()
        {
            // 描画範囲が足りなければスクロール出来るように
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawLogo();
            DrawInfomation();
            DrawTabs();
            DrawAnimatorController();
            if (isSpecifiedLayerTab())
            {
                DrawToggleButtons();
                DrawMainOptions();
                DrawLayers();
            }
            else if (isSpecifiedTransitionTab())
            {
                DrawTransitionMenu();
            }
            DrawErrorBox();
            DrawSettingsFoldOut();
            DrawExecuteButton();
            DrawDebugTransitionName();
            
            //スクロール箇所終了
            EditorGUILayout.EndScrollView();
        }

        void DrawDebugTransitionName()
        {
            if (GUILayout.Button("Debug Transition Name"))
            {
                destSourceTransitionPairs = Utility.GetDestSourceTransitionPairs(animatorController); // ステート名辞書を取得
                foreach (KeyValuePair<int, string> kvp in destSourceTransitionPairs)
                {
                    Debug.Log("Key:" + kvp.Key + " Value:" + kvp.Value);
                }
            }
        }

        /// <summary>
        /// ロゴを描画するためのメソッドです。
        /// </summary>
        private static void DrawLogo()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            logo = Resources.Load<Texture2D>("Icon/Logo");
            EditorGUILayout.LabelField(new GUIContent(logo), GUILayout.Height(100), GUILayout.Width(400));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// タブを描画するためのメソッドです。
        /// </summary>
        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(_tabIndex, _tabToggles, new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);
            }
        }

        /// <summary>
        /// Animator Controller関連の描画を行うためのメソッドです。
        /// </summary>
        private void DrawAnimatorController()
        {
            animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
            if (animatorController == null)
            {
                return;
            }
            using (new EditorGUI.IndentLevelScope())
            {
                // AnimatorControllerに設定されているレイヤーの名前をチェックボックスとして表示する
                if (layerEnabled == null || layerEnabled.Length != animatorController.layers.Length)
                {
                    layerEnabled = new bool[animatorController.layers.Length];
                    for (int i = 0; i < animatorController.layers.Length; i++)
                    {
                        layerEnabled[i] = false; // デフォルトはアンチェック
                    }
                }
            }
        }

        /// <summary>
        /// レイヤーのリストを描画を行うためのメソッドです。
        /// </summary>
        private void DrawLayers()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                if (animatorController != null)
                {
                    for (int i = 0; i < animatorController.layers.Length; i++)
                    {
                        layerEnabled[i] = EditorGUILayout.ToggleLeft("　" + animatorController.layers[i].name, layerEnabled[i]);
                    }
                }
            }
        }

        /// <summary>
        /// トランジション指定モードの描画を行うためのメソッドです。
        /// </summary>
        private void DrawTransitionMenu()
        {
            if (animatorController == null)
            {
                return;
            }
            selectedStateTransitions = Selection.objects.OfType<AnimatorStateTransition>().Where(t => t != null).ToArray(); // 選択中ステートマシンのトランジション
            selectedStateMachineTransitions = Selection.objects.OfType<AnimatorTransition>().Where(t => t != null).ToArray(); // 選択中サブステートマシンのトランジション
            selectedTransitionCount = selectedStateTransitions.Length + selectedStateMachineTransitions.Length; // 選択中のトランジションの数を合算
            destSourceTransitionPairs = Utility.GetDestSourceTransitionPairs(animatorController); // ステート名辞書を取得
            GUILayout.Label(string.Format(Localization.lang.selectedLayer, Utility.GetSelectedLayerName(animatorController))); // 選択中のレイヤーラベル
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Localization.lang.selectAllTransitionsButton))
                {
                    Utility.SelectAllTransitions(Utility.GetSelectedLayer(animatorController));
                }
                if (GUILayout.Button(Localization.lang.unselectTransitionsButton))
                {
                    Utility.UnselectTransitions();
                }
            }
            EditorGUILayout.BeginVertical("box");
            // 選択中のトランジションのフォールドを表示（デフォルト表示）
            showTransitions = EditorGUILayout.Foldout(showTransitions, string.Format(Localization.lang.selectedTransitionsCount, selectedTransitionCount));
            // 遷移元 -> 遷移先のリストを描画
            if (showTransitions)
            {
                EditorGUI.indentLevel++;
                // ステートマシンの遷移元 -> 遷移先のリストを描画
                foreach (AnimatorStateTransition transition in selectedStateTransitions)
                {
                    string sourceStateName = string.Empty;
                    string destStateName = string.Empty;
                    if (transition.destinationState != null)
                    {
                        sourceStateName = destSourceTransitionPairs[transition.destinationState.GetInstanceID()];
                        // Debug.Log("transition instance ID :" + transition.GetInstanceID());
                        destStateName = transition.destinationState.name;
                        if (sourceStateName == null)
                        {
                            Debug.Log("Null source state name");
                        }
                    }
                    else if (transition.destinationStateMachine != null)
                    {
                        sourceStateName = destSourceTransitionPairs[transition.destinationStateMachine.GetInstanceID()];
                        destStateName = transition.destinationStateMachine.name;
                    }
                    else if (transition.isExit)
                    {
                        sourceStateName = destSourceTransitionPairs[transition.GetInstanceID()];
                        destStateName = "Exit";
                    }
                    if (!string.IsNullOrEmpty(destStateName) && !string.IsNullOrEmpty(sourceStateName))
                    {
                        EditorGUILayout.LabelField($"{sourceStateName} -> {destStateName}");
                    }
                }
                // サブステートマシンの遷移元 -> 遷移先のリストを描画
                foreach (AnimatorTransition transition in selectedStateMachineTransitions)
                {
                    string sourceStateName = string.Empty;
                    string destStateName = string.Empty;
                    if (transition.destinationState != null)
                    {
                        if (transition.destinationState != null)
                        {
                            sourceStateName = destSourceTransitionPairs[transition.destinationState.GetInstanceID()];
                            destStateName = transition.destinationState.name;
                        }
                        else if (transition.destinationStateMachine != null)
                        {
                            sourceStateName = destSourceTransitionPairs[transition.destinationStateMachine.GetInstanceID()];
                            destStateName = transition.destinationStateMachine.name;
                        }
                    }
                    else if (transition.destinationStateMachine != null)
                    {
                        sourceStateName = destSourceTransitionPairs[transition.destinationStateMachine.GetHashCode()];
                        destStateName = transition.destinationStateMachine.name;
                    }
                    else if (transition.isExit)
                    {
                        sourceStateName = destSourceTransitionPairs[transition.GetInstanceID()];
                        destStateName = "Exit";
                    }
                    if (!string.IsNullOrEmpty(destStateName) && !string.IsNullOrEmpty(sourceStateName))
                    {
                        EditorGUILayout.LabelField($"{sourceStateName} -> {destStateName}");
                    }
                }
                EditorGUI.indentLevel = 0;
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// トグルボタンの描画を行うためのメソッドです。
        /// </summary>
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

        /// <summary>
        /// 説明の描画を行うためのメソッドです。
        /// </summary>
        private void DrawInfomation()
        {
            EditorGUILayout.HelpBox(Localization.lang.infoExplainMessage, MessageType.Info);
        }

        /// <summary>
        /// 確認ダイアログを表示するためのメソッドです。
        /// </summary>
        /// <returns>ダイアログでのユーザーの選択結果を表すbool値</returns>
        private bool DisplayConfirmDialog()
        {
            return EditorUtility.DisplayDialog(
            Localization.lang.confirmTitle,
            Localization.lang.confirmContent,
            Localization.lang.answerYes,
            Localization.lang.answerNo);
        }

        /// <summary>
        /// 実行ボタンの描画を行うためのメソッドです。
        /// </summary>
        private void DrawExecuteButton()
        {
            // アニメーターが選択されていないまたはレイヤーが1つも選択されていない場合、実行ボタンをDisable
            executeButtonDisabled = isAnimatorControlerEmpty() || !(isSpecifiedLayerTab() && isLayerSelectedAtLeastOne()) && !isSeletectedTransitions();
            EditorGUI.BeginDisabledGroup(executeButtonDisabled);
            if (GUILayout.Button(Localization.lang.setupButtonText, GUILayout.Height(40)))
            {
                if (!DisplayConfirmDialog())
                {
                    return;
                }
                // レイヤー指定の場合
                if (isSpecifiedLayerTab())
                {
                    SetupLayers();
                }
                // トランジション指定の場合
                else if (isSpecifiedTransitionTab())
                {
                    setupSelectedTransitions();
                }
                // 変更を保存する
                Utility.SaveChanges();
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// レイヤーの設定を行うためのメソッドです。レイヤー指定モードに使用。
        /// </summary>
        private void SetupLayers()
        {
            List<AnimatorStateTransition> transitions;
            List<AnimatorControllerLayer> layers = GetTargetLayer(animatorController.layers.ToList());
            List<AnimatorState> states = null;
            // トランジション全取得
            if (includeSubStateMachine)
            {
                // サブステートマシンがある場合
                transitions = GetSubStateMachineTransitions(layers);
                states = GetSubStateMachineStates(layers);
            }
            else
            {
                // ステートのみの場合
                transitions = layers.Select(c => c.stateMachine).SelectMany(c => c.states).SelectMany(c => c.state.transitions).ToList();
            }
            // Write Defaultsの設定
            setupWriteDefaultsToLayer(states);
            //取得したtransition全てに適用させる
            foreach (AnimatorStateTransition transition in transitions)
            {
                SetTransitionValue(transition);
            }
        }

        /// <summary>
        /// 選択されたトランジションのセットアップを行うためのメソッドです。トランジション指定モードに使用。
        /// </summary>
        private void setupSelectedTransitions()
        {
            AnimatorStateTransition[] selectedTransitions = Selection.objects.Select(x => x as AnimatorStateTransition).Where(y => y != null).ToArray();
            foreach (var selectedTransition in selectedTransitions)
            {
                SetTransitionValue(selectedTransition);
            }
        }

        /// <summary>
        /// レイヤー指定モードの「サブステートマシンを含む」「WriteDefaultsをOFFにする」チェックボックスを描画するためのメソッドです。
        /// </summary>
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

        /// <summary>
        /// SubStateMachineを含むチェックボックスを描画するためのメソッドです。
        /// </summary>
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

        /// <summary>
        /// 設定の描画を行うためのメソッドです。
        /// </summary>
        private void DrawSettingsFoldOut()
        {
            float labelWidth = Utility.GetNormalFontStyle().CalcSize(new GUIContent(Localization.lang.keepWriteDefaultsOfBlendTree)).x + SETTINGS_LABEL_WIDTH_OFFSET;
            showSettings = EditorGUILayout.Foldout(showSettings, Localization.lang.settingsLabelText);
            if (showSettings)
            {
                using (new LabelWidthScope(labelWidth))
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
                using (new LabelWidthScope(labelWidth))
                {
                    exitTime = EditorGUILayout.FloatField("Exit Time", exitTime);
                    fixedDuration = EditorGUILayout.Toggle("Fixed Duration", fixedDuration);
                    transitionDuration = EditorGUILayout.IntField("Transition Duration", transitionDuration);
                    transitionOffset = EditorGUILayout.IntField("Transition Offset", transitionOffset);
                    keepWriteDefaultsOfBlendTree = EditorGUILayout.Toggle(Localization.lang.keepWriteDefaultsOfBlendTree, keepWriteDefaultsOfBlendTree);
                }
            }
        }

        /// <summary>
        /// ターゲットレイヤーを取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト</param>
        /// <returns>ターゲットレイヤーのリスト。</returns>
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

        /// <summary>
        /// トランジションの設定を行うためのメソッドです。
        /// </summary>
        /// <param name="transition">設定するトランジション</param>
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

        /// <summary>
        /// Write Defaultsの設定を行うためのメソッドです。
        /// </summary>
        /// <param name="states">ステートのリスト</param>
        private void setupWriteDefaultsToLayer(List<AnimatorState> states)
        {
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
        }

        /// <summary>
        /// サブステートマシンのトランジションを全て取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト。</param>
        /// <returns>サブステートマシンのトランジションのリスト</returns>
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

        /// <summary>
        /// サブステートマシン内のステートを再帰的に取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト。</param>
        /// <returns>サブステートマシン内のステートのリスト</returns>
        private List<AnimatorState> GetSubStateMachineStates(List<AnimatorControllerLayer> layers)
        {
            List<AnimatorState> result = new List<AnimatorState>();
            foreach (AnimatorControllerLayer layer in layers)
            {
                Utility.GetAllStates(layer.stateMachine, null, result);
            }
            return result;
        }

        /// <summary>
        /// エラーメッセージを取得するためのメソッドです。
        /// </summary>
        /// <returns>エラーメッセージのリスト</returns>
        private List<string> GetErrorMessages()
        {
            List<string> messages = new List<string>();
            if (isAnimatorControlerEmpty())
            {
                messages.Add(Localization.lang.errorMessage);
            }
            else if (isSpecifiedLayerTab())
            {
                if (!isLayerSelectedAtLeastOne())
                {
                    messages.Add(Localization.lang.errorNeedsToSelectLayer);
                }
            }
            else if (isSpecifiedTransitionTab())
            {
                if (!isSeletectedTransitions())
                {
                    messages.Add(Localization.lang.errorNeedsToSelectTransition);
                }
            }

            return messages;
        }

        /// <summary>
        /// Animator Controllerが空の状態かどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>Animator Controllerが空の状態であればtrue、そうでなければfalse</returns>
        private bool isAnimatorControlerEmpty()
        {
            return animatorController == null;
        }

        /// <summary>
        /// レイヤーが一つ以上選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>レイヤーが一つ以上選択されていればtrue、そうでなければfalse</returns>
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

        /// <summary>
        /// 表示中のレイヤーでトランジションが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>トランジション選択されていればtrue、そうでなければfalse</returns>
        private bool isSeletectedTransitions()
        {
            if (isSpecifiedTransitionTab() && selectedStateTransitions != null)
            {
                return selectedTransitionCount > 0;
            }
            return false;
        }

        /// <summary>
        /// レイヤー指定モードタブが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>レイヤー指定モードタブが選択されていればtrue、そうでなければfalse。</returns>
        private bool isSpecifiedLayerTab()
        {
            return _tabIndex == 0;
        }

        /// <summary>
        /// トランジション指定モードが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>トランジション指定モードが選択されていればtrue、そうでなければfalse。</returns>
        private bool isSpecifiedTransitionTab()
        {
            return _tabIndex == 1;
        }
    }
}