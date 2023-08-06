#ifndef _CHARACTER_UTILS_INCLUDED
#define _CHARACTER_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float3 GetCharacterHeadBoneForwardWS()
{
    // 游戏模型的头骨骼是旋转过的
    // +Y 是 Forward
    #if defined(_MODEL_GAME)
        return normalize(UNITY_MATRIX_M._m01_m11_m21);
    #endif

    // 一般情况下是 +Z
    return normalize(UNITY_MATRIX_M._m02_m12_m22);
}

float3 GetCharacterHeadBoneUpWS()
{
    // 游戏模型的头骨骼是旋转过的
    // -X 是 Up
    #if defined(_MODEL_GAME)
        return normalize(-UNITY_MATRIX_M._m00_m10_m20);
    #endif

    // 一般情况下是 +Y
    return normalize(UNITY_MATRIX_M._m01_m11_m21);
}

float3 GetCharacterHeadBoneRightWS()
{
    // 游戏模型的头骨骼是旋转过的
    // -Z 是 Right
    #if defined(_MODEL_GAME)
        return normalize(-UNITY_MATRIX_M._m02_m12_m22);
    #endif

    // 一般情况下是 +X
    return normalize(UNITY_MATRIX_M._m00_m10_m20);
}

#endif
