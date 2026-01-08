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
        private const int k_DownsampleMipCountMax = MipCountMax - 1;

        private class DepthPyramidPassData : ComputePassData<DepthPyramidPassData>
        {
            DownsampleHighZParams passParam;

            public override void CopyUserData(DepthPyramidPassData other)
            {
                passParam = other.passParam;
            }
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
        private int m_DownsampleMipCount = 2;
        public int MipCount
        {
            get => m_DownsampleMipCount + 1;
            set => m_DownsampleMipCount = value - 1;
        }

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
            m_DownsampleMipCount = Math.Clamp(mipCount - 1, 0, MipCountMax);
        }

#if UNITY_EDITOR
        private bool debug = false;
        private int debugMipLevel = 0;
        private int debugDownsampleMipLevel => debugMipLevel - 1;

        public void UpdateDebugSettings(bool showPyramid, int mipLevel)
        {
            debug = showPyramid;
            debugMipLevel = Mathf.Clamp(mipLevel, 0, k_DownsampleMipCountMax);
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

                m_LastResolution = resolution;
            }
            
            if (k_DepthPyramidHandles.MipCount != m_DownsampleMipCount)
            {
                if (m_DownsampleMipCount > 0)
                {
                    k_DepthPyramidHandles.Create(m_DepthPyramidDescriptor, m_DownsampleMipCount);
                }
                else
                {
                    k_DepthPyramidHandles.Resize(m_DownsampleMipCount);
                }
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

            // The following line ensures that the render pass doesn't blit from the back buffer and the color texture attachment is valid
            if (resourceData.isActiveTargetBackBuffer || !srcDepthBuffer.IsValid())
            {
                return;
            }

            UdpateSettings(cameraData);

            TextureHandle mipN0 = default;
            TextureHandle mipN1 = srcDepthBuffer;
            for(int i = 0; i < k_DepthPyramidHandles.MipCount - 1; ++i)
            {
                mipN0 = mipN1;
                mipN1 = k_DepthPyramidHandles[i].ToRenderGraphHandle(renderGraph);

                if (!mipN0.IsValid() || !mipN1.IsValid())
                    break;

                k_PassData.SetInput(mipN0, k_SourceId);
                k_PassData.SetDestination(mipN1);
                RenderPassRecorder.AddComputePass(renderGraph, k_DownsampleHighZKernelName, k_PassData);
            }


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
                TextureHandle debugHandle = default;
                if (debugDownsampleMipLevel < 0)
                {
                    debugHandle = srcDepthBuffer;
                }
                else
                {
                    debugHandle = k_DepthPyramidHandles[debugDownsampleMipLevel].ToRenderGraphHandle(renderGraph);
                }
                     
                renderGraph.AddBlitPass(debugHandle, resourceData.activeColorTexture, Vector2.one, Vector2.zero);
                return;
            }
#endif
        }
    }
}