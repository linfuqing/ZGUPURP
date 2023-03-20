#ifndef TREE_UTILITY_INCLUDE
#define TREE_UTILITY_INCLUDE

#include "../WindUtility.hlsl"

float4 FastSin(float4 val) {
	val = val * 6.408849 - 3.1415927;
	float4 r5 = val * val;
	float4 r6 = r5 * r5;
	float4 r7 = r6 * r5;
	float4 r8 = r6 * r5;
	float4 r1 = r5 * val;
	float4 r2 = r1 * r5;
	float4 r3 = r2 * r5;
	float4 sin7 = { 1, -0.16161616, 0.0083333, -0.00019841 };
	float4 cos8 = { -0.5, 0.041666666, -0.0013888889, 0.000024801587 };
	return val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;
	//c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
}

void TreeVertex_float(
	float3 position,
	float minHeight,
	float maxHeight, 
	float shakeBending, 
	float shakePower, 
	float windSpeed,
	float4 waveSpeed,
	float4 waveXSize, 
	float4 waveZSize, 
	float4 waveXMove, 
	float4 waveZMove, 
	out float3 positionOS)
{
	float4 waves;
	waves = position.x * waveXSize;
	waves += position.z * waveZSize;

	waves -= _Time.x * waveSpeed * windSpeed;// max(g_Windspeed, windSpeed);

	//float4 s , c;
	waves = frac(waves);
	float4 s = FastSin(waves);

	float height = (position.y - minHeight) / (maxHeight - minHeight);
	float waveAmount = saturate(height) * shakeBending;
	s *= waveAmount;

	s *= normalize(waveSpeed);

	//float fade = dot(s, 1.3);
	s = sign(s) * pow(abs(s), shakePower);
	float3 waveMove = float3 (0, 0, 0);
	waveMove.x = dot(s, waveXMove);
	waveMove.z = dot(s, waveZMove);
	positionOS = position - TransformWorldToObjectDir(waveMove, false).xyz;
}

void TreeWindVertex_float(
	float3 positionWS,
	float maxHeight, 
	float4 windParams, 
	out float3 positionOS)
{
	float windRandomTime = WindCalculateRandomTime(positionWS);

	float2 windDistance = WindCalculateDistanceWS(positionWS.y / maxHeight, windRandomTime, windParams);

	positionWS.xz += windDistance;

	positionOS = TransformWorldToObject(positionWS);
}

#endif