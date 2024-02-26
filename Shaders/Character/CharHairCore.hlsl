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

#ifndef _CHAR_HAIR_CORE_INCLUDED
#define _CHAR_HAIR_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Shared/CharCore.hlsl"
#include "Shared/CharDepthOnly.hlsl"
#include "Shared/CharDepthNormals.hlsl"
#include "Shared/CharOutline.hlsl"
#include "Shared/CharShadow.hlsl"

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_LightMap); SAMPLER(sampler_LightMap);
TEXTURE2D(_RampMapCool); SAMPLER(sampler_RampMapCool);
TEXTURE2D(_RampMapWarm); SAMPLER(sampler_RampMapWarm);

CBUFFER_START(UnityPerMaterial)
    float _ModelScale;
    float _AlphaTestThreshold;

    float4 _Color;
    float4 _BackColor;
    float4 _Maps_ST;

    float _RampCoolWarmLerpFactor;

    float4 _SpecularColor0;
    float _SpecularShininess0;
    float _SpecularIntensity0;
    float _SpecularEdgeSoftness0;

    float4 _EmissionColor;
    float _EmissionThreshold;
    float _EmissionIntensity;

    float _BloomIntensity0;
    float4 _BloomColor0;

    float _RimIntensity;
    float _RimIntensityBackFace;
    float _RimThresholdMin;
    float _RimThresholdMax;
    float _RimEdgeSoftness;
    float _RimWidth0;
    float4 _RimColor0;
    float _RimDark0;

    float _OutlineWidth;
    float _OutlineZOffset;
    float4 _OutlineColor0;

    float _HairBlendAlpha;

    float _DitherAlpha;

    float4 _MMDHeadBoneForward;
    float4 _MMDHeadBoneUp;
    float4 _MMDHeadBoneRight;
CBUFFER_END

CharCoreVaryings HairVertex(CharCoreAttributes i)
{
    return CharCoreVertex(i, _Maps_ST);
}

float4 BaseHairOpaqueFragment(
    inout CharCoreVaryings i,
    FRONT_FACE_TYPE isFrontFace)
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    float4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    Light light = GetMainLight(i.shadowCoord);
    Directions dirWS = GetWorldSpaceDirections(light, i.positionWS, i.normalWS);

    DiffuseData diffuseData;
    diffuseData.NoL = dirWS.NoL;
    diffuseData.singleMaterial = true;
    diffuseData.rampCoolOrWarm = _RampCoolWarmLerpFactor;

    SpecularData specularData;
    specularData.color = _SpecularColor0.rgb;
    specularData.NoH = dirWS.NoV * (dirWS.NoL > 0); // 感觉 NoV 做头发高光更好看！有随视线流动的效果
    specularData.shininess = _SpecularShininess0;
    specularData.edgeSoftness = _SpecularEdgeSoftness0;
    specularData.intensity = _SpecularIntensity0;
    specularData.metallic = 0;

    RimLightData rimLightData;
    rimLightData.color = _RimColor0.rgb;
    rimLightData.width = _RimWidth0;
    rimLightData.edgeSoftness = _RimEdgeSoftness;
    rimLightData.thresholdMin = _RimThresholdMin;
    rimLightData.thresholdMax = _RimThresholdMax;
    rimLightData.darkenValue = _RimDark0;
    rimLightData.intensityFrontFace = _RimIntensity;
    rimLightData.intensityBackFace = _RimIntensityBackFace;
    rimLightData.modelScale = _ModelScale;
    rimLightData.ditherAlpha = _DitherAlpha;

    EmissionData emissionData;
    emissionData.color = _EmissionColor.rgb;
    emissionData.value = texColor.a;
    emissionData.threshold = _EmissionThreshold;
    emissionData.intensity = _EmissionIntensity;

    float3 diffuse = GetRampDiffuse(diffuseData, i.color, texColor.rgb, light.color, lightMap,
        TEXTURE2D_ARGS(_RampMapCool, sampler_RampMapCool), TEXTURE2D_ARGS(_RampMapWarm, sampler_RampMapWarm),
        light.shadowAttenuation);
    float3 specular = GetSpecular(specularData, texColor.rgb, light.color, lightMap, light.shadowAttenuation);
    float3 rimLight = GetRimLight(rimLightData, i.positionHCS, dirWS.N, isFrontFace, lightMap);
    float3 emission = GetEmission(emissionData, texColor.rgb);

    float3 diffuseAdd = 0;
    float3 specularAdd = 0;
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light lightAdd = GetAdditionalLight(lightIndex, i.positionWS);
        Directions dirWSAdd = GetWorldSpaceDirections(lightAdd, i.positionWS, i.normalWS);
        float attenuationAdd = saturate(lightAdd.distanceAttenuation);

        diffuseAdd += GetHalfLambertDiffuse(dirWSAdd.NoL, texColor.rgb, lightAdd.color) * attenuationAdd;

        SpecularData specularDataAdd;
        specularDataAdd.color = _SpecularColor0.rgb;
        specularDataAdd.NoH = dirWSAdd.NoH;
        specularDataAdd.shininess = _SpecularShininess0;
        specularDataAdd.edgeSoftness = _SpecularEdgeSoftness0;
        specularDataAdd.intensity = _SpecularIntensity0;
        specularDataAdd.metallic = 0;
        specularAdd += GetSpecular(specularDataAdd, texColor.rgb, lightAdd.color, lightMap, 1) * attenuationAdd;
    LIGHT_LOOP_END

    // Output
    return float4(diffuse + specular + rimLight + emission + diffuseAdd + specularAdd, texColor.a);
}

void HairOpaqueFragment(
    CharCoreVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC,
    out float4 colorTarget      : SV_Target0,
    out float4 bloomTarget      : SV_Target1)
{
    float4 hairColor = BaseHairOpaqueFragment(i, isFrontFace);

    colorTarget = float4(hairColor.rgb, 1);
    bloomTarget = float4(_BloomColor0.rgb, _BloomIntensity0);
}

void HairFakeTransparentFragment(
    CharCoreVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC,
    out float4 colorTarget      : SV_Target0,
    out float4 bloomTarget      : SV_Target1)
{
    // 手动做一次深度测试，保证只有最上面一层头发和眼睛做 alpha 混合。这样看上去更加通透
    float sceneDepth = GetLinearEyeDepthAnyProjection(LoadSceneDepth(i.positionHCS.xy - 0.5));
    float hairDepth = GetLinearEyeDepthAnyProjection(i.positionHCS);
    clip(sceneDepth - hairDepth); // if (hairDepth > sceneDepth) discard;

    float4 hairColor = BaseHairOpaqueFragment(i, isFrontFace);

    HeadDirections headDirWS = WORLD_SPACE_CHAR_HEAD_DIRECTIONS();
    float3 viewDirWS = GetWorldSpaceViewDir(i.positionWS);

    // Horizontal 70 度
    float3 viewDirXZ = normalize(viewDirWS - dot(viewDirWS, headDirWS.up) * headDirWS.up);
    float cosHorizontal = max(0, dot(viewDirXZ, headDirWS.forward));
    float alpha1 = saturate((1 - cosHorizontal) / 0.658); // 0.658: 1 - cos70°

    // Vertical 45 度
    float3 viewDirYZ = normalize(viewDirWS - dot(viewDirWS, headDirWS.right) * headDirWS.right);
    float cosVertical = max(0, dot(viewDirYZ, headDirWS.forward));
    float alpha2 = saturate((1 - cosVertical) / 0.293); // 0.293: 1 - cos45°

    // Output
    colorTarget = float4(hairColor.rgb, max(max(alpha1, alpha2), _HairBlendAlpha));
    bloomTarget = float4(_BloomColor0.rgb, _BloomIntensity0);
}

CharOutlineVaryings HairOutlineVertex(CharOutlineAttributes i)
{
    VertexPositionInputs vertexInputs = GetVertexPositionInputs(i.positionOS);

    OutlineData outlineData;
    outlineData.modelScale = _ModelScale;
    outlineData.width = _OutlineWidth;
    outlineData.zOffset = _OutlineZOffset;

    return CharOutlineVertex(outlineData, i, vertexInputs, _Maps_ST);
}

float4 HairOutlineFragment(CharOutlineVaryings i) : SV_Target0
{
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color;

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return float4(_OutlineColor0.rgb, 1);
}

CharShadowVaryings HairShadowVertex(CharShadowAttributes i)
{
    return CharShadowVertex(i, _Maps_ST);
}

void HairShadowFragment(
    CharShadowVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC)
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
}

CharDepthOnlyVaryings HairDepthOnlyVertex(CharDepthOnlyAttributes i)
{
    return CharDepthOnlyVertex(i, _Maps_ST);
}

float4 HairDepthOnlyFragment(
    CharDepthOnlyVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthOnlyFragment(i);
}

CharDepthNormalsVaryings HairDepthNormalsVertex(CharDepthNormalsAttributes i)
{
    return CharDepthNormalsVertex(i, _Maps_ST);
}

float4 HairDepthNormalsFragment(
    CharDepthNormalsVaryings i,
    FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    SetupDualFaceRendering(i.normalWS, i.uv, isFrontFace);

    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    texColor *= IS_FRONT_VFACE(isFrontFace, _Color, _BackColor);

    DoAlphaClip(texColor.a, _AlphaTestThreshold);
    DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

    return CharDepthNormalsFragment(i);
}

#endif
