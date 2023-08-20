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

#ifndef _CHARACTER_OUTLINE_INCLUDED
#define _CHARACTER_OUTLINE_INCLUDED

#include "CharacterDualFace.hlsl"
#include "CharacterUtils.hlsl"

struct CharacterOutlineAttributes
{
    float3 positionOS     : POSITION;

#if defined(_OUTLINENORMAL_NORMAL)
    float3 normalOS       : NORMAL;
#endif

#if defined(_OUTLINENORMAL_TANGENT)
    float4 tangentOS      : TANGENT;
#endif

    float4 color          : COLOR;
    float2 uv1            : TEXCOORD0;
    float2 uv2            : TEXCOORD1;
};

struct CharacterOutlineVaryings
{
    float4 positionHCS    : SV_POSITION;
    float4 uv             : TEXCOORD0;
};

void FixOutlineWidth(float3 positionVS, float4 vertexColor, float modelScale, inout float outlineWidth)
{
    outlineWidth *= modelScale;
    outlineWidth *= 1.0 / 17.0; // magic number

    // 游戏模型有顶点描边宽度
    #if defined(_MODEL_GAME)
        outlineWidth *= vertexColor.a;
    #else
        outlineWidth *= 0.5;
    #endif

    // unity_CameraProjection._m11: cot(FOV / 2)
    // 2.414 是 FOV 为 45 度时的值
    float fixScale = 2.414 / unity_CameraProjection._m11; // FOV 越小，角色越大，描边越细（在屏幕上看上去一致）
    fixScale *= (-positionVS.z / modelScale) / 40.0; // 近小远大
    outlineWidth *= clamp(fixScale, 0.04, 0.1);
}

float4 ApplyOutline(
    float3 positionVS,
    float3 normalVS,
    float4 vertexColor,
    float modelScale,
    float outlineWidth,
    float outlineZOffset)
{
    FixOutlineWidth(positionVS, vertexColor, modelScale, outlineWidth);

    normalVS.z = -0.1; // 向后拍扁
    positionVS += normalize(normalVS) * outlineWidth;
    positionVS.z += outlineZOffset * modelScale; // 用于隐藏面片的描边
    return TransformWViewToHClip(positionVS);
}

float4 ApplyFaceOutline(
    float3 positionWS,
    float3 positionVS,
    float3 normalVS,
    float4 vertexColor,
    float4 mmdHeadBoneForward,
    float modelScale,
    float outlineWidth,
    float outlineZOffset)
{
    FixOutlineWidth(positionVS, vertexColor, modelScale, outlineWidth);

    // 当嘴从侧面看在脸外面时再启用描边
    #if defined(_MODEL_GAME)
        float3 F = GetCharacterHeadBoneForwardWS(mmdHeadBoneForward);
        float3 V = normalize(GetWorldSpaceViewDir(positionWS));
        float FdotV = pow(max(0, dot(F, V)), 0.8);
        outlineWidth *= smoothstep(-0.05, 0, 1 - FdotV - vertexColor.b);
    #endif

    // TODO: Fix 脸颊的描边。大概是用 vertexColor.g

    normalVS.z = -0.1; // 向后拍扁
    positionVS += normalize(normalVS) * outlineWidth;
    positionVS.z += outlineZOffset * modelScale; // 用于隐藏面片的描边
    return TransformWViewToHClip(positionVS);
}

CharacterOutlineVaryings CharacterOutlineVertex(
    CharacterOutlineAttributes i,
    float4 mapST,
    float modelScale,
    float outlineWidth,
    float outlineZOffset)
{
    CharacterOutlineVaryings o;

    float3 normalOS = 0;

    #if defined(_OUTLINENORMAL_NORMAL)
        normalOS = i.normalOS;
    #elif defined(_OUTLINENORMAL_TANGENT)
        normalOS = i.tangentOS.xyz;
    #endif

    float3 positionWS = TransformObjectToWorld(i.positionOS);
    float3 positionVS = TransformWorldToView(positionWS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    float3 normalVS = TransformWorldToViewNormal(normalWS);

    o.positionHCS = ApplyOutline(positionVS, normalVS, i.color, modelScale, outlineWidth, outlineZOffset);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    return o;
}

CharacterOutlineVaryings CharacterFaceOutlineVertex(
    CharacterOutlineAttributes i,
    float4 mapST,
    float4 mmdHeadBoneForward,
    float modelScale,
    float outlineWidth,
    float outlineZOffset)
{
    CharacterOutlineVaryings o;

    float3 normalOS = 0;

    #if defined(_OUTLINENORMAL_NORMAL)
        normalOS = i.normalOS;
    #elif defined(_OUTLINENORMAL_TANGENT)
        normalOS = i.tangentOS.xyz;
    #endif

    float3 positionWS = TransformObjectToWorld(i.positionOS);
    float3 positionVS = TransformWorldToView(positionWS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    float3 normalVS = TransformWorldToViewNormal(normalWS);

    o.positionHCS = ApplyFaceOutline(positionWS, positionVS, normalVS, i.color, mmdHeadBoneForward, modelScale, outlineWidth, outlineZOffset);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    return o;
}

#endif
