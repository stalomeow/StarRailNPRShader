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

namespace HSR.NPRShader.Editor.MaterialGUI
{
    public enum HSRMaterialIDEnum
    {
        All = -1,
        None = -2,
        [InspectorName("0")] Material0 = 0,
        [InspectorName("1")] Material1 = 1,
        [InspectorName("2")] Material2 = 2,
        [InspectorName("3")] Material3 = 3,
        [InspectorName("4")] Material4 = 4,
        [InspectorName("5")] Material5 = 5,
        [InspectorName("6")] Material6 = 6,
        [InspectorName("7")] Material7 = 7,
    }
}
