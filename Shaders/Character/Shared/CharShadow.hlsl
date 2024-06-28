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

#ifndef _CHAR_SHADOW_INCLUDED
#define _CHAR_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "CharRenderingHelpers.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct CharShadowAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv1          : TEXCOORD0;
    float2 uv2          : TEXCOORD1;
};

struct CharShadowVaryings
{
    float4 positionHCS  : SV_POSITION;
    float3 normalWS     : NORMAL;
    float4 uv           : TEXCOORD0;
};

float3 ApplySelfShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection, float2 selfShadowBias)
{
    float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    float scale = invNdotL * selfShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = lightDirection * selfShadowBias.xxx + positionWS;
    positionWS = normalWS * scale.xxx + positionWS;
    return positionWS;
}

float4 GetShadowPositionHClip(float3 positionOS, float3 normalWS, float2 selfShadowBias)
{
    float3 positionWS = TransformObjectToWorld(positionOS);

#if !_CASTING_SELF_SHADOW && _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

#if _CASTING_SELF_SHADOW
    float4 positionCS = TransformWorldToHClip(ApplySelfShadowBias(positionWS, normalWS, lightDirectionWS, selfShadowBias));
#else
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#endif

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

CharShadowVaryings CharShadowVertex(CharShadowAttributes i, float4 mapST, float selfShadowDepthBias, float selfShadowNormalBias)
{
    float2 selfShadowBias = float2(selfShadowDepthBias, selfShadowNormalBias);

    CharShadowVaryings o;
    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
    o.positionHCS = GetShadowPositionHClip(i.positionOS.xyz, o.normalWS, selfShadowBias);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    return o;
}

#endif
