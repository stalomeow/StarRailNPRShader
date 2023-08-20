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

Shader "Hidden/Honkai Star Rail/Post Processing/Bloom"
{
    Properties
    {
        _BloomThreshold("Bloom Threshold", Vector) = (0.3, 0.3, 0.3, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "../Includes/HSRGBuffer.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BloomThreshold;
        CBUFFER_END;
        ENDHLSL

        Pass
        {
            Name "Highlight"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings i) : SV_Target
            {
                float3 color = FragBlit(i, sampler_LinearClamp).rgb;
                float intensity = HSRSampleGBuffer0(i.texcoord).r;
                color = max(0, color * intensity - _BloomThreshold.rgb);
                return float4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float _BloomScatter;
            float2 _BlitTexture_TexelSize;

            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                static float weight[3] = { 0.4026, 0.2442, 0.0545 };
                float scatter = _BloomScatter * _ScaledScreenParams.y / 1080.0; // 适应不同分辨率

                float2 uv[5] =
                {
                    i.texcoord,
                    i.texcoord + float2(0, _BlitTexture_TexelSize.y * 1.0) * scatter,
                    i.texcoord - float2(0, _BlitTexture_TexelSize.y * 1.0) * scatter,
                    i.texcoord + float2(0, _BlitTexture_TexelSize.y * 2.0) * scatter,
                    i.texcoord - float2(0, _BlitTexture_TexelSize.y * 2.0) * scatter
                };

                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[0], _BlitMipLevel) * weight[0];

                UNITY_UNROLL
                for (int j = 1; j < 3; j++)
                {
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[j * 2 - 1], _BlitMipLevel) * weight[j];
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[j * 2    ], _BlitMipLevel) * weight[j];
                }

                return float4(color.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float _BloomScatter;
            float2 _BlitTexture_TexelSize;

            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                static float weight[3] = { 0.4026, 0.2442, 0.0545 };
                float scatter = _BloomScatter * _ScaledScreenParams.x / 1920.0; // 适应不同分辨率

                float2 uv[5] =
                {
                    i.texcoord,
                    i.texcoord + float2(_BlitTexture_TexelSize.x * 1.0, 0) * scatter,
                    i.texcoord - float2(_BlitTexture_TexelSize.x * 1.0, 0) * scatter,
                    i.texcoord + float2(_BlitTexture_TexelSize.x * 2.0, 0) * scatter,
                    i.texcoord - float2(_BlitTexture_TexelSize.x * 2.0, 0) * scatter
                };

                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[0], _BlitMipLevel) * weight[0];

                UNITY_UNROLL
                for (int j = 1; j < 3; j++)
                {
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[j * 2 - 1], _BlitMipLevel) * weight[j];
                    color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv[j * 2    ], _BlitMipLevel) * weight[j];
                }

                return float4(color.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Add"

            Blend One One
            BlendOp Add
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings i) : SV_Target
            {
                return FragBlit(i, sampler_LinearClamp);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
