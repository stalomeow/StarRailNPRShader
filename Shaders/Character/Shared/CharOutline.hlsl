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

#ifndef _CHAR_OUTLINE_INCLUDED
#define _CHAR_OUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "CharRenderingHelpers.hlsl"

struct CharOutlineAttributes
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

struct CharOutlineVaryings
{
    float4 positionHCS    : SV_POSITION;
    float4 uv             : TEXCOORD0;
    float3 positionWS     : TEXCOORD1;
    real   fogFactor      : TEXCOORD2;
};

struct OutlineData
{
    float modelScale;
    float width;
    float zOffset;
};

float3 GetOutlinePositionVS(OutlineData data, float3 positionVS, float3 normalVS, float4 vertexColor)
{
    float outlineWidth = data.width * data.modelScale * 0.0588;

    // 游戏模型有顶点描边宽度
    #if defined(_MODEL_GAME)
        outlineWidth *= vertexColor.a;
    #else
        outlineWidth *= 0.5;
    #endif

    float fixScale;
    if (IsPerspectiveProjection())
    {
        // unity_CameraProjection._m11: cot(FOV / 2)
        // 2.414 是 FOV 为 45 度时的值
        fixScale = 2.414 / unity_CameraProjection._m11; // FOV 越小，角色越大，描边越细（在屏幕上看上去一致）
    }
    else
    {
        // unity_CameraProjection._m11: (1 / Size)
        // 1.5996 纯 Magic Number
        fixScale = 1.5996 / unity_CameraProjection._m11; // Size 越小，角色越大，描边越细（在屏幕上看上去一致）
    }
    fixScale *= -positionVS.z / data.modelScale; // 近小远大
    outlineWidth *= clamp(fixScale * 0.025, 0.04, 0.1);

    normalVS.z = -0.1; // 向后拍扁
    positionVS += normalize(normalVS) * outlineWidth;
    positionVS.z += data.zOffset * data.modelScale; // 用于隐藏面片的描边
    return positionVS;
}

CharOutlineVaryings CharOutlineVertex(
    OutlineData data,
    CharOutlineAttributes i,
    VertexPositionInputs vertexInputs,
    float4 mapST)
{
    CharOutlineVaryings o;
    float3 normalOS = 0;

    #if defined(_OUTLINENORMAL_NORMAL)
        normalOS = i.normalOS;
    #elif defined(_OUTLINENORMAL_TANGENT)
        normalOS = i.tangentOS.xyz;
    #endif

    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    float3 normalVS = TransformWorldToViewNormal(normalWS);
    float3 positionVS = GetOutlinePositionVS(data, vertexInputs.positionVS, normalVS, i.color);

    o.positionHCS = TransformWViewToHClip(positionVS);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    o.positionWS = TransformViewToWorld(positionVS);

    #if defined(_FOG_FRAGMENT)
        o.fogFactor = 0.0;
    #else
        o.fogFactor = ComputeFogFactor(o.positionHCS.z);
    #endif

    return o;
}

#endif
