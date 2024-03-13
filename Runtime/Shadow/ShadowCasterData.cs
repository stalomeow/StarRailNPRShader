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
using System.Collections.Generic;
using UnityEngine;

namespace HSR.NPRShader.Shadow
{
    public struct ShadowCasterData : IComparable<ShadowCasterData>
    {
        internal float Priority;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;
        public IReadOnlyList<ShadowRendererData> ShadowRenderers;

        public int CompareTo(ShadowCasterData other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
