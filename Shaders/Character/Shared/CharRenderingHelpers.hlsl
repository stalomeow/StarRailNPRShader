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

float2 GetRampUV(float NoL, bool singleMaterial, float4 vertexColor, float4 lightMap, half shadowAttenuation)
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

    shadow = lerp(0.20, shadow, saturate(shadowAttenuation + HALF_EPS));
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
    Light light,
    float4 vertexColor,
    float3 baseColor,
    float4 lightMap,
    TEXTURE2D_PARAM(rampMapCool, sampler_rampMapCool),
    TEXTURE2D_PARAM(rampMapWarm, sampler_rampMapWarm))
{
    float2 rampUV = GetRampUV(data.NoL, data.singleMaterial, vertexColor, lightMap, light.shadowAttenuation);
    float3 rampCool = SAMPLE_TEXTURE2D(rampMapCool, sampler_rampMapCool, rampUV).rgb;
    float3 rampWarm = SAMPLE_TEXTURE2D(rampMapWarm, sampler_rampMapWarm, rampUV).rgb;
    float3 rampColor = lerp(rampCool, rampWarm, data.rampCoolOrWarm);
    return rampColor * baseColor * light.color * light.distanceAttenuation;
}

float3 GetAdditionalLightDiffuse(float3 baseColor, Light light)
{
    float attenuation = light.shadowAttenuation * saturate(light.distanceAttenuation);
    return baseColor * light.color * attenuation;
}

struct SpecularData
{
    float3 color;
    float NoH;
    float shininess;
    float roughness;
    float intensity;
};

float3 GetSpecular(SpecularData data, Light light, float3 baseColor, float4 lightMap)
{
    // lightMap.r: specular intensity
    // lightMap.b: specular threshold

    float attenuation = light.shadowAttenuation * saturate(light.distanceAttenuation);
    float blinnPhong = pow(max(0.01, data.NoH), data.shininess) * attenuation;

    float threshold = 1.03 - lightMap.b; // 0.03 is an offset
    float specular = smoothstep(threshold - data.roughness, threshold + data.roughness, blinnPhong);
    specular *= lightMap.r * data.intensity;

    // 游戏里似乎没有区分金属和非金属
    // float3 fresnel = lerp(0.04, baseColor, data.metallic);

    return data.color * baseColor * light.color * specular;
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
    return data.color * baseColor * max(0, emissionMask * data.intensity);
}

struct RimLightMaskData
{
    float3 color;
    float width;
    float edgeSoftness;
    float modelScale;
    float ditherAlpha;
};

float3 GetRimLightMask(
    RimLightMaskData rlmData,
    Directions dirWS,
    float4 svPosition,
    float4 lightMap)
{
    float invModelScale = rcp(rlmData.modelScale);
    float rimWidth = rlmData.width / 2000.0; // rimWidth 表示的是屏幕上像素的偏移量，和 modelScale 无关

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
    rimWidth *= 10.0 * rsqrt(depth * invModelScale); // 近大远小

    float indexOffsetX = -sign(cross(dirWS.V, dirWS.N).y) * rimWidth;
    uint2 index = clamp(svPosition.xy - 0.5 + float2(indexOffsetX, 0), 0, _ScaledScreenParams.xy - 1); // 避免出界
    float offsetDepth = GetLinearEyeDepthAnyProjection(LoadSceneDepth(index));

    // 只有 depth 小于 offsetDepth 的时候再画
    float intensity = smoothstep(0.12, 0.18, (offsetDepth - depth) * invModelScale);

    // 用于柔化边缘光，edgeSoftness 越大，越柔和
    float fresnel = pow(max(1 - dirWS.NoV, 0.01), max(rlmData.edgeSoftness, 0.01));

    // Dither Alpha 效果会扣掉角色的一部分像素，导致角色身上出现不该有的边缘光
    // 所以这里在 ditherAlpha 较强时隐去边缘光
    float ditherAlphaFadeOut = smoothstep(0.9, 1, rlmData.ditherAlpha);

    return rlmData.color * saturate(intensity * fresnel * ditherAlphaFadeOut);
}

struct RimLightData
{
    float darkenValue;
    float intensityFrontFace;
    float intensityBackFace;
};

float3 GetRimLight(RimLightData rimData, float3 rimMask, float NoL, Light light, FRONT_FACE_TYPE isFrontFace)
{
    float attenuation = saturate(NoL * light.shadowAttenuation * light.distanceAttenuation);
    float intensity = IS_FRONT_VFACE(isFrontFace, rimData.intensityFrontFace, rimData.intensityBackFace);
    return rimMask * light.color * (lerp(rimData.darkenValue, 1, attenuation) * max(0, intensity));
}

struct RimShadowData
{
    float ct;
    float intensity;
    float3 offset;
    float3 color;
    float width;
    float feather;
};

float3 GetRimShadow(RimShadowData data, Directions dirWS)
{
    float3 viewDirVS = TransformWorldToViewDir(dirWS.V);
    float3 normalVS = TransformWorldToViewNormal(dirWS.N);
    float rim = saturate(dot(normalize(viewDirVS - data.offset), normalVS));
    float rimShadow = saturate(pow(max(1 - rim, 0.001), data.ct) * data.width);
    rimShadow = smoothstep(data.feather, 1, rimShadow) * data.intensity * 0.25;
    return lerp(1, data.color * 2, max(rimShadow, 0));
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

float3 MixBloomColor(float3 colorTarget, float3 bloomColor, float bloomIntensity)
{
    return colorTarget * (1 + max(0, bloomIntensity) * bloomColor);
}

float3 CombineColorPreserveLuminance(float3 color, float3 colorAdd)
{
    float3 hsv = RgbToHsv(color + colorAdd);
    hsv.z = max(RgbToHsv(color).z, RgbToHsv(colorAdd).z);
    return HsvToRgb(hsv);
}

Light GetCharacterMainLight(float4 shadowCoord, float3 positionWS)
{
    Light light = GetMainLight();

    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        half4 shadowParams = GetMainLightShadowParams();

        // 我自己试下来，在角色身上 LowQuality 比 Medium 和 High 好
        // Medium 和 High 采样数多，过渡的区间大，在角色身上更容易出现 Perspective aliasing
        shadowSamplingData.softShadowQuality = SOFT_SHADOW_QUALITY_LOW;
        light.shadowAttenuation = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare), shadowCoord, shadowSamplingData, shadowParams, false);
        light.shadowAttenuation = lerp(light.shadowAttenuation, 1, GetMainLightShadowFade(positionWS));
    #endif

    #ifdef _LIGHT_LAYERS
        if (!IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
        {
            // 偷个懒，直接把强度改成 0
            light.distanceAttenuation = 0;
            light.shadowAttenuation = 0;
        }
    #endif

    return light;
}

Light GetCharacterAdditionalLight(uint lightIndex, float3 positionWS)
{
    Light light = GetAdditionalLight(lightIndex, positionWS);
    // light.distanceAttenuation = saturate(light.distanceAttenuation);

    #if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
        light.shadowAttenuation = AdditionalLightRealtimeShadow(lightIndex, positionWS, light.direction);
        light.shadowAttenuation = lerp(light.shadowAttenuation, 1, GetAdditionalLightShadowFade(positionWS));
    #endif

    #ifdef _LIGHT_LAYERS
        if (!IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
        {
            // 偷个懒，直接把强度改成 0
            light.distanceAttenuation = 0;
            light.shadowAttenuation = 0;
        }
    #endif

    return light;
}

#if USE_FORWARD_PLUS
    // Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl
    struct ForwardPlusDummyInputData
    {
        float3 positionWS;
        float2 normalizedScreenSpaceUV;
    };

    #define CHAR_LIGHT_LOOP_BEGIN(posWS, posHCS) { \
        uint pixelLightCount = GetAdditionalLightsCount(); \
        ForwardPlusDummyInputData inputData; \
        inputData.positionWS = posWS; \
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(posHCS); \
        LIGHT_LOOP_BEGIN(pixelLightCount)
#else
    #define CHAR_LIGHT_LOOP_BEGIN(posWS, posHCS) { \
        uint pixelLightCount = GetAdditionalLightsCount(); \
        LIGHT_LOOP_BEGIN(pixelLightCount)
#endif

#define CHAR_LIGHT_LOOP_END } LIGHT_LOOP_END

#endif
