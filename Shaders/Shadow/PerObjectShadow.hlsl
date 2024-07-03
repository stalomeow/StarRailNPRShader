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

#ifndef _PER_OBJECT_SHADOW_INCLUDED
#define _PER_OBJECT_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#define MAX_PER_OBJECT_SHADOW_COUNT 16

float4 TransformWorldToPerObjectShadowCoord(float4x4 shadowMatrix, float3 positionWS)
{
    return mul(shadowMatrix, float4(positionWS, 1));
}

float PerObjectShadow(
    TEXTURE2D_SHADOW_PARAM(shadowMap, sampler_shadowMap),
    float4 shadowMapRects,
    float4 shadowCoord,
    ShadowSamplingData shadowSamplingData,
    half4 shadowParams,
    bool isPerspectiveProjection)
{
    if (shadowCoord.x < shadowMapRects.x ||
        shadowCoord.x > shadowMapRects.y ||
        shadowCoord.y < shadowMapRects.z ||
        shadowCoord.y > shadowMapRects.w)
    {
        return 1; // 超出阴影图范围，当作没有阴影
    }

    return SampleShadowmap(TEXTURE2D_SHADOW_ARGS(shadowMap, sampler_shadowMap),
            shadowCoord, shadowSamplingData, shadowParams, isPerspectiveProjection);
}

// ---------------------------------------------------------
// Scene
// ---------------------------------------------------------

TEXTURE2D_SHADOW(_PerObjSceneShadowMap);
SAMPLER_CMP(sampler_PerObjSceneShadowMap);

int _PerObjSceneShadowCount;
float4x4 _PerObjSceneShadowMatrices[MAX_PER_OBJECT_SHADOW_COUNT];
float4 _PerObjSceneShadowMapRects[MAX_PER_OBJECT_SHADOW_COUNT];
float _PerObjSceneShadowCasterIds[MAX_PER_OBJECT_SHADOW_COUNT];

float4 _PerObjSceneShadowOffset0;
float4 _PerObjSceneShadowOffset1;
float4 _PerObjSceneShadowMapSize;

ShadowSamplingData GetMainLightPerObjectSceneShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = _PerObjSceneShadowOffset0;
    shadowSamplingData.shadowOffset1 = _PerObjSceneShadowOffset1;

    // shadowmapSize is used in SampleShadowmapFiltered otherwise
    shadowSamplingData.shadowmapSize = _PerObjSceneShadowMapSize;
    shadowSamplingData.softShadowQuality = _MainLightShadowParams.y;

    return shadowSamplingData;
}

float MainLightPerObjectSceneShadow(float3 positionWS)
{
    ShadowSamplingData shadowSamplingData = GetMainLightPerObjectSceneShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();
    float shadow = 1;

    for (int i = 0; i < _PerObjSceneShadowCount; i++)
    {
        float4 shadowCoord = TransformWorldToPerObjectShadowCoord(_PerObjSceneShadowMatrices[i], positionWS);
        shadow = min(shadow, PerObjectShadow(TEXTURE2D_SHADOW_ARGS(_PerObjSceneShadowMap, sampler_PerObjSceneShadowMap),
            _PerObjSceneShadowMapRects[i], shadowCoord, shadowSamplingData, shadowParams, false));
    }

    return shadow;
}

// ---------------------------------------------------------
// Self
// ---------------------------------------------------------

TEXTURE2D_SHADOW(_PerObjSelfShadowMap);
SAMPLER_CMP(sampler_PerObjSelfShadowMap);

int _PerObjSelfShadowCount;
float4x4 _PerObjSelfShadowMatrices[MAX_PER_OBJECT_SHADOW_COUNT];
float4 _PerObjSelfShadowMapRects[MAX_PER_OBJECT_SHADOW_COUNT];
float _PerObjSelfShadowCasterIds[MAX_PER_OBJECT_SHADOW_COUNT];

float4 _PerObjSelfShadowOffset0;
float4 _PerObjSelfShadowOffset1;
float4 _PerObjSelfShadowMapSize;

ShadowSamplingData GetMainLightPerObjectSelfShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = _PerObjSelfShadowOffset0;
    shadowSamplingData.shadowOffset1 = _PerObjSelfShadowOffset1;

    // shadowmapSize is used in SampleShadowmapFiltered otherwise
    shadowSamplingData.shadowmapSize = _PerObjSelfShadowMapSize;
    shadowSamplingData.softShadowQuality = _MainLightShadowParams.y;

    return shadowSamplingData;
}

float MainLightPerObjectSelfShadow(float3 positionWS, float casterId)
{
    ShadowSamplingData shadowSamplingData = GetMainLightPerObjectSelfShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();

    for (int i = 0; i < _PerObjSelfShadowCount; i++)
    {
        if (abs(_PerObjSelfShadowCasterIds[i] - casterId) <= 0.001)
        {
            float4 shadowCoord = TransformWorldToPerObjectShadowCoord(_PerObjSelfShadowMatrices[i], positionWS);
            return PerObjectShadow(TEXTURE2D_SHADOW_ARGS(_PerObjSelfShadowMap, sampler_PerObjSelfShadowMap),
            _PerObjSelfShadowMapRects[i], shadowCoord, shadowSamplingData, shadowParams, false);
        }
    }

    return 1;
}

#endif
