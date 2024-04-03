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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace HSR.NPRShader.Passes
{
    public class ForwardDrawObjectsPass : DrawObjectsPass
    {
        public ForwardDrawObjectsPass(string profilerTag, bool isOpaque, params ShaderTagId[] shaderTagIds)
            : this(profilerTag, isOpaque,
                // 放在最后绘制，这样就不需要清理被挡住的角色的 Stencil
                isOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingTransparents,
                shaderTagIds) { }

        public ForwardDrawObjectsPass(string profilerTag, bool isOpaque, RenderPassEvent evt, params ShaderTagId[] shaderTagIds)
            : this(profilerTag, isOpaque, -1, evt, shaderTagIds) { }

        public ForwardDrawObjectsPass(string profilerTag, bool isOpaque, LayerMask layerMask, RenderPassEvent evt, params ShaderTagId[] shaderTagIds)
            : base(profilerTag, shaderTagIds, isOpaque, evt,
                isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                layerMask, new StencilState(), 0) { }
    }
}
