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

namespace HSR.PostProcessing
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Honkai Star Rail/Bloom", typeof(UniversalRenderPipeline))]
    public class CustomBloom : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter Intensity = new(0, 0);
        public ColorParameter Tint = new(Color.white, false, false, true);

        [Header("Threshold")]

        public MinFloatParameter ThresholdR = new(0.6f, 0);
        public MinFloatParameter ThresholdG = new(0.6f, 0);
        public MinFloatParameter ThresholdB = new(0.6f, 0);

        [Header("Scatter")]

        public ClampedFloatParameter Scatter1 = new(0.6f, 0.2f, 3.0f);
        public ClampedFloatParameter Scatter2 = new(0.8f, 0.2f, 3.0f);
        public ClampedFloatParameter Scatter3 = new(1.0f, 0.2f, 3.0f);
        public ClampedFloatParameter Scatter4 = new(1.2f, 0.2f, 3.0f);

        public CustomBloom()
        {
            displayName = "HSR Bloom";
        }

        public bool IsActive() => Intensity.value > 0;

        public bool IsTileCompatible() => false;
    }
}
