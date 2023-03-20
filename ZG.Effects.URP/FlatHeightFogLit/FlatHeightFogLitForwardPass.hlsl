#ifndef FLAT_HEIGHT_FOG_PBR_FORWARD_PASS_INCLUDE
#define FLAT_HEIGHT_FOG_PBR_FORWARD_PASS_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

Varyings HeightFogVertex(Attributes input) 
{
    Varyings output = LitPassVertex(input);

#if defined(_FOG_FRAGMENT)
    float fogFactor = ComputeFogFactor(output.positionCS.z);
#else
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    float fogFactor = output.fogFactorAndVertexLight.x;
#else
    float fogFactor = output.fogFactor;
#endif
#endif

    float height;
#if REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
    height = input.positionWS.y;
#else
    height = TransformObjectToWorld(input.positionOS.xyz).y;
#endif

    height -= _FogHeightOffset;
    height = saturate(height * _FogParams.y + _FogParams.x);

    float outFogFactor = lerp(max(g_FogFactor, fogFactor), fogFactor, height);
    outFogFactor = lerp(fogFactor, outFogFactor, saturate(output.positionCS.w * _FogParams.w + _FogParams.z));

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight.x = outFogFactor;
#else
    output.fogFactor = outFogFactor;
#endif

    return output;
}

half4 HeightFogFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
    float2 uv = input.uv;
    half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half3 diffuse = diffuseAlpha.rgb * _BaseColor.rgb;

    half alpha = diffuseAlpha.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

    half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    half3 emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    half4 specular = SampleSpecularSmoothness(uv, alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    half smoothness = specular.a;

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    float factor = max(dot(inputData.normalWS, inputData.viewDirectionWS), 0.0f) * 0.5f + 0.5f;
    diffuse *= factor;
    emission *= 1.0f - factor;

    half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specular, smoothness, emission, alpha, normalTS);
#else
#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, input.viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    float factor = max(dot(inputData.normalWS, inputData.viewDirectionWS), 0.0f) * 0.5f + 0.5f;
    surfaceData.albedo *= factor;
    surfaceData.emission *= 1.0f - factor;
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
#endif

#if defined(_FOG_FRAGMENT)
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
#else
    inputData.fogCoord = input.fogFactor;
#endif
#endif

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);
    color.a = smoothstep(_AlphaEnd, _AlphaStart, input.positionCS.w);

    return color;
}

#endif