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

TEXTURE2D_SHADOW(_PerObjShadowMap);
SAMPLER_CMP(sampler_PerObjShadowMap);

int _PerObjShadowCount;
float4x4 _PerObjShadowMatrices[MAX_PER_OBJECT_SHADOW_COUNT];
float4 _PerObjShadowMapRects[MAX_PER_OBJECT_SHADOW_COUNT];
float _PerObjShadowCasterIds[MAX_PER_OBJECT_SHADOW_COUNT];

float4 _PerObjShadowOffset0;
float4 _PerObjShadowOffset1;
float4 _PerObjShadowMapSize;

float4 TransformWorldToPerObjectShadowCoord(int index, float3 positionWS)
{
    return mul(_PerObjShadowMatrices[index], float4(positionWS, 1));
}

float PerObjectShadow(int index, float4 shadowCoord, ShadowSamplingData shadowSamplingData, half4 shadowParams)
{
    float4 rects = _PerObjShadowMapRects[index];
    if (shadowCoord.x < rects.x || shadowCoord.x > rects.y || shadowCoord.y < rects.z || shadowCoord.y > rects.w)
    {
        return 1; // 超出阴影图范围，当作没有阴影
    }

    return SampleShadowmap(TEXTURE2D_SHADOW_ARGS(_PerObjShadowMap, sampler_PerObjShadowMap),
            shadowCoord, shadowSamplingData, shadowParams, false);
}

ShadowSamplingData GetMainLightPerObjectShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;

    // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
    shadowSamplingData.shadowOffset0 = _PerObjShadowOffset0;
    shadowSamplingData.shadowOffset1 = _PerObjShadowOffset1;

    // shadowmapSize is used in SampleShadowmapFiltered otherwise
    shadowSamplingData.shadowmapSize = _PerObjShadowMapSize;
    shadowSamplingData.softShadowQuality = _MainLightShadowParams.y;

    return shadowSamplingData;
}

float MainLightPerObjectShadow(float3 positionWS, float casterId)
{
    ShadowSamplingData shadowSamplingData = GetMainLightPerObjectShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();

    for (int i = 0; i < _PerObjShadowCount; i++)
    {
        if (abs(_PerObjShadowCasterIds[i] - casterId) <= 0.001)
        {
            float4 shadowCoord = TransformWorldToPerObjectShadowCoord(i, positionWS);
            return PerObjectShadow(i, shadowCoord, shadowSamplingData, shadowParams);
        }
    }

    return 1;
}

float MainLightPerObjectShadow(float3 positionWS)
{
    ShadowSamplingData shadowSamplingData = GetMainLightPerObjectShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();
    float shadow = 1;

    for (int i = 0; i < _PerObjShadowCount; i++)
    {
        float4 shadowCoord = TransformWorldToPerObjectShadowCoord(i, positionWS);
        shadow = min(shadow, PerObjectShadow(i, shadowCoord, shadowSamplingData, shadowParams));
    }

    return shadow;
}

#endif
