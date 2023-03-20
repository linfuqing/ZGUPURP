using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FarPlaneOverridePassFeature : ScriptableRendererFeature
{
    private class RenderPass : ScriptableRenderPass
    {
        private int __cullingMask;
        private float __farClipPlane;

        private List<ShaderTagId> __shaderTagIdLis;

        private FilteringSettings __filteringSettings = new FilteringSettings(null);

        public RenderPass(int cullingMask, float farClipPlane, string[] shaderTags)
        {
            __cullingMask = cullingMask;
            __farClipPlane = farClipPlane;

            __shaderTagIdLis = new List<ShaderTagId>(shaderTags.Length);
            foreach(string shaderTag in shaderTags)
                __shaderTagIdLis.Add(new ShaderTagId(shaderTag));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            int cullingMask = camera.cullingMask;
            float farClipPlane = camera.farClipPlane;//, nearClipPlane = camera.nearClipPlane;

            //camera.nearClipPlane = farClipPlane;
            camera.farClipPlane = __farClipPlane;
            camera.cullingMask = __cullingMask;
            if (camera.TryGetCullingParameters(false, out var cullingParameters))
            {
                var cullingResults = context.Cull(ref cullingParameters);

                var drawingSettings = CreateDrawingSettings(__shaderTagIdLis, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                context.DrawRenderers(cullingResults, ref drawingSettings, ref __filteringSettings);
            }

            //camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            camera.cullingMask = cullingMask;
            if(camera.orthographic)
            {
                float orthographicSize = camera.orthographicSize, 
                    top = orthographicSize, 
                    bottom = -top, 
                    right = orthographicSize * camera.aspect,
                    left = -right;

                camera.projectionMatrix = Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, __farClipPlane);
            }
            else
                camera.projectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, __farClipPlane);
        }
    }

    public string[] shaderTags = new string[]
    {
        "SRPDefaultUnlit",
        "UniversalForward",
        "UniversalForwardOnly",
        "LightweightForward"
    };

    public float farClipPlane = 1000.0f;
    public LayerMask cullingMask = -1;

    private RenderPass __renderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        __renderPass = new RenderPass(cullingMask, farClipPlane, shaderTags);

        // Configures where the render pass should be injected.
        __renderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(__renderPass);
    }
}
