using Rayforge.Diagnostics;
using Rayforge.ManagedResources.Abstractions;
using Rayforge.Rendering.Helpers;
using Rayforge.Utility.RenderGraphs.Collections;
using Rayforge.Utility.RenderGraphs.Helpers;
using Rayforge.Utility.RenderGraphs.Rendering;
using Rayforge.Rendering.Passes;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;
using System.Linq;

namespace Rayforge.Utility.RendererFeatures.DepthPyramid
{
    public class DepthPyramidPass : ScriptableRenderPass, IDisposable
    {
        private class DepthPyramidPassData : ComputePassData<DepthPyramidPassData>
        {

        }

        private const string k_DownsampleHighZKernelName = "DownsampleHighZ";

        private static readonly int k_SourceId = Shader.PropertyToID("_Source");
        private static readonly int k_DestId = Shader.PropertyToID("_Dest");

        // Bright pass parameters
        [StructLayout(LayoutKind.Sequential)]
        private struct DownsampleHighZParams : IComputeData<DownsampleHighZParams>
        {
            public Vector2Int sourceRes;
            public Vector2Int destRes;

            public DownsampleHighZParams RawData => this;
        }
        private static readonly int k_DownsampleHighZParamsId = Shader.PropertyToID("_DownsampleHighZParams");

        private Vector2Int m_LastResolution = new Vector2Int(-1, -1);

        private readonly RTHandleMipChain k_DepthPyramidHandles;
        private RenderTextureDescriptor m_DepthPyramidDescriptor;

        private const string k_DepthTextureMipName = "";

        private readonly DepthPyramidPassData k_PassData = new();

        public DepthPyramidPass(ComputeShader shader)
        {
            Assertions.NotNull(shader);

            var hasKernel = shader.HasKernel(k_DownsampleHighZKernelName);
            Assertions.IsTrue(hasKernel);
            if (!hasKernel) return;

            var kernelId = shader.FindKernel(k_DownsampleHighZKernelName);

            k_DepthPyramidHandles = new RTHandleMipChain((
                ref RTHandle handle,
                RenderTextureDescriptor desc,
                int mip) =>
            {
                var created = RenderingUtils.ReAllocateHandleIfNeeded(ref handle, desc);
                Assertions.IsTrue(created);
            });

            m_DepthPyramidDescriptor = DefaultDescriptors.DepthBufferFullScreen();

            k_PassData.PassMeta = new(shader, kernelId);

        }

        public void Dispose()
        {

        }

        private void CheckAndUpdateTextures(Vector2Int resolution)
        {
            if(m_LastResolution != resolution)
            {
                m_DepthPyramidDescriptor.width = resolution.x;
                m_DepthPyramidDescriptor.height = resolution.y;

                m_DepthPyramidDescriptor.colorFormat = RenderTextureFormat.RFloat;
                m_DepthPyramidDescriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;

                k_DepthPyramidHandles.Create(m_DepthPyramidDescriptor, 2);

                m_LastResolution = resolution;
            }
        }

        private void UdpateSettings(UniversalCameraData cameraData)
        {
            var camera = cameraData.camera;
            var resolution = new Vector2Int { x = camera.pixelWidth, y = camera.pixelHeight };

            CheckAndUpdateTextures(resolution);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle srcDepthBuffer = resourceData.activeDepthTexture;
            TextureHandle srcCamColor = resourceData.activeColorTexture;

            // The following line ensures that the render pass doesn't blit from the back buffer and the color texture attachment is valid
            if (resourceData.isActiveTargetBackBuffer || !srcDepthBuffer.IsValid() || !srcCamColor.IsValid())
            {
                return;
            }

            UdpateSettings(cameraData);

            var destHandle = k_DepthPyramidHandles[0].ToRenderGraphHandle(renderGraph);
            if(!destHandle.IsValid())
            {
                return;
            }

            renderGraph.AddBlitPass(srcDepthBuffer, destHandle, Vector2.one, Vector2.zero);
            /*
            // initial blit
            var dispatchMeta = new ComputeDispatchMeta(k_KernelMeta, Mathf.CeilToInt(m_LastResolution.x / 8.0f), Mathf.CeilToInt(m_LastResolution.y / 8.0f), 1);


            k_PassData.AdditionalData = new ComputePassMeta(dispatchMeta);
            k_PassData.SetInput(k_SourceId, srcDepthBuffer);
            k_PassData.Destination = new TextureMeta { propertyId = k_DestId, handle = destHandle };

            RenderPassRecorder.AddComputePass(renderGraph, k_DownsampleHighZKernelName, k_PassData);
            */
            renderGraph.AddBlitPass(destHandle, srcCamColor, Vector2.one, Vector2.zero);
        }
    }
}