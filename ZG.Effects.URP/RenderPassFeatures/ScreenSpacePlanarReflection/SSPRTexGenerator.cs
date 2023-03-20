using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

namespace ZG
{
    public class SSPRTexGenerator
    {
        public static readonly int ReflectionTex = Shader.PropertyToID("_ReflectionTex");

        private ComputeShader __computeShader;
        //private BlurBlitter _blurBlitter = new BlurBlitter();

        private int __kernalPass1;
        private int __kernalPass2;

        private int __kernelClear;

        public SSPRTexGenerator(ComputeShader cp)
        {
            __computeShader = cp;

            __kernelClear = __computeShader.FindKernel("Clear");
            __kernalPass1 = __computeShader.FindKernel("DrawReflectionTex1");
            __kernalPass2 = __computeShader.FindKernel("DrawReflectionTex2");
        }

        public void Configure(CommandBuffer cmd, in RenderTextureDescriptor cameraTextureDescriptor)
        {
            var reflectionTexDes = cameraTextureDescriptor;
            reflectionTexDes.enableRandomWrite = true;
            reflectionTexDes.msaaSamples = 1;
            cmd.GetTemporaryRT(ReflectionTex, reflectionTexDes);
        }

        public void Render(
            CommandBuffer cmd, 
            ref RenderingData renderingData, 
            ref PlanarDescriptor planarDescriptor)
        {
            var rtWidth = renderingData.cameraData.cameraTargetDescriptor.width;
            var rtHeight = renderingData.cameraData.cameraTargetDescriptor.height;

            ///==== Compute Shader Begin ===== ///

            var cameraData = renderingData.cameraData;
            var viewMatrix = cameraData.camera.worldToCameraMatrix;
            //不知道为什么，第二个参数是false才能正常得到世界坐标
            var projectMatrix = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(),false);
            var matrixVP = projectMatrix * viewMatrix;
            var invMatrixVP = matrixVP.inverse;

            var threadGroupX = rtWidth / 8;
            var threadGroupY = rtHeight / 8;

            RenderTargetIdentifier cameraColorTex = ShaderProperties.CameraOpaqueTexture;
            
            cmd.SetComputeVectorParam(__computeShader,ShaderProperties.MainTexelSize, new Vector4(1.0f / rtWidth,1.0f /rtHeight,rtWidth,rtHeight));
            cmd.SetComputeMatrixParam(__computeShader,ShaderProperties.MatrixVP, matrixVP);
            cmd.SetComputeMatrixParam(__computeShader,ShaderProperties.MatrixInvVP, invMatrixVP);
            cmd.SetComputeVectorParam(__computeShader,ShaderProperties.PlanarPosition, planarDescriptor.position);
            cmd.SetComputeVectorParam(__computeShader,ShaderProperties.PlanarNormal, planarDescriptor.normal);

            //clear the reflection texture
            cmd.SetComputeTextureParam(__computeShader, __kernelClear, ShaderProperties.Result, ReflectionTex);
            cmd.DispatchCompute(__computeShader, __kernelClear, threadGroupX,threadGroupY, 1);
            
            cmd.SetComputeTextureParam(__computeShader, __kernalPass1,ShaderProperties.CameraOpaqueTexture, cameraColorTex);
            cmd.SetComputeTextureParam(__computeShader, __kernalPass1,ShaderProperties.Result, ReflectionTex);
            cmd.DispatchCompute(__computeShader, __kernalPass1,threadGroupX,threadGroupY,1);

            cmd.SetComputeTextureParam(__computeShader, __kernalPass2, ShaderProperties.CameraOpaqueTexture, cameraColorTex);
            cmd.SetComputeTextureParam(__computeShader, __kernalPass2, ShaderProperties.Result, ReflectionTex);
            cmd.DispatchCompute(__computeShader, __kernalPass2,threadGroupX,threadGroupY,1);
    
            /*// ====== blur begin ===== ///
            _blurBlitter.SetSource(_reflectionTexID,reflectionTexDes);
            _blurBlitter.blurType = BlurType.BoxBilinear;
            _blurBlitter.iteratorCount = 1;
            _blurBlitter.downSample = 1;
            _blurBlitter.Render(cmd);*/

            cmd.SetGlobalTexture(ReflectionTex, ReflectionTex);
        }      

        public void ReleaseTemporary(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(ReflectionTex);
        }

        private static class ShaderProperties
        {
            public static readonly int Result = Shader.PropertyToID("_Result");
            public static readonly int CameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");
            public static readonly int PlanarPosition = Shader.PropertyToID("_PlanarPosition");
            public static readonly int PlanarNormal = Shader.PropertyToID("_PlanarNormal");
            public static readonly int MatrixVP = Shader.PropertyToID("_MatrixVP");
            public static readonly int MatrixInvVP = Shader.PropertyToID("_MatrixInvVP");
            public static readonly int MainTexelSize = Shader.PropertyToID("_MainTex_TexelSize");
        }
    }
}
