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

Shader "Honkai Star Rail/Character/Body (Transparent)"
{
    Properties
    {
        [KeywordEnum(Game, MMD)] _Model("Model Type", Float) = 0
        _ModelScale("Model Scale", Float) = 1
        [HSRMaterialIDSelector] _SingleMaterialID("Material ID", Float) = -1

        [HeaderFoldout(Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0                       // 默认 Off
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendColor("Src Blend (RGB)", Float) = 5  // 默认 SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendColor("Dst Blend (RGB)", Float) = 10 // 默认 OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0    // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0    // 默认 Zero

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
        [If(_MODEL_GAME)] [Toggle] _BackFaceUV2("Back Face Use UV2", Float) = 0

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
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "ComplexLit" // Packages/com.unity.render-pipelines.universal/Runtime/Passes/GBufferPass.cs: Fill GBuffer, but skip lighting pass for ComplexLit
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "BodyTransparent"

            Tags
            {
                "LightMode" = "HSRTransparent"
            }

            // 透明部分和角色的 Stencil
            Stencil
            {
                Ref 5
                WriteMask 5  // 透明和角色位
                Comp Always
                Pass Replace // 写入透明和角色位
                Fail Keep
            }

            Cull [_Cull]
            ZWrite Off

            Blend 0 [_SrcBlendColor] [_DstBlendColor], [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex BodyVertex
            #pragma fragment BodyColorFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            // #pragma shader_feature_local_fragment _ _ALPHATEST_ON
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

            #define CHAR_BODY_SHADER_TRANSPARENT
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

            Stencil
            {
                Ref 5
                ReadMask 4    // 透明位
                WriteMask 1   // 角色位
                Comp NotEqual // 不透明部分
                Pass Replace  // 写入角色位
                Fail Keep
            }

            Cull Front
            ZTest LEqual
            ZWrite Off

            Blend 0 [_SrcBlendColor] [_DstBlendColor], [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex BodyOutlineVertex
            #pragma fragment BodyOutlineFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            // #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SINGLEMATERIAL_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma shader_feature_local_vertex _OUTLINENORMAL_TANGENT _OUTLINENORMAL_NORMAL

            #pragma multi_compile_fog

            #define CHAR_BODY_SHADER_TRANSPARENT
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
            ZWrite On // 写入 Shadow Map
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma target 2.0

            #pragma vertex BodyShadowVertex
            #pragma fragment BodyShadowFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            // #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_vertex _ _CASTING_SELF_SHADOW

            #define CHAR_BODY_SHADER_TRANSPARENT
            #include "CharBodyCore.hlsl"

            ENDHLSL
        }

        // No Depth
        // No Motion Vectors
    }

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
