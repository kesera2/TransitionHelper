using System.Collections.Generic;
using UnityEditor.Animations;

namespace TransitionHelper
{
    internal static class Utility
    {

        public static string GetParentPath(AnimatorStateMachine stateMachine, string parentPath)
        {
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentPath += ".";
            }
            parentPath += stateMachine.name;
            return parentPath;
        }
        public static void AddState(AnimatorStateMachine stateMachine, List<UnityEditor.Animations.AnimatorState> result)
        {
            foreach (var state in stateMachine.states)
            {
                result.Add(state.state);
            }
        }

        public static void AddTransition(AnimatorStateMachine stateMachine, List<AnimatorStateTransition[]> result)
        {
            foreach (var state in stateMachine.states)
            {
                result.Add(state.state.transitions);
            }
        }

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
    }
}