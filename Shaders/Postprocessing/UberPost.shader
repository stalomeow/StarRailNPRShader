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

Shader "Hidden/Honkai Star Rail/Post Processing/UberPost"
{
    Properties
    {
        _BloomIntensity("Bloom Intensity", Float) = 0
        _BloomTint("Bloom Tint", Color) = (1, 1, 1, 1)
        _BloomTexture("Bloom Texture", 2D) = "black" {}

        _ACESParamA("ACES Param A", Float) = 2.80
        _ACESParamB("ACES Param B", Float) = 0.40
        _ACESParamC("ACES Param C", Float) = 2.10
        _ACESParamD("ACES Param D", Float) = 0.50
        _ACESParamE("ACES Param E", Float) = 1.50
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local_fragment _ _BLOOM
            #pragma multi_compile_local_fragment _ _BLOOM_USE_RGBM
            #pragma multi_compile_local_fragment _ _TONEMAPPING_ACES
            #pragma multi_compile_local_fragment _ _USE_FAST_SRGB_LINEAR_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BloomIntensity;
                float4 _BloomTint;

                float _ACESParamA;
                float _ACESParamB;
                float _ACESParamC;
                float _ACESParamD;
                float _ACESParamE;
            CBUFFER_END

            #if defined(_BLOOM)
                TEXTURE2D(_BloomTexture);
            #endif

            float3 CustomACESTonemapping(float3 x)
            {
                float3 u = _ACESParamA * x + _ACESParamB;
                float3 v = _ACESParamC * x + _ACESParamD;
                return saturate((x * u) / (x * v + _ACESParamE));
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float4 color = FragBlit(i, sampler_PointClamp);

                // Gamma space... Just do the rest of Uber in linear and convert back to sRGB at the end
                #if UNITY_COLORSPACE_GAMMA
                    color = GetSRGBToLinear(color);
                #endif

                #if defined(_BLOOM)
                    float2 bloomUV = i.texcoord;

                    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
                        UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
                        {
                            bloomUV = RemapFoveatedRenderingNonUniformToLinear(bloomUV);
                        }
                    #endif

                    float4 bloom = SAMPLE_TEXTURE2D(_BloomTexture, sampler_LinearClamp, bloomUV);

                    #if UNITY_COLORSPACE_GAMMA
                        bloom.xyz *= bloom.xyz; // γ to linear
                    #endif

                    #if defined(_BLOOM_USE_RGBM)
                        color.rgb += DecodeRGBM(bloom) * _BloomTint.rgb * _BloomIntensity;
                    #else
                        color.rgb += bloom.rgb * _BloomTint.rgb * _BloomIntensity;
                    #endif
                #endif

                #if defined(_TONEMAPPING_ACES)
                    color.rgb = CustomACESTonemapping(color.rgb);
                #endif

                // Back to sRGB
                #if UNITY_COLORSPACE_GAMMA
                    color = GetLinearToSRGB(color);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
