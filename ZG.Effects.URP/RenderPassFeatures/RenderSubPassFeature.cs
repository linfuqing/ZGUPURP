using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace ZG
{
    public class RenderSubPassFeature: NativeRenderPassBase
    {
        private class RenderPass : ScriptableRenderPass
        {
            //private ProfilingSampler __profilingSampler;

            internal bool _isUseRenderPass;
            
            private SynchronisationStageFlags __stage;
            
            private RenderQueueType __renderQueueType;
            
            private FilteringSettings __filteringSettings;
            private RenderStateBlock __renderStateBlock;
            
            private List<ShaderTagId> __shaderTagIDs = new List<ShaderTagId>();

            /// <summary>
            /// The constructor for render objects pass.
            /// </summary>
            /// <param name="renderPassEvent">Controls when the render pass executes.</param>
            /// <param name="shaderTags">List of shader tags to render with.</param>
            /// <param name="renderQueueType">The queue type for the objects to render.</param>
            /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
            public RenderPass(
                //SynchronisationStageFlags stage, 
                RenderPassEvent renderPassEvent, 
                RenderQueueType renderQueueType, 
                int layerMask, 
                string[] shaderTags)
            {
                //__profilingSampler = new ProfilingSampler(nameof(RenderObjectsSyncPass));

                //__stage = stage;

                this.renderPassEvent = renderPassEvent;
                __renderQueueType = renderQueueType;
                
                var renderQueueRange = renderQueueType == RenderQueueType.Transparent
                    ? RenderQueueRange.transparent
                    : RenderQueueRange.opaque;
                __filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

                if (shaderTags != null && shaderTags.Length > 0)
                {
                    foreach (var passName in shaderTags)
                        __shaderTagIDs.Add(new ShaderTagId(passName));
                }
                else
                {
                    __shaderTagIDs.Add(new ShaderTagId("SRPDefaultUnlit"));
                    __shaderTagIDs.Add(new ShaderTagId("UniversalForward"));
                    __shaderTagIDs.Add(new ShaderTagId("UniversalForwardOnly"));
                }

                __renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                //var cmd = CommandBufferPool.Get();
   
                //using (new ProfilingScope(cmd, __profilingSampler))
                {
                    //context.ExecuteCommandBuffer(cmd);

                    //cmd.Clear();
                    //画地形
                    {
                        //var attachmentIndices = new NativeArray<int>(0, Allocator.Temp);
                        /*if (!depthOnly)
                        {
                            for (int i = 0; i < attachmentIndicesCount; ++i)
                            {
                                attachmentIndices[i] = renderPass.m_ColorAttachmentIndices[i];
                            }
                        }*/
                        
                        //attachmentIndices.Dispose();
                        
                        var sortingCriteria = (__renderQueueType == RenderQueueType.Transparent)
                            ? SortingCriteria.CommonTransparent
                            : renderingData.cameraData.defaultOpaqueSortFlags;

                        var drawingSettings =
                            CreateDrawingSettings(__shaderTagIDs, ref renderingData, sortingCriteria);

                        // Render the objects...
                        context.DrawRenderers(
                            renderingData.cullResults,
                            ref drawingSettings,
                            ref __filteringSettings,
                            ref __renderStateBlock);
                        
                        if(_isUseRenderPass)
                            NextSubPass(this, context);
                    }

                    /*var fence = Graphics.CreateGraphicsFence(
                        GraphicsFenceType.AsyncQueueSynchronisation,
                        __stage);

                    Graphics.WaitOnAsyncGraphicsFence(fence);*/
                    
                    //context.ExecuteCommandBuffer(cmd);
                }

                //CommandBufferPool.Release(cmd);
            }
        }

        //[SerializeField]
        //internal SynchronisationStageFlags _stage = SynchronisationStageFlags.VertexProcessing;
        [SerializeField]
        internal RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        [SerializeField]
        internal RenderQueueType _renderQueueType = RenderQueueType.Opaque;
        [SerializeField]
        internal LayerMask _layerMask = 1 << 31;
        [SerializeField]
        internal string[] _shaderTags;
        
        private RenderPass __renderPass;

        public override void Create()
        {
            __renderPass = new RenderPass(
                //_stage, 
                _renderPassEvent, 
                _renderQueueType, 
                _layerMask, 
                _shaderTags);
            
            //Debug.LogError(SystemInfo.supportsAsyncCompute);
            //Debug.LogError(SystemInfo.supportsGraphicsFence);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            __renderPass._isUseRenderPass = IsRenderPass(renderer, __renderPass, ref renderingData);

            renderer.EnqueuePass(__renderPass);
        }
    }

}