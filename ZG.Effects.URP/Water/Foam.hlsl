#ifndef WATER_FOAM_INCLUDED
#define WATER_FOAM_INCLUDED

#if _FOAM_ON
TEXTURE2D(_FoamNoise); 
SAMPLER(sampler_FoamNoise);
//float4 _FoamNoise_ST;

half CalculateFoam(float facing, float viewWaterHeight, float2 uv)
{
	float distance = lerp(_FoamMaxDistance, _FoamMinDistance, facing);
	float t = saturate(distance / viewWaterHeight);
	float range = pow(t, _FoamPower);
	float noise = SAMPLE_TEXTURE2D(_FoamNoise, sampler_FoamNoise, uv * _FoamNoise_ST.xy + _Time.x * _FoamNoise_ST.zw).r;
	//foamNoise = pow(foamNoise, _FoamNoisePower);
	return step(noise, range) * (_FoamColor.a/* - t */ );
	//return lerp(color, _FoamColor.rgb, step(noise, range) * (_FoamColor.a - t));
}
#endif

#endif
