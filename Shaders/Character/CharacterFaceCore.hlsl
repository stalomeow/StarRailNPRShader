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

#ifndef _CHARACTER_FACE_CORE_INCLUDED
#define _CHARACTER_FACE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "CharacterOutline.hlsl"
#include "CharacterCommon.hlsl"
#include "CharacterUtils.hlsl"
#include "CharacterShadow.hlsl"
#include "CharacterDepthOnly.hlsl"

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_FaceMap); SAMPLER(sampler_FaceMap);
TEXTURE2D(_ExpressionMap); SAMPLER(sampler_ExpressionMap);

CBUFFER_START(UnityPerMaterial)
    float _ModelScale;
    float _AlphaTestThreshold;

    float4 _Color;
    float4 _Maps_ST;

    float4 _ShadowColor;
    float4 _EyeShadowColor;

    float4 _EmissionColor;
    float _EmissionThreshold;
    float _EmissionIntensity;

    float _BloomIntensity0;

    float _OutlineWidth;
    float _OutlineZOffset;
    float4 _OutlineColor0;

    float4 _NoseLineColor;
    float _NoseLinePower;

    float _MaxEyeHairDistance;

    float4 _ExCheekColor;
    float _ExCheekIntensity;
    float4 _ExShyColor;
    float _ExShyIntensity;
    float4 _ExShadowColor;
    float4 _ExEyeColor;
    float _ExShadowIntensity;

    float _DitherAlpha;

    float4 _MMDHeadBoneForward;
    float4 _MMDHeadBoneUp;
    float4 _MMDHeadBoneRight;
CBUFFER_END

CharacterVaryings FaceVertex(CharacterAttributes i)
{
    return CharacterVertex(i, _Maps_ST);
}

float3 GetFaceOrEyeDiffuse(Light light, float4 uv, float3 baseColor, float4 faceMap)
{
    // 游戏模型才有 UV2
    #if defined(_MODEL_GAME) && defined(_FACEMAPUV2_ON)
        uv.xyzw = uv.zwxy;
    #endif

    float3 up = GetCharacterHeadBoneUpWS(_MMDHeadBoneUp);
    float3 right = GetCharacterHeadBoneRightWS(_MMDHeadBoneRight);
    float3 forward = GetCharacterHeadBoneForwardWS(_MMDHeadBoneForward);
    float3 lightDirProj = normalize(light.direction - dot(light.direction, up) * up); // 做一次投影

    bool isRight = dot(lightDirProj, right) > 0;
    float2 sdfUV = isRight ? float2(1 - uv.x, uv.y) : uv.xy;
    float threshold = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, sdfUV).a;

    float FoL01 = dot(forward, lightDirProj) * 0.5 + 0.5;
    float3 faceShadow = lerp(_ShadowColor.rgb, 1, step(1 - threshold, FoL01)); // SDF Shadow
    float3 eyeShadow = lerp(_EyeShadowColor.rgb, 1, smoothstep(0.3, 0.5, FoL01));
    return baseColor * lerp(faceShadow, eyeShadow, faceMap.r);
}

void FaceOpaqueAndZFragment(
    CharacterVaryings i,
    out float4 colorTarget : SV_Target0,
    out float4 bloomTarget : SV_Target1)
{
    // Textures
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    // 游戏模型才有 UV2
    #if defined(_MODEL_GAME) && defined(_FACEMAPUV2_ON)
        float4 faceMap = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.zw);
        float4 exprMap = SAMPLE_TEXTURE2D(_ExpressionMap, sampler_ExpressionMap, i.uv.zw);
    #else
        float4 faceMap = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.xy);
        float4 exprMap = SAMPLE_TEXTURE2D(_ExpressionMap, sampler_ExpressionMap, i.uv.xy);
    #endif

    // Colors
    float3 baseColor = texColor.rgb;
    float alpha = texColor.a;

    DoAlphaClip(alpha, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    // Calc
    Light light = GetMainLight();

    // Nose Line
    float3 F = GetCharacterHeadBoneForwardWS(_MMDHeadBoneForward);
    float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
    float3 FdotV = pow(abs(dot(F, V)), _NoseLinePower);
    baseColor = lerp(baseColor, baseColor * _NoseLineColor.rgb, step(1.03 - faceMap.b, FdotV));

    // Expression
    float3 exCheek = lerp(baseColor, baseColor * _ExCheekColor.rgb, exprMap.r);
    baseColor = lerp(baseColor, exCheek, _ExCheekIntensity);
    float3 exShy = lerp(baseColor, baseColor * _ExShyColor.rgb, exprMap.g);
    baseColor = lerp(baseColor, exShy, _ExShyIntensity);
    float3 exShadow = lerp(baseColor, baseColor * _ExShadowColor.rgb, exprMap.b);
    baseColor = lerp(baseColor, exShadow, _ExShadowIntensity);
    float3 exEyeShadow = lerp(baseColor, baseColor * _ExEyeColor.rgb, faceMap.r);
    baseColor = lerp(baseColor, exEyeShadow, _ExShadowIntensity);

    // Diffuse
    float3 diffuse = GetFaceOrEyeDiffuse(light, i.uv, baseColor, faceMap);

    // 眼睛的高亮
    float3 emission = GetEmission(baseColor, alpha, _EmissionThreshold, _EmissionIntensity, _EmissionColor.rgb);

    // TODO: 嘴唇 Outline: 0.5 < faceMap.g < 0.95

    // Output
    colorTarget = float4(diffuse * light.color + emission, alpha);
    bloomTarget = float4(_BloomIntensity0, 0, 0, 0);
}

void FaceWriteEyeStencilFragment(CharacterVaryings i)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    // （尽量）避免后一个角色的眼睛透过前一个角色的头发
    float sceneDepth = LinearEyeDepth(LoadSceneDepth(i.positionHCS.xy), _ZBufferParams);
    float eyeDepth = LinearEyeDepth(i.positionHCS.z, _ZBufferParams);
    float depthMask = step(abs(sceneDepth - eyeDepth), _MaxEyeHairDistance * _ModelScale);

    // 眼睛、眼眶、眉毛的遮罩（不包括高光）
    #if defined(_MODEL_GAME)
        // 游戏模型使用 uv2 采样！！！景元和刃只有一边的眼睛需要写 Stencil，用 uv1 会把两只眼睛的都写进去
        float eyeMask = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.zw).g;
    #else
        // MMD 模型没办法，不管上面两个角色了
        float eyeMask = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.xy).g;
    #endif

    clip(eyeMask * depthMask - 0.5);
}

CharacterOutlineVaryings FaceOutlineVertex(CharacterOutlineAttributes i)
{
    return CharacterFaceOutlineVertex(i, _Maps_ST, _MMDHeadBoneForward, _ModelScale, _OutlineWidth, _OutlineZOffset);
}

float4 FaceOutlineFragment(CharacterOutlineVaryings i) : SV_Target0
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return float4(_OutlineColor0.rgb, 1);
}

CharacterShadowVaryings FaceShadowVertex(CharacterShadowAttributes i)
{
    return CharacterShadowVertex(i, _Maps_ST);
}

void FaceShadowFragment(CharacterShadowVaryings i)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);
}

CharacterDepthOnlyVaryings FaceDepthOnlyVertex(CharacterDepthOnlyAttributes i)
{
    return CharacterDepthOnlyVertex(i, _Maps_ST);
}

float4 FaceDepthOnlyFragment(CharacterDepthOnlyVaryings i) : SV_Target
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharacterDepthOnlyFragment(i);
}

#endif
