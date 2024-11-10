using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SocialPlatforms;

// Copyright (c) 2023-2024 kesera2
namespace dev.kesera2.transition_helper
{
    /// <summary>
    /// TransitionHelperViewクラスは、UnityのEditorWindowを拡張してトランジションの操作を補助するためのウィンドウを提供します。
    /// </summary>
    public class TransitionHelperView : EditorWindow
    {
        private const string ToolName = "Transition Helper"; // ツール名
        private const int WindowMinHeight = 600;
        private const int WindowMinWidth = 550;
        private static Texture2D _logo; // ロゴのテクスチャ
        private Vector2 _scrollPosition = Vector2.zero; // スクロール位置
        private AnimatorController _animatorController; // アニメーターコントローラー指定時のAnimatorController
        private bool[] _layerEnabled = { }; // 有効状態のレイヤー
        private bool _includeSubStateMachine = true; // サブステートマシンのチェックボックスの有効状態
        private bool _ignoreNoCondition = true; // Conditionsの指定のないHasExitTimeの設定を無視する
        private bool _writeDefaultsOff = true; // 設定
        private bool _showSettings; // 設定を表示するかどうか
        private bool _hasExitTime; // トランジションに出口時間があるかどうか
        private float _exitTime; // 遷移終了時間（0から1の範囲）
        private bool _fixedDuration = true; // トランジションの固定時間を使用するかどうか
        private int _transitionDuration; // トランジションの固定時間（ミリ秒）
        private int _transitionOffset; // トランジションのオフセット時間（ミリ秒）
        private bool _keepWriteDefaultsOfBlendTree = true; // Blend Treeのデフォルト値を保持するかどうか
        private const float SettingsLabelWidthOffset = 10f; // 設定のラベルとコンテンツの間の空欄の幅
        private string[] _tabToggles; // Tabの表示名
        private int _tabIndex; // 選択中のTab
        private AnimatorStateTransition[] _selectedStateTransitions; // ステートからのトランジション
        private AnimatorTransition[] _selectedStateMachineTransitions; // ステートマシンからのトランジション
        private Dictionary<int, string> _destSourceTransitionPairs; // ステートのインスタンスIDとトランジションを紐付ける辞書
        private int _selectedTransitionCount; // 選択中のトランジションの数
        private bool _executeButtonDisabled; // 実行ボタンの非活性の有無
        private bool _showTransitions = true; // 選択中のトランジションを表示するかどうか(Foldに使用）
        private readonly List<Tuple<string, MessageType>> _messages =  new List<Tuple<string, MessageType>>(); // メッセージ
        private int _lastSelectedLanguage;
        private bool IsInvalidTransitionSelected;

        [MenuItem("Tools/kesera2/" + ToolName)]
        public static void OpenWindow()
        {
            Localization.LoadLanguage();
            var window = GetWindow<TransitionHelperView>();
            window.titleContent = new GUIContent(ToolName);
            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
            window.Show();
        }

        private void OnEnable()
        {
            _lastSelectedLanguage = Localization.CurrentLanguage;
            _tabToggles = new[]
                { Localization.S("layerSpecificationMode"), Localization.S("transitionSpecificationMode") };
        }

        private void OnDisable()
        {
            Localization.SaveLanguage();
        }

        public void OnInspectorUpdate()
        {
            // レイヤー名の更新がマウスオーバー時になるのを防ぐ
            Repaint();
        }

        private void OnGUI()
        {
            // 描画範囲が足りなければスクロール出来るように
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawLogo();
            DrawLanguagePopup();
            DrawInformation();
            DrawTabs();
            DrawAnimatorController();
            if (IsSpecifiedLayerTab())
            {
                DrawToggleButtons();
                DrawMainOptions();
                DrawLayers();
            }
            else if (IsSpecifiedTransitionTab())
            {
                DrawTransitionMenu();
                // 遷移元 -> 遷移先のリストを描画
                DrawTransitionInfo();
            }

            DrawErrorBox();
            DrawSettingsFoldOut();
            DrawExecuteButton();
            IsStateTransitionSelected();
            //スクロール箇所終了
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// ロゴを描画するためのメソッドです。
        /// </summary>
        private static void DrawLogo()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _logo = Resources.Load<Texture2D>("Icon/Logo");
            EditorGUILayout.LabelField(new GUIContent(_logo), GUILayout.Height(100), GUILayout.Width(400));
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
                _tabIndex = GUILayout.Toolbar(_tabIndex, _tabToggles, new GUIStyle(EditorStyles.toolbarButton),
                    GUI.ToolbarButtonSize.FitToContents);
            }
        }

        /// <summary>
        /// Animator Controller関連の描画を行うためのメソッドです。
        /// </summary>
        private void DrawAnimatorController()
        {
            _animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller",
                _animatorController, typeof(AnimatorController), false);
            if (!_animatorController) return;
            using (new EditorGUI.IndentLevelScope())
            {
                // AnimatorControllerに設定されているレイヤーの名前をチェックボックスとして表示する
                if (_layerEnabled != null && _layerEnabled.Length == _animatorController.layers.Length) return;
                _layerEnabled = new bool[_animatorController.layers.Length];
                _layerEnabled = _animatorController.layers.Select(_ => false).ToArray(); // デフォルトはアンチェック
            }
        }

        /// <summary>
        /// レイヤーのリストを描画を行うためのメソッドです。
        /// </summary>
        private void DrawLayers()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                if (!_animatorController) return;
                for (var i = 0; i < _animatorController.layers.Length; i++)
                {
                    _layerEnabled[i] =
                        EditorGUILayout.ToggleLeft("　" + _animatorController.layers[i].name, _layerEnabled[i]);
                }
            }
        }

        /// <summary>
        /// トランジション指定モードの描画を行うためのメソッドです。
        /// </summary>
        private void DrawTransitionMenu()
        {
            if (!_animatorController) return;
            _selectedStateTransitions =
                Selection.objects.OfType<AnimatorStateTransition>().Where(_ => true).ToArray(); // 選択中ステートマシンのトランジション
            _selectedStateMachineTransitions =
                Selection.objects.OfType<AnimatorTransition>().Where(_ => true).ToArray(); // 選択中サブステートマシンのトランジション
            _selectedTransitionCount =
                _selectedStateTransitions.Length + _selectedStateMachineTransitions.Length; // 選択中のトランジションの数を合算
            _destSourceTransitionPairs = Utility.GetDestSourceTransitionPairs(_animatorController); // ステート名辞書を取得
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Localization.S("selectAllTransitionsButton")))
                {
                    Utility.SelectAllTransitions(Utility.GetSelectedLayer(_animatorController));
                }

                if (GUILayout.Button(Localization.S("unselectTransitionsButton")))
                {
                    Utility.UnselectTransitions();
                }
            }

            EditorGUILayout.BeginVertical("box");
            // 選択中のトランジションのフォールドを表示（デフォルト表示）
            _showTransitions = EditorGUILayout.Foldout(_showTransitions,
                string.Format(Localization.S("selectedTransitionsCount"), _selectedTransitionCount));

            EditorGUILayout.EndVertical();
        }

        private bool CheckIsInvalidTransition( int instanceID)
        {
            if (_destSourceTransitionPairs.ContainsKey(instanceID)) return false;
            IsInvalidTransitionSelected = true;
            EditorGUI.indentLevel = 0;
            return true;
        }
        
        private void DrawTransitionInfo()
        {
            if (!_showTransitions) return;
            EditorGUI.indentLevel++;

            IsInvalidTransitionSelected = false;
            // ステートマシンの遷移元 -> 遷移先のリストを描画
            foreach (var transition in _selectedStateTransitions)
            {
                var sourceStateName = string.Empty;
                var destStateName = string.Empty;
                
                if (transition.destinationState)
                {
                    if (CheckIsInvalidTransition(transition.destinationState.GetInstanceID()))
                    {
                        return;
                    }
                    sourceStateName = _destSourceTransitionPairs[transition.destinationState.GetInstanceID()];
                    destStateName = transition.destinationState.name;
                }
                else if (transition.destinationStateMachine)
                {
                    if (CheckIsInvalidTransition(transition.destinationStateMachine.GetInstanceID()))
                    {
                        return;
                    }
                    sourceStateName = _destSourceTransitionPairs[transition.destinationStateMachine.GetInstanceID()];
                    destStateName = transition.destinationStateMachine.name;
                }
                else if (transition.isExit)
                {
                    if (CheckIsInvalidTransition(transition.GetInstanceID()))
                    {
                        return;
                    }
                    sourceStateName = _destSourceTransitionPairs[transition.GetInstanceID()];
                    destStateName = "Exit";
                }

                if (!string.IsNullOrEmpty(destStateName) && !string.IsNullOrEmpty(sourceStateName))
                {
                    EditorGUILayout.LabelField($"{sourceStateName} -> {destStateName}");
                }
            }

            // サブステートマシンの遷移元 -> 遷移先のリストを描画
            foreach (var transition in _selectedStateMachineTransitions)
            {
                var sourceStateName = string.Empty;
                var destStateName = string.Empty;
                if (transition.destinationState)
                {
                    if (transition.destinationState)
                    {
                        if (CheckIsInvalidTransition(transition.destinationState.GetInstanceID()))
                        {
                            return;
                        }
                        sourceStateName = _destSourceTransitionPairs[transition.destinationState.GetInstanceID()];
                        destStateName = transition.destinationState.name;
                    }
                    else if (transition.destinationStateMachine)
                    {
                        if (CheckIsInvalidTransition(transition.destinationStateMachine.GetInstanceID()))
                        {
                            return;
                        }
                        sourceStateName =
                            _destSourceTransitionPairs[transition.destinationStateMachine.GetInstanceID()];
                        destStateName = transition.destinationStateMachine.name;
                    }
                }
                else if (transition.destinationStateMachine)
                {
                    if (CheckIsInvalidTransition(transition.destinationStateMachine.GetInstanceID()))
                    {
                        return;
                    }
                    sourceStateName = _destSourceTransitionPairs[transition.destinationStateMachine.GetHashCode()];
                    destStateName = transition.destinationStateMachine.name;
                }
                else if (transition.isExit)
                {
                    if (CheckIsInvalidTransition(transition.GetInstanceID()))
                    {
                        return;
                    }
                    sourceStateName = _destSourceTransitionPairs[transition.GetInstanceID()];
                    destStateName = "Exit";
                }

                if (!string.IsNullOrEmpty(destStateName) && !string.IsNullOrEmpty(sourceStateName))
                {
                    EditorGUILayout.LabelField($"{sourceStateName} -> {destStateName}");
                }
            }

            EditorGUI.indentLevel = 0;
        }

        /// <summary>
        /// トグルボタンの描画を行うためのメソッドです。
        /// </summary>
        private void DrawToggleButtons()
        {
            if (_animatorController) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                // すべてのチェックボックスをONにするボタン
                if (GUILayout.Button(Localization.S("toggleAll")))
                {
                    for (var i = 0; i < _layerEnabled.Length; i++)
                    {
                        _layerEnabled[i] = true;
                    }
                }

                // すべてのチェックボックスをOFFにするボタン
                if (!GUILayout.Button(Localization.S("toggleNone"))) return;
                {
                    for (var i = 0; i < _layerEnabled.Length; i++)
                    {
                        _layerEnabled[i] = false;
                    }
                }
            }
        }

        /// <summary>
        /// 説明の描画を行うためのメソッドです。
        /// </summary>
        private void DrawInformation()
        {
            EditorGUILayout.HelpBox(Localization.S("infoExplainMessage"), MessageType.Info);
        }

        /// <summary>
        /// 確認ダイアログを表示するためのメソッドです。
        /// </summary>
        /// <returns>ダイアログでのユーザーの選択結果を表すbool値</returns>
        private bool DisplayConfirmDialog()
        {
            return EditorUtility.DisplayDialog(
                Localization.S("confirmTitle"),
                Localization.S("confirmContent"),
                Localization.S("answerYes"),
                Localization.S("answerNo"));
        }

        private void DisplayComplete()
        {
            EditorUtility.DisplayDialog(
                Localization.S("confirmTitle"),
                Localization.S("completeMessage"),
                "OK");
        }

        /// <summary>
        /// 実行ボタンの描画を行うためのメソッドです。
        /// </summary>
        private void DrawExecuteButton()
        {
            // アニメーターが選択されていないまたはレイヤーが1つも選択されていない場合、実行ボタンをDisable
            _executeButtonDisabled = IsAnimatorControllerEmpty() ||
                                     !(IsSpecifiedLayerTab() && isLayerSelectedAtLeastOne()) &&
                                     !IsSelectedTransitions() || HasErrorMessage();
            EditorGUI.BeginDisabledGroup(_executeButtonDisabled);
            if (GUILayout.Button(Localization.S("setupButtonText"), GUILayout.Height(40)))
            {
                if (!DisplayConfirmDialog())
                {
                    return;
                }

                // レイヤー指定の場合
                if (IsSpecifiedLayerTab())
                {
                    SetupLayers();
                }
                // トランジション指定の場合
                else if (IsSpecifiedTransitionTab())
                {
                    SetupSelectedTransitions();
                }

                // 変更を保存する
                Utility.SaveChanges();
                DisplayComplete();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// レイヤーの設定を行うためのメソッドです。レイヤー指定モードに使用。
        /// </summary>
        private void SetupLayers()
        {
            List<AnimatorStateTransition> transitions;
            List<AnimatorControllerLayer> layers = GetTargetLayer(_animatorController.layers.ToList());
            List<AnimatorState> states = null;
            // トランジション全取得
            if (_includeSubStateMachine)
            {
                // サブステートマシンがある場合
                transitions = GetSubStateMachineTransitions(layers);
                states = GetSubStateMachineStates(layers);
            }
            else
            {
                // ステートのみの場合
                transitions = layers.Select(c => c.stateMachine).SelectMany(c => c.states)
                    .SelectMany(c => c.state.transitions).ToList();
            }

            // Write Defaultsの設定
            SetupWriteDefaultsToLayer(states);
            //取得したtransition全てに適用させる
            foreach (AnimatorStateTransition transition in transitions)
            {
                SetTransitionValue(transition);
            }
        }

        /// <summary>
        /// 選択されたトランジションのセットアップを行うためのメソッドです。トランジション指定モードに使用。
        /// </summary>
        private void SetupSelectedTransitions()
        {
            var selectedTransitions =
                Selection.objects.Select(x => x as AnimatorStateTransition).Where(y => y).ToArray();
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
                    _includeSubStateMachine = EditorGUILayout.ToggleLeft(Localization.S("includeSubStateMachineText"),
                        _includeSubStateMachine);
                    _writeDefaultsOff =
                        EditorGUILayout.ToggleLeft(Localization.S("writeDefaultsOffText"), _writeDefaultsOff);
                }
            }
        }

        /// <summary>
        /// SubStateMachineを含むチェックボックスを描画するためのメソッドです。
        /// </summary>
        private void DrawErrorBox()
        {
            GetErrorMessages();
            if (_messages != null)
            {
                foreach (var message in _messages)
                {
                    EditorGUILayout.HelpBox(message.Item1, message.Item2);
                }
            }
        }

        private void DrawLanguagePopup()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                _lastSelectedLanguage = EditorGUILayout.Popup(_lastSelectedLanguage, Localization.DisplayNames);
                if (Localization.CurrentLanguage != _lastSelectedLanguage)
                {
                    Localization.CurrentLanguage = _lastSelectedLanguage;
                    Localization.Localize();
                    _tabToggles = new[]
                        { Localization.S("layerSpecificationMode"), Localization.S("transitionSpecificationMode") };
                }
            }
        }

        /// <summary>
        /// 設定の描画を行うためのメソッドです。
        /// </summary>
        private void DrawSettingsFoldOut()
        {
            var labelWidth =
                Utility.GetNormalFontStyle().CalcSize(new GUIContent(Localization.S("keepWriteDefaultsOfBlendTree")))
                    .x + SettingsLabelWidthOffset;
            _showSettings = EditorGUILayout.Foldout(_showSettings, Localization.S("settingsLabelText"));
            if (!_showSettings) return;
            using (new LabelWidthScope(labelWidth))
            {
                _hasExitTime = EditorGUILayout.Toggle("Has Exit Time", _hasExitTime);
            }

            if (!_hasExitTime)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _ignoreNoCondition =
                        EditorGUILayout.ToggleLeft(Localization.S("ignoreNoConditionText"), _ignoreNoCondition);
                    if (!_ignoreNoCondition)
                    {
                        EditorGUILayout.HelpBox(Localization.S("warnNeedsConditionOrExitTime"), MessageType.Warning);
                    }
                }
            }

            using (new LabelWidthScope(labelWidth))
            {
                _exitTime = EditorGUILayout.FloatField("Exit Time", _exitTime);
                _fixedDuration = EditorGUILayout.Toggle("Fixed Duration", _fixedDuration);
                _transitionDuration = EditorGUILayout.IntField("Transition Duration", _transitionDuration);
                _transitionOffset = EditorGUILayout.IntField("Transition Offset", _transitionOffset);
                _keepWriteDefaultsOfBlendTree = EditorGUILayout.Toggle(Localization.S("keepWriteDefaultsOfBlendTree"),
                    _keepWriteDefaultsOfBlendTree);
            }
        }

        /// <summary>
        /// ターゲットレイヤーを取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト</param>
        /// <returns>ターゲットレイヤーのリスト。</returns>
        private List<AnimatorControllerLayer> GetTargetLayer(List<AnimatorControllerLayer> layers)
        {
            return layers.Where((_, index) => _layerEnabled[index]).ToList();
        }

        /// <summary>
        /// トランジションの設定を行うためのメソッドです。
        /// </summary>
        /// <param name="transition">設定するトランジション</param>
        private void SetTransitionValue(AnimatorStateTransition transition)
        {
            if (!(transition.conditions.Length == 0 && _ignoreNoCondition))
            {
                transition.hasExitTime = _hasExitTime;
            }

            transition.exitTime = _exitTime;
            transition.hasFixedDuration = _fixedDuration;
            transition.duration = _transitionDuration;
            transition.offset = _transitionOffset;
        }

        /// <summary>
        /// Write Defaultsの設定を行うためのメソッドです。
        /// </summary>
        /// <param name="states">ステートのリスト</param>
        private void SetupWriteDefaultsToLayer(List<AnimatorState> states)
        {
            foreach (var state in states
                         .Where(state => !Utility.IsBlendTreeState(state) || !_keepWriteDefaultsOfBlendTree)
                         .Where(_ => _writeDefaultsOff))
            {
                state.writeDefaultValues = false;
            }
        }

        /// <summary>
        /// サブステートマシンのトランジションを全て取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト。</param>
        /// <returns>サブステートマシンのトランジションのリスト</returns>
        private static List<AnimatorStateTransition> GetSubStateMachineTransitions(List<AnimatorControllerLayer> layers)
        {
            var result = new List<AnimatorStateTransition>();
            var allTransitionsList = new List<AnimatorStateTransition[]>();
            foreach (var layer in layers)
            {
                Utility.GetAllStatesTransitions(layer.stateMachine, null, allTransitionsList);
                result.AddRange(allTransitionsList.SelectMany(transitions => transitions));
            }

            return result;
        }

        /// <summary>
        /// サブステートマシン内のステートを再帰的に取得するためのメソッドです。
        /// </summary>
        /// <param name="layers">全体のレイヤーリスト。</param>
        /// <returns>サブステートマシン内のステートのリスト</returns>
        private static List<AnimatorState> GetSubStateMachineStates(List<AnimatorControllerLayer> layers)
        {
            var result = new List<AnimatorState>();
            foreach (var layer in layers)
            {
                Utility.GetAllStates(layer.stateMachine, null, result);
            }

            return result;
        }

        /// <summary>
        /// エラーメッセージを取得するためのメソッドです。
        /// </summary>
        /// <returns>エラーメッセージのリスト</returns>
        private void GetErrorMessages()
        {
            _messages.Clear();
            if (IsAnimatorControllerEmpty())
            {
                _messages.Add(Tuple.Create(Localization.S("errorMessage"), MessageType.Error));
            }
            else if (IsSpecifiedLayerTab())
            {
                if (!isLayerSelectedAtLeastOne())
                {
                    _messages.Add(Tuple.Create(Localization.S("errorNeedsToSelectLayer"), MessageType.Error));
                }
            }
            else if (IsSpecifiedTransitionTab())
            {
                if (!IsSelectedTransitions())
                {
                    _messages.Add(Tuple.Create(Localization.S("errorNeedsToSelectTransition"), MessageType.Error));
                }
                else if (IsInvalidTransitionSelected)
                {
                    _messages.Add(Tuple.Create(Localization.S("errorInvalidTransitionSelected"),
                        MessageType.Error));
                }
                else if (IsStateTransitionSelected() && IsStateMachineTransitionSelected())
                {
                    _messages.Add(Tuple.Create(Localization.S("warnStateMachineTransitionSelected"),
                        MessageType.Warning));
                }
                else if (IsStateMachineTransitionSelected())
                {
                    _messages.Add(Tuple.Create(Localization.S("errorNeedsToSelectStateTransition"),
                        MessageType.Error));
                }
            }
        }

        /// <summary>
        /// Animator Controllerが空の状態かどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>Animator Controllerが空の状態であればtrue、そうでなければfalse</returns>
        private bool IsAnimatorControllerEmpty()
        {
            return _animatorController == null;
        }

        /// <summary>
        /// レイヤーが一つ以上選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>レイヤーが一つ以上選択されていればtrue、そうでなければfalse</returns>
        private bool isLayerSelectedAtLeastOne()
        {
            return _layerEnabled.Any(isEnabled => isEnabled);
        }

        /// <summary>
        /// 表示中のレイヤーでトランジションが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>トランジション選択されていればtrue、そうでなければfalse</returns>
        private bool IsSelectedTransitions()
        {
            if (IsSpecifiedTransitionTab() && _selectedStateTransitions != null)
            {
                return _selectedTransitionCount > 0;
            }

            return false;
        }

        /// <summary>
        /// レイヤー指定モードタブが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>レイヤー指定モードタブが選択されていればtrue、そうでなければfalse。</returns>
        private bool IsSpecifiedLayerTab()
        {
            return _tabIndex == 0;
        }

        /// <summary>
        /// トランジション指定モードが選択されているかどうかを判定するためのメソッドです。
        /// </summary>
        /// <returns>トランジション指定モードが選択されていればtrue、そうでなければfalse。</returns>
        private bool IsSpecifiedTransitionTab()
        {
            return _tabIndex == 1;
        }

        /**
         * ステートのトランジションが選択されているか
         */
        private bool IsStateTransitionSelected()
        {
            return _selectedStateTransitions != null && _selectedStateTransitions.Any();
        }

        /**
         * ステートマシンのトランジションが選択されているか
         */
        private bool IsStateMachineTransitionSelected()
        {
            return _selectedStateMachineTransitions != null && _selectedStateMachineTransitions.Any();
        }

        private bool HasErrorMessage()
        {
            return _messages != null && _messages.Any(m => m.Item2 == MessageType.Error);
        }
    }
}