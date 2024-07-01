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
        _Color("Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Self Shadow Caster)]
        _SelfShadowDepthBias("Depth Bias", Float) = -0.01
        _SelfShadowNormalBias("Normal Bias", Float) = 0

        [HideInInspector] _DitherAlpha("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "ComplexLit" // Packages/com.unity.render-pipelines.universal/Runtime/Passes/GBufferPass.cs: Fill GBuffer, but skip lighting pass for ComplexLit
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Shared/CharCore.hlsl"
            #include "Shared/CharRenderingHelpers.hlsl"
            #include "Shared/CharMotionVectors.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _SelfShadowDepthBias;
                float _SelfShadowNormalBias;
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

            HLSLPROGRAM

            #pragma multi_compile_fog

            #pragma vertex vert
            #pragma fragment frag

            CharCoreVaryings vert(CharCoreAttributes i)
            {
                return CharCoreVertex(i, 0);
            }

            void frag(CharCoreVaryings i,
                out float4 colorTarget : SV_Target0)
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

                colorTarget = _Color;

                // Fog
                real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
                colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);
            }

            ENDHLSL
        }

        Pass
        {
            Name "FaceMaskShadow"

            Tags
            {
                "LightMode" = "HSRPerObjectShadowCaster"
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
            #pragma multi_compile_vertex _ _CASTING_SELF_SHADOW

            #include "Shared/CharShadow.hlsl"

            CharShadowVaryings FaceMaskShadowVertex(CharShadowAttributes i)
            {
                return CharShadowVertex(i, 0, _SelfShadowDepthBias, _SelfShadowNormalBias);
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

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
