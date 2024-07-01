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

Shader "Honkai Star Rail/Character/Hair"
{
    Properties
    {
        [KeywordEnum(Game, MMD)] _Model("Model Type", Float) = 0
        _ModelScale("Model Scale", Float) = 1

        [HeaderFoldout(Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0                    // 默认 Off
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0 // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0 // 默认 Zero
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
        [If(_MODEL_GAME)] [Toggle] _BackFaceUV2("Back Face Use UV2", Float) = 0

        [HeaderFoldout(Transparent Fron Hair)]
        _HairBlendAlpha("Alpha", Range(0, 1)) = 0.6

        [HeaderFoldout(Specular)]
        _SpecularColor0("Color", Color) = (1,1,1,1)
        _SpecularShininess0("Shininess", Range(0.1, 500)) = 10
        _SpecularIntensity0("Intensity", Range(0, 100)) = 1
        _SpecularRoughness0("Roughness", Range(0, 1)) = 0.02

        [HeaderFoldout(Emission, Use Albedo.a as emission map)]
        _EmissionColor("Color", Color) = (1, 1, 1, 1)
        _EmissionThreshold("Threshold", Range(0, 1)) = 1
        _EmissionIntensity("Intensity", Float) = 0

        [HeaderFoldout(Bloom)]
        _mmBloomIntensity0("Intensity", Float) = 0
        _BloomColor0("Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Rim Light)]
        _RimIntensity("Intensity (Front Main)", Float) = 0.5
        _RimIntensityAdditionalLight("Intensity (Front Additional)", Float) = 0.5
        _RimIntensityBackFace("Intensity (Back Main)", Float) = 0
        _RimIntensityBackFaceAdditionalLight("Intensity (Back Additional)", Float) = 0
        _RimWidth0("Width", Float) = 0.5
        _RimColor0("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimDark0("Darken Value", Range(0, 1)) = 0.5
        _RimEdgeSoftness("Edge Softness", Float) = 0.05

        [HeaderFoldout(Rim Shadow)]
        _RimShadowCt("Ct", Float) = 1
        _RimShadowIntensity("Intensity", Float) = 1
        _RimShadowOffset("Offset", Vector) = (0, 0, 0, 0)
        _RimShadowColor0("Color", Color) = (1, 1, 1, 1)
        _RimShadowWidth0("Width", Float) = 1
        _RimShadowFeather0("Feather", Range(0.01, 0.99)) = 0.01

        [HeaderFoldout(Outline)]
        [KeywordEnum(Tangent, Normal)] _OutlineNormal("Normal Source", Float) = 0
        _OutlineWidth("Width", Range(0,4)) = 1
        _OutlineZOffset("Z Offset", Float) = 0
        _OutlineColor0("Color", Color) = (0, 0, 0, 1)

        [HeaderFoldout(Self Shadow Caster)]
        _SelfShadowDepthBias("Depth Bias", Float) = -0.01
        _SelfShadowNormalBias("Normal Bias", Float) = 0

        [HideInInspector] _RampCoolWarmLerpFactor("Cool / Warm", Range(0, 1)) = 1
        [HideInInspector] _DitherAlpha("Alpha", Range(0, 1)) = 1
        [HideInInspector] _MMDHeadBoneForward("MMD Head Bone Forward", Vector) = (0, 0, 1, 0)
        [HideInInspector] _MMDHeadBoneUp("MMD Head Bone Up", Vector) = (0, 1, 0, 0)
        [HideInInspector] _MMDHeadBoneRight("MMD Head Bone Right", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "ComplexLit" // Packages/com.unity.render-pipelines.universal/Runtime/Passes/GBufferPass.cs: Fill GBuffer, but skip lighting pass for ComplexLit
            "Queue" = "Geometry+20"  // 必须在脸和眼睛之后绘制
        }

        Pass
        {
            Name "HairOpaque"

            Tags
            {
                "LightMode" = "HSRHair"
            }

            Stencil
            {
                Ref 3
                ReadMask 2   // 眼睛位
                WriteMask 1  // 角色
                Comp Always  // 不管眼睛
                Pass Replace // 写入角色位
                Fail Keep
            }

            Cull [_Cull]
            ZWrite On

            Blend 0 One Zero, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex HairVertex
            #pragma fragment HairOpaqueFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_fog

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairOpaque"

            Tags
            {
                "LightMode" = "HSRHairPreserveEye"
            }

            // 没有遮住眼睛的部分
            Stencil
            {
                Ref 3
                ReadMask 2    // 眼睛位
                WriteMask 1   // 角色
                Comp NotEqual // 排除眼睛
                Pass Replace  // 写入角色位
                Fail Keep
            }

            Cull [_Cull]
            ZWrite On

            Blend 0 One Zero, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex HairVertex
            #pragma fragment HairOpaqueFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_fog

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairFakeTransparent"

            Tags
            {
                "LightMode" = "HSRHairFakeTransparent"
            }

            // 遮住眼睛的部分
            Stencil
            {
                Ref 3
                ReadMask 2   // 眼睛位
                Comp Equal
                // 不能清除眼睛的 Stencil
                // 有时候头发存在重叠，如果被盖在下面的头发先画并清除了眼睛的 Stencil，那么上面的头发就画不上了
                // 上面的头发画不上的话，深度也写不上，后面描边就会出问题
                Pass Keep
                Fail Keep
            }

            // 这个 pass 画的是刘海，Back Face 一般情况下看不见
            // 把 Back Face 剔除掉，避免 alpha 混合时和 Front Face 叠加导致颜色错误
            Cull Back // [_Cull]
            ZWrite On

            Blend 0 SrcAlpha OneMinusSrcAlpha, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex HairVertex
            #pragma fragment HairFakeTransparentFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _BACKFACEUV2_ON

            #pragma multi_compile_fog

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairOutline"

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

            ColorMask RGB 0

            HLSLPROGRAM

            #pragma vertex HairOutlineVertex
            #pragma fragment HairOutlineFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #pragma shader_feature_local_vertex _OUTLINENORMAL_TANGENT _OUTLINENORMAL_NORMAL

            #pragma multi_compile_fog

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairShadow"

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

            #pragma vertex HairShadowVertex
            #pragma fragment HairShadowFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_vertex _ _CASTING_SELF_SHADOW

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairDepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex HairDepthOnlyVertex
            #pragma fragment HairDepthOnlyFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairDepthOnlyHSR"

            Tags
            {
                "LightMode" = "HSRHairDepthOnly"
            }

            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex HairDepthOnlyVertex
            #pragma fragment HairDepthOnlyFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairDepthNormals"

            Tags
            {
                "LightMode" = "DepthNormals"
            }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM

            #pragma vertex HairDepthNormalsVertex
            #pragma fragment HairDepthNormalsFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharHairCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "HairMotionVectors"

            Tags
            {
                "LightMode" = "MotionVectors"
            }

            Cull [_Cull]

            HLSLPROGRAM

            #pragma vertex HairMotionVectorsVertex
            #pragma fragment HairMotionVectorsFragment

            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharHairCore.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
