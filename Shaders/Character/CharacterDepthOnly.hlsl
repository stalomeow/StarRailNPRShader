#ifndef _CHARACTER_DEPTH_ONLY_INCLUDED
#define _CHARACTER_DEPTH_ONLY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "CharacterDualFace.hlsl"

struct CharacterDepthOnlyAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv1          : TEXCOORD0;
    float2 uv2          : TEXCOORD1;
};

struct CharacterDepthOnlyVaryings
{
    float4 positionHCS  : SV_POSITION;
    float3 normalWS     : NORMAL;
    float4 uv           : TEXCOORD0;
};

CharacterDepthOnlyVaryings CharacterDepthOnlyVertex(CharacterDepthOnlyAttributes i, float4 mapST)
{
    CharacterDepthOnlyVaryings o;

    o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);
    return o;
}

float4 CharacterDepthOnlyFragment(CharacterDepthOnlyVaryings i)
{
    return i.positionHCS.z;
}

#endif
