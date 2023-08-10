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

    float _DitherAlpha;

    float4 _MMDHeadBoneForward;
    float4 _MMDHeadBoneUp;
    float4 _MMDHeadBoneRight;
CBUFFER_END

CharacterVaryings FaceVertex(CharacterAttributes i)
{
    return CharacterVertex(i, _Maps_ST);
}

float3 SDFFaceShadow(Light light, float4 uv)
{
    // 游戏模型才有 UV2
    #if defined(_MODEL_GAME) && defined(_FACEMAPUV2_ON)
        uv.xyzw = uv.zwxy;
    #endif

    float3 up = GetCharacterHeadBoneUpWS(_MMDHeadBoneUp);
    float3 right = GetCharacterHeadBoneRightWS(_MMDHeadBoneRight);
    float3 forward = GetCharacterHeadBoneForwardWS(_MMDHeadBoneForward);

    // light 在物体 XZ 平面的投影方向
    float3 fixedLightDir = normalize(light.direction - dot(light.direction, up) * up);

    bool isRight = dot(fixedLightDir, right) > 0;
    float2 sdfUV = isRight ? float2(1 - uv.x, uv.y) : uv.xy;
    float threshold = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, sdfUV).a;

    float FdotL01 = dot(forward, fixedLightDir) * 0.5 + 0.5;
    return lerp(_ShadowColor.rgb, 1, step(1 - threshold, FdotL01));
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
    #else
        float4 faceMap = SAMPLE_TEXTURE2D(_FaceMap, sampler_FaceMap, i.uv.xy);
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
    baseColor *= lerp(1, _NoseLineColor.rgb, step(1.03 - faceMap.b, FdotV));

    // SDF
    float3 diffuse = baseColor * SDFFaceShadow(light, i.uv);

    // TODO: faceMap R 通道未使用
    // TODO: 嘴唇 Outline: 0.5 < faceMap.g < 0.95

    // 眼睛的高亮
    float3 emission = GetEmission(baseColor, alpha, _EmissionThreshold, _EmissionIntensity, _EmissionColor.rgb);

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

    // 眼睛、眼眶、眉毛的遮罩（不包括高光点）
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
