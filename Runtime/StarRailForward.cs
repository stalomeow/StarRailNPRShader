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
using HSR.NPRShader.Passes;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader
{
    [DisallowMultipleRendererFeature]
    public class StarRailForward : ScriptableRendererFeature
    {
        private static readonly string[] s_GBufferNames =
        {
            "_HSRGBuffer0" // rgb: bloom color, a: bloom intensity
        };

        private static readonly GraphicsFormat[] s_GBufferFormats =
        {
            GraphicsFormat.R8G8B8A8_UNorm
        };

        // -------------------------------------------------------

        [Header("MainLightPerObjectShadow")]

        [Range(0, 10)] public float DepthBias = 1;
        [Range(0, 10)] public float NormalBias = 0;

        // -------------------------------------------------------

        [NonSerialized] private RTHandle[] m_GBuffers;
        [NonSerialized] private int[] m_GBufferNameIds;

        // -------------------------------------------------------

        [NonSerialized] private MainLightPerObjectShadowCasterPass m_MainLightPerObjShadowPass;
        [NonSerialized] private ScreenSpaceShadowsPass m_ScreenSpaceShadowPass;
        [NonSerialized] private ScreenSpaceShadowsPostPass m_ScreenSpaceShadowPostPass;
        [NonSerialized] private ClearRTPass m_ClearGBufferPass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward1Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward2Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward3Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueOutlinePass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawTransparentPass;
        [NonSerialized] private SetGlobalRTPass m_SetGBufferPass;
        [NonSerialized] private PostProcessPass m_PostProcessPass;

        public override void Create()
        {
            m_GBuffers = new RTHandle[s_GBufferNames.Length];
            m_GBufferNameIds = Array.ConvertAll(s_GBufferNames, Shader.PropertyToID);

            // -------------------------------------------------------

            m_MainLightPerObjShadowPass = new MainLightPerObjectShadowCasterPass();
            m_ScreenSpaceShadowPass = new ScreenSpaceShadowsPass();
            m_ScreenSpaceShadowPostPass = new ScreenSpaceShadowsPostPass();
            m_ClearGBufferPass = new ClearRTPass(RenderPassEvent.AfterRenderingOpaques);
            m_DrawOpaqueForward1Pass = new MRTDrawObjectsPass("DrawStarRailOpaque (1)", true,
                new ShaderTagId("HSRForward1"));
            m_DrawOpaqueForward2Pass = new MRTDrawObjectsPass("DrawStarRailOpaque (2)", true,
                new ShaderTagId("HSRForward2"));
            m_DrawOpaqueForward3Pass = new MRTDrawObjectsPass("DrawStarRailOpaque (3)", true,
                new ShaderTagId("HSRForward3"));
            m_DrawOpaqueOutlinePass = new MRTDrawObjectsPass("DrawStarRailOpaque (Outline)", true,
                new ShaderTagId("HSROutline"));
            m_DrawTransparentPass = new MRTDrawObjectsPass("DrawStarRailTransparent", false,
                new ShaderTagId("HSRTransparent"), new ShaderTagId("HSROutline"));
            m_SetGBufferPass = new SetGlobalRTPass(RenderPassEvent.BeforeRenderingPostProcessing);
            m_PostProcessPass = new PostProcessPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera)
            {
                // PreviewCamera 没有 SetupRenderPasses，所以不做 MRT
                renderer.EnqueuePass(m_DrawOpaqueForward1Pass.SetupPreview());
                renderer.EnqueuePass(m_DrawOpaqueForward2Pass.SetupPreview());
                renderer.EnqueuePass(m_DrawOpaqueForward3Pass.SetupPreview());
                renderer.EnqueuePass(m_DrawOpaqueOutlinePass.SetupPreview());
                renderer.EnqueuePass(m_DrawTransparentPass.SetupPreview());
                return;
            }

            // AfterRenderingShadows
            renderer.EnqueuePass(m_MainLightPerObjShadowPass);

            // BeforeRenderingOpaques
            renderer.EnqueuePass(m_ScreenSpaceShadowPass);

            // AfterRenderingOpaques
            renderer.EnqueuePass(m_ScreenSpaceShadowPostPass);
            renderer.EnqueuePass(m_ClearGBufferPass);
            renderer.EnqueuePass(m_DrawOpaqueForward1Pass);
            renderer.EnqueuePass(m_DrawOpaqueForward2Pass);
            renderer.EnqueuePass(m_DrawOpaqueForward3Pass);
            renderer.EnqueuePass(m_DrawOpaqueOutlinePass);
            renderer.EnqueuePass(m_DrawTransparentPass);

            // BeforeRenderingPostProcessing
            renderer.EnqueuePass(m_SetGBufferPass);
            renderer.EnqueuePass(m_PostProcessPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // PreviewCamera 不会执行这部分代码！！！

            base.SetupRenderPasses(renderer, in renderingData);

            RTHandle colorTarget = renderer.cameraColorTargetHandle;
            RTHandle depthTarget = renderer.cameraDepthTargetHandle;

            ReAllocateGBuffersIfNeeded(in renderingData.cameraData.cameraTargetDescriptor);

            m_MainLightPerObjShadowPass.Setup(DepthBias, NormalBias);

            m_ClearGBufferPass.SetupClear(m_GBuffers, ClearFlag.All, Color.black);
            m_DrawOpaqueForward1Pass.Setup(colorTarget, depthTarget, m_GBuffers);
            m_DrawOpaqueForward2Pass.Setup(colorTarget, depthTarget, m_GBuffers);
            m_DrawOpaqueForward3Pass.Setup(colorTarget, depthTarget, m_GBuffers);
            m_DrawOpaqueOutlinePass.Setup(colorTarget, depthTarget, m_GBuffers);
            m_DrawTransparentPass.Setup(colorTarget, depthTarget, m_GBuffers);

            m_SetGBufferPass.Setup(m_GBufferNameIds, m_GBuffers);
        }

        private void ReAllocateGBuffersIfNeeded(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.depthBufferBits = 0;

            for (int i = 0; i < m_GBuffers.Length; i++)
            {
                descriptor.graphicsFormat = s_GBufferFormats[i];
                RenderingUtils.ReAllocateIfNeeded(ref m_GBuffers[i], in descriptor, name: s_GBufferNames[i]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            for (int i = 0; i < m_GBuffers.Length; i++)
            {
                m_GBuffers[i]?.Release();
                m_GBuffers[i] = null;
            }

            m_MainLightPerObjShadowPass.Dispose();
            m_ScreenSpaceShadowPass.Dispose();
            m_PostProcessPass.Dispose();

            base.Dispose(disposing);
        }
    }
}
