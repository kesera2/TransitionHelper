using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TransitionHelper
{
    internal static class Utility
    {
        /// <summary>
        /// 指定したアニメーターステートマシン内の親ステートのパスを取得します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="parentPath">親ステートのパス</param>
        /// <returns>親ステートのパス</returns>
        public static string GetParentPath(AnimatorStateMachine stateMachine, string parentPath)
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
        public static void AddState(AnimatorStateMachine stateMachine, List<UnityEditor.Animations.AnimatorState> result)
        {
            foreach (var state in stateMachine.states)
            {
                result.Add(state.state);
            }
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートのトランジションを変数に追加します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="result">結果を格納するリスト</param>
        public static void AddTransition(AnimatorStateMachine stateMachine, List<AnimatorStateTransition[]> result)
        {
            foreach (var state in stateMachine.states)
            {
                result.Add(state.state.transitions);
            }
        }

        /// <summary>
        /// 指定したアニメーターステートマシン内のすべてのステートを再帰的に取得します。
        /// </summary>
        /// <param name="stateMachine">対象のアニメーターステートマシン</param>
        /// <param name="parentPath">ステートの親パス</param>
        /// <param name="result">結果を格納するリスト</param>
        public static void GetAllStates(AnimatorStateMachine stateMachine, string parentPath, List<UnityEditor.Animations.AnimatorState> result)
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
        public static GUIStyle GetNomalFontStyle()
        {
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Normal;
            return style;
        }

        /// <summary>
        /// アニメーターコントローラーから、遷移先のステート名をキーにインスタンスIDをバリューにした辞書を取得します。
        /// </summary>
        /// <param name="animatorController">対象のアニメーターコントローラー</param>
        /// <returns>遷移先のステート名をキーにインスタンスIDをバリューにした辞書</returns>
        public static Dictionary<int, string> GetDestSourceTransitionPairs(AnimatorController animatorController)
        {
            Dictionary<int, string> destSourceTransitionPairs = new Dictionary<int, string>();
            if (animatorController == null)
            {
                return null;
            }
            List<AnimatorControllerLayer> layers = animatorController.layers.ToList();

            // FIXME: 選択中のレイヤーに限定する <- 同じレイヤー名があると表示がバグる
            foreach (AnimatorControllerLayer layer in layers)
            {

                // ステートマシンからサブステートマシンへ
                foreach (ChildAnimatorState state in layer.stateMachine.states)
                {
                    foreach (AnimatorStateTransition transition in state.state.transitions)
                    {
                        string destName = null;
                        int instanceId = 0;
                        if (transition.destinationState != null)
                        {
                            destName = transition.destinationState.name;
                            instanceId = transition.destinationState.GetInstanceID();
                        }
                        else if (transition.destinationStateMachine != null)
                        {
                            destName = transition.destinationStateMachine.name;
                            instanceId = transition.destinationStateMachine.GetInstanceID();
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

                // サブステートマシンからステートマシンへ
                foreach (ChildAnimatorStateMachine stateMachine in layer.stateMachine.stateMachines)
                {
                    string stateMachineName = stateMachine.stateMachine.name;
                    AnimatorTransition[] transitions = layer.stateMachine.GetStateMachineTransitions(stateMachine.stateMachine);
                    foreach (AnimatorTransition transition in transitions)
                    {
                        int instanceId = 0;
                        if (transition.destinationState != null)
                        {
                            instanceId = transition.destinationState.GetInstanceID();
                            string destName = transition.destinationState.name;
                        }
                        else if (transition.destinationStateMachine != null)
                        {
                            instanceId = transition.destinationStateMachine.GetInstanceID();
                        }
                        else if (transition.isExit)
                        {
                            instanceId = transition.GetInstanceID();
                        }
                        if (instanceId != 0 && !destSourceTransitionPairs.ContainsKey(instanceId))
                        {
                            destSourceTransitionPairs.Add(instanceId, stateMachineName);
                        }
                    }
                }
            }
            return destSourceTransitionPairs;
        }

        /// <summary>
        /// 指定したアニメーターコントローラーレイヤー内のすべてのトランジションを選択します。
        /// </summary>
        /// <param name="layer">対象のアニメーターコントローラーレイヤー</param>
        public static void selectAllTransitions(AnimatorControllerLayer layer)
        {
            ChildAnimatorState[] states = layer.stateMachine.states;
            List<Object> transitions = new List<Object>();
            foreach (ChildAnimatorState state in states)
            {
                foreach (AnimatorStateTransition transition in state.state.transitions)
                {
                    transitions.Add(transition);
                }
            }
            foreach (ChildAnimatorStateMachine stateMachine in layer.stateMachine.stateMachines)
            {
                AnimatorTransition[] subStateTransitions = layer.stateMachine.GetStateMachineTransitions(stateMachine.stateMachine);
                foreach (AnimatorTransition transition in subStateTransitions)
                {
                    if (transition.destinationState != null)
                    {
                        transitions.Add(transition);
                    }
                    else if (transition.destinationStateMachine != null)
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
        public static void unselectTransitions()
        {
            AnimatorTransition[] transition = new AnimatorTransition[0];
            Selection.objects = transition;
        }

        /// <summary>
        /// 選択中のレイヤーのインデックスを取得します。
        /// </summary>
        /// <returns>選択中のレイヤーのインデックス</returns>
        public static int getSelectedLayerIndex()
        {
            // NOTE: Reflectionを使ってEditorWindowクラスのプロパティからレイヤーのインデックスを取得
            var asm = Assembly.Load("UnityEditor.Graphs");
            var editorGraphModule = asm.GetModule("UnityEditor.Graphs.dll");
            var typeAnimatorWindow = editorGraphModule.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            var animatorWindow = EditorWindow.GetWindow(typeAnimatorWindow);
            var selectedLayerIndex = typeAnimatorWindow.GetProperty("selectedLayerIndex").GetValue(animatorWindow);
            return (int)selectedLayerIndex;
        }

        /// <summary>
        /// 指定したアニメーターコントローラー内で選択中のレイヤーを取得します。
        /// </summary>
        /// <param name="animatorController">対象のアニメーターコントローラー</param>
        /// <returns>選択中のレイヤー</returns>
        public static AnimatorControllerLayer getSelectedLayer(AnimatorController animatorController)
        {
            return animatorController.layers[getSelectedLayerIndex()];
        }

        /// <summary>
        /// 指定したアニメーターコントローラー内で選択中のレイヤーの名前を取得します。
        /// </summary>
        /// <param name="animatorController">対象のアニメーターコントローラー</param>
        /// <returns>選択中のレイヤーの名前</returns>
        public static string getSelectedLayerName(AnimatorController animatorController)
        {
            return getSelectedLayer(animatorController).name;
        }

        /// <summary>
        /// 設定の保存を行います。
        /// </summary>
        public static void SaveChanges()
        {
            Debug.Log(Localization.lang.logMessage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // デバッグ用
        private static void ForDebug(Dictionary<int, string> destSourceTransitionPairs)
        {
            foreach (KeyValuePair<int, string> kvp in destSourceTransitionPairs)
            {
                Debug.Log(string.Format("Key: {0}, Value: {1}", kvp.Key, kvp.Value));
            }
        }
    }
}