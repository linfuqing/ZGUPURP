#ifndef WIND_UTILITY_INCLUDE
#define WIND_UTILITY_INCLUDE

#include "NoiseUtility.hlsl"

uniform float4 g_WindParams;

#define WIND_DIR g_WindParams.xy
#define WIND_SPEED g_WindParams.z
#define WIND_POS_SCALE g_WindParams.w

float WindCalculateRandomTime(float3 positionWS)
{
	return Noise(positionWS.xz * WIND_POS_SCALE);
}

float WindCalculateForce(float windRandomTime)
{
	return (sin(_Time.y + windRandomTime) + 1.0f) * 0.5f;
}

float2 WindCalculateDistanceWS(float height, float windRandomTime, float4 windParams)
{
	float windSpeed = max(WIND_SPEED, windParams.x), 
		windForce = (sin(windSpeed * _Time.y + windRandomTime) + 1.0f) * 0.5f,
		windStrength = /*windSpeed * */ lerp(windParams.y, windParams.z, windForce);

	return WIND_DIR * (windStrength * pow(height, windParams.w));
}

float3 WindCalculateNoiseDistanceWS(
	float3 positionWS, 
	float4 windNoiseST, 
	float4 windNoiseParams,
	float windForce)
{
	float windAngle = Noise(positionWS.xz * windNoiseST.xy + windNoiseST.zw + max(WIND_SPEED, windNoiseParams.x) * _Time.y * WIND_DIR) * (PI * 2.0f);

	return float3(cos(windAngle), 0.0f, sin(windAngle)) * (lerp(windNoiseParams.y, windNoiseParams.z, windForce) * pow(positionWS.y, windNoiseParams.w));
}

#endif
