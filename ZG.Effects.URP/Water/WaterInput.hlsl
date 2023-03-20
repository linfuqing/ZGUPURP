#ifndef WATER_INPUT_INCLUDED
#define WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _DepthColor;
    half4 _SpecColor;
    half4 _EmissionColor;

    half3 _NormalScale;
    half3 _NormalSpeed;

    half _EmissionNormalScale;
    //half _SpecNormalScale;
    half _AlbedoNormalScale;

    half _Cutoff;
    half _Smoothness;
    half _Metallic;

    half _RefractionStrength;
    half _ReflectionMarchScale;
    half _ReflectionMarchOffset;

    half _WaterClarity;
    half _WaterClarityAttenuation;

    half _DepthFogDensity;

    float4 _DiffuseToon;
    float4 _SpecularToon;
    float4 _FresnelToon;

    half _CausticsStrength;
    half _CausticsFocalDepth;
    half _CausticsInvDepthOfField;

    half _CausticsDistortionScale;
    half _CausticsDistortionStrength;

    float4 _CausticsST1;
    float4 _CausticsST2;

    float4 _FoamNoise_ST;
    half _FoamMaxDistance;
    half _FoamMinDistance;
    half _FoamPower;
    half4 _FoamColor;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _DepthColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)

    UNITY_DOTS_INSTANCED_PROP(float3, _NormalSpeed)
    UNITY_DOTS_INSTANCED_PROP(float3, _NormalScale)

    UNITY_DOTS_INSTANCED_PROP(float, _EmissionNormalScale)
    //UNITY_DOTS_INSTANCED_PROP(float, _SpecNormalScale)
    UNITY_DOTS_INSTANCED_PROP(float, _AlbedoNormalScale)

    UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float, _Metallic)

    UNITY_DOTS_INSTANCED_PROP(float, _RefractionStrength)
    UNITY_DOTS_INSTANCED_PROP(float, _ReflectionMarchScale)
    UNITY_DOTS_INSTANCED_PROP(float, _ReflectionMarchOffset)

    UNITY_DOTS_INSTANCED_PROP(float, _WaterClarity)
    UNITY_DOTS_INSTANCED_PROP(float, _WaterClarityAttenuation)

    UNITY_DOTS_INSTANCED_PROP(float, _DepthFogDensity)

    UNITY_DOTS_INSTANCED_PROP(float4, _DiffuseToon)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularToon)
    UNITY_DOTS_INSTANCED_PROP(float4, _FresnelToon)

    UNITY_DOTS_INSTANCED_PROP(float, _CausticsStrength)
    UNITY_DOTS_INSTANCED_PROP(float, _CausticsFocalDepth)
    UNITY_DOTS_INSTANCED_PROP(float, _CausticsInvDepthOfField)

    UNITY_DOTS_INSTANCED_PROP(float, _CausticsDistortionScale)
    UNITY_DOTS_INSTANCED_PROP(float, _CausticsDistortionStrength)

    UNITY_DOTS_INSTANCED_PROP(float4, _CausticsST1)
    UNITY_DOTS_INSTANCED_PROP(float4, _CausticsST2)

    UNITY_DOTS_INSTANCED_PROP(float4, _FoamNoise_ST)

    UNITY_DOTS_INSTANCED_PROP(float,  _FoamMaxDistance)
    UNITY_DOTS_INSTANCED_PROP(float,  _FoamMinDistance)

    UNITY_DOTS_INSTANCED_PROP(float,  _FoamPower)
    UNITY_DOTS_INSTANCED_PROP(float4, _FoamColor)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _BaseColor)
#define _DepthColor                 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _DepthColor)
#define _SpecColor                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _SpecColor)
#define _EmissionColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _EmissionColor)

#define _NormalSpeed                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3  , _NormalSpeed)
#define _NormalScale                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3  , _NormalScale)

#define _EmissionNormalScale        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _EmissionNormalScale)
//#define _SpecNormalScale            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _SpecNormalScale)
#define _AlbedoNormalScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _AlbedoNormalScale)

#define _Cutoff                     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _Cutoff)
#define _Smoothness                 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _Smoothness)
#define _Metallic                   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _Metallic)

#define _RefractionStrength         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _RefractionStrength)
#define _ReflectionMarchScale       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _ReflectionMarchScale)
#define _ReflectionMarchOffset      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _ReflectionMarchOffset)

#define _WaterClarity               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _WaterClarity)
#define _WaterClarityAttenuation    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _WaterClarityAttenuation)

#define _DepthFogDensity            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _DepthFogDensity)

#if _TOON_ON
#define _DiffuseToon                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _DiffuseToon)
#define _SpecularToon               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _SpecularToon)
#define _FresnelToon                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _FresnelToon)
#endif

#if _CAUSTICS_ON
#define _CausticsStrength           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _CausticsStrength)
#define _CausticsFocalDepth         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _CausticsFocalDepth)
#define _CausticsInvDepthOfField    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _CausticsInvDepthOfField)

#if _CAUSTICS_NORMAL
#define _CausticsDistortionScale    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _CausticsDistortionScale)
#define _CausticsDistortionStrength UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _CausticsDistortionStrength)
#endif

#define _CausticsST1                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _CausticsST1)
#define _CausticsST2                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _CausticsST2)
#endif

#if _FOAM_ON
#define _FoamNoise_ST               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _FoamNoise_ST)

#define _FoamMaxDistance            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _FoamMaxDistance)
#define _FoamMinDistance            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _FoamMinDistance)

#define _FoamPower                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float   , _FoamPower)
#define _FoamColor                  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4  , _FoamColor)
#endif

#endif

/*TEXTURE2D(_OcclusionMap);           SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);       SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);           SAMPLER(sampler_SpecGlossMap);

#ifdef _SPECULAR_SETUP
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
#define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif*/

half4 SampleMetallicSpecGloss(/*float2 uv, */ half albedoAlpha)
{
    half4 specGloss;

/*#ifdef _METALLICSPECGLOSSMAP
    specGloss = SAMPLE_METALLICSPECULAR(uv);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _Smoothness;
#else
    specGloss.a *= _Smoothness;
#endif
#else // _METALLICSPECGLOSSMAP*/
#if _SPECULAR_SETUP
    specGloss.rgb = _SpecColor.rgb;
#else
    specGloss.rgb = _Metallic.rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    specGloss.a = albedoAlpha * _Smoothness;
#else
    specGloss.a = _Smoothness;
#endif
//#endif

    return specGloss;
}

float3 SampleNormalMap(float time, float2 uv, float3 scale, float3 speed)
{
    //return SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0f);

    float2 uv1 = uv * scale.x + time * speed.x * float2(1, 1);
    float3 normal = SampleNormal(uv1, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0f);
    float2 uv2 = uv * scale.y + time * speed.y * float2(1, -1);
    normal += SampleNormal(uv2, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0f);
    float2 uv3 = uv * scale.z + time * speed.z * float2(-1, 1);
    normal += SampleNormal(uv3, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0f);

    return normalize(normal);
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    outSurfaceData.normalTS = SampleNormalMap(_Time.y, uv, _NormalScale, _NormalSpeed);

    float alpha;
#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
    outSurfaceData.alpha = 1.0f;

    outSurfaceData.albedo = 1.0f;

    alpha = 1.0f;
#else
    half4 albedoAlpha = SampleAlbedoAlpha(uv + outSurfaceData.normalTS.xy * _AlbedoNormalScale, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    outSurfaceData.albedo = albedoAlpha.rgb;

    alpha = albedoAlpha.a;
#endif

    half4 specGloss = SampleMetallicSpecGloss(/*uv + outSurfaceData.normalTS.xy * _SpecNormalScale, */alpha);

#if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.occlusion = 1.0h;
    outSurfaceData.emission = SampleEmission(uv + outSurfaceData.normalTS.xy * _EmissionNormalScale, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.clearCoatMask = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
}

#endif
