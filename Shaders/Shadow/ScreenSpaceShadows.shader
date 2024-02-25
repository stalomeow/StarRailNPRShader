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

Shader "Hidden/Honkai Star Rail/Shadow/ScreenSpaceShadows"
{
    Properties
    {
        // [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ScreenSpaceShadows"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #pragma vertex   Vert
            #pragma fragment Fragment

            //Keep compiler quiet about Shadows.hlsl.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MAX_PER_OBJECT_SHADOW_COUNT 16

            TEXTURE2D_SHADOW(_PerObjShadowMap);
            SAMPLER_CMP(sampler_PerObjShadowMap);

            int _PerObjShadowCount;
            float4x4 _PerObjShadowMatrices[MAX_PER_OBJECT_SHADOW_COUNT];
            float4 _PerObjShadowMapRects[MAX_PER_OBJECT_SHADOW_COUNT];

            float4 _PerObjShadowOffset0;
            float4 _PerObjShadowOffset1;
            float4 _PerObjShadowMapSize;

            ShadowSamplingData GetPerObjectShadowSamplingData()
            {
                ShadowSamplingData shadowSamplingData;

                // shadowOffsets are used in SampleShadowmapFiltered for low quality soft shadows.
                shadowSamplingData.shadowOffset0 = _PerObjShadowOffset0;
                shadowSamplingData.shadowOffset1 = _PerObjShadowOffset1;

                // shadowmapSize is used in SampleShadowmapFiltered otherwise
                shadowSamplingData.shadowmapSize = _PerObjShadowMapSize;
                shadowSamplingData.softShadowQuality = _MainLightShadowParams.y;

                return shadowSamplingData;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if UNITY_REVERSED_Z
                float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
    #else
                float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
                deviceDepth = deviceDepth * 2.0 - 1.0;
    #endif

                // Fetch shadow coordinates for cascade.
                float3 wpos = ComputeWorldSpacePosition(input.texcoord.xy, deviceDepth, unity_MatrixInvVP);
                float4 coords = TransformWorldToShadowCoord(wpos);

                // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
                half realtimeShadow = MainLightRealtimeShadow(coords);

                ShadowSamplingData shadowSamplingData = GetPerObjectShadowSamplingData();
                half4 shadowParams = GetMainLightShadowParams();

                // PerObjectShadow
                for (int i = 0; i < _PerObjShadowCount; i++)
                {
                    float4 shadowPos = mul(_PerObjShadowMatrices[i], float4(wpos, 1));
                    float4 rects = _PerObjShadowMapRects[i];

                    if (shadowPos.x >= rects.x && shadowPos.x <= rects.y && shadowPos.y >= rects.z && shadowPos.y <= rects.w)
                    {
                        realtimeShadow = min(realtimeShadow, SampleShadowmap(
                            TEXTURE2D_SHADOW_ARGS(_PerObjShadowMap,sampler_PerObjShadowMap),
                            shadowPos,
                            shadowSamplingData,
                            shadowParams,
                            false));
                    }
                }

                return realtimeShadow;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
