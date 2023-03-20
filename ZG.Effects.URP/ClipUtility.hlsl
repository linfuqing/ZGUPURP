#ifndef CLIP_UTILITY_CGINC  
#define CLIP_UTILITY_CGINC

#ifdef CLIP_GLOBAL
float g_ClipInvDist;
float g_ClipNearDivDist;
float g_ClipFarDivDist;
#endif

float3 CalculatePixelPos(float4 screenPos)
{
	float3 pixelPos = screenPos.xyz / screenPos.w;
	pixelPos.xy *= _ScreenParams.xy;
	return pixelPos;
}

float CalculateAlphaClipThreshold(float2 pixelPos)
{
	const float4x4 thresholdMatrix =
	{
		1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
		13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
		4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
		16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
	};

	return thresholdMatrix[fmod(pixelPos.x, 4)][fmod(pixelPos.y, 4)];
}

float CalculateNearFarClipThreshold(float screenDepth, float invDist, float nearDivDist, float farDivDist)
{
#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	float linearEyeDepth = LinearEyeDepth((screenDepth + 1.0f) * 0.5f, _ZBufferParams);
#else
	float linearEyeDepth = LinearEyeDepth(screenDepth, _ZBufferParams);
#endif

	float depthDivDist = linearEyeDepth * invDist;

	return saturate(depthDivDist - nearDivDist) * saturate(farDivDist - depthDivDist);
}

float CalculateMixedClipThreshold(float4 screenPos, float invDist, float nearDivDist, float farDivDist, float alpha)
{
	float3 pixelPos = CalculatePixelPos(screenPos);

	alpha *= 1.0f - CalculateNearFarClipThreshold(pixelPos.z, invDist, nearDivDist, farDivDist);
	return alpha + CalculateAlphaClipThreshold(pixelPos.xy);
}

void AlphaClip_float(float4 screenPos, out float threshold)
{
#ifdef UNITY_PASS_SHADOWCASTER
	threshold = 0.0f;
#else
	threshold = CalculateAlphaClipThreshold(screenPos.xy / screenPos.w * _ScreenParams.xy); 
#endif
}

void MixedClip_float(float4 screenPos, float invDist, float nearDivDist, float farDivDist, float alpha, out float threshold)
{
#ifdef UNITY_PASS_SHADOWCASTER
	threshold = 0.0f;
#else

#ifdef CLIP_GLOBAL
	invDist = g_ClipInvDist;
	nearDivDist = g_ClipNearDivDist;
	farDivDist = g_ClipFarDivDist;
#endif

	threshold = CalculateMixedClipThreshold(screenPos, invDist, nearDivDist, farDivDist, alpha);
#endif
}
#endif