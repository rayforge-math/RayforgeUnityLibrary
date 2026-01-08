using Rayforge.Diagnostics;
using Rayforge.ManagedResources.Abstractions;
using Rayforge.Rendering.Helpers;
using Rayforge.Utility.RenderGraphs.Collections;
using Rayforge.Utility.RenderGraphs.Helpers;
using Rayforge.Utility.RenderGraphs.Rendering;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

namespace Rayforge.Utility.RendererFeatures.DepthPyramid
{
    public class DepthPyramidPass : ScriptableRenderPass, IDisposable
    {
        public const int MipCountMax = 16;

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
        private int m_MipCount = 2;
        public int MipCount => m_MipCount;

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
                return RenderingUtils.ReAllocateHandleIfNeeded(ref handle, desc);
            });

            m_DepthPyramidDescriptor = DefaultDescriptors.DepthBufferFullScreen();

            k_PassData.PassMeta = new(shader, kernelId);

        }

        public void Dispose()
        {

        }

        public void UpdateMipCount(int mipCount)
        {
            m_MipCount = Math.Clamp(mipCount, 0, MipCountMax);
        }

#if UNITY_EDITOR
        private bool debug = false;
        private int debugMipLevel = 0;

        public void UpdateDebugSettings(bool showPyramid, int mipLevel)
        {
            debug = showPyramid;
            debugMipLevel = Mathf.Clamp(mipLevel, 0, MipCount - 1);
        }
#endif

        private void CheckAndUpdateTextures(Vector2Int resolution)
        {
            if(m_LastResolution != resolution)
            {
                m_DepthPyramidDescriptor.width = resolution.x;
                m_DepthPyramidDescriptor.height = resolution.y;

                m_DepthPyramidDescriptor.colorFormat = RenderTextureFormat.RFloat;
                m_DepthPyramidDescriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;

                k_DepthPyramidHandles.Create(m_DepthPyramidDescriptor, m_MipCount);

                m_LastResolution = resolution;
            }
            else if (k_DepthPyramidHandles.MipCount != m_MipCount)
            {
                k_DepthPyramidHandles.Create(m_DepthPyramidDescriptor, m_MipCount);
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

#if UNITY_EDITOR
            if (debug)
            {
                var debugHandle = k_DepthPyramidHandles[debugMipLevel].ToRenderGraphHandle(renderGraph);
                renderGraph.AddBlitPass(debugHandle, srcCamColor, Vector2.one, Vector2.zero);
                return;
            }
#endif
        }
    }
}