#ifndef WATER_CAUSTICS_INCLUDED
#define WATER_CAUSTICS_INCLUDED

#ifdef _CAUSTICS_ON
TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);

float2 Panner(float2 uv, float2 offset, float tiling)
{
	return  _Time.y * offset + uv * tiling;
}

#if _CAUSTICS_NORMAL
half3 TexCaustics(float2 uv, float mipLod)
{
	float2 normal = _CausticsDistortionStrength * SampleNormal(uv * _CausticsDistortionScale, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1.0f).xz;

	float2 uv1 = normal * _CausticsST1.y + Panner(uv, _CausticsST1.zw, _CausticsST1.x);// float4((normal * _CausticsST1.y + Panner(uv, _CausticsST1.zw, _CausticsST1.x)), 0.0f, mipLod);
	float2 uv2 = normal * _CausticsST2.y + Panner(uv, _CausticsST2.zw, _CausticsST2.x);// float4((normal * _CausticsST2.y + Panner(uv, _CausticsST2.zw, _CausticsST2.x)), 0.0f, mipLod);

	return SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uv1, mipLod).xyz + SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uv2, mipLod).xyz;
}
#elif _CAUSTICS_COLOUR
half3 RGBSplit(float split, float2 uv, float mipLod)
{
	float2 uvr = uv + float2(split, split);// float4(uv + float2(split, split), 0, mipLod);
	float2 uvg = uv + float2(split, -split);// float4(uv + float2(split, -split), 0, mipLod);
	float2 uvb = uv + float2(-split, -split);// float4(uv + float2(-split, -split), 0, mipLod);

	half r = SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uvr, mipLod).r;
	half g = SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uvg, mipLod).g;
	half b = SAMPLE_TEXTURE2D_LOD(_CausticsTex, sampler_CausticsTex, uvb, mipLod).b;

	return half3(r, g, b);
}

half3 TexCaustics(float2 uv, float mipLod)
{
	float2 uv1 = Panner(uv, _CausticsST1.zw, _CausticsST1.x);
	float2 uv2 = Panner(uv, _CausticsST2.zw, _CausticsST2.x);

	half3 texture1 = RGBSplit(_CausticsST1.y, uv1, mipLod);
	half3 texture2 = RGBSplit(_CausticsST2.y, uv2, mipLod);

	half3 textureCombined = min(texture1, texture2);

	return textureCombined;
}
#else
half3 TexCaustics(float2 uv, float mipLod)
{
	return 0;
}
#endif

half3 ApplyCaustics(
	half3 lightColor,
	half3 lightDirectionWS,
	float3 scenePos)
{
	// Compute mip index manually, with bias based on sea floor depth. We compute it manually because if it is computed automatically it produces ugly patches
	// where samples are stretched/dilated. The bias is to give a focusing effect to caustics - they are sharpest at a particular depth. This doesn't work amazingly
	// well and could be replaced.
	float mipLod = abs(scenePos.y - _CausticsFocalDepth) * _CausticsInvDepthOfField;
	// project along light dir, but multiply by a fudge factor reduce the angle bit - compensates for fact that in real life
	// caustics come from many directions and don't exhibit such a strong directonality
	float2 surfacePosXZ = scenePos.xz + lightDirectionWS.xz * (scenePos.y / (4.0 * lightDirectionWS.y));
	
	//half alpha = saturate(scenePos.y / _CausticsFocalDepth);

	// Scale caustics strength by primary light, depth fog density and scene depth.
	half3 strength = /*alpha * */ _CausticsStrength * lightColor;

	//return causticsStrength * tex2Dlod(_MainTex, uv).xyz;
	return strength * TexCaustics(surfacePosXZ, mipLod);
}
#endif
#endif
