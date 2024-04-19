Shader "ZG/DualBlurURP"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    float4 _BlitTexture_TexelSize;

    float2 _Offset;

    half4 FragDownSample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float4 uv01, uv23;

        uv01.xy = uv - _BlitTexture_TexelSize.xy * _Offset;//top right
        uv01.zw = uv + _BlitTexture_TexelSize.xy * _Offset;//bottom left
        uv23.xy = uv - float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * _Offset;//top left
        uv23.zw = uv + float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * _Offset;//bottom right

        half4 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * 4.0;
        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv01.xy);
        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv01.zw);
        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv23.xy);
        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv23.zw);

        return sum * 0.125;
    }

    half4 FragUpSample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord),
        size = _BlitTexture_TexelSize.xy * 0.5f;
        float4 uv01, uv23, uv45, uv67;

        uv01.xy = uv + float2(-size.x * 2, 0) * _Offset;
        uv01.zw = uv + float2(-size.x, size.y) * _Offset;
        uv23.xy = uv + float2(0, size.y * 2) * _Offset;
        uv23.zw = uv + size * _Offset;
        uv45.xy = uv + float2(size.x * 2, 0) * _Offset;
        uv45.zw = uv + float2(size.x, -size.y) * _Offset;
        uv67.xy = uv + float2(0, -size.y * 2) * _Offset;
        uv67.zw = uv - size * _Offset;

        half4 sum = 0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv01.xy);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv01.zw) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv23.xy);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv23.zw) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv45.xy);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv45.zw) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv67.xy);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv67.zw) * 2;

        return sum * 0.0833f;
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Dual Blur Down Sample"

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragDownSample
            ENDHLSL
        }

        Pass
        {
            Name "Dual Blur Up Sample"

            HLSLPROGRAM
                #pragma vertex Vert//FullscreenVert
                #pragma fragment FragUpSample
            ENDHLSL
        }
    }
}
