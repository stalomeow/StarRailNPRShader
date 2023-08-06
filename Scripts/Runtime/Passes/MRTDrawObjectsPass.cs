using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace HSR.Passes
{
    public class MRTDrawObjectsPass : DrawObjectsPass
    {
        private readonly ForwardGBuffers m_GBuffers;
        private readonly RTHandle[] m_ColorAttachments;
        private RTHandle m_DepthAttachment;

        public MRTDrawObjectsPass(ForwardGBuffers gBuffers, bool isOpaque, string profilerTag, ShaderTagId shaderTagId, LayerMask layerMask)
            : base(profilerTag, new[] { shaderTagId }, isOpaque,
                // 在 UniversalForward 之后绘制，利用深度测试尽量少写入无用像素
                isOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingTransparents,
                isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                layerMask, new StencilState(), 0)
        {
            m_GBuffers = gBuffers;
            m_ColorAttachments = new RTHandle[1 + gBuffers.GBuffers.Length];
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            m_ColorAttachments[0] = renderer.cameraColorTargetHandle;
            m_DepthAttachment = renderer.cameraDepthTargetHandle;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            // ClearGBufferPass 在 Configure 中申请 GBuffers
            // 所以，必须在 Configure 中拷贝 GBuffers
            m_GBuffers.GBuffers.CopyTo(m_ColorAttachments, 1);
            ConfigureTarget(m_ColorAttachments, m_DepthAttachment);
            ConfigureClear(ClearFlag.None, Color.black);
        }
    }
}
