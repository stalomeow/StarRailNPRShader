using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HSR.PostProcessing
{
    public enum CustomTonemappingMode
    {
        None = 0,

        [InspectorName("ACES (Custom)")]
        ACES = 1,
    }

    [Serializable]
    public sealed class CustomTonemappingModeParameter : VolumeParameter<CustomTonemappingMode>
    {
        public CustomTonemappingModeParameter(CustomTonemappingMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
