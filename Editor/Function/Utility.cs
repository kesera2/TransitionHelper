using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace dev.kesera2.transition_helper
{
    internal static class Utility
    {
        /// <summary>
        /// 指定したアニメーターステートマシン内の親ステートのパスを取得します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="parentPath">親ステートのパス</param>
        /// <returns>親ステートのパス</returns>
        private static string GetParentPath(AnimatorStateMachine stateMachine, string parentPath)
        {
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentPath += ".";
            }
            parentPath += stateMachine.name;
            return parentPath;
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートを変数に追加します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="result">結果を格納するリスト</param>
        private static void AddState(AnimatorStateMachine stateMachine, List<AnimatorState> result)
        {
            result.AddRange(stateMachine.states.Select(state => state.state));
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートのトランジションを変数に追加します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="result">結果を格納するリスト</param>
        private static void AddTransition(AnimatorStateMachine stateMachine, List<AnimatorStateTransition[]> result)
        {
            result.AddRange(stateMachine.states.Select(state => state.state.transitions));
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートを再帰的に取得します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="parentPath">ステートの親パス</param>
        /// <param name="result">結果を格納するリスト</param>
        public static void GetAllStates(AnimatorStateMachine stateMachine, string parentPath, List<AnimatorState> result)
        {
            parentPath = GetParentPath(stateMachine, parentPath);

            // 全てのステートを処理
            AddState(stateMachine, result);

            // サブステートマシンを再帰的に処理
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                GetAllStates(subStateMachine.stateMachine, parentPath, result);
            }
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートのトランジションを取得します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="parentPath">ステートの親パス</param>
        /// <param name="result">結果を格納するリスト</param>

        public static void GetAllStatesTransitions(AnimatorStateMachine stateMachine, string parentPath, List<AnimatorStateTransition[]> result)
        {
            parentPath = GetParentPath(stateMachine, parentPath);

            // 全てのステートを処理
            AddTransition(stateMachine, result);

            // サブステートマシンを再帰的に処理
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                GetAllStatesTransitions(subStateMachine.stateMachine, parentPath, result);
            }
        }

        /// <summary>
        /// 指定したアニメーターステートがBlendTreeであるかを判定します。
        /// </summary>
        /// <param name="state">対象のアニメーターステート</param>
        /// <returns>BlendTreeである場合はtrue、そうでない場合はfalse</returns>
        public static bool IsBlendTreeState(AnimatorState state)
        {
            return state.motion is BlendTree;
        }

        // スタイルを取得
        public static GUIStyle GetNormalFontStyle()
        {
            var style = new GUIStyle
            {
                fontStyle = FontStyle.Normal
            };
            return style;
        }

        /// <summary>
        /// アニメーターコントローラーから、遷移先のステート名をキーにインスタンスIDをバリューにした辞書を取得します。
        /// </summary>
        /// <param name="animatorController">対象のアニメーターコントローラー</param>
        /// <returns>遷移先のステート名をキーにインスタンスIDをバリューにした辞書</returns>
        public static Dictionary<int, string> GetDestSourceTransitionPairs(AnimatorController animatorController)
        {
            var destSourceTransitionPairs = new Dictionary<int, string>();
            if (!animatorController)
            {
                return null;
            }
            // FIXME: 選択中のレイヤーに限定する <- 同じレイヤー名があると表示がバグる
            foreach (var layer in animatorController.layers)
            {
                GetStateMachineInfo(destSourceTransitionPairs, layer.stateMachine);
                GetSubStateMachineInfo(destSourceTransitionPairs, layer.stateMachine);
            }
            return destSourceTransitionPairs;
        }

        private static void GetStateMachineInfo(Dictionary<int, string> destSourceTransitionPairs,
            AnimatorStateMachine parentStateMachine)
        {
            if (!parentStateMachine) return;
            foreach (var state in parentStateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    // var sourceName = state.state.name;
                    // string destName = null;
                    var instanceId = 0;
                    // State -> State
                    if (transition.destinationState)
                    {
                        // destName = transition.destinationState.name;
                        // Debug.Log($"State -> State: {sourceName} -> {destName}");
                        instanceId = transition.destinationState.GetInstanceID();
                        GetStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                        GetSubStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                    }
                    // State -> SubState
                    else if (transition.destinationStateMachine)
                    {
                        // destName = transition.destinationStateMachine.name;
                        // Debug.Log($"State -> SubState: {sourceName} -> {destName}");
                        instanceId = transition.destinationStateMachine.GetInstanceID();
                        GetStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                        GetSubStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                    }
                    else if (transition.isExit)
                    {
                        instanceId = transition.GetInstanceID();
                    }
                    if (instanceId != 0 && !destSourceTransitionPairs.ContainsKey(instanceId))
                    {
                        destSourceTransitionPairs.Add(instanceId, state.state.name);
                    }
                }
            }
        }
        
        private static void GetSubStateMachineInfo(Dictionary<int, string> destSourceTransitionPairs,AnimatorStateMachine parentStateMachine)
        {
            if(!parentStateMachine) return;
            // サブステートマシンからステートマシンへ
            foreach (var stateMachine in parentStateMachine.stateMachines)
            {
                var stateMachineName = stateMachine.stateMachine.name;
                var transitions = parentStateMachine.GetStateMachineTransitions(stateMachine.stateMachine);
                foreach (var transition in transitions)
                {
                    // var sourceName = stateMachineName;
                    var instanceId = 0;
                    // SubState -> State
                    if (transition.destinationState)
                    {
                        // string destName = transition.destinationState.name;
                        // Debug.Log($"SubState -> State : {sourceName} -> {destName}");
                        instanceId = transition.destinationState.GetInstanceID();
                        GetStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                        GetSubStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                    }
                    // SubState -> SubState
                    else if (transition.destinationStateMachine)
                    {
                        // string destName = transition.destinationStateMachine.name;
                        // Debug.Log($"SubState -> SubState : {sourceName} -> {destName}");
                        instanceId = transition.destinationStateMachine.GetInstanceID();
                        GetStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                        GetSubStateMachineInfo(destSourceTransitionPairs, transition.destinationStateMachine);
                    }
                    else if (transition.isExit)
                    {
                        instanceId = transition.GetInstanceID();
                    }
                    if (instanceId != 0)
                    {
                        destSourceTransitionPairs.TryAdd(instanceId, stateMachineName);
                    }
                }
            }
        }

        /// <summary>
        /// 指定したアニメーターコントローラーレイヤー内のすべてのトランジションを選択します。
        /// </summary>
        /// <param name="layer">対象のアニメーターコントローラーレイヤー</param>
        public static void SelectAllTransitions(AnimatorControllerLayer layer)
        {
            var states = layer.stateMachine.states;
            var transitions = new List<Object>();
            transitions.AddRange(states.SelectMany(state => state.state.transitions));
            foreach (var stateMachine in layer.stateMachine.stateMachines)
            {
                var subStateTransitions = layer.stateMachine.GetStateMachineTransitions(stateMachine.stateMachine);
                foreach (var transition in subStateTransitions)
                {
                    if (transition.destinationState)
                    {
                        transitions.Add(transition);
                    }
                    else if (transition.destinationStateMachine)
                    {
                        transitions.Add(transition);
                    }
                    else if (transition.isExit)
                    {
                        transitions.Add(transition);
                    }
                }
            }
            Selection.objects = transitions.ToArray();
        }

        /// <summary>
        /// 指定したアニメーターコントローラーレイヤー内のトランジションの選択を解除します。
        /// </summary>
        public static void UnselectTransitions()
        {
            var transition = Array.Empty<Object>();
            Selection.objects = transition;
        }

        /// <summary>
        /// 選択中のレイヤーのインデックスを取得します。
        /// </summary>
        /// <returns>選択中のレイヤーのインデックス</returns>
        private static int GetSelectedLayerIndex()
        {
            // NOTE: Reflectionを使ってEditorWindowクラスのプロパティからレイヤーのインデックスを取得
            var asm = Assembly.Load("UnityEditor.Graphs");
            var editorGraphModule = asm.GetModule("UnityEditor.Graphs.dll");
            var typeAnimatorWindow = editorGraphModule.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            var animatorWindow = EditorWindow.GetWindow(typeAnimatorWindow);
            var selectedLayerIndex = typeAnimatorWindow.GetProperty("selectedLayerIndex")!.GetValue(animatorWindow);
            return (int)selectedLayerIndex;
        }

        /// <summary>
        /// 指定したアニメーターコントローラー内で選択中のレイヤーを取得します。
        /// </summary>
        /// <param name="animatorController">対象のアニメーターコントローラー</param>
        /// <returns>選択中のレイヤー</returns>
        public static AnimatorControllerLayer GetSelectedLayer(AnimatorController animatorController)
        {
            return animatorController.layers[GetSelectedLayerIndex()];
        }

        /// <summary>
        /// 設定の保存を行います。
        /// </summary>
        public static void SaveChanges()
        {
            // Debug.Log(Localization.lang.logMessage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // デバッグ用
        // private static void ForDebug(Dictionary<int, string> destSourceTransitionPairs)
        // {
        //     foreach (var kvp in destSourceTransitionPairs)
        //     {
        //         Debug.Log(string.Format("Key: {0}, Value: {1}", kvp.Key, kvp.Value));
        //     }
        // }
    }
}