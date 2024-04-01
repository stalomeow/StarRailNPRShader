#ifndef _CHAR_G_BUFFER_INCLUDED
#define _CHAR_G_BUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "CharRenderingHelpers.hlsl"

struct CharGBufferAttributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv1          : TEXCOORD0;
    float2 uv2          : TEXCOORD1;
};

struct CharGBufferVaryings
{
    float4 positionHCS  : SV_POSITION;
    float3 normalWS     : NORMAL;
    float4 uv           : TEXCOORD0;
};

CharGBufferVaryings CharGBufferVertex(CharGBufferAttributes i, float4 mapST)
{
    CharGBufferVaryings o;

    o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(i.normalOS);
    o.uv = CombineAndTransformDualFaceUV(i.uv1, i.uv2, mapST);

    return o;
}

FragmentOutput CharGBufferFragment(CharGBufferVaryings i)
{
    half3 packedNormalWS = PackNormal(i.normalWS);

    FragmentOutput output = (FragmentOutput)0;
    output.GBuffer2 = half4(packedNormalWS, 0);

    #if _RENDER_PASS_ENABLED
        output.GBuffer4 = inputData.positionCS.z;
    #endif

    return output;
}

#endif
