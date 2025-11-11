using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Rayforge.CustomRenderGraphRecorder.Example
{
#if UNITY_PIPELINE_URP
    public class TestRenderPass : ScriptableRenderPass
    {
        private const string k_PassName = "MyPass";
        private static readonly int k_PassId = Shader.PropertyToID(k_PassName);

        private RenderTextureDescriptor m_Desc;
        private const string k_TexName = "MyTex";

        private Vector2 m_UvOffset = new Vector2(0.5f, 0.5f);
        private static readonly int k_UvOffsetId = Shader.PropertyToID("_UvOffset");

        Material m_Material;

        public TestRenderPass(Material material)
        {
            m_Material = material;

            m_Desc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var handle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, m_Desc, k_TexName, false, FilterMode.Bilinear);

            CustomRecorder.AddUnsafeRenderPass<SingleInputPassData>(renderGraph, k_PassName,
                (passData) =>
                {
                    passData.Material = m_Material;
                    passData.PassId = k_PassId;
                    passData.Destination = handle;
                    passData.SetInput(BlitParameters.BlitTextureId, srcCamColor);
                },
                (cmd, mpb) =>
                {
                    mpb.SetVector(k_UvOffsetId, m_UvOffset);
                });
        }
    }
#endif
}