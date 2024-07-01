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

Shader "Honkai Star Rail/Character/EyeShadow"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha("Src Blend (A)", Float) = 0 // 默认 Zero
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha("Dst Blend (A)", Float) = 0 // 默认 Zero
        _Color("Color", Color) = (0.6770648, 0.7038123, 0.8018868, 0.7647059)
        [HideInInspector] _DitherAlpha("Dither Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "ComplexLit" // Packages/com.unity.render-pipelines.universal/Runtime/Passes/GBufferPass.cs: Fill GBuffer, but skip lighting pass for ComplexLit
            "Queue" = "Geometry+10"  // 必须在脸之后绘制
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Shared/CharCore.hlsl"
            #include "Shared/CharRenderingHelpers.hlsl"
            #include "Shared/CharMotionVectors.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _DitherAlpha;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "EyeShadow"

            Tags
            {
                "LightMode" = "HSRForward2"
            }

            // 眼睛部分
            Stencil
            {
                Ref 2
                ReadMask 2   // 眼睛位
                Comp Equal
                Pass Keep
                Fail Keep
            }

            Cull Back
            ZWrite Off // 不写入深度，仅仅是附加在图像上面

            Blend DstColor Zero, [_SrcBlendAlpha] [_DstBlendAlpha]

            ColorMask RGBA 0
            ColorMask 0 1

            HLSLPROGRAM

            #pragma multi_compile_fog

            #pragma vertex vert
            #pragma fragment frag

            CharCoreVaryings vert(CharCoreAttributes i)
            {
                return CharCoreVertex(i, 0);
            }

            float4 frag(CharCoreVaryings i) : SV_Target0
            {
                DoDitherAlphaEffect(i.positionHCS, _DitherAlpha);

                float4 colorTarget = _Color;

                // Fog
                real fogFactor = InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor);
                colorTarget.rgb = MixFog(colorTarget.rgb, fogFactor);

                return colorTarget;
            }

            ENDHLSL
        }

        Pass
        {
            Name "EyeShadowMotionVectors"

            Tags
            {
                "LightMode" = "MotionVectors"
            }

            Cull Back
            ZWrite Off // 不写入深度，仅仅是附加在图像上面

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
        // No Shadow
        // No Depth
    }

    CustomEditor "StarRailShaderGUI"
    Fallback Off
}
