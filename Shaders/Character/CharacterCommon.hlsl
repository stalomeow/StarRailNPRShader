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

#ifndef _CHARACTER_COMMON_INCLUDED
#define _CHARACTER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "CharacterDualFace.hlsl"

// ==========================
// Stencil 使用低三位，高 -> 低
// 1      1      1
// 头发    脸     眼睛
// ==========================

struct CharacterAttributes
{
    float3 positionOS     : POSITION;
    float3 normalOS       : NORMAL;
    float4 color          : COLOR;
    float2 uv1            : TEXCOORD0;
    float2 uv2            : TEXCOORD1;
};

struct CharacterVaryings
{
    float4 positionHCS    : SV_POSITION;
    float3 normalWS       : NORMAL;
    float4 color          : COLOR;
    float4 uv             : TEXCOORD0;
    float3 positionWS     : TEXCOORD1;
};

CharacterVaryings CharacterVertex(CharacterAttributes i, float4 mapST)
{
    CharacterVaryings o;

    float3 positionWS = TransformObjectToWorld(i.positionOS);
    o.positionHCS = TransformWorldToHClip(positionWS);
    o.normalWS = TransformObjectToWorldNormal(i.normalOS, true);
    o.color = i.color;
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    o.positionWS = positionWS;

    return o;
}

float2 GetRampUV(float NoL, float4 vertexColor, float4 lightMap, bool singleMaterial)
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
    float threshold = (NoL01 + ao) * 0.5;
    float shadowStrength = (0.5 - threshold) / 0.5;
    float shadow = 1 - saturate(shadowStrength / 0.5);

    // Ramp 图从左到右的变化规律：暗 -> 亮 -> 暗 -> 亮
    // 左边一部分应该是混合 Shadow Map 的
    shadow = lerp(0.20, 1, shadow); // 这里只用右边一半
    shadow = lerp(0, shadow, step(0.05, ao)); // AO < 0.05 的区域（自阴影区域）永远不受光
    shadow = lerp(1, shadow, step(ao, 0.95)); // AO > 0.95 的区域永远受最强光

    // TODO: 混合 Shadow Map（需要改管线）

    return float2(shadow, material + 0.05);
}

float3 GetDiffuse(
    float NoL,
    float4 vertexColor,
    float4 lightMap,
    bool singleMaterial,
    float3 baseColor,
    TEXTURE2D_PARAM(rampMapCool, sampler_rampMapCool),
    TEXTURE2D_PARAM(rampMapWarm, sampler_rampMapWarm),
    float rampCoolWarmLerpFactor)
{
    float2 rampUV = GetRampUV(NoL, vertexColor, lightMap, singleMaterial);
    float3 rampCool = SAMPLE_TEXTURE2D(rampMapCool, sampler_rampMapCool, rampUV).rgb;
    float3 rampWarm = SAMPLE_TEXTURE2D(rampMapWarm, sampler_rampMapWarm, rampUV).rgb;
    float3 rampColor = lerp(rampCool, rampWarm, rampCoolWarmLerpFactor);
    return rampColor * baseColor;
}

float3 GetSpecular(
    float NoH,
    float4 lightMap,
    float3 baseColor,
    float3 specularColor,
    float shininess,
    float edgeSoftness,
    float intensity,
    float metallic)
{
    // lightMap.r: specular intensity
    // lightMap.b: specular threshold

    float threshold = 1.03 - lightMap.b; // 0.03 is an offset
    float blinnPhong = pow(max(0.01, NoH), shininess);
    blinnPhong = smoothstep(threshold, threshold + edgeSoftness, blinnPhong);

    // float3 f0 = lerp(0.04, baseColor, metallic);
    // float3 fresnel = F_Schlick(f0, max(0, LoH));

    // 用 F_Schlick 的效果不好看
    float3 fresnel = lerp(0.04, baseColor, metallic);

    return specularColor * fresnel * (blinnPhong * lightMap.r * intensity);
}

float3 GetEmission(
    float3 baseColor,
    float mainTexAlpha,
    float threshold,
    float intensity,
    float3 emissionColor)
{
    float emissionMask = 1 - step(mainTexAlpha, threshold);
    return emissionColor * baseColor * (emissionMask * intensity);
}

float3 GetRimLight(
    float4 positionHCSFrag,
    float3 normalWS,
    float4 lightMap,
    float modelScale,
    float3 rimColor,
    float rimWidth,
    float rimEdgeSoftness,
    float rimThresholdMin,
    float rimThresholdMax,
    float rimDarkenValue,
    float rimIntensityFrontFace,
    float rimIntensityBackFace,
    FRONT_FACE_TYPE isFrontFace)
{
    rimWidth *= 1.0 / 2000.0; // rimWidth 表示的是屏幕上像素的偏移量，和 modelScale 无关
    rimThresholdMin *= modelScale * 10.0;
    rimThresholdMax *= modelScale * 10.0;

    float depth = LinearEyeDepth(positionHCSFrag.z, _ZBufferParams);

    rimWidth *= lightMap.r; // 有些地方不要边缘光
    rimWidth *= _ScaledScreenParams.y; // 在不同分辨率下看起来等宽

    // unity_CameraProjection._m11: cot(FOV / 2)
    // 2.414 是 FOV 为 45 度时的值
    float fixScale = unity_CameraProjection._m11 / 2.414; // FOV 越小，角色越大，边缘光越宽
    fixScale *= 10.0 * rsqrt(depth / modelScale); // 近大远小
    rimWidth *= fixScale;

    float3 normalVS = TransformWorldToViewNormal(normalWS);
    float2 uvOffset = normalize(normalVS.xy) * rimWidth;
    uint2 uv = clamp(positionHCSFrag.xy + uvOffset, 0, _ScaledScreenParams.xy - 1); // 避免出界
    float offsetDepth = LinearEyeDepth(LoadSceneDepth(uv), _ZBufferParams);

    float depthDelta = (offsetDepth - depth) * 50; // 只有 depth 小于 offsetDepth 的时候再画
    float intensity = rimDarkenValue * smoothstep(-rimEdgeSoftness, 0, depthDelta - rimThresholdMin);
    intensity = lerp(intensity, 1, smoothstep(0, rimEdgeSoftness, depthDelta - rimThresholdMax));
    intensity *= IS_FRONT_VFACE(isFrontFace, rimIntensityFrontFace, rimIntensityBackFace);

    return rimColor * intensity;
}

static float DitherAlphaThresholds[16] =
{
    01.0 / 17.0, 09.0 / 17.0, 03.0 / 17.0, 11.0 /17.0,
    13.0 / 17.0, 05.0 / 17.0, 15.0 / 17.0, 07.0 /17.0,
    04.0 / 17.0, 12.0 / 17.0, 02.0 / 17.0, 10.0 /17.0,
    16.0 / 17.0, 08.0 / 17.0, 14.0 / 17.0, 06.0 /17.0
};

void DitherAlphaEffect(float4 positionHCSFrag, float ditherAlpha)
{
    uint index = fmod(positionHCSFrag.x, 4) * 4 + fmod(positionHCSFrag.y, 4);
    clip(ditherAlpha - min(DitherAlphaThresholds[index], 0.9));
}

void DoAlphaClip(float alpha, float cutoff)
{
    #if _ALPHATEST_ON
        clip(alpha - cutoff);
    #endif
}

#endif
