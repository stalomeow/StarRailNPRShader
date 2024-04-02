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

#ifndef _CHAR_MOTION_VECTORS_INCLUDED
#define _CHAR_MOTION_VECTORS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "CharRenderingHelpers.hlsl"

struct CharMotionVectorsAttributes
{
    float4 positionOS     : POSITION;
    float3 positionOSOld  : TEXCOORD4;
    float3 normalOS       : NORMAL;
    float2 uv1            : TEXCOORD0;
    float2 uv2            : TEXCOORD1;
};

struct CharMotionVectorsVaryings
{
    float4 positionHCS                 : SV_POSITION;
    float4 positionHCSNoJitter         : TEXCOORD0;
    float4 previousPositionHCSNoJitter : TEXCOORD1;
    float3 normalWS                    : NORMAL;
    float4 uv                          : TEXCOORD2;
};

CharMotionVectorsVaryings CharMotionVectorsVertex(CharMotionVectorsAttributes i, float4 mapST)
{
    CharMotionVectorsVaryings o;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

    // Jittered. Match the frame.
    o.positionHCS = vertexInput.positionCS;

    // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
    #if defined(UNITY_REVERSED_Z)
        o.positionHCS.z -= unity_MotionVectorsParams.z * o.positionHCS.w;
    #else
        o.positionHCS.z += unity_MotionVectorsParams.z * o.positionHCS.w;
    #endif

    o.positionHCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, i.positionOS));

    const float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(i.positionOSOld, 1) : i.positionOS;
    o.previousPositionHCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));

    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);

    return o;
}

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
    // since uv remap functions use floats
    #define POS_NDC_TYPE float2
#else
    #define POS_NDC_TYPE half2
#endif

half4 CharMotionVectorsFragment(CharMotionVectorsVaryings i)
{
    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        return half4(0.0, 0.0, 0.0, 0.0);
    }

    // Calculate positions
    float4 posCS = i.positionHCSNoJitter;
    float4 prevPosCS = i.previousPositionHCSNoJitter;

    POS_NDC_TYPE posNDC = posCS.xy * rcp(posCS.w);
    POS_NDC_TYPE prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

    half2 velocity;
    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        // Convert velocity from NDC space (-1..1) to screen UV 0..1 space since FoveatedRendering remap needs that range.
        half2 posUV = RemapFoveatedRenderingLinearToNonUniform(posNDC * 0.5 + 0.5);
        half2 prevPosUV = RemapFoveatedRenderingPrevFrameLinearToNonUniform(prevPosNDC * 0.5 + 0.5);

        // Calculate forward velocity
        velocity = (posUV - prevPosUV);
        #if UNITY_UV_STARTS_AT_TOP
            velocity.y = -velocity.y;
        #endif
    }
    else
    #endif
    {
        // Calculate forward velocity
        velocity = (posNDC.xy - prevPosNDC.xy);
        #if UNITY_UV_STARTS_AT_TOP
            velocity.y = -velocity.y;
        #endif

        // Convert velocity from NDC space (-1..1) to UV 0..1 space
        // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
        // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
        velocity.xy *= 0.5;
    }

    return half4(velocity, 0, 0);
}

#endif
