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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.Passes
{
    public class PostProcessPass : ScriptableRenderPass, IDisposable
    {
        private readonly LazyMaterial m_BloomMaterial = new(StarRailBuiltinShaders.BloomShader);
        private readonly LazyMaterial m_UberMaterial = new(StarRailBuiltinShaders.UberPostShader);

        private readonly ProfilingSampler m_BloomSampler;
        private readonly ProfilingSampler m_UberPostSampler;

        private RTHandle m_BloomHighlight;
        private RTHandle[] m_BloomMipDown1;
        private RTHandle[] m_BloomMipDown2;

        private CustomBloom m_BloomConfig;
        private CustomTonemapping m_TonemappingConfig;

        public PostProcessPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("Render StarRail PostProcessing");

            m_BloomSampler = new ProfilingSampler("Bloom");
            m_UberPostSampler = new ProfilingSampler("UberPostProcess");

            m_BloomHighlight = null;
            m_BloomMipDown1 = Array.Empty<RTHandle>();
            m_BloomMipDown2 = Array.Empty<RTHandle>();
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

            EnsureBloomMipDownArraySize();

            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.RGB111110Float;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            for (int i = -1; i < m_BloomConfig.Iteration.value; i++)
            {
                descriptor.width = Mathf.Max(1, descriptor.width >> 1);
                descriptor.height = Mathf.Max(1, descriptor.height >> 1);

                if (i == -1)
                {
                    RenderingUtils.ReAllocateIfNeeded(ref m_BloomHighlight, in descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
                    continue;
                }

                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipDown1[i], in descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipDown2[i], in descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
            }
        }

        private void EnsureBloomMipDownArraySize()
        {
            for (int i = m_BloomConfig.Iteration.value; i < m_BloomMipDown1.Length; i++)
            {
                m_BloomMipDown1[i]?.Release();
            }

            for (int i = m_BloomConfig.Iteration.value; i < m_BloomMipDown2.Length; i++)
            {
                m_BloomMipDown2[i]?.Release();
            }

            Array.Resize(ref m_BloomMipDown1, m_BloomConfig.Iteration.value);
            Array.Resize(ref m_BloomMipDown2, m_BloomConfig.Iteration.value);
        }

        private void ReleaseBloomRTHandles()
        {
            m_BloomHighlight?.Release();
            m_BloomHighlight = null;

            foreach (RTHandle rtHandle in m_BloomMipDown1)
            {
                rtHandle?.Release();
            }

            foreach (RTHandle rtHandle in m_BloomMipDown2)
            {
                rtHandle?.Release();
            }

            m_BloomMipDown1 = Array.Empty<RTHandle>();
            m_BloomMipDown2 = Array.Empty<RTHandle>();
        }

        private void ExecuteBloom(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!m_BloomConfig.IsActive())
            {
                return;
            }

            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            RTHandle colorTargetHandle = renderer.cameraColorTargetHandle;
            Material bloomMaterial = m_BloomMaterial.Value;
            Material blitMaterial = Blitter.GetBlitMaterial(TextureXR.dimension);

            using (new ProfilingScope(cmd, m_BloomSampler))
            {
                // Set threshold
                float thresholdR = Mathf.GammaToLinearSpace(m_BloomConfig.ThresholdR.value);
                float thresholdG = Mathf.GammaToLinearSpace(m_BloomConfig.ThresholdG.value);
                float thresholdB = Mathf.GammaToLinearSpace(m_BloomConfig.ThresholdB.value);
                Vector4 threshold = new Vector4(thresholdR, thresholdG, thresholdB);
                bloomMaterial.SetVector(PropertyIds._BloomThreshold, threshold);

                // Prefilter
                Blitter.BlitCameraTexture(cmd, colorTargetHandle, m_BloomHighlight,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 0);

                // Mip down
                RTHandle lastRTHandle = m_BloomHighlight;
                for (int i = 0; i < m_BloomMipDown1.Length; i++)
                {
                    // use bilinear (pass 1)
                    Blitter.BlitCameraTexture(cmd, lastRTHandle, m_BloomMipDown1[i],
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, blitMaterial, 1);
                    lastRTHandle = m_BloomMipDown1[i];
                }

                // Blur vertical
                for (int i = 0; i < m_BloomMipDown1.Length; i++)
                {
                    cmd.SetGlobalFloat(PropertyIds._BloomScatter, m_BloomConfig.Scatter.value);
                    Blitter.BlitCameraTexture(cmd, m_BloomMipDown1[i], m_BloomMipDown2[i],
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 1);
                }

                // Blur horizontal
                for (int i = 0; i < m_BloomMipDown1.Length; i++)
                {
                    cmd.SetGlobalFloat(PropertyIds._BloomScatter, m_BloomConfig.Scatter.value);
                    Blitter.BlitCameraTexture(cmd, m_BloomMipDown2[i], m_BloomMipDown1[i],
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 2);
                }

                // Mip up
                for (int i = m_BloomMipDown1.Length - 1; i >= 1; i--)
                {
                    Blitter.BlitCameraTexture(cmd, m_BloomMipDown1[i], m_BloomMipDown1[i - 1],
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, bloomMaterial, 3);
                }
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

                    material.SetFloat(PropertyIds._BloomIntensity, m_BloomConfig.Intensity.value);
                    material.SetColor(PropertyIds._BloomTint, m_BloomConfig.Tint.value);
                    material.SetTexture(PropertyIds._BloomTexture, m_BloomMipDown1[0]);
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

                Blit(cmd, ref renderingData, material);
            }
        }

        #endregion

        private static class KeywordNames
        {
            public static readonly string _BLOOM = StringHelpers.MemberName();
            public static readonly string _TONEMAPPING_ACES = StringHelpers.MemberName();
        }

        private static class PropertyIds
        {
            public static readonly int _BloomThreshold = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _BloomScatter = StringHelpers.ShaderPropertyIDFromMemberName();

            public static readonly int _BloomIntensity = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _BloomTint = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _BloomTexture = StringHelpers.ShaderPropertyIDFromMemberName();

            public static readonly int _ACESParamA = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ACESParamB = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ACESParamC = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ACESParamD = StringHelpers.ShaderPropertyIDFromMemberName();
            public static readonly int _ACESParamE = StringHelpers.ShaderPropertyIDFromMemberName();
        }
    }
}
