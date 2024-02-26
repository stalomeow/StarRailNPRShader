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
using UnityEngine.Rendering.Universal.Internal;

namespace HSR.NPRShader.Passes
{
    public class MRTDrawObjectsPass : DrawObjectsPass
    {
        private RTHandle[] m_ColorAttachments;
        private RTHandle m_DepthAttachment;

        public MRTDrawObjectsPass(string profilerTag, bool isOpaque, params ShaderTagId[] shaderTagIds)
            : this(profilerTag, isOpaque, -1, shaderTagIds) { }

        public MRTDrawObjectsPass(string profilerTag, bool isOpaque, LayerMask layerMask, params ShaderTagId[] shaderTagIds)
            : base(profilerTag, shaderTagIds, isOpaque,
                // 在 UniversalForward 之后绘制，利用深度测试尽量少写入无用像素
                isOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingTransparents,
                isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                layerMask, new StencilState(), 0)
        {
            m_ColorAttachments = Array.Empty<RTHandle>();

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(RTHandle colorTarget, RTHandle depthTarget, RTHandle[] gBuffers)
        {
            if (m_ColorAttachments.Length != gBuffers.Length + 1)
            {
                Array.Resize(ref m_ColorAttachments, gBuffers.Length + 1);
            }

            gBuffers.CopyTo(m_ColorAttachments, 1);
            m_ColorAttachments[0] = colorTarget;
            m_DepthAttachment = depthTarget;

            ConfigureTarget(m_ColorAttachments, m_DepthAttachment);
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public MRTDrawObjectsPass SetupPreview()
        {
            ResetTarget();
            ConfigureClear(ClearFlag.None, Color.black);
            return this;
        }
    }
}
