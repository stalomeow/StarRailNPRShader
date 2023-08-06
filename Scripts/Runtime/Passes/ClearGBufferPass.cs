using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.Passes
{
    public class ClearGBufferPass : ScriptableRenderPass
    {
        private readonly ForwardGBuffers m_GBuffers;

        public ClearGBufferPass(ForwardGBuffers gBuffers)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

            m_GBuffers = gBuffers;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            m_GBuffers.ReAllocateBuffersIfNeeded(in cameraTextureDescriptor);

            ConfigureTarget(m_GBuffers.GBuffers);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }
}
