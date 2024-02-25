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

#ifndef _CHAR_RENDERING_HELPERS_INCLUDED
#define _CHAR_RENDERING_HELPERS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float4 CombineAndTransformDualFaceUV(float2 uv1, float2 uv2, float4 mapST)
{
    return float4(uv1, uv2) * mapST.xyxy + mapST.zwzw;
}

void SetupDualFaceRendering(inout float3 normalWS, inout float4 uv, FRONT_FACE_TYPE isFrontFace)
{
    #if defined(_MODEL_GAME)
        if (IS_FRONT_VFACE(isFrontFace, 1, 0))
            return;

        // 游戏内的部分模型用了双面渲染
        // 渲染背面的时候需要调整一些值，这样就不需要修改之后的计算了

        // 反向法线
        normalWS *= -1;

        // 交换 uv1 和 uv2
        #if defined(_BACKFACEUV2_ON)
            uv.xyzw = uv.zwxy;
        #endif
    #endif
}

float GetLinearEyeDepthAnyProjection(float depth)
{
    if (IsPerspectiveProjection())
    {
        return LinearEyeDepth(depth, _ZBufferParams);
    }

    return LinearDepthToEyeDepth(depth);
}

// works only in fragment shader
float GetLinearEyeDepthAnyProjection(float4 svPosition)
{
    // 透视投影时，Scene View 里直接返回 svPosition.w 会出问题，Game View 里没事

    return GetLinearEyeDepthAnyProjection(svPosition.z);
}

struct Directions
{
    // Common Directions
    float3 N;
    float3 V;
    float3 L;
    float3 H;

    // Dot Products
    float NoL;
    float NoH;
    float NoV;
    float LoH;
};

Directions GetWorldSpaceDirections(Light light, float3 positionWS, float3 normalWS)
{
    Directions dirWS;

    dirWS.N = normalize(normalWS);
    dirWS.V = normalize(GetWorldSpaceViewDir(positionWS));
    dirWS.L = normalize(light.direction);
    dirWS.H = normalize(dirWS.V + dirWS.L);

    dirWS.NoL = dot(dirWS.N, dirWS.L);
    dirWS.NoH = dot(dirWS.N, dirWS.H);
    dirWS.NoV = dot(dirWS.N, dirWS.V);
    dirWS.LoH = dot(dirWS.L, dirWS.H);

    return dirWS;
}

struct HeadDirections
{
    float3 forward;
    float3 right;
    float3 up;
};

HeadDirections GetWorldSpaceCharHeadDirectionsImpl(
    float4 mmdHeadBoneForward,
    float4 mmdHeadBoneUp,
    float4 mmdHeadBoneRight)
{
    HeadDirections dirWS;

    #if defined(_MODEL_GAME)
        // 游戏模型的头骨骼是旋转过的
        dirWS.forward = normalize(UNITY_MATRIX_M._m01_m11_m21); // +Y 是 Forward
        dirWS.right = normalize(-UNITY_MATRIX_M._m02_m12_m22);  // -Z 是 Right
        dirWS.up = normalize(-UNITY_MATRIX_M._m00_m10_m20);     // -X 是 Up
    #elif defined(_MODEL_MMD)
        // MMD 模型只有一个根骨骼上的 Renderer，头骨骼信息需要额外获取
        dirWS.forward = mmdHeadBoneForward.xyz;
        dirWS.right = mmdHeadBoneRight.xyz;
        dirWS.up = mmdHeadBoneUp.xyz;
    #else
        dirWS.forward = normalize(UNITY_MATRIX_M._m02_m12_m22); // 其他情况下是 +Z
        dirWS.right = normalize(UNITY_MATRIX_M._m00_m10_m20);   // 其他情况下是 +X
        dirWS.up = normalize(UNITY_MATRIX_M._m01_m11_m21);      // 其他情况下是 +Y
    #endif

    return dirWS;
}

#define WORLD_SPACE_CHAR_HEAD_DIRECTIONS() \
    GetWorldSpaceCharHeadDirectionsImpl(_MMDHeadBoneForward, _MMDHeadBoneUp, _MMDHeadBoneRight)

// ----------------------------------------------------------------------------------
// NPR
// ----------------------------------------------------------------------------------

float2 GetRampUV(float NoL, bool singleMaterial, float4 vertexColor, float4 lightMap, float shadowAttenuation)
{
    // 头发 Ramp 上一共 2 条颜色，对应一个材质
    // 身体 Ramp 上一共 16 条颜色，每两条对应一个材质，共 8 种材质

    float ao = lightMap.g;
    float material = singleMaterial ? 0 : lightMap.a;

    // 游戏内模型有顶点 AO
    #if defined(_MODEL_GAME)
        ao *= vertexColor.r;
    #endif

    float NoL01 = NoL * 0.5 + 0.5;

    float shadow = min(1.0f, dot(NoL01.xx, 2 * ao.xx));
    shadow = max(0.001f, shadow) * 0.75f + 0.25f;
    shadow = (shadow > 1) ? 0.99f : shadow;

    shadow = lerp(0.20, shadow, shadowAttenuation);
    shadow = lerp(0, shadow, step(0.05, ao)); // AO < 0.05 的区域（自阴影区域）永远不受光
    shadow = lerp(1, shadow, step(ao, 0.95)); // AO > 0.95 的区域永远受最强光

    return float2(shadow, material + 0.05);
}

struct DiffuseData
{
    float NoL;
    bool singleMaterial;
    float rampCoolOrWarm;
};

float3 GetRampDiffuse(
    DiffuseData data,
    float4 vertexColor,
    float3 baseColor,
    float3 lightColor,
    float4 lightMap,
    TEXTURE2D_PARAM(rampMapCool, sampler_rampMapCool),
    TEXTURE2D_PARAM(rampMapWarm, sampler_rampMapWarm),
    float shadowAttenuation)
{
    float2 rampUV = GetRampUV(data.NoL, data.singleMaterial, vertexColor, lightMap, shadowAttenuation);
    float3 rampCool = SAMPLE_TEXTURE2D(rampMapCool, sampler_rampMapCool, rampUV).rgb;
    float3 rampWarm = SAMPLE_TEXTURE2D(rampMapWarm, sampler_rampMapWarm, rampUV).rgb;
    float3 rampColor = lerp(rampCool, rampWarm, data.rampCoolOrWarm);
    return rampColor * baseColor * lightColor;
}

float3 GetHalfLambertDiffuse(float NoL, float3 baseColor, float3 lightColor)
{
    float halfLambert = pow(NoL * 0.5 + 0.5, 2);
    return baseColor * lightColor * halfLambert;
}

struct SpecularData
{
    float3 color;
    float NoH;
    float shininess;
    float edgeSoftness;
    float intensity;
    float metallic;
};

float3 GetSpecular(SpecularData data, float3 baseColor, float3 lightColor, float4 lightMap, float shadowAttenuation)
{
    // lightMap.r: specular intensity
    // lightMap.b: specular threshold

    float threshold = 1.03 - lightMap.b; // 0.03 is an offset
    float blinnPhong = pow(max(0.01, data.NoH), data.shininess);
    blinnPhong = smoothstep(threshold, threshold + data.edgeSoftness, blinnPhong);

    // 用 F_Schlick 的效果不好看，我直接用 f0 了
    float3 fresnel = lerp(0.04, baseColor, data.metallic);

    return data.color * fresnel * lightColor * (blinnPhong * lightMap.r * data.intensity * shadowAttenuation);
}

struct EmissionData
{
    float3 color;
    float value;
    float threshold;
    float intensity;
};

float3 GetEmission(EmissionData data, float3 baseColor)
{
    float emissionMask = 1 - step(data.value, data.threshold);
    return data.color * baseColor * (emissionMask * data.intensity);
}

struct RimLightData
{
    float3 color;
    float width;
    float edgeSoftness;
    float thresholdMin;
    float thresholdMax;
    float darkenValue;
    float intensityFrontFace;
    float intensityBackFace;
    float modelScale;
    float ditherAlpha;
};

float3 GetRimLight(
    RimLightData rimLightData,
    float4 svPosition,
    float3 normalWS,
    FRONT_FACE_TYPE isFrontFace,
    float4 lightMap)
{
    float rimWidth = rimLightData.width / 2000.0; // rimWidth 表示的是屏幕上像素的偏移量，和 modelScale 无关
    float rimThresholdMin = rimLightData.thresholdMin * rimLightData.modelScale * 10.0;
    float rimThresholdMax = rimLightData.thresholdMax * rimLightData.modelScale * 10.0;

    rimWidth *= lightMap.r; // 有些地方不要边缘光
    rimWidth *= _ScaledScreenParams.y; // 在不同分辨率下看起来等宽

    if (IsPerspectiveProjection())
    {
        // unity_CameraProjection._m11: cot(FOV / 2)
        // 2.414 是 FOV 为 45 度时的值
        rimWidth *= unity_CameraProjection._m11 / 2.414; // FOV 越小，角色越大，边缘光越宽
    }
    else
    {
        // unity_CameraProjection._m11: (1 / Size)
        // 1.5996 纯 Magic Number
        rimWidth *= unity_CameraProjection._m11 / 1.5996; // Size 越小，角色越大，边缘光越宽
    }

    float depth = GetLinearEyeDepthAnyProjection(svPosition);
    rimWidth *= 10.0 * rsqrt(depth / rimLightData.modelScale); // 近大远小

    float3 normalVS = TransformWorldToViewNormal(normalWS);
    float2 indexOffset = float2(sign(normalVS.x), 0) * rimWidth; // 只横向偏移
    uint2 index = clamp(svPosition.xy - 0.5 + indexOffset, 0, _ScaledScreenParams.xy - 1); // 避免出界
    float offsetDepth = GetLinearEyeDepthAnyProjection(LoadSceneDepth(index));

    float depthDelta = (offsetDepth - depth) * 50; // 只有 depth 小于 offsetDepth 的时候再画
    float intensity = rimLightData.darkenValue * smoothstep(-rimLightData.edgeSoftness, 0, depthDelta - rimThresholdMin);
    intensity = lerp(intensity, 1, smoothstep(0, rimLightData.edgeSoftness, depthDelta - rimThresholdMax));
    intensity *= IS_FRONT_VFACE(isFrontFace, rimLightData.intensityFrontFace, rimLightData.intensityBackFace);

    // Dither Alpha 效果会扣掉角色的一部分像素，导致角色身上出现不该有的边缘光
    // 所以这里在 ditherAlpha 较强时隐去边缘光
    float ditherAlphaFadeOut = smoothstep(0.8, 1, rimLightData.ditherAlpha);

    return rimLightData.color * (intensity * ditherAlphaFadeOut);
}

void DoDitherAlphaEffect(float4 svPosition, float ditherAlpha)
{
    // 阈值矩阵，存成 const float4 数组比较省
    static const float4 thresholds[4] =
    {
        float4(01.0 / 17.0, 09.0 / 17.0, 03.0 / 17.0, 11.0 / 17.0),
        float4(13.0 / 17.0, 05.0 / 17.0, 15.0 / 17.0, 07.0 / 17.0),
        float4(04.0 / 17.0, 12.0 / 17.0, 02.0 / 17.0, 10.0 / 17.0),
        float4(16.0 / 17.0, 08.0 / 17.0, 14.0 / 17.0, 06.0 / 17.0)
    };

    uint xIndex = fmod(svPosition.x - 0.5, 4);
    uint yIndex = fmod(svPosition.y - 0.5, 4);
    clip(ditherAlpha - thresholds[yIndex][xIndex]);
}

void DoAlphaClip(float alpha, float cutoff)
{
    #if defined(_ALPHATEST_ON)
        clip(alpha - cutoff);
    #endif
}

#endif
