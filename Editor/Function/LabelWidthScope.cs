﻿using System;
using UnityEditor;

public sealed class LabelWidthScope : IDisposable
{
    private readonly float m_oldLabelWidth;

    public LabelWidthScope(float labelWidth)
    {
        m_oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = labelWidth;
    }

    public void Dispose()
    {
        EditorGUIUtility.labelWidth = m_oldLabelWidth;
    }
}