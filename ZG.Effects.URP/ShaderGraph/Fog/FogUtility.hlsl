#ifndef FOG_UTILITY_INCLUDE
#define FOG_UTILITY_INCLUDE

uniform float g_FogFactor;

float FogLinear(float z, float fogScale, float fogOffset)
{
	float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

	// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))

	return saturate(clipZ_01 * fogScale + fogOffset);
}

float FogHeight(float3 positionWS, float4 fogParams)
{
	float z = TransformWorldToHClip(positionWS).z;

#ifdef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
	float fogFactor = ComputeFogFactor(z);
#else
	float fogFactor = 1.0f;
#endif

#ifdef _FOG_HEIGHT
	float fogHeight = saturate(positionWS.y * fogParams.y + fogParams.x);

	fogHeight = lerp(max(g_FogFactor, fogFactor), fogFactor, fogHeight);
	fogHeight = lerp(fogFactor, fogHeight, FogLinear(z, fogParams.w, fogParams.z));

#else
	float fogHeight = fogFactor;
#endif

/*#ifdef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
	fogHeight = ComputeFogIntensity(fogHeight);
#endif*/

	return fogHeight;
}

void FogHeight_float(float3 positionWS, float4 fogParams, float3 fragColor, out float3 fogColor)
{
	float fogFactor = FogHeight(positionWS, fogParams);

#ifdef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
	fogColor = MixFog(fragColor, fogFactor);
#else
	fogColor = fragColor;
#endif
}

#endif
