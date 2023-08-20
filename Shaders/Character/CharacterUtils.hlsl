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

#ifndef _CHARACTER_UTILS_INCLUDED
#define _CHARACTER_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float3 GetCharacterHeadBoneForwardWS(float4 mmdHeadBoneForward)
{
    #if defined(_MODEL_GAME)
        // 游戏模型的头骨骼是旋转过的
        // +Y 是 Forward
        return normalize(UNITY_MATRIX_M._m01_m11_m21);
    #elif defined(_MODEL_MMD)
        // MMD 模型只有一个根骨骼上的 Renderer，头骨骼信息需要额外获取
        return mmdHeadBoneForward.xyz;
    #endif

    // 一般情况下是 +Z
    return normalize(UNITY_MATRIX_M._m02_m12_m22);
}

float3 GetCharacterHeadBoneUpWS(float4 mmdHeadBoneUp)
{
    #if defined(_MODEL_GAME)
        // 游戏模型的头骨骼是旋转过的
        // -X 是 Up
        return normalize(-UNITY_MATRIX_M._m00_m10_m20);
    #elif defined(_MODEL_MMD)
        // MMD 模型只有一个根骨骼上的 Renderer，头骨骼信息需要额外获取
        return mmdHeadBoneUp.xyz;
    #endif

    // 一般情况下是 +Y
    return normalize(UNITY_MATRIX_M._m01_m11_m21);
}

float3 GetCharacterHeadBoneRightWS(float4 mmdHeadBoneRight)
{
    #if defined(_MODEL_GAME)
        // 游戏模型的头骨骼是旋转过的
        // -Z 是 Right
        return normalize(-UNITY_MATRIX_M._m02_m12_m22);
    #elif defined(_MODEL_MMD)
        // MMD 模型只有一个根骨骼上的 Renderer，头骨骼信息需要额外获取
        return mmdHeadBoneRight.xyz;
    #endif

    // 一般情况下是 +X
    return normalize(UNITY_MATRIX_M._m00_m10_m20);
}

#endif
