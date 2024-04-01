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
using HSR.NPRShader.Passes;
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
        public MinFloatParameter Threshold = new(0.7f, 0);
        public ColorParameter Tint = new(Color.white, false, false, true);
        public ClampedIntParameter MipDownCount = new(2, 2, 4);
        public BoolParameter CharactersOnly = new(true, BoolParameter.DisplayType.EnumPopup);

        [Header("Blur First RT Size")]

        [DisplayInfo(name = "Width")] public ClampedIntParameter BlurFirstRTWidth = new(310, 100, 500);
        [DisplayInfo(name = "Height")] public ClampedIntParameter BlurFirstRTHeight = new(174, 100, 500);

        [Header("Blur Kernels")]

        public ClampedIntParameter KernelSize1 = new(4, 4, PostProcessPass.BloomMaxKernelSize);
        public ClampedIntParameter KernelSize2 = new(4, 4, PostProcessPass.BloomMaxKernelSize);
        public ClampedIntParameter KernelSize3 = new(6, 4, PostProcessPass.BloomMaxKernelSize);
        public ClampedIntParameter KernelSize4 = new(14, 4, PostProcessPass.BloomMaxKernelSize);

        public CustomBloom()
        {
            displayName = "HSR Bloom";
        }

        public bool IsActive() => Intensity.value > 0;

        public bool IsTileCompatible() => false;
    }
}
