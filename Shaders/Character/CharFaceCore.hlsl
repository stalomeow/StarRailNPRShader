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

#ifndef _CHAR_FACE_CORE_INCLUDED
#define _CHAR_FACE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Shared/CharCore.hlsl"
#include "Shared/CharDepthOnly.hlsl"
#include "Shared/CharDepthNormals.hlsl"
#include "Shared/CharOutline.hlsl"
#include "Shared/CharShadow.hlsl"
#include "Shared/CharMotionVectors.hlsl"

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

    float _mBloomIntensity0;
    float4 _BloomColor0;

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

CharCoreVaryings FaceVertex(CharCoreAttributes i)
{
    return CharCoreVertex(i, _Maps_ST);
}

float3 GetFaceOrEyeDiffuse(
    Directions dirWS,
    HeadDirections headDirWS,
    float4 uv,
    float3 baseColor,
    float3 lightColor,
    float4 faceMap,
    half shadowAttenuation)
{
    // 游戏模型才有 UV2
    #if defined(_MODEL_GAME) && defined(_FACEMAPUV2_ON)
        uv.xyzw = uv.zwxy;
    #endif

    float3 lightDirProj = normalize(dirWS.L - dot(dirWS.L, headDirWS.up) * headDirWS.up); // 做一次投影

    bool isRight = dot(lightDirProj, headDirWS.right) > 0;
    float2 sdfUV = isRight ? float2(1 - uv.x, uv.y) : uv.xy;
    float threshold = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, sdfUV).a;

    float FoL01 = (dot(headDirWS.forward, lightDirProj) * 0.5 + 0.5);
    // 被阴影挡住时没有伦勃朗光
    float3 faceShadow = lerp(_ShadowColor.rgb, 1, step(1 - threshold, FoL01) * shadowAttenuation); // SDF Shadow
    float3 eyeShadow = lerp(_EyeShadowColor.rgb, 1, smoothstep(0.3, 0.5, FoL01) * shadowAttenuation);
    return baseColor * lightColor * lerp(faceShadow, eyeShadow, faceMap.r);
}

void FaceOpaqueAndZFragment(
    CharCoreVaryings i,
    out float4 colorTarget : SV_Target0,
    out float4 bloomTarget : SV_Target1)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    // 游戏模型才有 UV2
    #if defined(_MODEL_GAME) && defined(_FACEMAPUV2_ON)
        float4 faceMap = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.zw);
        float4 exprMap = SAMPLE_TEXTURE2D(_ExpressionMap, sampler_ExpressionMap, i.uv.zw);
    #else
        float4 faceMap = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.xy);
        float4 exprMap = SAMPLE_TEXTURE2D(_ExpressionMap, sampler_ExpressionMap, i.uv.xy);
    #endif

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    Light light = GetCharacterMainLight(i.shadowCoord);
    Directions dirWS = GetWorldSpaceDirections(light, i.positionWS, i.normalWS);
    HeadDirections headDirWS = WORLD_SPACE_CHAR_HEAD_DIRECTIONS();

    // Nose Line
    float3 FdotV = pow(abs(dot(headDirWS.forward, dirWS.V)), _NoseLinePower);
    texColor.rgb = lerp(texColor.rgb, texColor.rgb * _NoseLineColor.rgb, step(1.03 - faceMap.b, FdotV));

    // Expression
    float3 exCheek = lerp(texColor.rgb, texColor.rgb * _ExCheekColor.rgb, exprMap.r);
    texColor.rgb = lerp(texColor.rgb, exCheek, _ExCheekIntensity);
    float3 exShy = lerp(texColor.rgb, texColor.rgb * _ExShyColor.rgb, exprMap.g);
    texColor.rgb = lerp(texColor.rgb, exShy, _ExShyIntensity);
    float3 exShadow = lerp(texColor.rgb, texColor.rgb * _ExShadowColor.rgb, exprMap.b);
    texColor.rgb = lerp(texColor.rgb, exShadow, _ExShadowIntensity);
    float3 exEyeShadow = lerp(texColor.rgb, texColor.rgb * _ExEyeColor.rgb, faceMap.r);
    texColor.rgb = lerp(texColor.rgb, exEyeShadow, _ExShadowIntensity);

    // Diffuse
    float3 diffuse = GetFaceOrEyeDiffuse(dirWS, headDirWS, i.uv, texColor.rgb, light.color, faceMap, light.shadowAttenuation);

    EmissionData emissionData;
    emissionData.color = _EmissionColor.rgb;
    emissionData.value = texColor.a;
    emissionData.threshold = _EmissionThreshold;
    emissionData.intensity = _EmissionIntensity;

    // 眼睛的高亮
    float3 emission = GetEmission(emissionData, texColor.rgb);

    // TODO: 嘴唇 Outline: 0.5 < faceMap.g < 0.95

    float3 diffuseAdd = 0;

    #if defined(_ADDITIONAL_LIGHTS)
        CHAR_LIGHT_LOOP_BEGIN(i.positionWS, i.positionHCS)
            Light lightAdd = GetAdditionalLight(lightIndex, i.positionWS);
            float attenuationAdd = saturate(lightAdd.distanceAttenuation);
            diffuseAdd += texColor.rgb * lightAdd.color * attenuationAdd;
        CHAR_LIGHT_LOOP_END
    #endif

    // Output
    colorTarget = float4(CombineColorPreserveLuminance(diffuse, diffuseAdd) + emission, texColor.a);
    bloomTarget = EncodeBloomColor(_BloomColor0.rgb, _mBloomIntensity0);

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);
}

void FaceWriteEyeStencilFragment(CharCoreVaryings i)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    // （尽量）避免后一个角色的眼睛透过前一个角色的头发
    float sceneDepth = GetLinearEyeDepthAnyProjection(LoadSceneDepth(i.positionHCS.xy - 0.5));
    float eyeDepth = GetLinearEyeDepthAnyProjection(i.positionHCS);
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

CharOutlineVaryings FaceOutlineVertex(CharOutlineAttributes i)
{
    VertexPositionInputs vertexInputs = GetVertexPositionInputs(i.positionOS);

    OutlineData outlineData;
    outlineData.modelScale = _ModelScale;
    outlineData.width = _OutlineWidth;
    outlineData.zOffset = _OutlineZOffset;

    #if defined(_MODEL_GAME)
        HeadDirections headDirWS = WORLD_SPACE_CHAR_HEAD_DIRECTIONS();

        // 当嘴从侧面看在脸外面时再启用描边
        float3 viewDirWS = normalize(GetWorldSpaceViewDir(vertexInputs.positionWS));
        float FdotV = pow(max(0, dot(headDirWS.forward, viewDirWS)), 0.8);
        outlineData.width *= smoothstep(-0.05, 0, 1 - FdotV - i.color.b);

        // TODO: Fix 脸颊的描边。大概是用 vertexColor.g
    #endif

    return CharOutlineVertex(outlineData, i, vertexInputs, _Maps_ST);
}

float4 FaceOutlineFragment(CharOutlineVaryings i) : SV_Target0
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    float4 colorTarget = float4(_OutlineColor0.rgb, 1);

    // Fog
    real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
    colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

    return colorTarget;
}

CharShadowVaryings FaceShadowVertex(CharShadowAttributes i)
{
    return CharShadowVertex(i, _Maps_ST);
}

void FaceShadowFragment(CharShadowVaryings i)
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
}

CharDepthOnlyVaryings FaceDepthOnlyVertex(CharDepthOnlyAttributes i)
{
    return CharDepthOnlyVertex(i, _Maps_ST);
}

float4 FaceDepthOnlyFragment(CharDepthOnlyVaryings i) : SV_Target
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthOnlyFragment(i);
}

CharDepthNormalsVaryings FaceDepthNormalsVertex(CharDepthNormalsAttributes i)
{
    return CharDepthNormalsVertex(i, _Maps_ST);
}

float4 FaceDepthNormalsFragment(CharDepthNormalsVaryings i) : SV_Target
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthNormalsFragment(i);
}

CharMotionVectorsVaryings FaceMotionVectorsVertex(CharMotionVectorsAttributes i)
{
    return CharMotionVectorsVertex(i, _Maps_ST);
}

half4 FaceMotionVectorsFragment(CharMotionVectorsVaryings i) : SV_Target
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharMotionVectorsFragment(i);
}

#endif
