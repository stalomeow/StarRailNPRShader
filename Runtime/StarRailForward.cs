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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader
{
    [DisallowMultipleRendererFeature]
    public class StarRailForward : ScriptableRendererFeature
    {
        [NonSerialized] private ForwardGBuffers m_GBuffers;

        [NonSerialized] private ClearGBufferPass m_ClearGBufferPass;

        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward1Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward2Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueForward3Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawOpaqueOutlinePass;

        [NonSerialized] private MRTDrawObjectsPass m_DrawTransparentForward1Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawTransparentForward2Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawTransparentForward3Pass;
        [NonSerialized] private MRTDrawObjectsPass m_DrawTransparentOutlinePass;

        [NonSerialized] private PostProcessPass m_PostProcessPass;

        public override void Create()
        {
            m_GBuffers = new ForwardGBuffers();

            m_ClearGBufferPass = new ClearGBufferPass(m_GBuffers);

            m_DrawOpaqueForward1Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : true,
                profilerTag: "CustomOpaque (1)",
                shaderTagId: new ShaderTagId("HSRForward1"),
                layerMask  : -1
            );
            m_DrawOpaqueForward2Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : true,
                profilerTag: "CustomOpaque (2)",
                shaderTagId: new ShaderTagId("HSRForward2"),
                layerMask  : -1
            );
            m_DrawOpaqueForward3Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : true,
                profilerTag: "CustomOpaque (3)",
                shaderTagId: new ShaderTagId("HSRForward3"),
                layerMask  : -1
            );
            m_DrawOpaqueOutlinePass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : true,
                profilerTag: "CustomOpaque (Outline)",
                shaderTagId: new ShaderTagId("HSROutline"),
                layerMask  : -1
            );

            m_DrawTransparentForward1Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : false,
                profilerTag: "CustomTransparent (1)",
                shaderTagId: new ShaderTagId("HSRForward1"),
                layerMask  : -1
            );
            m_DrawTransparentForward2Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : false,
                profilerTag: "CustomTransparent (2)",
                shaderTagId: new ShaderTagId("HSRForward2"),
                layerMask  : -1
            );
            m_DrawTransparentForward3Pass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : false,
                profilerTag: "CustomTransparent (3)",
                shaderTagId: new ShaderTagId("HSRForward3"),
                layerMask  : -1
            );
            m_DrawTransparentOutlinePass = new MRTDrawObjectsPass
            (
                gBuffers   : m_GBuffers,
                isOpaque   : false,
                profilerTag: "CustomTransparent (Outline)",
                shaderTagId: new ShaderTagId("HSROutline"),
                layerMask  : -1
            );

            m_PostProcessPass = new PostProcessPass(m_GBuffers);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ClearGBufferPass);

            renderer.EnqueuePass(m_DrawOpaqueForward1Pass);
            renderer.EnqueuePass(m_DrawOpaqueForward2Pass);
            renderer.EnqueuePass(m_DrawOpaqueForward3Pass);
            renderer.EnqueuePass(m_DrawOpaqueOutlinePass);

            renderer.EnqueuePass(m_DrawTransparentForward1Pass);
            renderer.EnqueuePass(m_DrawTransparentForward2Pass);
            renderer.EnqueuePass(m_DrawTransparentForward3Pass);
            renderer.EnqueuePass(m_DrawTransparentOutlinePass);

            renderer.EnqueuePass(m_PostProcessPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_GBuffers.Dispose();
            m_PostProcessPass.Dispose();

            base.Dispose(disposing);
        }
    }
}
