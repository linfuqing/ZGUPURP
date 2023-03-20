using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZG
{
    public class RaindropPassFeature : ScriptableRendererFeature
    {
        private class RenderPass : ScriptableRenderPass
        {
            public enum Status
            {
                None, 
                Normal, 
                Active
            }
            public static readonly int ElpasedTime = Shader.PropertyToID("_ElpasedTime");
            public static readonly int RainZoom = Shader.PropertyToID("_RainZoom");
            public static readonly int RainAmount = Shader.PropertyToID("_RainAmount");
            public static readonly int MaxBlur = Shader.PropertyToID("_MaxBlur");
            public static readonly int MinBlur = Shader.PropertyToID("_MinBlur");

            //public static readonly int SourceTex = Shader.PropertyToID("_SourceTex");

            private Status __status;
            private float __elpasedTime;
            private float __timeScale;
            private float __timeScaleVecloity;
            private float __rainAmount;
            private float __rainAmountVelocity;
            private RaindropData __data;
            //private MaterialPropertyBlock __materialPropertyBlock;
            private Material __material;
            private RTHandle __raindropTex;
            private ProfilingSampler __profilingSampler = new ProfilingSampler("Raindrop");

            public RenderPass()
            {
                //__materialPropertyBlock = new MaterialPropertyBlock();

                __material = new Material(Shader.Find("ZG/RaindropURP"));

            }

            public bool Init(int volumeLayerMask)
            {
                if (RenderRaindrop.count < 1)
                    return false;

                bool isActive;
                var volumeManager = VolumeManager.instance;
                var volume = volumeManager.stack.GetComponent<RaindropVolume>();
                if (volume == null)
                    isActive = false;
                else
                {
                    __data = volume.data;

                    isActive = volume.active;
                    if (isActive)
                        isActive = volumeManager.IsComponentActiveInMask<RaindropVolume>(volumeLayerMask);
                }

                if (!isActive && Mathf.Approximately(__timeScale, 0.0f))
                {
                    __status = Status.None;

                    return false;
                }

                __status = isActive ? Status.Active : Status.Normal;

                return true;
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;

                RenderingUtils.ReAllocateIfNeeded(ref __raindropTex, descriptor, name: "_RaindropTex");

                /*ConfigureTarget(__raindropTex);
                ConfigureClear(ClearFlag.None, Color.black);*/
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, __profilingSampler))
                {
                    /*cmd.SetGlobalTexture(SourceTex, renderingData.cameraData.renderer.cameraColorTarget);

                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                    cmd.DrawMesh(
                        RenderingUtils.fullscreenMesh,
                        Matrix4x4.identity,
                        __material,
                        0,
                        0,
                        __materialPropertyBlock);

                    cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);*/

                    __material.SetFloat(ElpasedTime, __elpasedTime);
                    __material.SetFloat(RainZoom, __data.rainZoom);
                    __rainAmount = Mathf.SmoothDamp(__rainAmount, __status == Status.Active ? __data.rainAmount : 0.0f, ref __rainAmountVelocity, __data.rainAmountSmoothTime);
                    __material.SetFloat(RainAmount, __rainAmount);
                    __material.SetFloat(MaxBlur, __data.maxBlur);
                    __material.SetFloat(MinBlur, __data.minBlur);

                    Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, __raindropTex, __material);
                    Blit(cmd, __raindropTex, renderingData.cameraData.renderer.cameraColorTargetHandle);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                __timeScale = __data.timeSmoothTime > 0.0f ? Mathf.SmoothDamp(__timeScale, __status == Status.Active ? __data.timeScale : 0.0f, ref __timeScaleVecloity, __data.timeSmoothTime) : __timeScale;
                __elpasedTime += Time.deltaTime * __timeScale;
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            /*public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (RenderRaindrop.count < 1)
                    return;

                if (__status == Status.None)
                    return;

                cmd.ReleaseTemporaryRT(__raindropTex.id);
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
            if (!__renderPass.Init(renderingData.cameraData.volumeLayerMask))
                return;

            //__renderPass.ConfigureInput(ScriptableRenderPassInput.Color);

            renderer.EnqueuePass(__renderPass);
        }
    }
}