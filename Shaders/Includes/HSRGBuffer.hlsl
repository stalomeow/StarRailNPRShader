#ifndef _HSR_G_BUFFER_INCLUDED
#define _HSR_G_BUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_HSRGBuffer0);

float4 HSRLoadGBuffer0(uint2 coord)
{
    return LOAD_TEXTURE2D(_HSRGBuffer0, coord);
}

float4 HSRSampleGBuffer0(float2 uv)
{
    return SAMPLE_TEXTURE2D(_HSRGBuffer0, sampler_PointClamp, uv);
}

#endif
