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
        [HSRMaterialIDSelector] _SingleMaterialID("Material ID", Float) = -1

        [HeaderFoldout(Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0                      // 默认 Off
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendColor("Src Blend (RGB)", Float) = 1 // 默认 One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendColor("Dst Blend (RGB)", Float) = 0 // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0   // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0   // 默认 Zero
        [Toggle] _AlphaTest("Alpha Test", Float) = 0
        [If(_ALPHATEST_ON)] [Indent] _AlphaTestThreshold("Threshold", Range(0, 1)) = 0.5

        [HeaderFoldout(Maps)]
        [SingleLineTextureNoScaleOffset(_Color)] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1, 1, 1, 1)
        [SingleLineTextureNoScaleOffset] _LightMap("Light Map", 2D) = "white" {}
        [TextureScaleOffset] _Maps_ST("Maps Scale Offset", Vector) = (1, 1, 0, 0)
        [Header(Ramps)] [Space(5)]
        [RampTexture] _RampMapCool("Cool", 2D) = "white" {}
        [RampTexture] _RampMapWarm("Warm", 2D) = "white" {}
        [Header(Overrides)] [Space(5)]
        [If(_MODEL_GAME)] _BackColor("Back Face Color", Color) = (1, 1, 1, 1)
        [If(_MODEL_GAME)] [Toggle] _BackFaceUV2("Back Face Use UV2", Float) = 1

        [HeaderFoldout(Specular)]
        [HSRMaterialIDFoldout] _SpecularColor("Color", Float) = 0
        [HSRMaterialIDProperty(_SpecularColor, 0)] _SpecularColor0("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 1)] _SpecularColor1("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 2)] _SpecularColor2("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 3)] _SpecularColor3("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 4)] _SpecularColor4("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 5)] _SpecularColor5("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 6)] _SpecularColor6("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_SpecularColor, 7)] _SpecularColor7("Specular Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDFoldout] _SpecularShininess("Shininess", Float) = 0
        [HSRMaterialIDProperty(_SpecularShininess, 0)] _SpecularShininess0("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 1)] _SpecularShininess1("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 2)] _SpecularShininess2("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 3)] _SpecularShininess3("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 4)] _SpecularShininess4("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 5)] _SpecularShininess5("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 6)] _SpecularShininess6("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDProperty(_SpecularShininess, 7)] _SpecularShininess7("Specular Shininess", Range(0.1, 500)) = 10
        [HSRMaterialIDFoldout] _SpecularIntensity("Intensity", Float) = 0
        [HSRMaterialIDProperty(_SpecularIntensity, 0)] _SpecularIntensity0("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 1)] _SpecularIntensity1("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 2)] _SpecularIntensity2("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 3)] _SpecularIntensity3("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 4)] _SpecularIntensity4("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 5)] _SpecularIntensity5("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 6)] _SpecularIntensity6("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDProperty(_SpecularIntensity, 7)] _SpecularIntensity7("Specular Intensity", Range(0, 100)) = 1
        [HSRMaterialIDFoldout] _SpecularRoughness("Roughness", Float) = 0
        [HSRMaterialIDProperty(_SpecularRoughness, 0)] _SpecularRoughness0("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 1)] _SpecularRoughness1("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 2)] _SpecularRoughness2("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 3)] _SpecularRoughness3("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 4)] _SpecularRoughness4("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 5)] _SpecularRoughness5("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 6)] _SpecularRoughness6("Specular Roughness", Range(0, 1)) = 0.02
        [HSRMaterialIDProperty(_SpecularRoughness, 7)] _SpecularRoughness7("Specular Roughness", Range(0, 1)) = 0.02

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
        [HSRMaterialIDProperty(_BloomIntensity, 0)] _mmBloomIntensity0("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 1)] _mmBloomIntensity1("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 2)] _mmBloomIntensity2("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 3)] _mmBloomIntensity3("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 4)] _mmBloomIntensity4("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 5)] _mmBloomIntensity5("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 6)] _mmBloomIntensity6("Bloom Intensity", Float) = 0
        [HSRMaterialIDProperty(_BloomIntensity, 7)] _mmBloomIntensity7("Bloom Intensity", Float) = 0
        [HSRMaterialIDFoldout] _BloomColor("Color", Float) = 0
        [HSRMaterialIDProperty(_BloomColor, 0)] _BloomColor0("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 1)] _BloomColor1("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 2)] _BloomColor2("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 3)] _BloomColor3("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 4)] _BloomColor4("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 5)] _BloomColor5("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 6)] _BloomColor6("Bloom Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_BloomColor, 7)] _BloomColor7("Bloom Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Rim Light)]
        _RimIntensity("Intensity (Front Main)", Float) = 0.5
        _RimIntensityAdditionalLight("Intensity (Front Additional)", Float) = 0.5
        _RimIntensityBackFace("Intensity (Back Main)", Float) = 0
        _RimIntensityBackFaceAdditionalLight("Intensity (Back Additional)", Float) = 0
        [HSRMaterialIDFoldout] _RimWidth("Width", Float) = 0
        [HSRMaterialIDProperty(_RimWidth, 0)] _RimWidth0("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 1)] _RimWidth1("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 2)] _RimWidth2("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 3)] _RimWidth3("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 4)] _RimWidth4("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 5)] _RimWidth5("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 6)] _RimWidth6("Rim Width", Float) = 0.3
        [HSRMaterialIDProperty(_RimWidth, 7)] _RimWidth7("Rim Width", Float) = 0.3
        [HSRMaterialIDFoldout] _RimColor("Color", Float) = 0
        [HSRMaterialIDProperty(_RimColor, 0)] _RimColor0("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 1)] _RimColor1("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 2)] _RimColor2("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 3)] _RimColor3("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 4)] _RimColor4("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 5)] _RimColor5("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 6)] _RimColor6("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDProperty(_RimColor, 7)] _RimColor7("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HSRMaterialIDFoldout] _RimDark("Darken Value", Float) = 0
        [HSRMaterialIDProperty(_RimDark, 0)] _RimDark0("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 1)] _RimDark1("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 2)] _RimDark2("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 3)] _RimDark3("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 4)] _RimDark4("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 5)] _RimDark5("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 6)] _RimDark6("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDProperty(_RimDark, 7)] _RimDark7("Rim Darken Value", Range(0, 1)) = 0.5
        [HSRMaterialIDFoldout] _RimEdgeSoftness("Edge Softness", Float) = 0
        [HSRMaterialIDProperty(_RimEdgeSoftness, 0)] _RimEdgeSoftness0("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 1)] _RimEdgeSoftness1("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 2)] _RimEdgeSoftness2("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 3)] _RimEdgeSoftness3("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 4)] _RimEdgeSoftness4("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 5)] _RimEdgeSoftness5("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 6)] _RimEdgeSoftness6("Rim Edge Softness", Float) = 0.05
        [HSRMaterialIDProperty(_RimEdgeSoftness, 7)] _RimEdgeSoftness7("Rim Edge Softness", Float) = 0.05

        [HeaderFoldout(Rim Shadow)]
        _RimShadowCt("Ct", Float) = 1
        _RimShadowIntensity("Intensity", Float) = 1
        _RimShadowOffset("Offset", Vector) = (0, 0, 0, 0)
        [HSRMaterialIDFoldout] _RimShadowColor("Color", Float) = 0
        [HSRMaterialIDProperty(_RimShadowColor, 0)] _RimShadowColor0("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 1)] _RimShadowColor1("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 2)] _RimShadowColor2("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 3)] _RimShadowColor3("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 4)] _RimShadowColor4("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 5)] _RimShadowColor5("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 6)] _RimShadowColor6("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDProperty(_RimShadowColor, 7)] _RimShadowColor7("Rim Shadow Color", Color) = (1, 1, 1, 1)
        [HSRMaterialIDFoldout] _RimShadowWidth("Width", Float) = 0
        [HSRMaterialIDProperty(_RimShadowWidth, 0)] _RimShadowWidth0("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 1)] _RimShadowWidth1("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 2)] _RimShadowWidth2("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 3)] _RimShadowWidth3("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 4)] _RimShadowWidth4("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 5)] _RimShadowWidth5("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 6)] _RimShadowWidth6("Rim Shadow Width", Float) = 1
        [HSRMaterialIDProperty(_RimShadowWidth, 7)] _RimShadowWidth7("Rim Shadow Width", Float) = 1
        [HSRMaterialIDFoldout] _RimShadowFeather("Feather", Float) = 0
        [HSRMaterialIDProperty(_RimShadowFeather, 0)] _RimShadowFeather0("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 1)] _RimShadowFeather1("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 2)] _RimShadowFeather2("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 3)] _RimShadowFeather3("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 4)] _RimShadowFeather4("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 5)] _RimShadowFeather5("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 6)] _RimShadowFeather6("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01
        [HSRMaterialIDProperty(_RimShadowFeather, 7)] _RimShadowFeather7("Rim Shadow Feather", Range(0.01, 0.99)) = 0.01

        [HeaderFoldout(Outline)]
        [KeywordEnum(Tangent, Normal)] _OutlineNormal("Normal Source", Float) = 0
        _OutlineWidth("Width", Range(0, 4)) = 1
        _OutlineZOffset("Z Offset", Float) = 0
        [HSRMaterialIDFoldout] _OutlineColor("Color", Float) = 0
        [HSRMaterialIDProperty(_OutlineColor, 0)] _OutlineColor0("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 1)] _OutlineColor1("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 2)] _OutlineColor2("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 3)] _OutlineColor3("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 4)] _OutlineColor4("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 5)] _OutlineColor5("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 6)] _OutlineColor6("Outline Color", Color) = (0, 0, 0, 1)
        [HSRMaterialIDProperty(_OutlineColor, 7)] _OutlineColor7("Outline Color", Color) = (0, 0, 0, 1)

        [HeaderFoldout(Self Shadow Caster)]
        _SelfShadowDepthBias("Depth Bias", Float) = -0.01
        _SelfShadowNormalBias("Normal Bias", Float) = 0

        [HideInInspector] _RampCoolWarmLerpFactor("Cool / Warm", Range(0, 1)) = 1
        [HideInInspector] _DitherAlpha("Alpha", Range(0, 1)) = 1
        [HideInInspector] _PerObjShadowCasterId("Per Object Shadow Caster Id", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "ComplexLit" // Packages/com.unity.render-pipelines.universal/Runtime/Passes/GBufferPass.cs: Fill GBuffer, but skip lighting pass for ComplexLit
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

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex BodyVertex
            #pragma fragment BodyColorFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SINGLEMATERIAL_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_fog

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SELF_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

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

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex BodyOutlineVertex
            #pragma fragment BodyOutlineFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SINGLEMATERIAL_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma shader_feature_local_vertex _OUTLINENORMAL_TANGENT _OUTLINENORMAL_NORMAL

            #pragma multi_compile_fog

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "BodyShadow"

            Tags
            {
                "LightMode" = "HSRPerObjectShadowCaster"
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
            #pragma multi_compile_vertex _ _CASTING_SELF_SHADOW

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

        Pass
        {
            Name "BodyMotionVectors"

            Tags
            {
                "LightMode" = "MotionVectors"
            }

            Cull [_Cull]

            HLSLPROGRAM

            #pragma vertex BodyMotionVectorsVertex
            #pragma fragment BodyMotionVectorsFragment

            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #include "CharBodyCore.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
