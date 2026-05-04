using UnityEngine;

namespace OccaSoftware.MotionBlur.Runtime
{
    internal static class Params
    {
        public const string KernelId = "ComputeMotionBlur";


        public static int _ScreenTexture = Shader.PropertyToID("_ScreenTexture");
        public static int _ScreenSizePx = Shader.PropertyToID("_ScreenSizePx");
        public static int _DepthSeparationEnabled = Shader.PropertyToID("_DepthSeparationEnabled");
        public static int _DepthSeparationInverseDistance = Shader.PropertyToID(
          "_DepthSeparationInverseDistance"
        );
        public static int _VelocityScale = Shader.PropertyToID("_VelocityScale");
        public static int _MaxSamples = Shader.PropertyToID("_MaxSamples");
        public static int _FrameId = Shader.PropertyToID("_FrameId");
        public static int _MotionBlurTarget = Shader.PropertyToID("_MotionBlurTarget");
        public static int os_MotionBlurMask = Shader.PropertyToID("os_MotionBlurMask");
        public static int _HasMask = Shader.PropertyToID("_HasMask");


        public static int _MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");

    }
}
