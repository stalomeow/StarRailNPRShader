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

Shader "Honkai Star Rail/Character/Body"
{
    Properties
    {
        [KeywordEnum(Game, MMD)] _Model("Model Type", Float) = 0
        _ModelScale("Model Scale", Float) = 1

        [HeaderFoldout(Shader Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0                      // 默认 Off
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendColor("Src Blend (RGB)", Float) = 1 // 默认 One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendColor("Dst Blend (RGB)", Float) = 0 // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0   // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0   // 默认 Zero
        [Space(5)]
        [Toggle] _AlphaTest("Alpha Test", Float) = 0
        [If(_ALPHATEST_ON)] [Indent] _AlphaTestThreshold("Threshold", Range(0, 1)) = 0.5
        [Header(Debug)] [Space(5)]
        [Toggle] _SingleMaterial("Show Part", Float) = 0
        [If(_SINGLEMATERIAL_ON)] [Indent] [IntRange] _SingleMaterialID("Material Index", Range(-1, 7)) = -1

        [HeaderFoldout(Maps)]
        [SingleLineTextureNoScaleOffset(_Color)] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1, 1, 1, 1)
        [SingleLineTextureNoScaleOffset] _LightMap("Light Map", 2D) = "white" {}
        [TextureScaleOffset] _Maps_ST("Maps Scale Offset", Vector) = (1, 1, 0, 0)
        [Header(Overrides)] [Space(5)]
        [If(_MODEL_GAME)] _BackColor("Back Face Color", Color) = (1, 1, 1, 1)
        [If(_MODEL_GAME)] [Toggle] _BackFaceUV2("Back Face Use UV2", Float) = 0

        [HeaderFoldout(Diffuse)]
        [RampTexture] _RampMapCool("Ramp (Cool)", 2D) = "white" {}
        [RampTexture] _RampMapWarm("Ramp (Warm)", 2D) = "white" {}
        _RampCoolWarmLerpFactor("Cool / Warm", Range(0, 1)) = 1

        [HeaderFoldout(Specular)]
        [HSRMaterialIDFoldout] _SpecularColor("Color", Float) = 0
        [HSRMaterialIDProperty(_SpecularColor, 0)] _SpecularColor0("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 31)] _SpecularColor1("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 63)] _SpecularColor2("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 95)] _SpecularColor3("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 127)] _SpecularColor4("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 159)] _SpecularColor5("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 192)] _SpecularColor6("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 223)] _SpecularColor7("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDFoldout] _SpecularMetallic("Metallic", Float) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 0)] _SpecularMetallic0("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 31)] _SpecularMetallic1("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 63)] _SpecularMetallic2("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 95)] _SpecularMetallic3("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 127)] _SpecularMetallic4("Specular Metallic", Range(0, 1)) = 1 // 一般情况下是金属
        [HSRMaterialIDProperty(_SpecularMetallic, 159)] _SpecularMetallic5("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 192)] _SpecularMetallic6("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDProperty(_SpecularMetallic, 223)] _SpecularMetallic7("Specular Metallic", Range(0, 1)) = 0
        [HSRMaterialIDFoldout] _SpecularShininess("Shininess", Float) = 0
        [HSRMaterialIDProperty(_SpecularShininess, 0)] _SpecularShininess0("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 31)] _SpecularShininess1("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 63)] _SpecularShininess2("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 95)] _SpecularShininess3("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 127)] _SpecularShininess4("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 159)] _SpecularShininess5("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 192)] _SpecularShininess6("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 223)] _SpecularShininess7("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDFoldout] _SpecularIntensity("Intensity", Float) = 0
        [HSRMaterialIDProperty(_SpecularIntensity, 0)] _SpecularIntensity0("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 31)] _SpecularIntensity1("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 63)] _SpecularIntensity2("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 95)] _SpecularIntensity3("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 127)] _SpecularIntensity4("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 159)] _SpecularIntensity5("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 192)] _SpecularIntensity6("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 223)] _SpecularIntensity7("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDFoldout] _SpecularEdgeSoftness("Edge Softness", Float) = 0
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 0)] _SpecularEdgeSoftness0("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 31)] _SpecularEdgeSoftness1("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 63)] _SpecularEdgeSoftness2("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 95)] _SpecularEdgeSoftness3("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 127)] _SpecularEdgeSoftness4("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 159)] _SpecularEdgeSoftness5("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 192)] _SpecularEdgeSoftness6("Specular Edge Softness", Range(0, 1)) = 0.1
        [HSRMaterialIDProperty(_SpecularEdgeSoftness, 223)] _SpecularEdgeSoftness7("Specular Edge Softness", Range(0, 1)) = 0.1

        [HeaderFoldout(Stockings)]
        [PostHelpBox(Warning, DO NOT forget to set the Tiling.)]
        _StockingsMap("Range Texture", 2D) = "black" {}
        _StockingsColor("Stockings Color", Color) = (1, 1, 1, 1)
        _StockingsColorDark("Dark Rim Color", Color) = (1, 1, 1, 1)
        _StockingsDarkWidth("Dark Rim Width", Range(0, 0.96)) = 0.5
        _StockingsPower("Stockings Power", Range(0.04, 1)) = 1
        _StockingsLightedWidth("Lighted Width", Range(1, 32)) = 1
        _StockingsLightedIntensity("Lighted Intensity", Range(0, 1)) = 0.25
        _StockingsRoughness("Roughness", Range(0, 1)) = 1

        [HeaderFoldout(Emission, Use Albedo.a as emission map)]
        _EmissionColor("Color", Color) = (1, 1, 1, 1)
        _EmissionThreshold("Threshold", Range(0, 1)) = 1
        _EmissionIntensity("Intensity", Float) = 0

        [HeaderFoldout(Bloom)]
        [HSRMaterialIDFoldout] _BloomIntensity("Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 0)] _BloomIntensity0("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 31)] _BloomIntensity1("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 63)] _BloomIntensity2("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 95)] _BloomIntensity3("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 127)] _BloomIntensity4("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 159)] _BloomIntensity5("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 192)] _BloomIntensity6("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_BloomIntensity, 223)] _BloomIntensity7("Bloom Intensity", Range(0, 1)) = 0.5
        [HSRMaterialIDFoldout] _BloomColor("Color", Float) = 0
        [HSRMaterialIDProperty(_BloomColor, 0)] _BloomColor0("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 31)] _BloomColor1("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 63)] _BloomColor2("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 95)] _BloomColor3("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 127)] _BloomColor4("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 159)] _BloomColor5("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 192)] _BloomColor6("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 223)] _BloomColor7("Bloom Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Rim Light)]
        _RimIntensity("Intensity (Front)", Range(0, 1)) = 0.5
        _RimIntensityBackFace("Intensity (Back)", Range(0, 1)) = 0
        _RimThresholdMin("Threshold Min", Float) = 0.6
        _RimThresholdMax("Threshold Max", Float) = 0.9
        _RimEdgeSoftness("Edge Softness", Float) = 0.05
        [HSRMaterialIDFoldout] _RimWidth("Width", Float) = 0
        [HSRMaterialIDProperty(_RimWidth, 0)] _RimWidth0("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 31)] _RimWidth1("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 63)] _RimWidth2("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 95)] _RimWidth3("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 127)] _RimWidth4("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 159)] _RimWidth5("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 192)] _RimWidth6("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 223)] _RimWidth7("Rim Width", Range(0, 1)) = 0.3
        [HSRMaterialIDFoldout] _RimColor("Color", Float) = 0
        [HSRMaterialIDProperty(_RimColor, 0)] _RimColor0("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 31)] _RimColor1("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 63)] _RimColor2("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 95)] _RimColor3("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 127)] _RimColor4("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 159)] _RimColor5("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 192)] _RimColor6("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 223)] _RimColor7("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDFoldout] _RimDark("Darken Value", Float) = 0
        [HSRMaterialIDProperty(_RimDark, 0)] _RimDark0("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 31)] _RimDark1("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 63)] _RimDark2("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 95)] _RimDark3("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 127)] _RimDark4("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 159)] _RimDark5("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 192)] _RimDark6("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 223)] _RimDark7("Rim Darken Value", Range(0, 1)) = 0.5

        [HeaderFoldout(Outline)]
        [KeywordEnum(Tangent, Normal)] _OutlineNormal("Normal Source", Float) = 0
        _OutlineWidth("Width", Range(0, 4)) = 1
        _OutlineZOffset("Z Offset", Float) = 0
        [HSRMaterialIDFoldout] _OutlineColor("Color", Float) = 0
        [HSRMaterialIDProperty(_OutlineColor, 0)] _OutlineColor0("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 31)] _OutlineColor1("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 63)] _OutlineColor2("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 95)] _OutlineColor3("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 127)] _OutlineColor4("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 159)] _OutlineColor5("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 192)] _OutlineColor6("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 223)] _OutlineColor7("Outline Color", Color) = (0, 0, 0, 1)

        [HeaderFoldout(Dither)]
        _DitherAlpha("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry+30"  // 身体默认 +30，放在最后渲染
        }

        Pass
        {
            Name "BodyOpaque"

            Tags
            {
                // 在头发的两个 Pass 之后绘制，避免干扰透明刘海对眼睛区域的判断
                "LightMode" = "HSRForward3"
            }

            // 角色的 Stencil
            Stencil
            {
                Ref 1
                WriteMask 1
                Comp Always
                Pass Replace
                Fail Keep
            }

            Cull [_Cull]
            ZWrite On

            Blend 0 [_SrcBlendColor] [_DstBlendColor], [_SrcBlendAlpha] [_DstBlendAlpha]
            Blend 1 One Zero

            ColorMask RGBA 0
            ColorMask RGBA 1

            HLSLPROGRAM

            #pragma vertex BodyVertex
            #pragma fragment BodyColorFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SINGLEMATERIAL_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "BodyOutline"

            Tags
            {
                "LightMode" = "HSROutline"
            }

            // 角色的 Stencil
            Stencil
            {
                Ref 1
                WriteMask 1
                Comp Always
                Pass Replace
                Fail Keep
            }

            Cull Front
            ZTest LEqual
            ZWrite On

            Blend 0 [_SrcBlendColor] [_DstBlendColor], [_SrcBlendAlpha] [_DstBlendAlpha]
            Blend 1 Zero Zero

            ColorMask RGBA 0
            ColorMask 0 1

            HLSLPROGRAM

            #pragma vertex BodyOutlineVertex
            #pragma fragment BodyOutlineFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SINGLEMATERIAL_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma shader_feature_local_vertex _OUTLINENORMAL_TANGENT _OUTLINENORMAL_NORMAL

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "PerObjectShadow"

            Tags
            {
                "LightMode" = "HSRShadowCaster"
            }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma target 2.0

            #pragma vertex BodyShadowVertex
            #pragma fragment BodyShadowFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "BodyDepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex BodyDepthOnlyVertex
            #pragma fragment BodyDepthOnlyFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "BodyDepthNormals"

            Tags
            {
                "LightMode" = "DepthNormals"
            }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM

            #pragma vertex BodyDepthNormalsVertex
            #pragma fragment BodyDepthNormalsFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "StaloSRPShaderGUI"
    Fallback Off
}
