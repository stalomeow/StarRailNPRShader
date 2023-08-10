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
