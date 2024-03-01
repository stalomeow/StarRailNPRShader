/*
 * StarRailNPRShader - Fan-made shaders for Unity URP attempting to replicate
 * the shading of Honkai: Star Rail.
 * https://github.com/stalomeow/StarRailNPRShader
 *
 * Copyright (C) 2023 Stalo <stalowork@163.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

#ifndef _CHAR_BODY_CORE_INCLUDED
#define _CHAR_BODY_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Shared/CharCore.hlsl"
#include "Shared/CharDepthOnly.hlsl"
#include "Shared/CharDepthNormals.hlsl"
#include "Shared/CharOutline.hlsl"
#include "Shared/CharShadow.hlsl"
#include "Shared/CharMotionVectors.hlsl"
#include "CharBodyMaterials.hlsl"

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_LightMap); SAMPLER(sampler_LightMap);
TEXTURE2D(_RampMapCool); SAMPLER(sampler_RampMapCool);
TEXTURE2D(_RampMapWarm); SAMPLER(sampler_RampMapWarm);
TEXTURE2D(_StockingsMap); SAMPLER(sampler_StockingsMap);

CBUFFER_START(UnityPerMaterial)
    float _ModelScale;
    float _AlphaTestThreshold;
    float _SingleMaterialID;

    float4 _Color;
    float4 _BackColor;
    float4 _Maps_ST;

    float _RampCoolWarmLerpFactor;

    CHAR_MAT_PROP(float4, _SpecularColor);
    CHAR_MAT_PROP(float, _SpecularMetallic);
    CHAR_MAT_PROP(float, _SpecularShininess);
    CHAR_MAT_PROP(float, _SpecularIntensity);
    CHAR_MAT_PROP(float, _SpecularEdgeSoftness);

    float4 _StockingsMap_ST;
    float4 _StockingsColor;
    float4 _StockingsColorDark;
    float _StockingsDarkWidth;
    float _StockingsPower;
    float _StockingsLightedWidth;
    float _StockingsLightedIntensity;
    float _StockingsRoughness;

    float4 _EmissionColor;
    float _EmissionThreshold;
    float _EmissionIntensity;

    CHAR_MAT_PROP(float, _mBloomIntensity);
    CHAR_MAT_PROP(float4, _BloomColor);

    float _RimIntensity;
    float _RimIntensityBackFace;
    float _RimThresholdMin;
    float _RimThresholdMax;
    float _RimEdgeSoftness;
    CHAR_MAT_PROP(float, _RimWidth);
    CHAR_MAT_PROP(float4, _RimColor);
    CHAR_MAT_PROP(float, _RimDark);

    float _OutlineWidth;
    float _OutlineZOffset;
    CHAR_MAT_PROP(float4, _OutlineColor);

    float _DitherAlpha;
CBUFFER_END

void ApplyStockings(inout float3 baseColor, float2 uv, float NoV)
{
    // * Modified from °Nya°222's blender shader.

    float4 stockingsMap = SAMPLE_TEXTURE2D(_StockingsMap, sampler_StockingsMap, uv);
    stockingsMap.b = SAMPLE_TEXTURE2D(_StockingsMap, sampler_StockingsMap, TRANSFORM_TEX(uv, _StockingsMap)).b;

    NoV = saturate(NoV);

    float power = max(0.04, _StockingsPower);
    float darkWidth = max(0, _StockingsDarkWidth * power);

    float darkIntensity = (NoV - power) / (darkWidth - power);
    darkIntensity = saturate(darkIntensity * (1 - _StockingsLightedIntensity)) * stockingsMap.r;

    float3 darkColor = lerp(1, _StockingsColorDark.rgb, darkIntensity);
    darkColor = lerp(1, darkColor * baseColor, darkIntensity) * baseColor;

    float lightIntensity = lerp(0.5, 1, stockingsMap.b * _StockingsRoughness); // 映射到 0.5 - 1，太黑不好看
    lightIntensity *= stockingsMap.g;
    lightIntensity *= _StockingsLightedIntensity;
    lightIntensity *= max(0.004, pow(NoV, _StockingsLightedWidth));

    float3 stockings = lightIntensity * (darkColor + _StockingsColor.rgb) + darkColor;
    baseColor = lerp(baseColor, stockings, step(0.01, stockingsMap.r));
}

void ApplyDebugSettings(float4 lightMap, inout float4 colorTarget, inout float4 bloomTarget)
{
    #if _SINGLEMATERIAL_ON
        if (abs(floor(8 * lightMap.a) - _SingleMaterialID) > 0.01)
        {
            colorTarget.rgb = 0;
            bloomTarget = 0; // intensity
        }
    #endif
}

CharCoreVaryings BodyVertex(CharCoreAttributes i)
{
    return CharCoreVertex(i, _Maps_ST);
}

void BodyColorFragment(
    CharCoreVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC,
    out float4 colorTarget      : SV_Target0,
    out float4 bloomTarget      : SV_Target1)
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    SELECT_CHAR_MAT_PROPS_10(lightMap,
        float4, specularColor        = _SpecularColor,
        float , specularMetallic     = _SpecularMetallic,
        float , specularShininess    = _SpecularShininess,
        float , specularIntensity    = _SpecularIntensity,
        float , specularEdgeSoftness = _SpecularEdgeSoftness,
        float , rimWidth             = _RimWidth,
        float4, rimColor             = _RimColor,
        float , rimDark              = _RimDark,
        float , bloomIntensity       = _mBloomIntensity,
        float4, bloomColor           = _BloomColor
    );

    Light light = GetCharacterMainLight(i.shadowCoord);
    Directions dirWS = GetWorldSpaceDirections(light, i.positionWS, i.normalWS);

    ApplyStockings(texColor.rgb, i.uv.xy, dirWS.NoV);

    DiffuseData diffuseData;
    diffuseData.NoL = dirWS.NoL;
    diffuseData.singleMaterial = false;
    diffuseData.rampCoolOrWarm = _RampCoolWarmLerpFactor;

    SpecularData specularData;
    specularData.color = specularColor.rgb;
    specularData.NoH = dirWS.NoH;
    specularData.shininess = specularShininess;
    specularData.edgeSoftness = specularEdgeSoftness;
    specularData.intensity = specularIntensity;
    specularData.metallic = specularMetallic;

    RimLightData rimLightData;
    rimLightData.color = rimColor.rgb;
    rimLightData.width = rimWidth;
    rimLightData.edgeSoftness = _RimEdgeSoftness;
    rimLightData.thresholdMin = _RimThresholdMin;
    rimLightData.thresholdMax = _RimThresholdMax;
    rimLightData.darkenValue = rimDark;
    rimLightData.intensityFrontFace = _RimIntensity;
    rimLightData.intensityBackFace = _RimIntensityBackFace;
    rimLightData.modelScale = _ModelScale;
    rimLightData.ditherAlpha = _DitherAlpha;

    EmissionData emissionData;
    emissionData.color = _EmissionColor.rgb;
    emissionData.value = texColor.a;
    emissionData.threshold = _EmissionThreshold;
    emissionData.intensity = _EmissionIntensity;

    float3 diffuse = GetRampDiffuse(diffuseData, i.color, texColor.rgb, light.color, lightMap,
        TEXTURE2D_ARGS(_RampMapCool, sampler_RampMapCool), TEXTURE2D_ARGS(_RampMapWarm, sampler_RampMapWarm),
        light.shadowAttenuation);
    float3 specular = GetSpecular(specularData, texColor.rgb, light.color, lightMap, light.shadowAttenuation);
    float3 rimLight = GetRimLight(rimLightData, i.positionHCS, dirWS.N, isFrontFace, lightMap);
    float3 emission = GetEmission(emissionData, texColor.rgb);

    float3 diffuseAdd = 0;
    float3 specularAdd = 0;

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light lightAdd = GetAdditionalLight(lightIndex, i.positionWS);
            Directions dirWSAdd = GetWorldSpaceDirections(lightAdd, i.positionWS, i.normalWS);
            float attenuationAdd = saturate(lightAdd.distanceAttenuation);

            diffuseAdd += texColor.rgb * lightAdd.color * attenuationAdd;

            SpecularData specularDataAdd;
            specularDataAdd.color = specularColor.rgb;
            specularDataAdd.NoH = dirWSAdd.NoH;
            specularDataAdd.shininess = specularShininess;
            specularDataAdd.edgeSoftness = specularEdgeSoftness;
            specularDataAdd.intensity = specularIntensity;
            specularDataAdd.metallic = specularMetallic;
            specularAdd += GetSpecular(specularDataAdd, texColor.rgb, lightAdd.color, lightMap, 1) * attenuationAdd;
        LIGHT_LOOP_END
    #endif

    // Output
    colorTarget = float4(CombineColorPreserveLuminance(diffuse, diffuseAdd) + specular + specularAdd + rimLight + emission, texColor.a);
    bloomTarget = EncodeBloomColor(bloomColor.rgb, bloomIntensity);

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

    // Debug
    ApplyDebugSettings(lightMap, colorTarget, bloomTarget);
}

CharOutlineVaryings BodyOutlineVertex(CharOutlineAttributes i)
{
    VertexPositionInputs vertexInputs = GetVertexPositionInputs(i.positionOS);

    OutlineData outlineData;
    outlineData.modelScale = _ModelScale;
    outlineData.width = _OutlineWidth;
    outlineData.zOffset = _OutlineZOffset;

    return CharOutlineVertex(outlineData, i, vertexInputs, _Maps_ST);
}

void BodyOutlineFragment(
    CharOutlineVaryings i,
    out float4 colorTarget : SV_TARGET0)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    SELECT_CHAR_MAT_PROPS_1(lightMap,
        float4, outlineColor = _OutlineColor
    );

    colorTarget = float4(outlineColor.rgb, 1);
    float4 bloomTarget = 0;

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

    // Debug
    ApplyDebugSettings(lightMap, colorTarget, bloomTarget);
}

CharShadowVaryings BodyShadowVertex(CharShadowAttributes i)
{
    return CharShadowVertex(i, _Maps_ST);
}

void BodyShadowFragment(
    CharShadowVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC)
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
}

CharDepthOnlyVaryings BodyDepthOnlyVertex(CharDepthOnlyAttributes i)
{
    return CharDepthOnlyVertex(i, _Maps_ST);
}

float4 BodyDepthOnlyFragment(
    CharDepthOnlyVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthOnlyFragment(i);
}

CharDepthNormalsVaryings BodyDepthNormalsVertex(CharDepthNormalsAttributes i)
{
    return CharDepthNormalsVertex(i, _Maps_ST);
}

float4 BodyDepthNormalsFragment(
    CharDepthNormalsVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthNormalsFragment(i);
}

CharMotionVectorsVaryings BodyMotionVectorsVertex(CharMotionVectorsAttributes i)
{
    return CharMotionVectorsVertex(i, _Maps_ST);
}

half4 BodyMotionVectorsFragment(
    CharMotionVectorsVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharMotionVectorsFragment(i);
}

#endif
