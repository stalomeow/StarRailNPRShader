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

using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HSR.NPRShader.Passes
{
    public class SetGlobalRTPass : ScriptableRenderPass
    {
        private IReadOnlyList<int> m_NameIds;
        private IReadOnlyList<RTHandle> m_RTHandles;

        public SetGlobalRTPass(RenderPassEvent @event)
        {
            renderPassEvent = @event;
        }

        public void Setup(IReadOnlyList<int> nameIds, IReadOnlyList<RTHandle> rtHandles)
        {
            m_NameIds = nameIds;
            m_RTHandles = rtHandles;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            for (int i = 0; i < m_NameIds.Count && i < m_RTHandles.Count; i++)
            {
                cmd.SetGlobalTexture(m_NameIds[i], m_RTHandles[i].nameID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
