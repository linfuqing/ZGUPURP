#ifndef WATER_FEFRACTION_INCLUDED
#define WATER_FEFRACTION_INCLUDED

#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
#define REFRACTIVE_SIMPLE
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

float4 _CameraDepthTexture_TexelSize;

float2 AlignWithGrabTexel(float2 uv)
{
#if UNITY_UV_STARTS_AT_TOP
	if (_CameraDepthTexture_TexelSize.y < 0.0f)
		uv.y = 1 - uv.y;
#endif

	return (floor(uv * _CameraDepthTexture_TexelSize.zw) + 0.5f) * abs(_CameraDepthTexture_TexelSize.xy);
}

float3 CalculateRefractiveColor(float4 projectedPosition, float2 uv, out float sceneDepth, out float viewWaterDepth)
{
	//USING DEPTH TEXTURE(.W) BUT NOT ACTUAL RAYLENGTH IN WATER, NEED TO FIX
	float sceneDepthNoDistortion = LinearEyeDepth(SampleSceneDepth(projectedPosition.xy / projectedPosition.w), _ZBufferParams);

	float surfaceDepth = projectedPosition.w;
	float viewWaterDepthNoDistortion = sceneDepthNoDistortion - surfaceDepth;

	float4 distortedUV = projectedPosition;

	float2 uvOffset = uv * _RefractionStrength;

#ifndef REFRACTIVE_SIMPLE
	//Distortion near water surface should be attenuated
	uvOffset *= saturate(viewWaterDepthNoDistortion);
#endif

	distortedUV.xy = AlignWithGrabTexel(distortedUV.xy + uvOffset);

	//Resample depth to avoid false distortion above water
	sceneDepth = LinearEyeDepth(SampleSceneDepth(distortedUV.xy / distortedUV.w), _ZBufferParams);

	viewWaterDepth = sceneDepth - surfaceDepth;

#ifndef REFRACTIVE_SIMPLE
	float tmp = step(0.0f, viewWaterDepth);
	distortedUV.xy = lerp(AlignWithGrabTexel(projectedPosition.xy), distortedUV.xy, tmp);
	sceneDepth = lerp(sceneDepthNoDistortion, sceneDepth, tmp);
#endif

	return SampleSceneColor(distortedUV.xy / distortedUV.w);
}

#endif
