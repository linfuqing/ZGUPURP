using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZG;

public class DrawTextureRenderPassFeature : ScriptableRendererFeature
{
    private class RenderPass : ScriptableRenderPass
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private ProfilingSampler __profilingSampler = new ProfilingSampler("Draw Texture Command");
        private DrawTextureCommand __command;
        private MaterialPropertyBlock[] __materialPropertyBlocks;

        public void Init(Camera camera)
        {
            // Configures where the render pass should be injected.
            __command = camera.GetComponent<DrawTextureCommand>();
            if (__command == null)
                return;

            switch (__command.cameraEvent)
            {
                case CameraEvent.AfterImageEffectsOpaque:
                    renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
                    break;
            }

            __command.enabled = false;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Init(renderingData.cameraData.camera);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (__command == null)
                return;

            var matrix = Matrix4x4.TRS(new Vector3(0, 0, -1), Quaternion.identity, Vector3.one);
            //var cameraColorTarget = renderer.cameraColorTarget;
            var material = __command.material;
            var textures = __command.textures;
            int numTextures = textures.Count;
            if (__materialPropertyBlocks == null || __materialPropertyBlocks.Length < numTextures)
                Array.Resize(ref __materialPropertyBlocks, numTextures);

            var cmd = CommandBufferPool.Get("Draw Texture Command");
            using (new ProfilingScope(cmd, __profilingSampler))
            {
                //cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget, renderingData.cameraData.renderer.cameraDepthTarget);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                MaterialPropertyBlock materialPropertyBlock;
                for (int i = 0; i < numTextures; ++i)
                {
                    materialPropertyBlock = __materialPropertyBlocks[i];
                    if (materialPropertyBlock == null)
                    {
                        materialPropertyBlock = new MaterialPropertyBlock();

                        __materialPropertyBlocks[i] = materialPropertyBlock;
                    }

                    materialPropertyBlock.SetTexture(MainTex, textures[i]);

                    cmd.DrawMesh(
                        RenderingUtils.fullscreenMesh,
                        matrix,
                        material,
                        0,
                        __command.isUseDepthTexture && (input & ScriptableRenderPassInput.Depth) == ScriptableRenderPassInput.Depth ? 1 : 0,
                        materialPropertyBlock);
                }

                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    private RenderPass __scriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        __scriptablePass = new RenderPass();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        __scriptablePass.Init(renderingData.cameraData.camera);

        renderer.EnqueuePass(__scriptablePass);
    }
}


