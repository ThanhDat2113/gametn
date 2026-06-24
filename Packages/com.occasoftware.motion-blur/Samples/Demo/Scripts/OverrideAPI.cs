using OccaSoftware.MotionBlur.Runtime;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace OccaSoftware.MotionBlur.Demo
{
    [ExecuteAlways]
    public class OverrideAPI : MonoBehaviour
    {
        public Volume volume;
        public bool doOverride;

        public MotionBlur.Runtime.QualityLevel qualityLevel;

        private void Update()
        {
            if (doOverride && volume.profile.TryGet(out BetterMotionBlur motionBlur))
            {
                motionBlur.qualityLevel = new QualityLevelParameter(qualityLevel, true);
            }
        }
    }
}
