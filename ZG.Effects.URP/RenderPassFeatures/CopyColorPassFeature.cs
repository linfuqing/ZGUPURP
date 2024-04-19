using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class CopyColorPassFeature : ScriptableRendererFeature
{
    private class RenderPass : CopyColorPass
    {
        private RTHandle __opaqueColor;

        public RenderPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null) : 
            base(evt, samplingMaterial, copyColorMaterial)
        {
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Downsampling downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            ConfigureDescriptor(downsamplingMethod, ref descriptor, out var filterMode);

            RenderingUtils.ReAllocateIfNeeded(ref __opaqueColor, descriptor, filterMode, TextureWrapMode.Clamp, name: "_CameraOpaqueTexture");
            Setup(
                renderingData.cameraData.renderer.cameraColorTargetHandle, //((UniversalRenderer)renderingData.cameraData.renderer).GetActiveCameraColorAttachment()/*renderingData.cameraData.renderer.cameraColorTargetHandle*/, 
                __opaqueColor, 
                downsamplingMethod);

            base.OnCameraSetup(cmd, ref renderingData);
        }
    }

    public Shader samplingShader;
    public Shader copyingShader;

    private RenderPass __renderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        if (samplingShader == null || copyingShader == null)
            return;
        
        __renderPass = new RenderPass(
            RenderPassEvent.AfterRenderingTransparents + 1, 
            CoreUtils.CreateEngineMaterial(samplingShader),
            CoreUtils.CreateEngineMaterial(copyingShader));
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (__renderPass == null)
            return;
        
        renderer.EnqueuePass(__renderPass);
    }
}


