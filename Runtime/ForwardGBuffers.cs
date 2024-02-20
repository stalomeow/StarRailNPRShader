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
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader
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
