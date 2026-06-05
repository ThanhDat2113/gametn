using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;


#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace OccaSoftware.MotionBlur.Runtime
{

    internal class MotionBlurRenderPass : ScriptableRenderPass
    {

        private RTHandle motionBlurTarget;
        //private RTHandle screenColorCopy;//Research if this fixes the MSAA issue
        //private RTHandle motionVectorTarget;

        ComputeShader shader;

        string motionVectorTargetId = "_MotionVectorTexture";

        private const string motionBlurTargetId = "_MotionBlurTarget";

        private const string profilerTag = "[Motion Blur] Render Pass";
        private const string cmdBufferName = "[Motion Blur] Command Buffer";

        int targetKernel;
        int groupsX;
        int groupsY;

        public MotionBlurRenderPass()
        {
            //motionBlurTarget = RTHandles.Alloc(
            //  Shader.PropertyToID(motionBlurTargetId),
            //  name: motionBlurTargetId
            //);
            //motionVectorTarget = RTHandles.Alloc(
            //  Shader.PropertyToID(motionVectorTargetId),
            //  name: motionVectorTargetId
            //);

        }

        public void Setup(BetterMotionBlur motionBlur)
        {
            this.motionBlur = motionBlur;
        }

        BetterMotionBlur motionBlur;
        Material motionVectors;

        public void Dispose()
        {
            motionBlurTarget?.Release();
            motionBlurTarget = null;
            //screenColorCopy?.Release();
            //screenColorCopy = null;

            shader = null;
            motionVectors = null;
        }

        /// <summary>
        /// Loads the compute shader for Motion Blur.
        /// </summary>
        /// <returns>True if the shader was successfully loaded, false otherwise.</returns>
        public void SetShader(ComputeShader shader)
        {
            this.shader = shader;
        }

        private int GetGroupCount(int textureDimension, uint groupSize)
        {
            return Mathf.CeilToInt((textureDimension + groupSize - 1) / groupSize);
        }

#if UNITY_2023_3_OR_NEWER
        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle motionVector;
            internal RenderTextureDescriptor rtDescriptor;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(profilerTag, out var passData))
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                RenderTextureDescriptor descriptor = ConfigurePass(cameraData.cameraTargetDescriptor);

                passData.source = resourceData.cameraColor;
                passData.motionVector = resourceData.motionVectorColor;
                passData.rtDescriptor = descriptor;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.UseTexture(resourceData.motionVectorColor, AccessFlags.Read);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            ExecutePass(cmd, data.source, data.motionVector, data.rtDescriptor);
        }
#endif

#if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigurePass(renderingData.cameraData.cameraTargetDescriptor);
        }
#endif

        public RenderTextureDescriptor ConfigurePass(RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (motionVectors == null)
            {
                motionVectors = CoreUtils.CreateEngineMaterial(
                  Shader.Find("Hidden/Universal Render Pipeline/CameraMotionVectors")
                );
            }

            RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
            rtDescriptor.depthBufferBits = 0;
            rtDescriptor.enableRandomWrite = true;
            rtDescriptor.msaaSamples = 1;
            rtDescriptor.sRGB = cameraTextureDescriptor.sRGB;
            rtDescriptor.graphicsFormat = cameraTextureDescriptor.graphicsFormat;

            rtDescriptor.width = Mathf.Max(1, rtDescriptor.width);
            rtDescriptor.height = Mathf.Max(1, rtDescriptor.height);

            RenderingUtilsHelper.ReAllocateIfNeeded(
              ref motionBlurTarget,
              rtDescriptor,
              //FilterMode.Point,
              //TextureWrapMode.Clamp,
              name: motionBlurTargetId
            );

            //// Allocate a non-MSAA copy of the screen color for MSAA-safe compute sampling
            //RenderingUtilsHelper.ReAllocateIfNeeded(
            //  ref screenColorCopy,
            //  rtDescriptor,
            //  FilterMode.Bilinear,
            //  TextureWrapMode.Clamp,
            //  name: "_MotionBlurScreenColorCopy"
            //);

            //RenderTextureDescriptor rtmotionVectorTargetDescriptor = rtDescriptor;
            //rtmotionVectorTargetDescriptor.colorFormat = RenderTextureFormat.RGHalf;
            //rtmotionVectorTargetDescriptor.enableRandomWrite = true;
            //RenderingUtilsHelper.ReAllocateIfNeeded(
            //  ref motionVectorTarget,
            //  rtmotionVectorTargetDescriptor,
            //  name: motionVectorTargetId
            //);

            targetKernel = shader.FindKernel(Params.KernelId);

            shader.GetKernelThreadGroupSizes(
              targetKernel,
              out uint threadGroupSizeX,
              out uint threadGroupSizeY,
              out _
            );
            groupsX = GetGroupCount(rtDescriptor.width, threadGroupSizeX);
            groupsY = GetGroupCount(rtDescriptor.height, threadGroupSizeY);

            return rtDescriptor;
        }

        #if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(profilerTag);

            CommandBuffer cmd = CommandBufferPool.Get(cmdBufferName);

            // Set compute shader parameters
            ExecutePass(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, Shader.GetGlobalTexture(Params._MotionVectorTexture), renderingData.cameraData.cameraTargetDescriptor);

            // Execute and release command buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            Profiler.EndSample();
        }
#endif

        public void ExecutePass(CommandBuffer cmd, RTHandle source, Texture motionVector, RenderTextureDescriptor rtDescriptor)
        {
            //// Resolve MSAA source into a non-MSAA texture before compute sampling
            //Blitter.BlitCameraTexture(cmd, source, screenColorCopy);

            //cmd.SetComputeTextureParam(shader, targetKernel, Params._ScreenTexture, screenColorCopy);
            //cmd.SetComputeTextureParam(shader, targetKernel, Params._MotionBlurTarget, motionBlurTarget);
            //cmd.SetComputeTextureParam(shader, targetKernel, Params._MotionVectorTexture, motionVector);

            cmd.SetComputeTextureParam(shader, targetKernel, Params._ScreenTexture, source);
            cmd.SetComputeTextureParam(shader, targetKernel, Params._MotionBlurTarget, motionBlurTarget);
            cmd.SetComputeTextureParam(shader, targetKernel, Params._MotionVectorTexture, motionVector);

            Vector2 screenSizePx = new Vector2(
              rtDescriptor.width,
              rtDescriptor.height
            );
            cmd.SetComputeVectorParam(shader, Params._ScreenSizePx, screenSizePx);

            float currentFramerate = 1.0f / Time.unscaledDeltaTime;
            float shutterSpeed = motionBlur.shutterSpeed.value;
            float velocityScale = currentFramerate * shutterSpeed;
            float depthSeparationInvDistance = 1.0f / motionBlur.depthSeparationDistance.value;

            cmd.SetComputeIntParam(shader, Params._DepthSeparationEnabled, (int)motionBlur.depthSeparationMode.value);
            cmd.SetComputeFloatParam(shader, Params._DepthSeparationInverseDistance, depthSeparationInvDistance);
            cmd.SetComputeIntParam(shader, Params._FrameId, Time.frameCount);
            cmd.SetComputeFloatParam(shader, Params._VelocityScale, velocityScale);
            cmd.SetComputeIntParam(shader, Params._MaxSamples, motionBlur.QualityValue(motionBlur.qualityLevel.value));

            // Masking State
            if (motionBlur.maskMode.value == Mode.Off)
            {
                cmd.SetComputeTextureParam(shader, targetKernel, Params.os_MotionBlurMask, Texture2D.blackTexture);
            }

            cmd.SetComputeIntParam(
              shader,
              Params._HasMask,
              motionBlur.maskMode.value == Mode.On ? 1 : 0
            );

            // Dispatch compute shader and blit textures
            if (motionVector != null)
            {
                cmd.DispatchCompute(shader, targetKernel, groupsX, groupsY, 1);
                Blitter.BlitCameraTexture(cmd, motionBlurTarget, source);
            }

        }
    }
}
