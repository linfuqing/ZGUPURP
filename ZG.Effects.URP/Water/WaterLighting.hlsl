#ifndef WATER_LIGHTING_INCLUDED
#define WATER_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "../ToonLighting.hlsl"
#include "Caustics.hlsl"
#include "Foam.hlsl"

#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
	#define LIGHTING_SIMPLE
#else
	#ifdef _CAUSTICS_ON
		#define APPLY_CAUSTICS
	#endif
	#ifdef _FOAM_ON
		#define APPLY_FOAM
	#endif
#endif

half4 WaterBlinnPhong(
	InputData inputData, 
	half3 diffuse,
	half3 specular,
	half3 emission,
	half smoothness, 
	half alpha,
	half fog,
	float3 scenePos)
{
	// To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
	half4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
	half4 shadowMask = unity_ProbesOcclusion;
#else
	half4 shadowMask = half4(1, 1, 1, 1);
#endif

	Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

#if defined(_SCREEN_SPACE_OCCLUSION)
	AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
	mainLight.color *= aoFactor.directAmbientOcclusion;
	inputData.bakedGI *= aoFactor.indirectAmbientOcclusion;
#endif

	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

#ifdef _GLOSSINESS_FROM_BASE_ALPHA
	smoothness = exp2(10 * alpha + 1);
#else
	smoothness = exp2(10 * smoothness + 1);
#endif

	half3 diffuseColor, specularColor;
	ToonLighting(mainLight, inputData.normalWS, inputData.viewDirectionWS, specular, smoothness, diffuseColor, specularColor);

#ifdef APPLY_CAUSTICS
	emission += ApplyCaustics(fog * diffuseColor, mainLight.direction, scenePos);
#endif

	half3 finalDiffuseColor = inputData.bakedGI + diffuseColor;
	half3 finalSpecularColor = specularColor;

#ifdef _ADDITIONAL_LIGHTS
	uint pixelLightCount = GetAdditionalLightsCount();
	for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
	{
		Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
#if defined(_SCREEN_SPACE_OCCLUSION)
		light.color *= aoFactor.directAmbientOcclusion;
#endif

		ToonLighting(light, inputData.normalWS, inputData.viewDirectionWS, specular, smoothness, diffuseColor, specularColor);

#ifdef APPLY_CAUSTICS
		emission += ApplyCaustics(fog * diffuseColor, light.direction, scenePos);
#endif

		finalDiffuseColor += diffuseColor;
		finalSpecularColor += specularColor;

	}
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	finalDiffuseColor += inputData.vertexLighting;
#endif

	half3 finalColor = finalDiffuseColor * diffuse + emission;

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
	finalColor += finalSpecularColor;
#endif

	return half4(diffuse + emission/*finalColor */ , alpha);
}

half4 WaterPB(
	InputData inputData, 
	SurfaceData surfaceData, 
	half3 reflectDirWS,
	half3 reflection,
	half reflectTerm, 
	half fog, 
	half NoV,
	float2 uv, 
	float3 scenePos)
{
#ifdef _SPECULARHIGHLIGHTS_OFF
	bool specularHighlightsOff = true;
#else
	bool specularHighlightsOff = false;
#endif

	BRDFData brdfData;

	// NOTE: can modify alpha
	InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

#ifdef APPLY_FOAM
	half foam = CalculateFoam(NoV, scenePos.y, uv);

	brdfData.diffuse = lerp(brdfData.diffuse, _FoamColor.rgb, foam);
	brdfData.specular *= 1.0f - foam;
#endif

	BRDFData brdfDataClearCoat = (BRDFData)0;
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
	// base brdfData is modified here, rely on the compiler to eliminate dead computation by InitializeBRDFData()
	InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData, brdfDataClearCoat);
#endif

	// To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
	half4 shadowMask = inputData.shadowMask;
#elif !defined (LIGHTMAP_ON)
	half4 shadowMask = unity_ProbesOcclusion;
#else
	half4 shadowMask = half4(1, 1, 1, 1);
#endif

	Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

#if defined(_SCREEN_SPACE_OCCLUSION)
	AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
	mainLight.color *= aoFactor.directAmbientOcclusion;
	surfaceData.occlusion = min(surfaceData.occlusion, aoFactor.indirectAmbientOcclusion);
#endif

	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
	/*half3 color = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
		inputData.bakedGI, surfaceData.occlusion,
		inputData.normalWS, inputData.viewDirectionWS);*/

	half3 color = ToonGlobalIllumination(brdfData, inputData.bakedGI, reflection, reflectTerm, surfaceData.occlusion, NoV, reflectDirWS/*inputData.normalWS, inputData.viewDirectionWS*/), radiance;
	color += ToonLightingPB(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, radiance);

#ifdef APPLY_CAUSTICS
	color.rgb += ApplyCaustics(fog * radiance, mainLight.direction, scenePos);
#endif

#ifdef _ADDITIONAL_LIGHTS
	uint pixelLightCount = GetAdditionalLightsCount();
	for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
	{
		Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
		color += ToonLightingPB(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, radiance);

#ifdef APPLY_CAUSTICS
		color.rgb += ApplyCaustics(fog * radiance, mainLight.direction, scenePos);
#endif
	}
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	color += inputData.vertexLighting * brdfData.diffuse;
#endif

	color += surfaceData.emission;

	return half4(color, surfaceData.alpha);
}

half4 WaterLighting(
	InputData inputData,
	SurfaceData surfaceData,
	half3 reflectDirWS,
	half3 reflection,
	half reflectTerm,
	half fog,
	//half NoV,
	float2 uv, 
	float3 scenePos)
{
	fog = 1.0f - fog;

	float NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));

#ifdef LIGHTING_SIMPLE
	half3 diffuse = lerp(surfaceData.albedo, reflection, reflectTerm);
	half3 specular;

#if _SPECULAR_SETUP
	specular = surfaceData.specular;
#else
	specular = surfaceData.metallic;
#endif

#ifdef APPLY_FOAM
	half foam = CalculateFoam(NoV, scenePos.y, uv);

	diffuse = lerp(diffuse, _FoamColor.rgb, foam);
	specular *= 1.0f - foam;
#endif

	return WaterBlinnPhong(
		inputData, 
		diffuse, 
		specular,
		surfaceData.emission,
		surfaceData.smoothness, 
		surfaceData.alpha, 
		fog, 
		scenePos);
#else
	return WaterPB(
		inputData,
		surfaceData,
		reflectDirWS,
		reflection,
		reflectTerm,
		fog,
		NoV,
		uv, 
		scenePos);
#endif
}


#endif
