using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Rendering.RendererUtils;
using System.Collections.Generic;




#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace OccaSoftware.MotionBlur.Runtime
{
    internal class MotionBlurMaskRenderPass : ScriptableRenderPass
    {

        RTHandle motionBlurMask;
        string motionBlurMaskTargetId = "os_MotionBlurMask";

        private ComputeShader shader = null;
        BetterMotionBlur motionBlur;
        int targetKernel;
        private const string profilerTag = "[Motion Blur] Mask Pass";

        static readonly ShaderTagId[] shaderTagIds =
        {
            new ShaderTagId("DepthOnly"),
            new ShaderTagId("DepthNormals"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
        };

        public MotionBlurMaskRenderPass()
        {
            motionBlurMask = RTHandles.Alloc(
              Shader.PropertyToID(motionBlurMaskTargetId),
              name: motionBlurMaskTargetId
            );
        }

        public void Setup(BetterMotionBlur motionBlur)
        {
            this.motionBlur = motionBlur;
        }

        public void Dispose()
        {
            motionBlurMask?.Release();
            motionBlurMask = null;

            shader = null;
        }

        /// <summary>
        /// Loads the compute shader for Motion Blur.
        /// </summary>
        /// <returns>True if the shader was successfully loaded, false otherwise.</returns>
        public void SetShader(ComputeShader shader)
        {
            this.shader = shader;
        }

        RenderStateBlock renderStateBlock;


#if UNITY_2023_3_OR_NEWER
        public class PassData
        {
            public RendererListHandle RendererListHandle;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(profilerTag, out var passData))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();

                ConfigurePass(cameraData.cameraTargetDescriptor);

                var rendererListDesc = new RendererListDesc(shaderTagIds[0], renderingData.cullResults, cameraData.camera)
                {
                    sortingCriteria = cameraData.defaultOpaqueSortFlags,
                    renderQueueRange = RenderQueueRange.all,
                    layerMask = motionBlur.layerMask.value,
                };

                passData.RendererListHandle = renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.RendererListHandle);



                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData passData, UnsafeGraphContext context)
        {
            CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            CoreUtils.SetRenderTarget(
              cmd,
              motionBlurMask,
              RenderBufferLoadAction.DontCare,
              RenderBufferStoreAction.Store,
              ClearFlag.Color,
              Color.black);

            context.cmd.DrawRendererList(passData.RendererListHandle);

            cmd.SetComputeTextureParam(shader, targetKernel, "os_MotionBlurMask", motionBlurMask);
            cmd.SetComputeIntParam(shader, "_InvertMask", motionBlur.invertMask.value ? 1 : 0);
            cmd.SetComputeIntParam(shader, "_StrictMask", motionBlur.preferStrictMasks.value ? 1 : 0);
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
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.depthBufferBits = 0;
            //descriptor.enableRandomWrite = true;
            descriptor.msaaSamples = cameraTextureDescriptor.msaaSamples;
            descriptor.sRGB = false;

            descriptor.width = Mathf.Max(1, descriptor.width);
            descriptor.height = Mathf.Max(1, descriptor.height);
            descriptor.colorFormat = RenderTextureFormat.RFloat;
            RenderingUtilsHelper.ReAllocateIfNeeded(
              ref motionBlurMask,
              descriptor,
              name: motionBlurMaskTargetId
            );

            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            targetKernel = shader.FindKernel(Params.KernelId);

            return descriptor;
        }

#if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Profiler.BeginSample(profilerTag);
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            ConfigureTarget(motionBlurMask, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.Color, Color.black);

            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(
              shaderTagIds[0],
              ref renderingData,
              sortingCriteria
            );

            //Fallbacks
            for (int i = 1; i < shaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, shaderTagIds[i]);
            }

            FilteringSettings filteringSettings = new FilteringSettings(
              RenderQueueRange.all,
              motionBlur.layerMask.value
            );
            renderStateBlock.mask |= RenderStateMask.Depth;
            renderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual);
            context.DrawRenderers(
              renderingData.cullResults,
              ref drawingSettings,
              ref filteringSettings,
              ref renderStateBlock
            );

            cmd.SetComputeTextureParam(shader, targetKernel, "os_MotionBlurMask", motionBlurMask);
            cmd.SetComputeIntParam(shader, "_InvertMask", motionBlur.invertMask.value ? 1 : 0);
            cmd.SetComputeIntParam(shader, "_StrictMask", motionBlur.preferStrictMasks.value ? 1 : 0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            Profiler.EndSample();
        }
#endif

        public void ExecutePass(CommandBuffer cmd, RTHandle source)
        {

        }
    }
}
