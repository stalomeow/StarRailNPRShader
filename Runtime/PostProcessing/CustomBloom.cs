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

namespace HSR.NPRShader.PostProcessing
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Honkai Star Rail/Bloom", typeof(UniversalRenderPipeline))]
    public class CustomBloom : VolumeComponent, IPostProcessComponent
    {
        public MinFloatParameter Intensity = new(0, 0);
        public ClampedFloatParameter Scatter = new(0.6f, 0.2f, 4.0f);
        public ClampedIntParameter Iteration = new(4, 4, 8);
        public ColorParameter Tint = new(Color.white, false, false, true);

        [Header("Color Threshold")]

        [DisplayInfo(name = "Red")] public MinFloatParameter ThresholdR = new(0.6f, 0);
        [DisplayInfo(name = "Green")] public MinFloatParameter ThresholdG = new(0.6f, 0);
        [DisplayInfo(name = "Blue")] public MinFloatParameter ThresholdB = new(0.6f, 0);

        public CustomBloom()
        {
            displayName = "HSR Bloom";
        }

        public bool IsActive() => Intensity.value > 0;

        public bool IsTileCompatible() => false;
    }
}
