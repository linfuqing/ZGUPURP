using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZG
{
    [Serializable]
    public struct DualBlurData
    {
        public int downSample;
        public int iteration;
        public Vector2 offset;
    }

    public class DualBlurRenderPassFeature : ScriptableRendererFeature
    {
        private class RenderPass : ScriptableRenderPass
        {
            private struct Level
            {
                public RTHandle down;
                public RTHandle up;
            }

            public static readonly int Offset = Shader.PropertyToID("_Offset");
            //public static readonly int SourceTex = Shader.PropertyToID("_SourceTex");

            private ProfilingSampler __profilingSampler = new ProfilingSampler("Dual Blur");

            private Material __material = new Material(Shader.Find("ZG/DualBlurURP"));

            private Level[] __levels;

            private DualBlurData __data;

            public void Init(in DualBlurData data)
            {
                __data = data;
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                int numLevels = __levels == null ? 0 : __levels.Length;
                if (numLevels < __data.iteration)
                    Array.Resize(ref __levels, __data.iteration);

                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                int originWidth = descriptor.width >>  __data.downSample, originHeight = descriptor.height >> __data.downSample;

                for (int i = 0; i < __data.iteration; ++i)
                {
                    descriptor.width = Mathf.Max(originWidth >> i, 1);
                    descriptor.height = Mathf.Max(originHeight >> i, 1);
                    RenderingUtils.ReAllocateIfNeeded(ref __levels[i].down, descriptor, name: $"_BlurMipDown {i}");
                }

                // Upsample
                for (int i = __data.iteration - 2; i >= 0; --i)
                {
                    descriptor.width = Mathf.Max(originWidth >> i, 1);
                    descriptor.height = Mathf.Max(originHeight >> i, 1);
                    RenderingUtils.ReAllocateIfNeeded(ref __levels[i].up, descriptor, name: $"_BlurMipUp {i}");
                }

                //ConfigureTarget(renderingData.cameraData.renderer.cameraColorTarget);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                __material.SetVector(Offset, new Vector4(__data.offset.x, __data.offset.y));

                CommandBuffer cmd = CommandBufferPool.Get("Dual Blur");
                using (new ProfilingScope(cmd, __profilingSampler))
                {
                    // Downsample
                    RTHandle down, lastDown = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    for (int i = 0; i < __data.iteration; ++i)
                    {
                        down = __levels[i].down;

                        Blit(cmd, lastDown, down, __material, 0);

                        lastDown = down;
                    }

                    // Upsample
                    RTHandle lastUp = __levels[__data.iteration - 1].down, up;
                    for (int i = __data.iteration - 2; i >= 0; --i)
                    {
                        up = __levels[i].up;

                        Blit(cmd, lastUp, up, __material, 1);

                        lastUp = up;
                    }

                    Blit(cmd, lastUp, renderingData.cameraData.renderer.cameraColorTargetHandle, __material, 1);

                    /*cmd.SetRenderTarget(colorAttachment);

                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                    cmd.SetGlobalTexture(SourceTex, lastUp);

                    cmd.DrawMesh(
                        RenderingUtils.fullscreenMesh,
                        Matrix4x4.identity,
                        __material,
                        0,
                        1);

                    cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);*/
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /*public new void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
            {
                cmd.SetGlobalTexture(SourceTex, source);
                base.Blit(cmd, source, destination, material, passIndex);
            }*/

            // Cleanup any allocated resources that were created during the execution of this render pass.
            /*public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (__data.iteration < 1)
                    return;

                for (int i = 0; i < __data.iteration; ++i)
                    cmd.ReleaseTemporaryRT(__levels[i].down.id);

                for (int i = __data.iteration - 2; i >= 0; --i)
                    cmd.ReleaseTemporaryRT(__levels[i].up.id);
            }*/
        }

        private RenderPass __renderPass;

        /// <inheritdoc/>
        public override void Create()
        {
            __renderPass = new RenderPass();

            // Configures where the render pass should be injected.
            __renderPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var volumeManager = VolumeManager.instance;
            var volume = volumeManager.IsComponentActiveInMask<DualBlurVolume>(renderingData.cameraData.volumeLayerMask) ?
                volumeManager.stack.GetComponent<DualBlurVolume>() : null;

            var data = volume == null ? default : volume.data;
            if (data.iteration < 1)
                return;

            __renderPass.Init(data);

            //__renderPass.ConfigureInput(ScriptableRenderPassInput.Color);

            renderer.EnqueuePass(__renderPass);
        }
    }
}