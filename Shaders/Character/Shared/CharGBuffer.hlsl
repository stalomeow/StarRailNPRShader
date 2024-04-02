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

#ifndef _CHAR_G_BUFFER_INCLUDED
#define _CHAR_G_BUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "CharRenderingHelpers.hlsl"

struct CharGBufferAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv1          : TEXCOORD0;
    float2 uv2          : TEXCOORD1;
};

struct CharGBufferVaryings
{
    float4 positionHCS  : SV_POSITION;
    float3 normalWS     : NORMAL;
    float4 uv           : TEXCOORD0;
};

CharGBufferVaryings CharGBufferVertex(CharGBufferAttributes i, float4 mapST)
{
    CharGBufferVaryings o;

    o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);

    return o;
}

FragmentOutput CharGBufferFragment(CharGBufferVaryings i)
{
    half3 packedNormalWS = PackNormal(i.normalWS);

    FragmentOutput output = (FragmentOutput)0;
    output.GBuffer2 = half4(packedNormalWS, 0);

    #if _RENDER_PASS_ENABLED
        output.GBuffer4 = inputData.positionCS.z;
    #endif

    return output;
}

#endif
