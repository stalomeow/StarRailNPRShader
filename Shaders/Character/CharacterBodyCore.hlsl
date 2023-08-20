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

#ifndef _CHARACTER_BODY_CORE_INCLUDED
#define _CHARACTER_BODY_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "CharacterDualFace.hlsl"
#include "CharacterOutline.hlsl"
#include "CharacterCommon.hlsl"
#include "CharacterMaterials.hlsl"
#include "CharacterShadow.hlsl"
#include "CharacterDepthOnly.hlsl"

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

    CHARACTER_MATERIAL_PROPERTY(float4, _SpecularColor);
    CHARACTER_MATERIAL_PROPERTY(float, _SpecularMetallic);
    CHARACTER_MATERIAL_PROPERTY(float, _SpecularShininess);
    CHARACTER_MATERIAL_PROPERTY(float, _SpecularIntensity);
    CHARACTER_MATERIAL_PROPERTY(float, _SpecularEdgeSoftness);

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

    CHARACTER_MATERIAL_PROPERTY(float, _BloomIntensity);

    float _RimIntensity;
    float _RimIntensityBackFace;
    float _RimThresholdMin;
    float _RimThresholdMax;
    float _RimEdgeSoftness;
    CHARACTER_MATERIAL_PROPERTY(float, _RimWidth);
    CHARACTER_MATERIAL_PROPERTY(float4, _RimColor);
    CHARACTER_MATERIAL_PROPERTY(float, _RimDark);

    float _OutlineWidth;
    float _OutlineZOffset;
    CHARACTER_MATERIAL_PROPERTY(float4, _OutlineColor);

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
            bloomTarget.r = 0;
        }
    #endif
}

CharacterVaryings BodyVertex(CharacterAttributes i)
{
    return CharacterVertex(i, _Maps_ST);
}

void BodyColorFragment(
    CharacterVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC,
    out float4 colorTarget      : SV_Target0,
    out float4 bloomTarget      : SV_Target1)
{
    ValidateDualFaceVaryings(i.normalWS, i.uv, isFrontFace);

    // Textures
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    // Colors
    float3 baseColor = texColor.rgb;
    float alpha = texColor.a;

    DoAlphaClip(alpha, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    // HSR Material Properties
    INIT_CHARACTER_MATERIAL_PROPERTIES_9(lightMap,
        float4, specularColor        = _SpecularColor,
        float , specularMetallic     = _SpecularMetallic,
        float , specularShininess    = _SpecularShininess,
        float , specularIntensity    = _SpecularIntensity,
        float , specularEdgeSoftness = _SpecularEdgeSoftness,
        float , rimWidth             = _RimWidth,
        float4, rimColor             = _RimColor,
        float , rimDark              = _RimDark,
        float , bloomIntensity       = _BloomIntensity
    );

    // Calc
    Light light = GetMainLight();

    float3 N = normalize(i.normalWS);
    float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
    float3 L = normalize(light.direction);
    float3 H = normalize(V + L);

    float NoL = dot(N, L);
    float NoH = dot(N, H);
    float NoV = dot(N, V);

    ApplyStockings(baseColor, i.uv.xy, NoV);

    float3 diffuse = GetDiffuse(NoL, i.color, lightMap, false, baseColor, TEXTURE2D_ARGS(_RampMapCool, sampler_RampMapCool), TEXTURE2D_ARGS(_RampMapWarm, sampler_RampMapWarm), _RampCoolWarmLerpFactor);
    float3 specular = GetSpecular(NoH, lightMap, baseColor, specularColor.rgb, specularShininess, specularEdgeSoftness, specularIntensity, specularMetallic);
    float3 rimLight = GetRimLight(i.positionHCS, i.normalWS, lightMap, _ModelScale, rimColor.rgb, rimWidth, _RimEdgeSoftness, _RimThresholdMin, _RimThresholdMax, rimDark, _RimIntensity, _RimIntensityBackFace, isFrontFace);
    float3 emission = GetEmission(baseColor, alpha, _EmissionThreshold, _EmissionIntensity, _EmissionColor.rgb);

    // Output
    colorTarget = float4((diffuse + specular) * light.color + rimLight + emission, alpha);
    bloomTarget = float4(bloomIntensity, 0, 0, 0);
    ApplyDebugSettings(lightMap, colorTarget, bloomTarget);
}

CharacterOutlineVaryings BodyOutlineVertex(CharacterOutlineAttributes i)
{
    return CharacterOutlineVertex(i, _Maps_ST, _ModelScale, _OutlineWidth, _OutlineZOffset);
}

void BodyOutlineFragment(
    CharacterOutlineVaryings i,
    out float4 colorTarget : SV_TARGET0)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    INIT_CHARACTER_MATERIAL_PROPERTIES_1(lightMap,
        float4, outlineColor = _OutlineColor
    );

    colorTarget = float4(outlineColor.rgb, 1);
    float4 bloomTarget = 0;
    ApplyDebugSettings(lightMap, colorTarget, bloomTarget);
}

CharacterShadowVaryings BodyShadowVertex(CharacterShadowAttributes i)
{
    return CharacterShadowVertex(i, _Maps_ST);
}

void BodyShadowFragment(
    CharacterShadowVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC)
{
    ValidateDualFaceVaryings(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);
}

CharacterDepthOnlyVaryings BodyDepthOnlyVertex(CharacterDepthOnlyAttributes i)
{
    return CharacterDepthOnlyVertex(i, _Maps_ST);
}

float4 BodyDepthOnlyFragment(
    CharacterDepthOnlyVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    ValidateDualFaceVaryings(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharacterDepthOnlyFragment(i);
}

#endif
