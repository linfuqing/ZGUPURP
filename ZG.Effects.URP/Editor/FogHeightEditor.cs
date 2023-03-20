using UnityEngine;
using UnityEditor;

public class FogHeightEditor : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var fogHeightParams = FindProperty("_FogHeightParams", properties);

        var material = materialEditor.target as Material;
        EditorGUI.BeginChangeCheck();
        bool isFogHeight = EditorGUILayout.ToggleLeft("Fog Height", material.IsKeywordEnabled("_FOG_HEIGHT"));
        if (EditorGUI.EndChangeCheck())
        {
            if (isFogHeight)
            {
                foreach (Material target in materialEditor.targets)
                {
                    target.EnableKeyword("_FOG_HEIGHT");
                }
            }
            else
            {
                foreach (Material target in materialEditor.targets)
                {
                    target.DisableKeyword("_FOG_HEIGHT");
                }
            }
        }

        if (isFogHeight)
        {
            Vector4 fogParams = fogHeightParams.vectorValue;
            float heightStart = 1 / fogParams.y,
                heightEnd = fogParams.x * -heightStart,
                distanceStart = 1 / fogParams.w,
                distanceEnd = fogParams.z * -distanceStart;

            EditorGUI.BeginChangeCheck();

            heightStart = EditorGUILayout.FloatField("Height Start", heightStart + heightEnd);
            heightStart = Mathf.Max(heightStart, 0.0f);

            heightEnd = EditorGUILayout.FloatField("Height End", heightEnd);
            heightEnd = Mathf.Max(heightEnd, heightStart + 1.0f);

            distanceStart = EditorGUILayout.FloatField("Distance Start", distanceStart + distanceEnd);
            distanceStart = Mathf.Max(distanceStart, 0.0f);

            distanceEnd = EditorGUILayout.FloatField("Distance End", distanceEnd);
            distanceEnd = Mathf.Max(distanceEnd, distanceStart + 1.0f);

            if (EditorGUI.EndChangeCheck())
            {
                float height = 1.0f / (heightEnd - heightStart), distance = 1.0f / (distanceEnd - distanceStart);
                fogHeightParams.vectorValue = new Vector4(heightEnd * height, -height, distanceEnd * distance, -distance);
            }
        }
        //EditorGUILayout.EndFoldoutHeaderGroup();

        var results = new MaterialProperty[properties.Length - 1];
        int index = 0;
        foreach(var property in properties)
        {
            if (property == fogHeightParams)
                continue;

            results[index++] = property;
        }

        base.OnGUI(materialEditor, results);
    }
}
