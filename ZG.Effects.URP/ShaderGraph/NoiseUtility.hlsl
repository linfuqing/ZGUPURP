#ifndef NOISE_UTILITY_INCLUDE
#define NOISE_UTILITY_INCLUDE

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

#endif
