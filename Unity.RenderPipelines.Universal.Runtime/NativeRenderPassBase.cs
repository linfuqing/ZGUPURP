using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZG
{
    public abstract class NativeRenderPassBase : ScriptableRendererFeature
    {
        public static bool IsRenderPass(ScriptableRenderer scriptableRenderer, ScriptableRenderPass renderPass, ref RenderingData renderingData)
        {
            return renderPass.useNativeRenderPass && 
                   renderPass.m_UsesRTHandles && 
                   scriptableRenderer.useRenderPassEnabled && 
                   renderingData.cameraData.isRenderPassSupportedCamera;
        }
        
        public static NativeArray<int> CreateAttachmentIndices(ScriptableRenderPass renderPass, Allocator allocator, bool isDepthOnly)
        {
            var attachmentIndicesCount = ScriptableRenderer.GetSubPassAttachmentIndicesCount(renderPass);

            var attachmentIndices = new NativeArray<int>(isDepthOnly ? 0 : (int)attachmentIndicesCount, allocator);
            if (!isDepthOnly)
            {
                int colorAttachmentIndex;
                for (int i = 0; i < attachmentIndicesCount; ++i)
                {
                    colorAttachmentIndex = renderPass.m_ColorAttachmentIndices[i];
                    if(colorAttachmentIndex == -1)
                        continue;
                    
                    attachmentIndices[i] = colorAttachmentIndex;
                }
            }

            return attachmentIndices;
        }

        public static void NextSubPass(ScriptableRenderPass renderPass, ScriptableRenderContext context)
        {
            context.EndSubPass();
                        
            using(var attachmentIndices = CreateAttachmentIndices(renderPass, Allocator.Temp, false))
                context.BeginSubPass(attachmentIndices);
        }
        
        internal override bool SupportsNativeRenderPass() => true;
    }
}