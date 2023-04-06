using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZG
{
    public class BlurOutlineRenderPassFeature : ScriptableRendererFeature
    {
        private class RenderPass : ScriptableRenderPass
        {
            public static readonly int BlurOffsetX = Shader.PropertyToID("_BlurOffsetX");
            public static readonly int BlurOffsetY = Shader.PropertyToID("_BlurOffsetY");
            public static readonly int Strength = Shader.PropertyToID("_Strength");
            public static readonly int BlurTex = Shader.PropertyToID("_BlurTex");
            //public static readonly int SourceTex = Shader.PropertyToID("_SourceTex");
            /*public static readonly int SolidSilhouette = Shader.PropertyToID("SolidSilhouette");
            public static readonly int BlurSilhouette = Shader.PropertyToID("BlurSilhouette");
            public static readonly int BlurOutline = Shader.PropertyToID("BlurOutline");*/

            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("Blur Outline");

            private RTHandle __solidSilhouette;
            private RTHandle __blurSilhouette;
            private RTHandle __blurOutline;

            private IRenderBlurOutline __renderBlurOutline;
            private Material __blurOutlineMaterial;
            private Material[] __silhouetteMaterials;
            private List<ShaderTagId> __shaderTagIds = new List<ShaderTagId>(4);

            public RenderPass(params Shader[] silhouetteShaders)
            {
                int numSilhouettes = silhouetteShaders.Length;

                __silhouetteMaterials = new Material[numSilhouettes];
                for (int i = 0; i < numSilhouettes; ++i)
                    __silhouetteMaterials[i] = new Material(silhouetteShaders[i]);

                __shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
                __shaderTagIds.Add(new ShaderTagId("UniversalForward"));
                __shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
                __shaderTagIds.Add(new ShaderTagId("LightweightForward"));
            }

            public void Init(IRenderBlurOutline renderBlurOutline)
            {
                //ConfigureInput(ScriptableRenderPassInput.Color);

                __renderBlurOutline = renderBlurOutline;
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            /*public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                var descriptor = cameraTextureDescriptor;
                descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                RenderingUtils.ReAllocateIfNeeded(ref __solidSilhouette, descriptor, name: "Solid Silhouette");

                //cmd.GetTemporaryRT(SolidSilhouette, cameraTextureDescriptor);

                int downSample = __renderBlurOutline.data.downSample;
                descriptor.width >>= downSample;
                descriptor.height >>= downSample;
                //int width = cameraTextureDescriptor.width >> __renderBlurOutline.downSample, height = cameraTextureDescriptor.height >> __renderBlurOutline.downSample;
                RenderingUtils.ReAllocateIfNeeded(ref __blurSilhouette, descriptor, name: "Blur Silhouette");
                RenderingUtils.ReAllocateIfNeeded(ref __blurOutline, descriptor, name: "Blur Outline");
                //cmd.GetTemporaryRT(BlurSilhouette, descriptor.width, descriptor.height);
                //cmd.GetTemporaryRT(BlurOutline, descriptor.width, descriptor.height);

                ConfigureTarget(__solidSilhouette);
                ConfigureClear(ClearFlag.All, Color.clear); 
            }*/

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                RenderingUtils.ReAllocateIfNeeded(ref __solidSilhouette, descriptor, name: "Solid Silhouette");

                //cmd.GetTemporaryRT(SolidSilhouette, cameraTextureDescriptor);

                int downSample = __renderBlurOutline.data.downSample;
                descriptor.width >>= downSample;
                descriptor.height >>= downSample;
                //int width = cameraTextureDescriptor.width >> __renderBlurOutline.downSample, height = cameraTextureDescriptor.height >> __renderBlurOutline.downSample;
                RenderingUtils.ReAllocateIfNeeded(ref __blurSilhouette, descriptor, name: "Blur Silhouette");
                RenderingUtils.ReAllocateIfNeeded(ref __blurOutline, descriptor, name: "Blur Outline");
                //cmd.GetTemporaryRT(BlurSilhouette, descriptor.width, descriptor.height);
                //cmd.GetTemporaryRT(BlurOutline, descriptor.width, descriptor.height);

                if (__renderBlurOutline.needDepthSort)
                {
                    ConfigureTarget(__solidSilhouette, renderingData.cameraData.renderer.cameraDepthTargetHandle);

                    ConfigureClear(ClearFlag.Color, Color.clear);
                }
                else
                {
                    ConfigureTarget(__solidSilhouette);

                    ConfigureClear(ClearFlag.All, Color.clear);
                }
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, ProfilingSampler))
                {
                    /*cmd.SetRenderTarget(__solidSilhouette);

                    cmd.ClearRenderTarget(true, true, Color.clear);*/

                    var drawingSettings = CreateDrawingSettings(__shaderTagIds, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                    __renderBlurOutline.Draw(
                        __silhouetteMaterials, 
                        cmd, 
                        ref context, 
                        ref renderingData.cullResults, 
                        ref drawingSettings);

                    var data = __renderBlurOutline.data;

                    if (__blurOutlineMaterial == null)
                        __blurOutlineMaterial = new Material(Shader.Find("ZG/BlurOutlineURP"));

                    Blit(cmd, __solidSilhouette, __blurSilhouette, __blurOutlineMaterial, 0);

                    for (int i = 0; i < data.blurIterCount; ++i)
                    {
                        cmd.SetGlobalFloat(BlurOffsetX, 0.0f);
                        cmd.SetGlobalFloat(BlurOffsetY, data.blurScale.y);

                        Blit(cmd, __blurSilhouette, __blurOutline, __blurOutlineMaterial, 1);

                        cmd.SetGlobalFloat(BlurOffsetX, data.blurScale.x);
                        cmd.SetGlobalFloat(BlurOffsetY, 0.0f);

                        Blit(cmd, __blurOutline, __blurSilhouette, __blurOutlineMaterial, 1);
                    }

                    cmd.SetGlobalTexture(BlurTex, __blurSilhouette);
                    Blit(cmd, __solidSilhouette, __blurOutline, __blurOutlineMaterial, 2);

                    cmd.SetGlobalFloat(Strength, data.strength);
                    Blit(cmd, __blurOutline, renderingData.cameraData.renderer.cameraColorTargetHandle, __blurOutlineMaterial, 3);

                    //Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, SolidSilhouette, __blurOutlineMaterial, 0);

                    /*cmd.SetRenderTarget(colorAttachmentHandle);

                    cmd.SetGlobalFloat(Strength, __renderBlurOutline.strength);
                    cmd.SetGlobalTexture(BlurTex, BlurOutline);

                    //Blit(cmd, __solidSilhouette, renderer.cameraColorTarget, __blurOutlineMaterial, 3);

                    //cmd.SetGlobalTexture(SourceTex, SolidSilhouette);

                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                    cmd.DrawMesh(
                        RenderingUtils.fullscreenMesh,
                        Matrix4x4.identity,
                        __blurOutlineMaterial,
                        0,
                        3);

                    cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);*/

                }

                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (__renderBlurOutline == null)
                    return;

                /*RTHandles.Release(__solidSilhouette);
                RTHandles.Release(__blurSilhouette);
                RTHandles.Release(__blurOutline);*/

                /*cmd.ReleaseTemporaryRT(SolidSilhouette);
                cmd.ReleaseTemporaryRT(BlurSilhouette);
                cmd.ReleaseTemporaryRT(BlurOutline);*/
            }

            /*public new void Blit(CommandBuffer cmd, RTHandle source, RTHandle destination, Material material = null, int passIndex = 0)
            {
                cmd.SetGlobalTexture(SourceTex, source);
                base.Blit(cmd, source, destination, material, passIndex);
            }*/
        }

        public Shader solidColor;
        public Shader solidColorLinearBlendSkinning;
        public Shader solidColorComputeDeformation;

        private RenderPass __renderPass;

        /// <inheritdoc/>
        public override void Create()
        {
            if(__renderPass == null)
                __renderPass = new RenderPass(solidColor/*, solidColorLinearBlendSkinning, solidColorComputeDeformation*/);

            // Configures where the render pass should be injected.
            __renderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var renderBlurOutline = IRenderBlurOutline.instance;
            if (renderBlurOutline == null || !renderBlurOutline.isVail)
                return;

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                return;

            __renderPass.Init(renderBlurOutline);

            renderer.EnqueuePass(__renderPass);
        }
    }
}