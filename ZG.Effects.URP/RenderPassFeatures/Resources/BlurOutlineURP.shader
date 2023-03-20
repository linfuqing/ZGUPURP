Shader "ZG/BlurOutlineURP"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    /*TEXTURE2D_X(_SourceTex);
    float4 _SourceTex_TexelSize;*/
    float4 _BlitTexture_TexelSize;

    TEXTURE2D_X(_BlurTex);

    float _BlurOffsetX;
    float _BlurOffsetY;

    float _Strength;

    float4 FragTex(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
    }

    half4 FragBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        float2 offsets = float2(_BlitTexture_TexelSize.x * _BlurOffsetX, _BlitTexture_TexelSize.y * _BlurOffsetY);

        float4 uv01 = uv.xyxy + offsets.xyxy * float4(1, 1, -1, -1);
        float4 uv23 = uv.xyxy + offsets.xyxy * float4(1, 1, -1, -1) * 2.0;
        float4 uv45 = uv.xyxy + offsets.xyxy * float4(1, 1, -1, -1) * 3.0;

        half4 color = half4(0.0, 0.0, 0.0, 0.0);

        color += 0.40 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        color += 0.15 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv01.xy);
        color += 0.15 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv01.zw);
        color += 0.10 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv23.xy);
        color += 0.10 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv23.zw);
        color += 0.05 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv45.xy);
        color += 0.05 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv45.zw);

        return color;
    }

    half4 FragOutline(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        return SAMPLE_TEXTURE2D_X(_BlurTex, sampler_LinearClamp, uv) - SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
    }

    half4 FragFinal(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        //SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv) + 
        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * _Strength;
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Blur Outline-Tex"

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragTex
            ENDHLSL
        }

        Pass
        {
            Name "Blur Outline-Blur"

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragBlur
            ENDHLSL
        }

        Pass
        {
            Name "Blur Outline-Outline"

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragOutline
            ENDHLSL
        }

        Pass
        {
            Name "Blur Outline-Final"
            BlendOp Add
            Blend One One

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragFinal
            ENDHLSL
        }
    }
}
