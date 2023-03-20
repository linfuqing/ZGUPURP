using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;

namespace ZG
{
    public class FlatHeightFogLitEditor : BaseShaderGUI
    {
        // Properties
        private MaterialProperty __fogHeightOffset;
        private MaterialProperty __fogParams;

        private LitGUI.LitProperties __litProperties;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            __fogHeightOffset = FindProperty("_FogHeightOffset", properties, false);
            __fogParams = FindProperty("_FogParams", properties, false);

            __litProperties = new LitGUI.LitProperties(properties);
        }

        // material changed check
        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
        }

        public override void DrawBaseProperties(Material material)
        {
            bool isChanged = false;
            var materialEditor = base.materialEditor;
            EditorGUI.BeginChangeCheck();
            if(__fogHeightOffset != null)
                materialEditor.ShaderProperty(__fogHeightOffset, "Height Offset");

            if (EditorGUI.EndChangeCheck())
                isChanged = true;

            Vector4 fogParams = __fogParams.vectorValue;
            float heightStart = 1 / fogParams.y,
                heightEnd = fogParams.x * -heightStart,
                distanceStart = 1 / fogParams.w,
                distanceEnd = fogParams.z * -distanceStart;
            EditorGUI.BeginChangeCheck();

            heightStart = EditorGUILayout.FloatField("Height Start", heightStart + heightEnd);
            heightEnd = EditorGUILayout.FloatField("Height End", heightEnd);

            distanceStart = EditorGUILayout.FloatField("Distance Start", distanceStart + distanceEnd);
            distanceEnd = EditorGUILayout.FloatField("Distance End", distanceEnd);

            if (EditorGUI.EndChangeCheck())
            {
                float height = 1.0f / (heightEnd - heightStart), distance = 1.0f / (distanceEnd - distanceStart);
                __fogParams.vectorValue = new Vector4(heightEnd * height, -height, distanceEnd * distance, -distance);

                isChanged = true;
            }

            if (isChanged)
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }

            base.DrawBaseProperties(material);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            if (__litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, __litProperties.workflowMode, Enum.GetNames(typeof(LitGUI.WorkflowMode)));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(__litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (__litProperties.reflections != null && __litProperties.highlights != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(__litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(__litProperties.reflections, LitGUI.Styles.reflectionsText);
                if (EditorGUI.EndChangeCheck())
                    MaterialChanged(material);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));

            if(material.HasProperty("_Offset"))
                __fogHeightOffset.floatValue = material.GetFloat("_Offset");

            if (material.HasProperty("_Height") &&
                material.HasProperty("_Factor"))
            {
                var fogParams = __fogParams.vectorValue;

                fogParams.x = material.GetFloat("_Factor");
                fogParams.y = -1.0f / material.GetFloat("_Height");

                __fogParams.vectorValue = fogParams;
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

            MaterialChanged(material);
        }
    }
}