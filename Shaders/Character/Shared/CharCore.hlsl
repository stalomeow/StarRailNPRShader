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

#ifndef _CHAR_CORE_INCLUDED
#define _CHAR_CORE_INCLUDED

// ==========================
// Stencil 使用低三位，高 <- 低
// 1         1         1
// 透明物体   眼睛       角色
// ==========================

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "CharRenderingHelpers.hlsl"

struct CharCoreAttributes
{
    float3 positionOS     : POSITION;
    float3 normalOS       : NORMAL;
    float4 color          : COLOR;
    float2 uv1            : TEXCOORD0;
    float2 uv2            : TEXCOORD1;
};

struct CharCoreVaryings
{
    float4 positionHCS    : SV_POSITION;
    float3 normalWS       : NORMAL;
    float4 color          : COLOR;
    float4 uv             : TEXCOORD0;
    float3 positionWS     : TEXCOORD1;
    float4 shadowCoord    : TEXCOORD2;
    real   fogFactor      : TEXCOORD3;
};

CharCoreVaryings CharCoreVertex(CharCoreAttributes i, float4 mapST)
{
    CharCoreVaryings o;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(i.positionOS);

    o.positionHCS = positionInputs.positionCS;
    o.normalWS = TransformObjectToWorldNormal(i.normalOS, true);
    o.color = i.color;
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    o.positionWS = positionInputs.positionWS;
    o.shadowCoord = GetShadowCoord(positionInputs);

    #if defined(_FOG_FRAGMENT)
        o.fogFactor = 0.0;
    #else
        o.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
    #endif

    return o;
}

#endif
