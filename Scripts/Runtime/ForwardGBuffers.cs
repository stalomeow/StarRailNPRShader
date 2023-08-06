using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR
{
    public class ForwardGBuffers : IDisposable
    {
        public RTHandle[] GBuffers { get; }

        public ForwardGBuffers()
        {
            GBuffers = new RTHandle[ShaderConstants.GBufferNames.Length];
        }

        public void ReAllocateBuffersIfNeeded(in RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor bloomBufferDescriptor = cameraTextureDescriptor;
            bloomBufferDescriptor.colorFormat = RenderTextureFormat.R16;
            bloomBufferDescriptor.depthBufferBits = 0;

            for (int i = 0; i < GBuffers.Length; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref GBuffers[i], in bloomBufferDescriptor,
                    name: ShaderConstants.GBufferNames[i]);
            }
        }

        public void SetGlobalTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < GBuffers.Length; i++)
            {
                cmd.SetGlobalTexture(ShaderConstants.GBufferIDs[i], GBuffers[i]);
            }
        }

        public void Dispose()
        {
            foreach (RTHandle handle in GBuffers)
            {
                handle?.Release();
            }
        }

        private static class ShaderConstants
        {
            public static readonly string[] GBufferNames =
            {
                "_HSRGBuffer0" // R: bloom intensity
            };

            public static readonly int[] GBufferIDs = Array.ConvertAll(GBufferNames, Shader.PropertyToID);
        }
    }
}
