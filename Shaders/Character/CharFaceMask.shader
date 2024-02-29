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

Shader "Honkai Star Rail/Character/FaceMask"
{
    Properties
    {
        _DitherAlpha("Dither Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Shared/CharRenderingHelpers.hlsl"
            #include "Shared/CharMotionVectors.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DitherAlpha;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "FaceMaskStencilClear"

            Tags
            {
                "LightMode" = "HSRForward1"
            }

            // 清除角色 Stencil
            Stencil
            {
                Ref 0
                WriteMask 7 // 后三位
                Comp Always
                Pass Zero
                Fail Keep
            }

            Cull Off
            ZWrite On

            ColorMask RGBA 0
            ColorMask 0 1

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            float4 frag(float4 positionHCS : SV_POSITION) : SV_Target0
            {
                DoDitherAlphaEffect(positionHCS, _DitherAlpha);
                return 1;
            }

            ENDHLSL
        }

        Pass
        {
            Name "PerObjectShadow"

            Tags
            {
                "LightMode" = "HSRShadowCaster"
            }

            Cull Off
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma target 2.0

            #pragma vertex FaceMaskShadowVertex
            #pragma fragment FaceMaskShadowFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Shared/CharShadow.hlsl"

            CharShadowVaryings FaceMaskShadowVertex(CharShadowAttributes i)
            {
                return CharShadowVertex(i, 0);
            }

            void FaceMaskShadowFragment(CharShadowVaryings i)
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
            }

            ENDHLSL
        }

        Pass
        {
            Name "FaceMaskDepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex FaceMaskDepthOnlyVertex
            #pragma fragment FaceMaskDepthOnlyFragment

            #include "Shared/CharDepthOnly.hlsl"

            CharDepthOnlyVaryings FaceMaskDepthOnlyVertex(CharDepthOnlyAttributes i)
            {
                return CharDepthOnlyVertex(i, 0);
            }

            float4 FaceMaskDepthOnlyFragment(CharDepthOnlyVaryings i) : SV_Target
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
                return CharDepthOnlyFragment(i);
            }

            ENDHLSL
        }

        Pass
        {
            Name "FaceMaskDepthNormals"

            Tags
            {
                "LightMode" = "DepthNormals"
            }

            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex FaceMaskDepthNormalsVertex
            #pragma fragment FaceMaskDepthNormalsFragment

            #include "Shared/CharDepthNormals.hlsl"

            CharDepthNormalsVaryings FaceMaskDepthNormalsVertex(CharDepthNormalsAttributes i)
            {
                return CharDepthNormalsVertex(i, 0);
            }

            float4 FaceMaskDepthNormalsFragment(CharDepthNormalsVaryings i) : SV_Target
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
                return CharDepthNormalsFragment(i);
            }

            ENDHLSL
        }

        Pass
        {
            Name "FaceMaskMotionVectors"

            Tags
            {
                "LightMode" = "MotionVectors"
            }

            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            CharMotionVectorsVaryings vert(CharMotionVectorsAttributes i)
            {
                return CharMotionVectorsVertex(i, 0);
            }

            half4 frag(CharMotionVectorsVaryings i) : SV_Target
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);
                return CharMotionVectorsFragment(i);
            }

            ENDHLSL
        }

        // No Outline
    }

    CustomEditor "StaloSRPShaderGUI"
    Fallback Off
}
