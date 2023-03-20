﻿using UnityEditor;

public class ClipEditor : ShaderGUI
{
    private bool __isClip;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var invDist = FindProperty("_ClipInvDist", properties);
        var nearDivDist = FindProperty("_ClipNearDivDist", properties);
        var farDivDist = FindProperty("_ClipFarDivDist", properties);
        var targetWeight = FindProperty("_ClipTargetWeight", properties, false);

        __isClip = EditorGUILayout.BeginFoldoutHeaderGroup(__isClip, "Clip");
        if (__isClip)
        {
            var material = (UnityEngine.Material)materialEditor.target;

            bool isGlobal = material.IsKeywordEnabled("CLIP_GLOBAL");
            if(isGlobal != EditorGUILayout.Toggle("Global", isGlobal))
            {
                if (isGlobal)
                    material.DisableKeyword("CLIP_GLOBAL");
                else
                    material.EnableKeyword("CLIP_GLOBAL");
            }

            if (targetWeight != null)
            {
                bool isTarget = material.IsKeywordEnabled("CLIP_TARGET");
                if (isTarget != EditorGUILayout.Toggle("Target", isTarget))
                {
                    if (isTarget)
                        material.DisableKeyword("CLIP_TARGET");
                    else
                        material.EnableKeyword("CLIP_TARGET");
                }

                if (isTarget)
                {
                    ++EditorGUI.indentLevel;
                    materialEditor.FloatProperty(targetWeight, "Weight");
                    --EditorGUI.indentLevel;
                }
            }

            float distance = EditorGUILayout.FloatField("Distance", 1.0f / invDist.floatValue), invDistance = 1.0f / distance;
            invDist.floatValue = invDistance;
            nearDivDist.floatValue = EditorGUILayout.FloatField("Near", nearDivDist.floatValue * distance) * invDistance;
            farDivDist.floatValue = EditorGUILayout.FloatField("Far", farDivDist.floatValue * distance) * invDistance;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        int propertyCount = 3;
        if (targetWeight != null)
            ++propertyCount;

        var results = new MaterialProperty[properties.Length - propertyCount];
        int index = 0;
        foreach(var property in properties)
        {
            if (property == invDist ||
                property == nearDivDist ||
                property == farDivDist || 
                property == targetWeight)
                continue;

            results[index++] = property;
        }

        base.OnGUI(materialEditor, results);
    }
}
