#ifndef WATER_FOWARD_PASS_INCLUDED
#define WATER_FOWARD_PASS_INCLUDED

#include "WaterLighting.hlsl"
#include "Refraction.hlsl"
#include "Reflection.hlsl"
//#include "Foam.hlsl"

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float3 positionWS               : TEXCOORD2;

    float4 projectedPosition        : TEXCOORD3;

    float3 normalWS                 : TEXCOORD4;
#ifdef _NORMALMAP
    float4 tangentWS                : TEXCOORD5;    // xyz: tangent, w: sign
#endif
    float3 viewDirWS                : TEXCOORD6;

    half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD8;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 ReconstructionWorldPos(float3 viewDirectionWS, float sceneDepth)
{
    float3 viewForwardDir = GetViewForwardDir();
    return GetCurrentViewPosition() + viewDirectionWS * (sceneDepth / dot(viewForwardDir, viewDirectionWS));
}

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

#ifdef _NORMALMAP
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    half3 viewDirWS = SafeNormalize(input.viewDirWS);

    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings WaterForwardPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    half3 vertexLight = ToonVertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#ifdef _NORMALMAP
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionWS = vertexInput.positionWS;

    output.projectedPosition = vertexInput.positionNDC;

    output.positionCS = vertexInput.positionCS;

    return output;
}

// Used in Standard (Physically Based) shader
half4 WaterForwardPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    float sceneDepth, viewWaterDepth;
    half3 refraction = CalculateRefractiveColor(input.projectedPosition, surfaceData.normalTS.xy, sceneDepth, viewWaterDepth);

    float reflectTerm;
    half3 reflectDirWS = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half3 reflection = CalculateReflectiveColor(input.positionWS, reflectDirWS, input.viewDirWS.y, reflectTerm);// SampleReflectiveColor(input.projectedPosition, sceneDepth);// CalculateReflectiveColor(input.positionWS, input.viewDirWS, reflectDirWS, sceneDepth);

    float3 scenePos = ReconstructionWorldPos(inputData.viewDirectionWS, sceneDepth);
    scenePos.y = input.positionWS.y - scenePos.y;

#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
    float sceneWaterDepthFactor = saturate(viewWaterDepth * _WaterClarity);
#else
    float sceneWaterDepthFactor = pow(saturate(scenePos.y * _WaterClarity), _WaterClarityAttenuation);
#endif

    half4 color = lerp(_BaseColor, _DepthColor, sceneWaterDepthFactor);

    //viewWaterDepth = sceneDepth - input.projectedPosition.w;

    half fog = saturate(1.0h - exp(_DepthFogDensity * -viewWaterDepth));

    surfaceData.albedo.rgb *= color.rgb;// *fog;
    //surfaceData.emission.rgb += refraction.rgb * (1.0h - fog);

    //float NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));

/*#if _FOAM_ON
    surfaceData.albedo = ApplyFoam(NoV, viewWaterDepth, input.uv, surfaceData.albedo);
#endif*/

    color = WaterLighting(
        inputData, 
        surfaceData, 
        reflectDirWS, 
        reflection, 
        reflectTerm, 
        fog, 
        //NoV, 
        input.uv, 
        scenePos);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.rgb = lerp(refraction.rgb, color.rgb, fog);

    color.a = OutputAlpha(color.a, 1);

    return color;
}

#endif
