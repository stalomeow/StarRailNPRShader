using System;
using HSR.Passes;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR
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
