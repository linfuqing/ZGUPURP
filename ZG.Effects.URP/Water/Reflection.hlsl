#ifndef WATER_FEFLECTION_INCLUDED
#define WATER_FEFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

TEXTURE2D_X(_ReflectionTex);
SAMPLER(sampler_LinearClamp);

half3 SampleReflectiveColor(float4 positionNDC, float sceneDepth)
{
	float2 uv = positionNDC.xy / positionNDC.w;

	float compareDepth = step(sceneDepth, positionNDC.w);

	return SAMPLE_TEXTURE2D_X(_ReflectionTex, sampler_LinearClamp, uv).rgb;
}

half3 CalculateReflectiveColor(float3 positionWS, half3 reflectDirWS, float viewHeight, out float term)
{
	//float2 _MarchParams = float2(15, 1);

	float3 reflectPositionWS = reflectDirWS * (viewHeight * _ReflectionMarchScale + _ReflectionMarchOffset) + positionWS;

	float4 reflectPositionCS = TransformWorldToHClip(reflectPositionWS);
	float4 reflectPositionNDC = ComputeScreenPos(reflectPositionCS);

	float2 uv = reflectPositionNDC.xy / reflectPositionNDC.w;

	/*float reflectDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);

	float compareDepth = step(sceneDepth, reflectDepth);

	float compareZ = step(reflectDepth, _ProjectionParams.z);

	float facing = step(dot(viewDirWS, reflectDirWS), 0.0f);*/

	half2 result = max(0.0, 1.0 - uv * uv * uv * uv);

	term = result.x * result.y;

	//return lerp(skyColor, SampleSceneColor(uv), reflectDepth * compareDepth * facing * result);

	return /*compareDepth * compareZ * facing * */ SampleSceneColor(uv);
}

#endif
