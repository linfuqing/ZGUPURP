#ifndef GRASS_UTILITY_INCLUDE
#define GRASS_UTILITY_INCLUDE

uniform float g_Windspeed;
uniform float g_GrassMinDistance;
uniform float g_GrassMaxDistance;
uniform int g_GrassObstacleCount = 0;
uniform float4 g_GrassObstacles[32];

inline float Random(float2 st)
{
	return frac(sin(dot(st, float2(12.9898f, 78.233f))) * 43758.5453123f);
}

inline float Noise(float2 x)
{
	float2 i = floor(x);
	float2 f = frac(x);

	// Four corners in 2D of a tile
	float a = Random(i);
	float b = Random(i + float2(1.0, 0.0));
	float c = Random(i + float2(0.0, 1.0));
	float d = Random(i + float2(1.0, 1.0));

	// Simple 2D lerp using smoothstep envelope between the values.
	// return vec3(mix(mix(a, b, smoothstep(0.0, 1.0, f.x)),
	//			mix(c, d, smoothstep(0.0, 1.0, f.x)),
	//			smoothstep(0.0, 1.0, f.y)));

	// Same code, with the clamps in smoothstep and common subexpressions
	// optimized away.
	float2 u = f * f * (3.0 - 2.0 * f);
	return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
	/*float bottomOfGrid = lerp(a, b, u.x);
	float topOfGrid = lerp(c, d, u.x);
	return lerp(bottomOfGrid, topOfGrid, u.y);*/
}

/*inline float Noise(float2 st)
{
	float t = 0.0;

	float freq = pow(2.0, float(0));
	float amp = pow(0.5, float(3 - 0));
	t += ValueNoise(float2(st.x / freq, st.y / freq)) * amp;

	freq = pow(2.0, float(1));
	amp = pow(0.5, float(3 - 1));
	t += ValueNoise(float2(st.x / freq, st.y / freq)) * amp;

	freq = pow(2.0, float(2));
	amp = pow(0.5, float(3 - 2));
	t += ValueNoise(float2(st.x / freq, st.y / freq)) * amp;

	return t;
}*/

float HeightCalculateNoise(
	float minDistance,
	float maxDistance, 
	float minHeight, 
	float3 positionWS, 
	float4 heightST, 
	out float viewDistance)
{
	viewDistance = distance(_WorldSpaceCameraPos, positionWS);
	minDistance = max(minDistance, g_GrassMinDistance);
	maxDistance = max(maxDistance, g_GrassMaxDistance);

	return max(Noise(positionWS.xz * heightST.xy + heightST.zw), minHeight) * smoothstep(maxDistance, minDistance, viewDistance);
}

float3 WindCalculateWorldDistance(
	float height, 
	float3 positionWS, 
	float4 windDirection, 
	float4 windDirectionParams, 

	float4 windNoise,
	float4 windNoiseST,

	float4 windNoise2,
	float4 windNoiseST2)
{
	float windSpeedScale = max(g_Windspeed, 1.0f),
		windSpeed = windSpeedScale * windDirectionParams.x,
		randomTime = Noise(positionWS.xz * windDirection.w), 
		windForce = (sin(windSpeed * windDirection.z * _Time.y + randomTime) + 1.0f) * 0.5f, 
		windStrength = windSpeed * lerp(windDirectionParams.y, windDirectionParams.z, windForce);
	float3 windDistance;
	windDistance.y = 0.0f;
	windDistance.xz = windDirection.xy * (windStrength * pow(height, windDirectionParams.w));

	windForce = (sin(_Time.y + randomTime) + 1.0f) * 0.5f;
	float windAngle = Noise(positionWS.xz * windNoiseST.xy + windNoiseST.zw + windSpeedScale * windNoise.x * _Time.y * windDirection.xy) * (PI * 2.0f);
	windDistance += float3(cos(windAngle), 0.0f, sin(windAngle)) * (lerp(windNoise.y, windNoise.z, windForce) * pow(height, windNoise.w));

	windAngle = Noise(positionWS.xz * windNoiseST2.xy + windNoiseST2.zw + windSpeedScale * windNoise2.x * _Time.y * windDirection.xy) * (PI * 2.0f);
	windDistance += float3(cos(windAngle), 0.0f, sin(windAngle)) * (lerp(windNoise2.y, windNoise2.z, windForce) * pow(height, windNoise2.w));

	return windDistance;
}

float3 ObstacleCalcalateWorldDistance(float strength, float3 positionWS)
{
	float3 distance = 0.0f;
	for (int i = 0; i < g_GrassObstacleCount; ++i)
	{
		float4 obstacle = g_GrassObstacles[i];
		float3 obstacleDistance = positionWS - obstacle.xyz;
		//obstacleDistance.y = 0.0f;
		float obstacleLength = length(obstacleDistance), obstacleStrength = (1.0f - saturate(obstacleLength / obstacle.w));

		distance += obstacleDistance * obstacleStrength / obstacleLength;
		//worldDistance += obstacleDistance * (saturate((1 - obstacleLength + obstacle.w) * heightRate * _Strength) / obstacleLength);
	}

	return distance * strength;
}

float3 CalculateWorldDistance(
	float obstacleStrength,

	float minHeight,

	half heightRate,

	half heightScale, 
	half heightPower, 

	float4 vertical,

	float4 windDirection,
	float4 windDirectionParams,

	float4 windNoise,
	float4 windNoiseST,

	float4 windNoise2,
	float4 windNoiseST2,

	float4 heightST,

	float3 positionWS,
	out float3 normalWS, 
	out float heightNoise, 
	out float viewDistance)
{
	heightNoise = HeightCalculateNoise(vertical.z, vertical.w, minHeight, positionWS, heightST, viewDistance);

	float randomAngle = Random(round(positionWS.xz * vertical.x)) * PI * 2.0f,
		height = heightNoise * heightRate;

	float3 randomDistance = float3(cos(randomAngle), 0.0f, sin(randomAngle));
	randomDistance *= heightScale * pow(height, heightPower);

	float3 windDistance = WindCalculateWorldDistance(
		height,
		positionWS,
		windDirection,
		windDirectionParams,

		windNoise,
		windNoiseST,

		windNoise2,
		windNoiseST2);

	float3 distanceWS = randomDistance + windDistance;

	height *= vertical.y;
	distanceWS.y += height;

	float3 position = positionWS + distanceWS;

	distanceWS += ObstacleCalcalateWorldDistance(heightRate * obstacleStrength, position);

	normalWS = heightRate > 0.0f ? SafeNormalize(distanceWS) : float3(0.0f, 1.0f, 0.0f);

	return normalWS * height;
}

// Normalize to support uniform scaling
/*float3 TransformViewToWorldDir(float3 dirWS, bool doNormalize = true)
{
#ifndef SHADER_STAGE_RAY_TRACING
	float3 dirOS = mul((float3x3)GetWorldToViewMatrix(), dirWS);
#else
	float3 dirOS = mul((float3x3)WorldToView3x4(), dirWS);
#endif
	if (doNormalize)
		return normalize(dirOS);

	return dirOS;
}*/

// Transforms normal from world to object space
/*float3 TransformViewToWorldNormal(float3 normalVS, bool doNormalize = true)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
	return TransformViewToWorldDir(normalVS, doNormalize);
#else
	// Normal need to be multiply by inverse transpose
	float3 normalOS = mul(normalVS, (float3x3)GetWorldToViewMatrix());
	if (doNormalize)
		return SafeNormalize(normalOS);
#endif

	return normalOS;
}*/

float3 GrassCalculateWorldDistance(
	float obstacleStrength,
	float minHeight,

	half heightRate,

	float4 vertical,
	float4 horizontal,

	float4 windDirection,
	float4 windDirectionParams,

	float4 windNoise,
	float4 windNoiseST,

	float4 windNoise2,
	float4 windNoiseST2,

	float4 heightST,

	float3 positionWS,
	float3 normalOS,
	out float3 normalWS)
{
	float heightNoise, viewDistance;

	float3 distanceWS = CalculateWorldDistance(
		obstacleStrength,

		minHeight,

		heightRate,

		horizontal.z,
		horizontal.w,

		vertical,

		windDirection,
		windDirectionParams,

		windNoise,
		windNoiseST,

		windNoise2,
		windNoiseST2,

		heightST,

		positionWS,
		normalWS,
		heightNoise, 
		viewDistance);

	float horizontalValue = smoothstep(1.0f, horizontal.x, heightRate) * smoothstep(0.0f, horizontal.x, heightRate);
	float3 distanceNormal = TransformViewToWorldNormal(normalOS) * (horizontalValue * horizontal.y * heightNoise);
	//v.vertex.xyz += mul((float3x3)unity_WorldToObject, float3(0.0f, _Vertical.y, 0.0f)) * height;

	distanceWS += distanceNormal;

	return distanceWS;
}

void GrassNormal_float(
	float obstacleStrength,

	float4 vertical,
	float4 horizontal,

	float4 windDirection,
	float4 windDirectionParams,

	float4 windNoise,
	float4 windNoiseST,

	float4 windNoise2,
	float4 windNoiseST2,

	float4 heightST,

	float3 positionWS,
	out float3 normalWS,
	out float viewDistance)
{
	float heightNoise;
	float3 distanceWS = CalculateWorldDistance(
			obstacleStrength,

			horizontal.y,
			horizontal.x,

			horizontal.z,
			horizontal.w,

			vertical,

			windDirection,
			windDirectionParams,

			windNoise,
			windNoiseST,

			windNoise2,
			windNoiseST2,

			heightST,

			positionWS,
			normalWS, 
			heightNoise, 
			viewDistance);

	//normalOS = TransformWorldToObjectNormal(normalWS);
}

void GrassVertex_float(
	float heightRate,

	float obstacleStrength,
	float minHeight,

	float4 vertical,
	float4 horizontal,

	float4 windDirection,
	float4 windDirectionParams,

	float4 windNoise,
	float4 windNoiseST,

	float4 windNoise2,
	float4 windNoiseST2,

	float4 heightST,

	float3 positionWS,
	float3 normalOS,
	out float3 outPositionOS, 
	out float3 outNormalOS)
{
	float3 normalWS, 
		distanceWS = GrassCalculateWorldDistance(
			obstacleStrength,

			minHeight,
			heightRate, 

			vertical,
			horizontal,

			windDirection,
			windDirectionParams,

			windNoise,
			windNoiseST,

			windNoise2,
			windNoiseST2,

			heightST,

			positionWS,
			normalOS, 
			normalWS);

	outPositionOS = TransformWorldToObject(positionWS + distanceWS);
	outNormalOS = TransformWorldToObjectNormal(normalWS);
}
#endif