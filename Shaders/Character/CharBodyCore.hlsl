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

#if !defined(CHAR_BODY_SHADER_TRANSPARENT)
    TEXTURE2D(_StockingsMap); SAMPLER(sampler_StockingsMap);
#endif

CBUFFER_START(UnityPerMaterial)
    float _ModelScale;

#if !defined(CHAR_BODY_SHADER_TRANSPARENT)
    float _AlphaTestThreshold;
#endif

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

#if !defined(CHAR_BODY_SHADER_TRANSPARENT)
    float4 _StockingsMap_ST;
    float4 _StockingsColor;
    float4 _StockingsColorDark;
    float _StockingsDarkWidth;
    float _StockingsPower;
    float _StockingsLightedWidth;
    float _StockingsLightedIntensity;
    float _StockingsRoughness;
#endif

    float4 _EmissionColor;
    float _EmissionThreshold;
    float _EmissionIntensity;

    CHAR_MAT_PROP(float, _mmBloomIntensity);
    CHAR_MAT_PROP(float4, _BloomColor);

#if !defined(CHAR_BODY_SHADER_TRANSPARENT)
    float _RimIntensity;
    float _RimIntensityAdditionalLight;
    float _RimIntensityBackFace;
    float _RimIntensityBackFaceAdditionalLight;
    float _RimThresholdMin;
    float _RimThresholdMax;
    CHAR_MAT_PROP(float, _RimWidth);
    CHAR_MAT_PROP(float4, _RimColor);
    CHAR_MAT_PROP(float, _RimDark);
    CHAR_MAT_PROP(float, _RimEdgeSoftness);
#endif

    float _OutlineWidth;
    float _OutlineZOffset;
    CHAR_MAT_PROP(float4, _OutlineColor);

    float _DitherAlpha;
CBUFFER_END

void ApplyStockings(inout float3 baseColor, float2 uv, float NoV)
{
    // * Modified from °Nya°222's blender shader.

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
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
    #endif
}

void ApplyDebugSettings(float4 lightMap, inout float4 colorTarget)
{
    #if _SINGLEMATERIAL_ON
        if (abs(floor(8 * lightMap.a) - _SingleMaterialID) > 0.01)
        {
            colorTarget.rgb = 0;
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
    out float4 colorTarget      : SV_Target0)
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    SELECT_CHAR_MAT_PROPS_7(lightMap,
        float4, specularColor        = _SpecularColor,
        float , specularMetallic     = _SpecularMetallic,
        float , specularShininess    = _SpecularShininess,
        float , specularIntensity    = _SpecularIntensity,
        float , specularEdgeSoftness = _SpecularEdgeSoftness,
        float , bloomIntensity       = _mmBloomIntensity,
        float4, bloomColor           = _BloomColor
    );

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        SELECT_CHAR_MAT_PROPS_4(lightMap,
            float , rimWidth             = _RimWidth,
            float4, rimColor             = _RimColor,
            float , rimDark              = _RimDark,
            float , rimEdgeSoftness      = _RimEdgeSoftness
        );
    #endif

    Light light = GetCharacterMainLight(i.shadowCoord, i.positionWS);
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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        RimLightMaskData rimLightMaskData;
        rimLightMaskData.color = rimColor.rgb;
        rimLightMaskData.width = rimWidth;
        rimLightMaskData.edgeSoftness = rimEdgeSoftness;
        rimLightMaskData.thresholdMin = _RimThresholdMin;
        rimLightMaskData.thresholdMax = _RimThresholdMax;
        rimLightMaskData.modelScale = _ModelScale;
        rimLightMaskData.ditherAlpha = _DitherAlpha;
        rimLightMaskData.NoV = dirWS.NoV;
    #endif

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        RimLightData rimLightData;
        rimLightData.darkenValue = rimDark;
        rimLightData.intensityFrontFace = _RimIntensity;
        rimLightData.intensityBackFace = _RimIntensityBackFace;
    #endif

    EmissionData emissionData;
    emissionData.color = _EmissionColor.rgb;
    emissionData.value = texColor.a;
    emissionData.threshold = _EmissionThreshold;
    emissionData.intensity = _EmissionIntensity;

    float3 diffuse = GetRampDiffuse(diffuseData, light, i.color, texColor.rgb, lightMap,
        TEXTURE2D_ARGS(_RampMapCool, sampler_RampMapCool), TEXTURE2D_ARGS(_RampMapWarm, sampler_RampMapWarm));
    float3 specular = GetSpecular(specularData, light, texColor.rgb, lightMap);

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        float3 rimLightMask = GetRimLightMask(rimLightMaskData, i.positionHCS, dirWS.N, lightMap);
        float3 rimLight = GetRimLight(rimLightData, rimLightMask, dirWS.NoL, light, isFrontFace);
    #else
        float3 rimLight = 0;
    #endif
    float3 emission = GetEmission(emissionData, texColor.rgb);

    #if defined(_ADDITIONAL_LIGHTS)
        CHAR_LIGHT_LOOP_BEGIN(i.positionWS, i.positionHCS)
            Light lightAdd = GetCharacterAdditionalLight(lightIndex, i.positionWS);
            Directions dirWSAdd = GetWorldSpaceDirections(lightAdd, i.positionWS, i.normalWS);

            diffuse = CombineColorPreserveLuminance(diffuse, GetAdditionalLightDiffuse(texColor.rgb, lightAdd));

            SpecularData specularDataAdd;
            specularDataAdd.color = specularColor.rgb;
            specularDataAdd.NoH = dirWSAdd.NoH;
            specularDataAdd.shininess = specularShininess;
            specularDataAdd.edgeSoftness = specularEdgeSoftness;
            specularDataAdd.intensity = specularIntensity;
            specularDataAdd.metallic = specularMetallic;
            specular += GetSpecular(specularDataAdd, lightAdd, texColor.rgb, lightMap);

            #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
                RimLightData rimLightDataAdd;
                rimLightDataAdd.darkenValue = 0;
                rimLightDataAdd.intensityFrontFace = _RimIntensityAdditionalLight;
                rimLightDataAdd.intensityBackFace = _RimIntensityBackFaceAdditionalLight;
                rimLight += GetRimLight(rimLightDataAdd, rimLightMask, dirWSAdd.NoL, lightAdd, isFrontFace);
            #endif
        CHAR_LIGHT_LOOP_END
    #endif

    // Output
    colorTarget = float4(diffuse + specular + rimLight + emission, texColor.a);
    colorTarget.rgb = MixBloomColor(colorTarget.rgb, bloomColor.rgb, bloomIntensity);

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

    // Debug
    ApplyDebugSettings(lightMap, colorTarget);
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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    SELECT_CHAR_MAT_PROPS_1(lightMap,
        float4, outlineColor = _OutlineColor
    );

    colorTarget = float4(outlineColor.rgb, 1);

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

    // Debug
    ApplyDebugSettings(lightMap, colorTarget);
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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

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

    #if !defined(CHAR_BODY_SHADER_TRANSPARENT)
        DoAlphaClip(texColor.a, _AlphaTestThreshold);
    #endif

    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharMotionVectorsFragment(i);
}

#endif
