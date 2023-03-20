using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZG
{
    public class SSPRFeature : ScriptableRendererFeature
    {
        public class RenderPass : ScriptableRenderPass
        {
            private const string CommandBufferTag = "SSPR-Reflection";

            private Material __material;

            private SSPRTexGenerator __generator;

            private PlanarRendererGroups __planarRendererGroups = new PlanarRendererGroups();

            public RenderPass(Material material, SSPRTexGenerator generator)
            {
                __material = material;
                __generator = generator;
            }

            /*public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                //ConfigureTarget(renderingData.cameraData.renderer.cameraColorTarget);
            }*/
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                __generator.Configure(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get(CommandBufferTag);
                try
                {
                    ReflectPlanar.GetVisiblePlanarGroups(__planarRendererGroups);
                    foreach (var group in __planarRendererGroups.rendererGroups)
                    {
                        //cmd.Clear();
                        var planarDescriptor = group.descriptor;
                        var renderers = group.renderers;
                        __generator.Render(cmd, ref renderingData, ref planarDescriptor);
                        //cmd.SetRenderTarget(this.colorAttachment, this.depthAttachment);

                        foreach (var rd in renderers)
                            cmd.DrawRenderer(rd, __material);

                        context.ExecuteCommandBuffer(cmd);
                    }
                }
                finally
                {
                    CommandBufferPool.Release(cmd);
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                __generator.ReleaseTemporary(cmd);

                base.OnCameraCleanup(cmd);
            }
        }

        [SerializeField]
        internal Material _material;

        [SerializeField]
        internal ComputeShader _computeShader;

        private RenderPass __pass;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            renderer.EnqueuePass(__pass);
        }

        public override void Create()
        {
            __pass = new RenderPass(_material, new SSPRTexGenerator(_computeShader));

            __pass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }
    }
}
