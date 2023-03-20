#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if _TOON_ON
half Toon(half term, half4 params)
{
	return params.x * (params.y + floor(term * params.z) / params.w);
}
#endif

half3 ToonLightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
	half NdotL = saturate(dot(normal, lightDir)), diffuseTerm = NdotL;

#if _TOON_ON
	diffuseTerm = Toon(diffuseTerm, _DiffuseToon);
#endif

	return lightColor * diffuseTerm;
}

half3 ToonVertexLighting(float3 positionWS, half3 normalWS)
{
	half3 vertexLightColor = half3(0.0, 0.0, 0.0);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	uint lightsCount = GetAdditionalLightsCount();
	for (uint lightIndex = 0u; lightIndex < lightsCount; ++lightIndex)
	{
		Light light = GetAdditionalLight(lightIndex, positionWS);
		half3 lightColor = light.color * light.distanceAttenuation;
		vertexLightColor += ToonLightingLambert(lightColor, light.direction, normalWS);
	}
#endif

	return vertexLightColor;
}

half3 ToonLightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half3 specular, half smoothness)
{
	float3 halfVec = SafeNormalize(float3(lightDir)+float3(viewDir));
	half NdotH = saturate(dot(normal, halfVec));
	half modifier = pow(NdotH, smoothness);

	half specularTerm = modifier;

#if _TOON_ON
	specularTerm = Toon(specularTerm, _SpecularToon);
#endif

	half3 specularReflection = specular * specularTerm;
	return lightColor * specularReflection;
}

void ToonLighting(
	Light light,
	half3 normalWS,
	half3 viewDirectionWS, 
	half3 specular, 
	half smoothness, 
	out half3 diffuseColor,
	out half3 specularColor)
{
	half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);

	diffuseColor = ToonLightingLambert(attenuatedLightColor, light.direction, normalWS);
	specularColor = ToonLightingSpecular(attenuatedLightColor, light.direction, normalWS, viewDirectionWS, specular, smoothness);
}

half3 ToonGlobalIllumination(BRDFData brdfData, half3 bakedGI, half3 runtimeRefl, half reflTerm, half occlusion, half NoV, half3 reflectDirWS/*half3 normalWS, half3 viewDirectionWS*/)
{
	//half3 reflectVector = reflect(-viewDirectionWS, normalWS);
	half fresnelTerm = Pow4(1.0 - NoV);

#if _TOON_ON
	fresnelTerm = Toon(fresnelTerm, _FresnelToon);
#endif

	half3 indirectDiffuse = bakedGI * occlusion;
	half3 indirectSpecular = GlossyEnvironmentReflection(reflectDirWS/*reflectVector*/, brdfData.perceptualRoughness, occlusion);

	indirectSpecular = lerp(indirectSpecular, runtimeRefl, reflTerm);

	return EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
}

half3 ToonDirectBDRF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
#ifdef _SPECULARHIGHLIGHTS_OFF
	return brdfData.diffuse;
#else
	float3 halfDir = SafeNormalize(float3(lightDirectionWS)+float3(viewDirectionWS));

	float NoH = saturate(dot(normalWS, halfDir));
	half LoH = saturate(dot(lightDirectionWS, halfDir));

	// GGX Distribution multiplied by combined approximation of Visibility and Fresnel
	// BRDFspec = (D * V * F) / 4.0
	// D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
	// V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
	// See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
	// https://community.arm.com/events/1155

	// Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
	// We further optimize a few light invariant terms
	// brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
	float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

	half LoH2 = LoH * LoH;
	half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

#if _TOON_ON
	specularTerm = Toon(specularTerm, _SpecularToon);
#endif

	// On platforms where half actually means something, the denominator has a risk of overflow
	// clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
	// sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
	specularTerm = specularTerm - HALF_MIN;
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

	half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
	return color;
#endif
}

half3 ToonLightingPB(
	BRDFData brdfData, 
	Light light,
	half3 normalWS,
	half3 viewDirectionWS,
	out half3 radiance)
{
	half NdotL = saturate(dot(normalWS, light.direction)), diffuseTerm = light.distanceAttenuation * light.shadowAttenuation * NdotL;

#if _TOON_ON
	diffuseTerm = Toon(diffuseTerm, _DiffuseToon);
#endif

	radiance = light.color * diffuseTerm;
	return ToonDirectBDRF(brdfData, normalWS, light.direction, viewDirectionWS) * radiance;
}
#endif
