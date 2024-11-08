using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GetStateName :EditorWindow
{
    string selectionName = "";
    [MenuItem("もちもちまーと/てすと")]
    public static void Open()
    {
        GetStateName window = GetWindow<GetStateName>();
    }

    public void OnGUI()
    {
        if(Selection.gameObjects.Length > 0)
        {
            selectionName = Selection.gameObjects[0].name;
        }
        //EditorGUILayout.LabelField(selectionName);
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            GUILayout.Label(selectedObject.ToString());
            Debug.Log(selectedObject.ToString());
        }
    }
}