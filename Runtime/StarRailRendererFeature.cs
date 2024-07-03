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
using HSR.NPRShader.PerObjectShadow;
using HSR.NPRShader.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader
{
    [HelpURL("https://srshader.stalomeow.com/")]
    [DisallowMultipleRendererFeature("Honkai Star Rail")]
    public class StarRailRendererFeature : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES2)]
        [UnityEditor.ShaderKeywordFilter.SelectIf(true, keywordNames: ShaderKeywordStrings.MainLightShadowScreen)]
        private const bool k_RequiresScreenSpaceShadowsKeyword = true;
#endif

        [SerializeField] private DepthBits m_SceneShadowDepthBits = DepthBits.Depth16;
        [SerializeField] private ShadowTileResolution m_SceneShadowTileResolution = ShadowTileResolution._512;

        [SerializeField] private bool m_EnableSelfShadow = true;
        [SerializeField] private DepthBits m_SelfShadowDepthBits = DepthBits.Depth16;
        [SerializeField] private ShadowTileResolution m_SelfShadowTileResolution = ShadowTileResolution._1024;

        [SerializeField] private bool m_EnableFrontHairShadow = true;
        [SerializeField] private HairDepthOnlyPass.DownscaleMode m_FrontHairShadowDownscale = HairDepthOnlyPass.DownscaleMode.Half;
        [SerializeField] private DepthBits m_FrontHairShadowDepthBits = DepthBits.Depth16;

        [SerializeField] private bool m_EnableTransparentFrontHair = true;

        [NonSerialized] private ShadowCasterManager m_SceneShadowCasterManager;
        [NonSerialized] private ShadowCasterManager m_SelfShadowCasterManager;

        [NonSerialized] private SetKeywordPass m_EnableSelfShadowPass;
        [NonSerialized] private SetKeywordPass m_DisableSelfShadowPass;
        [NonSerialized] private SetKeywordPass m_EnableFrontHairShadowPass;
        [NonSerialized] private SetKeywordPass m_DisableFrontHairShadowPass;
        [NonSerialized] private PerObjectShadowCasterPass m_ScenePerObjShadowPass;
        [NonSerialized] private PerObjectShadowCasterPreviewPass m_ScenePerObjShadowPreviewPass;
        [NonSerialized] private PerObjectShadowCasterPass m_SelfPerObjShadowPass;
        [NonSerialized] private HairDepthOnlyPass m_HairDepthOnlyPass;
        [NonSerialized] private RequestResourcePass m_ForceDepthPrepassPass;
        [NonSerialized] private ScreenSpaceShadowsPass m_ScreenSpaceShadowPass;
        [NonSerialized] private ScreenSpaceShadowsPostPass m_ScreenSpaceShadowPostPass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawOpaqueForward1Pass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawOpaqueForward2Pass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawSimpleHairPass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawTransparentHairPass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawOpaqueForward3Pass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawOpaqueOutlinePass;
        [NonSerialized] private ForwardDrawObjectsPass m_DrawTransparentPass;
        [NonSerialized] private PostProcessPass m_PostProcessPass;

        public override void Create()
        {
            m_SceneShadowCasterManager = new ShadowCasterManager(ShadowUsage.Scene);
            m_SelfShadowCasterManager = new ShadowCasterManager(ShadowUsage.Self);

            m_EnableSelfShadowPass = new SetKeywordPass(KeywordNames._MAIN_LIGHT_SELF_SHADOWS, true, RenderPassEvent.BeforeRendering);
            m_DisableSelfShadowPass = new SetKeywordPass(KeywordNames._MAIN_LIGHT_SELF_SHADOWS, false, RenderPassEvent.BeforeRendering);
            m_EnableFrontHairShadowPass = new SetKeywordPass(KeywordNames._MAIN_LIGHT_FRONT_HAIR_SHADOWS, true, RenderPassEvent.BeforeRendering);
            m_DisableFrontHairShadowPass = new SetKeywordPass(KeywordNames._MAIN_LIGHT_FRONT_HAIR_SHADOWS, false, RenderPassEvent.BeforeRendering);
            m_ScenePerObjShadowPass = new PerObjectShadowCasterPass("MainLightPerObjectSceneShadow");
            m_ScenePerObjShadowPreviewPass = new PerObjectShadowCasterPreviewPass("MainLightPerObjectSceneShadow (Preview)", ShadowUsage.Scene);
            m_SelfPerObjShadowPass = new PerObjectShadowCasterPass("MainLightPerObjectSelfShadow");
            m_HairDepthOnlyPass = new HairDepthOnlyPass();
            m_ForceDepthPrepassPass = new RequestResourcePass(RenderPassEvent.AfterRenderingGbuffer, ScriptableRenderPassInput.Depth);
            m_ScreenSpaceShadowPass = new ScreenSpaceShadowsPass();
            m_ScreenSpaceShadowPostPass = new ScreenSpaceShadowsPostPass();
            m_DrawOpaqueForward1Pass = new ForwardDrawObjectsPass("DrawStarRailOpaque (1)", true, new ShaderTagId("HSRForward1"));
            m_DrawOpaqueForward2Pass = new ForwardDrawObjectsPass("DrawStarRailOpaque (2)", true, new ShaderTagId("HSRForward2"));
            m_DrawSimpleHairPass = new ForwardDrawObjectsPass("DrawStarRailHair", true, new ShaderTagId("HSRHair"));
            m_DrawTransparentHairPass = new ForwardDrawObjectsPass("DrawStarRailHair", true, new ShaderTagId("HSRHairPreserveEye"), new ShaderTagId("HSRHairFakeTransparent"));
            m_DrawOpaqueForward3Pass = new ForwardDrawObjectsPass("DrawStarRailOpaque (3)", true, new ShaderTagId("HSRForward3"));
            m_DrawOpaqueOutlinePass = new ForwardDrawObjectsPass("DrawStarRailOpaqueOutline", true, new ShaderTagId("HSROutline"));
            m_DrawTransparentPass = new ForwardDrawObjectsPass("DrawStarRailTransparent", false, new ShaderTagId("HSRTransparent"), new ShaderTagId("HSROutline"));
            m_PostProcessPass = new PostProcessPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool isPreviewCamera = renderingData.cameraData.isPreviewCamera;
            bool enableSceneShadow = !isPreviewCamera;
            bool enableSelfShadow = m_EnableSelfShadow && !isPreviewCamera;
            bool enableFrontHairShadow = m_EnableFrontHairShadow && !isPreviewCamera;
            bool enableTransparentFrontHair = m_EnableTransparentFrontHair;

            // BeforeRendering
            renderer.EnqueuePass(enableSelfShadow ? m_EnableSelfShadowPass : m_DisableSelfShadowPass);
            renderer.EnqueuePass(enableFrontHairShadow ? m_EnableFrontHairShadowPass : m_DisableFrontHairShadowPass);

            // AfterRenderingShadows
            renderer.EnqueuePass(enableSceneShadow ? m_ScenePerObjShadowPass : m_ScenePerObjShadowPreviewPass);

            if (enableSelfShadow)
            {
                renderer.EnqueuePass(m_SelfPerObjShadowPass);
            }

            // AfterRenderingPrePasses
            if (enableFrontHairShadow)
            {
                renderer.EnqueuePass(m_HairDepthOnlyPass);
            }

            // AfterRenderingGbuffer
            renderer.EnqueuePass(m_ForceDepthPrepassPass); // 保证 RimLight、眼睛等需要深度图的效果正常工作
            renderer.EnqueuePass(m_ScreenSpaceShadowPass);

            // AfterRenderingOpaques
            renderer.EnqueuePass(m_ScreenSpaceShadowPostPass);

            renderer.EnqueuePass(m_DrawOpaqueForward1Pass);
            renderer.EnqueuePass(m_DrawOpaqueForward2Pass);
            renderer.EnqueuePass(enableTransparentFrontHair ? m_DrawTransparentHairPass : m_DrawSimpleHairPass);
            renderer.EnqueuePass(m_DrawOpaqueForward3Pass);
            renderer.EnqueuePass(m_DrawOpaqueOutlinePass);

            // AfterRenderingTransparents
            renderer.EnqueuePass(m_DrawTransparentPass);

            // BeforeRenderingPostProcessing
            renderer.EnqueuePass(m_PostProcessPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // PreviewCamera 不会执行这部分代码！！！
            base.SetupRenderPasses(renderer, in renderingData);

            m_SceneShadowCasterManager.Cull(in renderingData, PerObjectShadowCasterPass.MaxShadowCount);
            m_ScenePerObjShadowPass.Setup(m_SceneShadowCasterManager, m_SceneShadowTileResolution, m_SceneShadowDepthBits);

            if (m_EnableSelfShadow)
            {
                m_SelfShadowCasterManager.Cull(in renderingData, PerObjectShadowCasterPass.MaxShadowCount);
                m_SelfPerObjShadowPass.Setup(m_SelfShadowCasterManager, m_SelfShadowTileResolution, m_SelfShadowDepthBits);
            }

            if (m_EnableFrontHairShadow)
            {
                m_HairDepthOnlyPass.Setup(m_FrontHairShadowDownscale, m_FrontHairShadowDepthBits);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_ScenePerObjShadowPass.Dispose();
            m_HairDepthOnlyPass.Dispose();
            m_ScreenSpaceShadowPass.Dispose();
            m_SelfPerObjShadowPass.Dispose();
            m_PostProcessPass.Dispose();

            base.Dispose(disposing);
        }

        private static class KeywordNames
        {
            public static readonly string _MAIN_LIGHT_SELF_SHADOWS = MemberNameHelpers.String();
            public static readonly string _MAIN_LIGHT_FRONT_HAIR_SHADOWS = MemberNameHelpers.String();
        }
    }
}
