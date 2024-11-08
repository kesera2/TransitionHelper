using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using TransitionHelper;

public class ButtonWindow : EditorWindow
{
    private AnimatorController animatorController;

    [MenuItem("もちもちまーと/ButtonWindow")]
    public static void ShowWindow()
    {
        ButtonWindow window = GetWindow<ButtonWindow>();
        window.titleContent = new GUIContent("Button Window");
        window.Show();
    }



    private void OnGUI()
    {
        Dictionary<string, string> destSourceTransitionPairs = new Dictionary<string, string>();

        GUILayout.Label("Button Example", EditorStyles.boldLabel);
        animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
        if (GUILayout.Button("Log Message"))
        {
            AnimatorStateTransition[] selectedTransitions = Selection.objects.Select(x => x as AnimatorStateTransition).Where(y => y != null).ToArray();
            //foreach (var selectedTransition in selectedTransitions)
            //{
            //    Debug.Log(selectedTransition.destinationState.name);
            //}
            List<AnimatorControllerLayer> layers = animatorController.layers.ToList();
            //List<AnimatorStateTransition> transitions = layers.Select(a => a.stateMachine).SelectMany(a => a.states).SelectMany(c => c.state.transitions).ToList();
            foreach (var layer in layers)
            {
                foreach (var state in layer.stateMachine.stateMachines)
                {
                    //Debug.Log("state name: " + state.stateMachine.name + ", defaultState: " + state.stateMachine.defaultState.name + ", ");
                    foreach( var behavior in state.stateMachine.behaviours)
                    {
                        Debug.Log(behavior.name);
                    }
                }
            }

            foreach (KeyValuePair<string, string> entry in destSourceTransitionPairs)
            {
                string sourceState = entry.Key;
                string destinationState = entry.Value;
                Debug.Log("Transition: " + sourceState + " -> " + destinationState);
            }
        }
    }

    //private void OnGUI()
    //{
    //    GUILayout.Label("Button Example", EditorStyles.boldLabel);
    //    animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
    //    if (GUILayout.Button("Log Message"))
    //    {
    //        Dictionary<string, string> destSourceTransitionPairs = Utility.GetDestSourceTransitionPairs(animatorController);
    //        foreach (KeyValuePair<string, string> entry in destSourceTransitionPairs)
    //        {
    //            string sourceState = entry.Key;
    //            string destinationState = entry.Value;
    //            Debug.Log("Transition: " + sourceState + " -> " + destinationState);
    //        }
    //    }
    //}
}