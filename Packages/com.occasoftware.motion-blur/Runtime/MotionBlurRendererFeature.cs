using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace OccaSoftware.MotionBlur.Runtime
{
    public class MotionBlurRendererFeature : ScriptableRendererFeature
    {

        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent maskPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public RenderPassEvent blurPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        MotionBlurRenderPass motionBlurPass = null;
        MotionBlurMaskRenderPass motionBlurMaskPass = null;
        private const string shaderName = "MotionBlurCompute";
        private ComputeShader shader = null;

        public override void Create()
        {
            Dispose();

            motionBlurPass = new MotionBlurRenderPass();
            motionBlurPass.renderPassEvent = settings.blurPassEvent;

            motionBlurMaskPass = new MotionBlurMaskRenderPass();
            motionBlurMaskPass.renderPassEvent = settings.maskPassEvent;
        }

        private bool DeviceSupportsComputeShaders()
        {
            const int _COMPUTE_SHADER_LEVEL = 45;
            if (SystemInfo.graphicsShaderLevel >= _COMPUTE_SHADER_LEVEL)
                return true;

            return false;
        }

        public bool LoadComputeShader()
        {
            if (shader != null)
                return true;

            shader = (ComputeShader)Resources.Load(shaderName);
            if (shader == null)
                return false;

            return true;
        }

        BetterMotionBlur motionBlur = null;

        /// <summary>
        /// Get the Motion Blur component from the Volume Manager stack.
        /// </summary>
        /// <returns>If Motion Blur component is null or inactive, returns false.</returns>
        internal bool RegisterMotionBlurStackComponent()
        {
            motionBlur = VolumeManager.instance.stack.GetComponent<BetterMotionBlur>();
            if (motionBlur == null)
                return false;

            return motionBlur.IsActive();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool isActive = RegisterMotionBlurStackComponent();
            if (!isActive)
                return;

            if (!renderingData.cameraData.camera.GetUniversalAdditionalCameraData().renderPostProcessing)
                return;

            if (!DeviceSupportsComputeShaders())
            {
                Debug.LogWarning("Motion Blur requires Compute Shader support.", this);
                return;
            }

            if (IsExcludedCameraType(renderingData.cameraData.camera.cameraType))
                return;

            if (!LoadComputeShader())
                return;

            motionBlurPass.SetShader(shader);
            motionBlurMaskPass.SetShader(shader);
            motionBlurPass.Setup(motionBlur);
            motionBlurMaskPass.Setup(motionBlur);

            renderingData.cameraData.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            motionBlurPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);

            if (motionBlur.maskMode.value == Mode.On)
            {
                renderer.EnqueuePass(motionBlurMaskPass);
            }

            renderer.EnqueuePass(motionBlurPass);

            int targetKernel = shader.FindKernel(Params.KernelId);
        }

        protected override void Dispose(bool disposing)
        {
            motionBlurPass?.Dispose();
            motionBlurPass = null;

            motionBlurMaskPass?.Dispose();
            motionBlurMaskPass = null;

            shader = null;

            base.Dispose(disposing);
        }

        private bool IsExcludedCameraType(CameraType type)
        {
            switch (type)
            {
                case CameraType.SceneView:
                    return true;
                case CameraType.Preview:
                    return true;
                case CameraType.Reflection:
                    return true;
                default:
                    return false;
            }
        }
    }
}
