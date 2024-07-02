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

Shader "Honkai Star Rail/Character/Face"
{
    Properties
    {
        [KeywordEnum(Game, MMD)] _Model("Model Type", Float) = 0
        _ModelScale("Model Scale", Float) = 1

        [HeaderFoldout(Options)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0   // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0   // 默认 Zero
        [Toggle] _AlphaTest("Alpha Test", Float) = 0
        [If(_ALPHATEST_ON)] [Indent] _AlphaTestThreshold("Threshold", Range(0, 1)) = 0.5

        [HeaderFoldout(Maps)]
        [SingleLineTextureNoScaleOffset(_Color)] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1, 1, 1, 1)
        [SingleLineTextureNoScaleOffset] _FaceMap("Face Map", 2D) = "white" {}
        [SingleLineTextureNoScaleOffset] _ExpressionMap("Expression Map", 2D) = "white" {}
        [TextureScaleOffset] _Maps_ST("Maps Scale Offset", Vector) = (1, 1, 0, 0)
        [Header(Overrides)] [Space(5)]
        [If(_MODEL_GAME)] [Toggle] _FaceMapUV2("Face Map Use UV2", Float) = 0

        [HeaderFoldout(Diffuse)]
        _ShadowColor("Face Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _EyeShadowColor("Eye Shadow Color", Color) = (1, 1, 1, 1)
        _EyeAlwaysLit("Eye Always Lit", Range(0, 1)) = 0.2
        [Header(Front Hair Shadow)][Space(5)]
        _HairShadowDistance("Distance", Range(0, 1)) = 0.2

        [HeaderFoldout(Transparent Front Hair)]
        _MaxEyeHairDistance("Max Eye Hair Distance", Float) = 0.2

        [HeaderFoldout(Emission, Use Albedo.a as emission map)]
        _EmissionColor("Color", Color) = (1, 1, 1, 1)
        _EmissionThreshold("Threshold", Range(0, 1)) = 0.1
        _EmissionIntensity("Intensity", Float) = 0.3

        [HeaderFoldout(Bloom)]
        _mmBloomIntensity0("Intensity", Float) = 0
        _BloomColor0("Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Expression)]
        _ExCheekColor("Cheek Color", Color) = (1, 1, 1, 1)
        _ExShyColor("Shy Color", Color) = (1, 1, 1, 1)
        _ExShadowColor("Shadow Face Color", Color) = (1, 1, 1, 1)
        _ExEyeColor("Shadow Eye Color", Color) = (1, 1, 1, 1)

        [HeaderFoldout(Outline)]
        [KeywordEnum(Tangent, Normal)] _OutlineNormal("Normal Source", Float) = 0
        _OutlineWidth("Width", Range(0, 4)) = 1
        _OutlineZOffset("Z Offset", Float) = 0
        _OutlineColor0("Color", Color) = (0, 0, 0, 1)
        [Header(Nose Line)][Space(5)]
        _NoseLineColor("Color", Color) = (1, 1, 1, 1)
        _NoseLinePower("Power", Range(0, 8)) = 1

        [HeaderFoldout(Self Shadow Caster)]
        _SelfShadowDepthBias("Depth Bias", Float) = -0.01
        _SelfShadowNormalBias("Normal Bias", Float) = 0

        [HideInInspector] _ExCheekIntensity("Cheek Intensity", Range(0, 1)) = 0
        [HideInInspector] _ExShyIntensity("Shy Intensity", Range(0, 1)) = 0
        [HideInInspector] _ExShadowIntensity("Shadow Intensity", Range(0, 1)) = 0
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
            "Queue" = "Geometry" // 最先渲染
        }

        Pass
        {
            Name "FaceOpaque+Z"

            Tags
            {
                "LightMode" = "HSRForward1"
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

            Cull Back
            ZWrite On

            Blend 0 One Zero, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex FaceVertex
            #pragma fragment FaceOpaqueAndZFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _FACEMAPUV2_ON

            #pragma multi_compile_fog

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _MAIN_LIGHT_FRONT_HAIR_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "WriteEyeStencil"

            Tags
            {
                "LightMode" = "HSRForward2"
            }

            // 角色和眼睛的 Stencil，需要在其他部分渲染之前写入
            Stencil
            {
                Ref 3
                WriteMask 3
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            Cull Back
            ZWrite Off
            ZTest LEqual // 眼白是在脸后面的，并且比眼睛要大，需要 ZTest 来剔除，然后再写 Stencil，这样才能准确抠出眼睛

            ColorMask 0 0

            HLSLPROGRAM

            #pragma vertex FaceVertex
            #pragma fragment FaceWriteEyeStencilFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "FaceOutline"

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

            Blend 0 One Zero, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0

            HLSLPROGRAM

            #pragma vertex FaceOutlineVertex
            #pragma fragment FaceOutlineFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #pragma shader_feature_local_vertex _OUTLINENORMAL_TANGENT _OUTLINENORMAL_NORMAL

            #pragma multi_compile_fog

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "FaceShadow"

            Tags
            {
                "LightMode" = "HSRPerObjectShadowCaster"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma target 2.0

            #pragma vertex FaceShadowVertex
            #pragma fragment FaceShadowFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_vertex _ _CASTING_SELF_SHADOW

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "FaceDepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM

            #pragma vertex FaceDepthOnlyVertex
            #pragma fragment FaceDepthOnlyFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "FaceDepthNormals"

            Tags
            {
                "LightMode" = "DepthNormals"
            }

            Cull Back
            ZWrite On

            HLSLPROGRAM

            #pragma vertex FaceDepthNormalsVertex
            #pragma fragment FaceDepthNormalsFragment

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "FaceMotionVectors"

            Tags
            {
                "LightMode" = "MotionVectors"
            }

            Cull Back

            HLSLPROGRAM

            #pragma vertex FaceMotionVectorsVertex
            #pragma fragment FaceMotionVectorsFragment

            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            #pragma shader_feature_local _MODEL_GAME _MODEL_MMD
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            #include "CharFaceCore.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
