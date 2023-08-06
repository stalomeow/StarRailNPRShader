#ifndef _CHARACTER_DUAL_FACE_INCLUDED
#define _CHARACTER_DUAL_FACE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float4 CombineAndTransformDualFaceUV(float2 uv1, float2 uv2, float4 mapST)
{
    return float4(uv1, uv2) * mapST.xyxy + mapST.zwzw;
}

void ValidateDualFaceVaryings(inout float3 normalWS, inout float4 uv, FRONT_FACE_TYPE isFrontFace)
{
    // 游戏内的部分模型用了双面渲染

    #if defined(_MODEL_GAME)
        if (IS_FRONT_VFACE(isFrontFace, 1, 0))
            return;

        normalWS *= -1;

        #if defined(_BACKFACEUV2_ON)
            uv.xyzw = uv.zwxy; // Swap two uv
        #endif
    #endif
}

#endif
