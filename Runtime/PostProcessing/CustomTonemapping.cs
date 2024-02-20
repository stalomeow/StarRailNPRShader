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
    [VolumeComponentMenuForRenderPipeline("Honkai Star Rail/Tonemapping", typeof(UniversalRenderPipeline))]
    public class CustomTonemapping : VolumeComponent, IPostProcessComponent
    {
        public CustomTonemappingModeParameter Mode = new(CustomTonemappingMode.None);

        [Header("ACES Parameters")]

        [DisplayInfo(name = "Param A")] public FloatParameter ACESParamA = new(2.80f);
        [DisplayInfo(name = "Param B")] public FloatParameter ACESParamB = new(0.40f);
        [DisplayInfo(name = "Param C")] public FloatParameter ACESParamC = new(2.10f);
        [DisplayInfo(name = "Param D")] public FloatParameter ACESParamD = new(0.50f);
        [DisplayInfo(name = "Param E")] public FloatParameter ACESParamE = new(1.50f);

        public CustomTonemapping()
        {
            displayName = "HSR Tonemapping";
        }

        public bool IsActive() => Mode.value != CustomTonemappingMode.None;

        // 返回 true 的原因，请参考 Native Render Pass 的内容
        public bool IsTileCompatible() => true;
    }
}
