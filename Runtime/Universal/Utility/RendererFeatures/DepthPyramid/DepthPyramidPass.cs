using Rayforge.Diagnostics;
using Rayforge.ManagedResources.Abstractions;
using Rayforge.ManagedResources.NativeMemory;
using Rayforge.Rendering.Helpers;
using Rayforge.ShaderExtensions.Blitter;
using Rayforge.Utility.RenderGraphs.Collections;
using Rayforge.Utility.RenderGraphs.Rendering;
using System;
using System.ComponentModel;
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
        private const string k_DownsampleHighZKernelName = "DownsampleHighZ";
        private readonly int k_DownsampleHighZKernelId;

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

        private readonly ComputeShader k_DownsampleHighZShader;

        private Vector2Int m_LastResolution = new Vector2Int(-1, -1);

        private readonly RTHandleMipChain k_DepthPyramidHandles;
        private RenderTextureDescriptor m_DepthPyramidDescriptor;

        private const string k_DepthTextureMipName = "";

        private readonly SingleInputPassData<ComputePassMeta> k_PassData = new();

        public DepthPyramidPass(ComputeShader shader)
        {
            Assertions.NotNull(shader);
            k_DownsampleHighZShader = shader;

            var hasKernel = k_DownsampleHighZShader.HasKernel(k_DownsampleHighZKernelName);
            Assertions.IsTrue(hasKernel);
            if (!hasKernel) return;

            k_DownsampleHighZKernelId = k_DownsampleHighZShader.FindKernel(k_DownsampleHighZKernelName);

            k_DepthPyramidHandles = new RTHandleMipChain((
                ref RTHandle handle,
                RenderTextureDescriptor desc,
                int mip) =>
            {
                var created = RenderingUtils.ReAllocateHandleIfNeeded(ref handle, desc);
                Assertions.IsTrue(created);
            });

            m_DepthPyramidDescriptor = DefaultDescriptors.DepthBufferFullScreen();
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

                k_DepthPyramidHandles.Create(m_DepthPyramidDescriptor);

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

            // The following line ensures that the render pass doesn't blit from the back buffer and the color texture attachment is valid
            if (resourceData.isActiveTargetBackBuffer || !srcDepthBuffer.IsValid())
            {
                return;
            }

            UdpateSettings(cameraData);

            // initial blit
            k_PassData.SetInput(k_SourceId, srcDepthBuffer);
            k_PassData.Set
            RenderPassRecorder.AddComputePass(renderGraph, k_DownsampleHighZKernelName, k_PassData);

            BlitMaterialParameters param = new BlitMaterialParameters()
            {
                source = 
            }
            renderGraph.AddBlitPass()
        }
    }
}