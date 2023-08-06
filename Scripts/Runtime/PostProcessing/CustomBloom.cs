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
