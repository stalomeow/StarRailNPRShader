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

using System;
using HSR.NPRShader.PostProcessing;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.Passes
{
    public class PostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const int BloomMaxKernelSize = 32;
        public const int BloomMipDownBlurCount = 4;
        public const int BloomAtlasPadding = 1;

        private readonly LazyMaterial m_BloomMaterial = new(StarRailBuiltinShaders.BloomShader);
        private readonly LazyMaterial m_UberMaterial = new(StarRailBuiltinShaders.UberPostShader);

        private readonly ProfilingSampler m_BloomSampler;
        private readonly ProfilingSampler m_UberPostSampler;

        private readonly GraphicsFormat m_DefaultHDRFormat;
        private readonly bool m_UseRGBM;

        private RTHandle[] m_BloomMipDown;
        private RTHandle m_BloomAtlas1;
        private RTHandle m_BloomAtlas2;
        private RTHandle m_BloomCharacterColor;
        private readonly Rect[] m_BloomAtlasViewports;
        private readonly Vector4[] m_BloomAtlasUVMinMax;
        private readonly float[][] m_BloomKernels;

        private CustomBloom m_BloomConfig;
        private CustomTonemapping m_TonemappingConfig;

        public PostProcessPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("Render StarRail PostProcessing");

            m_BloomSampler = new ProfilingSampler("Bloom");
            m_UberPostSampler = new ProfilingSampler("UberPostProcess");

            m_BloomMipDown = Array.Empty<RTHandle>();
            m_BloomAtlasViewports = new Rect[BloomMipDownBlurCount];
            m_BloomAtlasUVMinMax = new Vector4[BloomMipDownBlurCount];
            m_BloomKernels = new float[BloomMipDownBlurCount][];

            for (int i = 0; i < BloomMipDownBlurCount; i++)
            {
                m_BloomKernels[i] = new float[BloomMaxKernelSize];
            }

            // Texture format pre-lookup
            const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage))
            {
                m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                m_UseRGBM = false;
            }
            else
            {
                m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
                m_UseRGBM = true;
            }
        }

        public void Dispose()
        {
            m_BloomMaterial.DestroyCache();
            m_UberMaterial.DestroyCache();

            ReleaseBloomRTHandles();
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            VolumeStack stack = VolumeManager.instance.stack;
            m_BloomConfig = stack.GetComponent<CustomBloom>();
            m_TonemappingConfig = stack.GetComponent<CustomTonemapping>();

            AllocateBloomRTHandles(in cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera || !renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                ExecuteBloom(cmd, ref renderingData);
                ExecuteUber(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #region Bloom

        private void AllocateBloomRTHandles(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!m_BloomConfig.IsActive())
            {
                return;
            }

            ReAllocateBloomMipDownArrayIfNeeded(in cameraTextureDescriptor);
            ReAllocateBloomAtlasIfNeeded(in cameraTextureDescriptor);
            ReAllocateBloomCharacterColorIfNeeded(in cameraTextureDescriptor);

            for (int i = 0; i < m_BloomKernels.Length; i++)
            {
                CalculateBloomKernel(m_BloomKernels[i], GetBloomKernelSize(i));
            }
        }

        private void ReAllocateBloomMipDownArrayIfNeeded(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor mipDesc = cameraTextureDescriptor;
            mipDesc.graphicsFormat = m_DefaultHDRFormat;
            mipDesc.depthBufferBits = (int)DepthBits.None;
            mipDesc.msaaSamples = 1;

            int mipDownCountExtra = m_BloomConfig.MipDownCount.value;
            Array.Resize(ref m_BloomMipDown, mipDownCountExtra + BloomMipDownBlurCount);

            for (int i = 0; i < m_BloomMipDown.Length; i++)
            {
                if (i == mipDownCountExtra)
                {
                    // 不同屏幕分辨率下，保证做高斯模糊的 RT 的分辨率一致，这样模糊效果才一样
                    mipDesc.width = m_BloomConfig.BlurFirstRTWidth.value;
                    mipDesc.height = m_BloomConfig.BlurFirstRTHeight.value;
                }
                else
                {
                    mipDesc.width = Mathf.Max(1, mipDesc.width >> 1);
                    mipDesc.height = Mathf.Max(1, mipDesc.height >> 1);
                }

                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipDown[i], in mipDesc, FilterMode.Bilinear, TextureWrapMode.Clamp);
            }
        }

        private void ReAllocateBloomAtlasIfNeeded(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            // TODO 这部分目前是硬编码

            Vector2 baseSize = GetBiggestBlurRTHandle().referenceSize;

            RenderTextureDescriptor atlasDesc = cameraTextureDescriptor;
            atlasDesc.graphicsFormat = m_DefaultHDRFormat;
            atlasDesc.depthBufferBits = (int)DepthBits.None;
            atlasDesc.msaaSamples = 1;

            // 加几个像素的 padding，防止最后 bilinear combine 时混合到其他部分导致漏光
            atlasDesc.width = Mathf.CeilToInt(1.5f * baseSize.x) + BloomAtlasPadding;
            atlasDesc.height = Mathf.CeilToInt(baseSize.y);

            RenderingUtils.ReAllocateIfNeeded(ref m_BloomAtlas1, in atlasDesc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "BloomAtlas1");
            RenderingUtils.ReAllocateIfNeeded(ref m_BloomAtlas2, in atlasDesc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "BloomAtlas2");

            m_BloomAtlasViewports[0] = new Rect(Vector2.zero, baseSize);
            m_BloomAtlasViewports[1] = new Rect(new Vector2(m_BloomAtlasViewports[0].xMax + BloomAtlasPadding, 0), baseSize * 0.5f);
            m_BloomAtlasViewports[2] = new Rect(new Vector2(m_BloomAtlasViewports[1].x, m_BloomAtlasViewports[1].yMax + BloomAtlasPadding), baseSize * 0.25f);
            m_BloomAtlasViewports[3] = new Rect(new Vector2(m_BloomAtlasViewports[2].xMax + BloomAtlasPadding, m_BloomAtlasViewports[2].y), baseSize * 0.125f);

            m_BloomAtlasUVMinMax[0] = ViewportToUVMinMax(m_BloomAtlasViewports[0], atlasDesc.width, atlasDesc.height);
            m_BloomAtlasUVMinMax[1] = ViewportToUVMinMax(m_BloomAtlasViewports[1], atlasDesc.width, atlasDesc.height);
            m_BloomAtlasUVMinMax[2] = ViewportToUVMinMax(m_BloomAtlasViewports[2], atlasDesc.width, atlasDesc.height);
            m_BloomAtlasUVMinMax[3] = ViewportToUVMinMax(m_BloomAtlasViewports[3], atlasDesc.width, atlasDesc.height);
        }

        private void ReAllocateBloomCharacterColorIfNeeded(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!m_BloomConfig.CharactersOnly.value)
            {
                m_BloomCharacterColor?.Release();
                m_BloomCharacterColor = null;
                return;
            }

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.depthBufferBits = (int)DepthBits.None;
            RenderingUtils.ReAllocateIfNeeded(ref m_BloomCharacterColor, in desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BloomCharacterColor");
        }

        private static Vector4 ViewportToUVMinMax(Rect rect, float textureWidth, float textureHeight)
        {
            return new Vector4(rect.x / textureWidth, rect.y / textureHeight,
                rect.xMax / textureWidth, rect.yMax / textureHeight);
        }

        private static void CalculateBloomKernel(float[] kernel, int size)
        {
            // 用杨辉三角近似高斯分布，去掉最左边两个和最右边两个数
            int n = size + 3;
            long sum = (1L << n) - 2 * (1 + n);
            double value = n / (double)sum;

            for (int i = 0; i < size; i++)
            {
                int k = i + 1;
                value *= n - k;
                value /= k + 1;
                kernel[i] = (float)value;
            }
        }

        private int GetBloomKernelSize(int index) => index switch
        {
            0 => m_BloomConfig.KernelSize1.value,
            1 => m_BloomConfig.KernelSize2.value,
            2 => m_BloomConfig.KernelSize3.value,
            3 => m_BloomConfig.KernelSize4.value,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        private void ReleaseBloomRTHandles()
        {
            for (int i = 0; i < m_BloomMipDown.Length; i++)
            {
                m_BloomMipDown[i]?.Release();
                m_BloomMipDown[i] = null;
            }

            m_BloomAtlas1?.Release();
            m_BloomAtlas2?.Release();
            m_BloomCharacterColor?.Release();
            m_BloomAtlas1 = null;
            m_BloomAtlas2 = null;
            m_BloomCharacterColor = null;
        }

        private RTHandle GetBiggestBlurRTHandle()
        {
            // 需要做模糊的，最大的 RT
            return m_BloomMipDown[^BloomMipDownBlurCount];
        }

        private void ExecuteBloom(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!m_BloomConfig.IsActive())
            {
                return;
            }

            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            RTHandle colorTargetHandle = renderer.cameraColorTargetHandle;
            Material material = m_BloomMaterial.Value;
            Vector4 scaleBias = new Vector4(1, 1, 0, 0);

            using (new ProfilingScope(cmd, m_BloomSampler))
            {
                if (m_BloomConfig.CharactersOnly.value)
                {
                    RTHandle depthTargetHandle = renderer.cameraDepthTargetHandle;
                    CoreUtils.SetRenderTarget(cmd, m_BloomCharacterColor, depthTargetHandle, ClearFlag.Color, Color.black);
                    Blitter.BlitTexture(cmd, colorTargetHandle, scaleBias, material, 5);
                    colorTargetHandle = m_BloomCharacterColor;
                }

                CoreUtils.SetKeyword(material, ShaderKeywordStrings.UseRGBM, m_UseRGBM);

                cmd.SetGlobalFloat(PropertyIds._BloomThreshold, m_BloomConfig.Threshold.value);
                cmd.SetGlobalVectorArray(PropertyIds._BloomUVMinMax, m_BloomAtlasUVMinMax);

                // Prefilter
                Blitter.BlitCameraTexture(cmd, colorTargetHandle, m_BloomMipDown[0],
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 0);

                // Mip down
                for (int i = 1; i < m_BloomMipDown.Length; i++)
                {
                    Blitter.BlitCameraTexture(cmd, m_BloomMipDown[i - 1], m_BloomMipDown[i],
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 1);
                }

                int blurStartIndex = m_BloomMipDown.Length - BloomMipDownBlurCount;

                // Alpha Channel 也要清空为 0，适配 RGBM 编码
                CoreUtils.SetRenderTarget(cmd, m_BloomAtlas1, ClearFlag.All, new Color(0, 0, 0, 0));

                // Blur vertical
                for (int i = blurStartIndex; i < m_BloomMipDown.Length; i++)
                {
                    int atlasIndex = i - blurStartIndex;
                    cmd.SetGlobalInt(PropertyIds._BloomKernelSize, GetBloomKernelSize(atlasIndex));
                    cmd.SetGlobalFloatArray(PropertyIds._BloomKernel, m_BloomKernels[atlasIndex]);

                    cmd.SetViewport(m_BloomAtlasViewports[atlasIndex]);
                    Blitter.BlitTexture(cmd, m_BloomMipDown[i], scaleBias, material, 2);
                }

                // Alpha Channel 也要清空为 0，适配 RGBM 编码
                CoreUtils.SetRenderTarget(cmd, m_BloomAtlas2, ClearFlag.All, new Color(0, 0, 0, 0));

                // Blur horizontal
                for (int i = blurStartIndex; i < m_BloomMipDown.Length; i++)
                {
                    int atlasIndex = i - blurStartIndex;
                    cmd.SetGlobalInt(PropertyIds._BloomUVIndex, atlasIndex);
                    cmd.SetGlobalInt(PropertyIds._BloomKernelSize, GetBloomKernelSize(atlasIndex));
                    cmd.SetGlobalFloatArray(PropertyIds._BloomKernel, m_BloomKernels[atlasIndex]);

                    cmd.SetViewport(m_BloomAtlasViewports[atlasIndex]);
                    Blitter.BlitTexture(cmd, m_BloomAtlas1, scaleBias, material, 3);
                }

                // Combine
                Blitter.BlitCameraTexture(cmd, m_BloomAtlas2, GetBiggestBlurRTHandle(),
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 4);
            }
        }

        #endregion

        #region Uber

        private void ExecuteUber(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Material material = m_UberMaterial.Value;

            using (new ProfilingScope(cmd, m_UberPostSampler))
            {
                if (m_BloomConfig.IsActive())
                {
                    material.EnableKeyword(KeywordNames._BLOOM);
                    CoreUtils.SetKeyword(material, KeywordNames._BLOOM_USE_RGBM, m_UseRGBM);

                    float bloomIntensity = m_BloomConfig.Intensity.value * 0.6f; // 稍微压低一点
                    material.SetFloat(PropertyIds._BloomIntensity, bloomIntensity);
                    material.SetColor(PropertyIds._BloomTint, m_BloomConfig.Tint.value);
                    material.SetTexture(PropertyIds._BloomTexture, GetBiggestBlurRTHandle());
                }
                else
                {
                    material.DisableKeyword(KeywordNames._BLOOM);
                }

                if (m_TonemappingConfig.IsActive())
                {
                    material.EnableKeyword(KeywordNames._TONEMAPPING_ACES);

                    material.SetFloat(PropertyIds._ACESParamA, m_TonemappingConfig.ACESParamA.value);
                    material.SetFloat(PropertyIds._ACESParamB, m_TonemappingConfig.ACESParamB.value);
                    material.SetFloat(PropertyIds._ACESParamC, m_TonemappingConfig.ACESParamC.value);
                    material.SetFloat(PropertyIds._ACESParamD, m_TonemappingConfig.ACESParamD.value);
                    material.SetFloat(PropertyIds._ACESParamE, m_TonemappingConfig.ACESParamE.value);
                }
                else
                {
                    material.DisableKeyword(KeywordNames._TONEMAPPING_ACES);
                }

                CoreUtils.SetKeyword(material, KeywordNames._USE_FAST_SRGB_LINEAR_CONVERSION,
                    renderingData.postProcessingData.useFastSRGBLinearConversion);

                Blit(cmd, ref renderingData, material);
            }
        }

        #endregion

        private static class KeywordNames
        {
            public static readonly string _BLOOM = MemberNameHelpers.String();
            public static readonly string _BLOOM_USE_RGBM = MemberNameHelpers.String();
            public static readonly string _TONEMAPPING_ACES = MemberNameHelpers.String();
            public static readonly string _USE_FAST_SRGB_LINEAR_CONVERSION = MemberNameHelpers.String();
        }

        private static class PropertyIds
        {
            public static readonly int _BloomThreshold = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomUVMinMax = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomUVIndex = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomKernelSize = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomKernel = MemberNameHelpers.ShaderPropertyID();

            public static readonly int _BloomIntensity = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomTint = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _BloomTexture = MemberNameHelpers.ShaderPropertyID();

            public static readonly int _ACESParamA = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ACESParamB = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ACESParamC = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ACESParamD = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _ACESParamE = MemberNameHelpers.ShaderPropertyID();
        }
    }
}
